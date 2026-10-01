using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PSAcademyBack.Services;

public class PistonExecutionService : IPistonExecutionService
{
    /// <summary>
    /// Slugs de la aplicación traducidos al nombre de runtime que espera el ejecutor.
    ///
    /// Estos son los nombres CANÓNICOS que devuelve GET /api/v2/runtimes.
    /// Los alias (gcc, cpp, g++, mono, node-js...) también se aceptan, pero ojo: "gcc"
    /// es alias de "c", no de "c++", así que usarlo compilaría C++ como C y fallaría
    /// con errores confusos en lugar de un "lenguaje no soportado".
    ///
    /// "pseint" no existe como runtime en Piston: se traduce a Python con
    /// <see cref="PSeintTranslator"/> antes de enviarlo.
    /// </summary>
    private static readonly Dictionary<string, (string PistonLanguage, string FileName)> SupportedLanguages =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["python"] = ("python", "main.py"),
            ["pseint"] = ("python", "main.py"),
            ["java"] = ("java", "Main.java"),
            ["javascript"] = ("javascript", "main.js"),
            ["typescript"] = ("typescript", "index.ts"),
            ["csharp"] = ("csharp", "main.cs"),
            ["c"] = ("c", "main.c"),
            ["cpp"] = ("c++", "main.cpp"),
            ["kotlin"] = ("kotlin", "main.kt"),
            ["go"] = ("go", "main.go"),
            ["rust"] = ("rust", "main.rs"),
            ["ruby"] = ("ruby", "main.rb"),
            ["php"] = ("php", "main.php"),
            ["swift"] = ("swift", "main.swift")
        };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<PistonExecutionService> _logger;
    private readonly PistonOptions _options;

    public PistonExecutionService(
        HttpClient httpClient,
        IOptions<PistonOptions> options,
        ILogger<PistonExecutionService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsSupported(string languageSlug)
        => SupportedLanguages.ContainsKey(languageSlug.Trim());

    public async Task<PistonExecutionResult> ExecuteAsync(
        string languageSlug,
        string code,
        string? stdin = null,
        CancellationToken cancellationToken = default)
    {
        var slug = languageSlug.Trim();

        if (!SupportedLanguages.TryGetValue(slug, out var target))
        {
            _logger.LogWarning("Slug de lenguaje no soportado por el ejecutor: {Slug}", slug);
            return PistonExecutionResult.UnsupportedLanguage(slug);
        }

        // PSeint no tiene runtime propio: se traduce a Python. Un error de traducción
        // es un fallo del código del alumno (se le muestra), no de la infraestructura.
        if (string.Equals(slug, "pseint", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                code = PSeintTranslator.ToPython(code);
            }
            catch (PSeintTranslationException ex)
            {
                return new PistonExecutionResult
                {
                    Succeeded = true,
                    Stdout = string.Empty,
                    Stderr = Truncate(
                        $"No se pudo interpretar el pseudocódigo. {ex.Message}",
                        _options.MaxOutputLength),
                    ExitCode = null
                };
            }
        }

        // NO se envían run_timeout ni compile_timeout: Piston rechaza la ejecución
        // completa con HTTP 400 si superan su límite configurado, y ese límite varía
        // por despliegue y por lenguaje (run 3s / compile 10s en una instancia stock).
        // Se delega en el límite propio de Piston y aquí solo se traduce su corte.
        var payload = new
        {
            language = target.PistonLanguage,
            version = "*",
            // Los "valores del leer" llegan aquí como líneas de stdin. Piston los
            // entrega al programa en el mismo orden.
            stdin = stdin ?? string.Empty,
            files = new[]
            {
                new { name = target.FileName, content = code }
            }
        };

        // El corte de tiempo debe dispararlo antes Piston (su propio run_timeout) que
        // esta cancelación: así responde con status "TO" y se puede traducir a un
        // intento normal del alumno. Si esta dispara primero, se reporta 502 como
        // fallo de infraestructura, que sería engañoso para un bucle infinito.
        var cancellationGraceSeconds = 10;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds + cancellationGraceSeconds));

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("execute", payload, timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var body = await SafeReadAsync(response, timeoutCts.Token);
                var upstreamMessage = TryReadMessage(body);

                _logger.LogError(
                    "Piston devolvió {StatusCode}: {Body}",
                    (int)response.StatusCode,
                    Truncate(body, _options.MaxOutputLength));

                return PistonExecutionResult.Failure(
                    string.IsNullOrWhiteSpace(upstreamMessage)
                        ? $"El ejecutor remoto rechazó la petición (HTTP {(int)response.StatusCode})."
                        : $"El ejecutor remoto rechazó la petición (HTTP {(int)response.StatusCode}): {upstreamMessage}");
            }

            var result = await response.Content
                .ReadFromJsonAsync<PistonExecuteResponse>(JsonOptions, timeoutCts.Token);

            if (result is null)
            {
                return PistonExecutionResult.Failure("El ejecutor remoto devolvió una respuesta vacía.");
            }

            var stdout = Truncate(result.Run?.Stdout ?? string.Empty, _options.MaxOutputLength);

            // v2 separa la fase de compilación (compile.stderr) de la de ejecución.
            // v3 no tiene bloque "compile": el error de compilación llega en run.stderr.
            // Se prioriza compile cuando existe para no perder detalle.
            var stderr = Truncate(
                !string.IsNullOrWhiteSpace(result.Compile?.Stderr)
                    ? result.Compile!.Stderr!
                    : result.Run?.Stderr ?? string.Empty,
                _options.MaxOutputLength);

            // Piston señala el corte por límite de tiempo con status "TO", code null y
            // signal SIGKILL, dejando stdout/stderr vacíos. Sin traducirlo, el alumno
            // vería "sin salida" en vez de saber que su programa no terminó.
            if (IsTimeout(result.Run))
            {
                stderr = Truncate(
                    string.IsNullOrWhiteSpace(stderr)
                        ? "El programa excedió el tiempo límite de ejecución y fue detenido. Revisa posibles bucles infinitos."
                        : stderr,
                    _options.MaxOutputLength);
            }

            return new PistonExecutionResult
            {
                Succeeded = true,
                Stdout = stdout,
                Stderr = stderr,
                ExitCode = ResolveExitCode(result.Run),
                LanguageVersion = ResolveLanguageVersion(result)
            };
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Timeout de {Seconds}s al ejecutar código en Piston", _options.TimeoutSeconds);
            return PistonExecutionResult.Failure(
                $"El código excedió el tiempo límite de {_options.TimeoutSeconds} segundos.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fallo de red al invocar Piston");
            return PistonExecutionResult.Failure("No se pudo contactar con el ejecutor remoto.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Respuesta con formato inválido desde Piston");
            return PistonExecutionResult.Failure("El ejecutor remoto devolvió una respuesta ilegible.");
        }
    }

    /// <summary>
    /// Piston devuelve los errores como { "message": "..." }. Recuperarlo evita
    /// esconder la causa real detrás de un "HTTP 401" sin explicación.
    /// </summary>
    private static string? TryReadMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("message", out var message)
                && message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }
        }
        catch (JsonException)
        {
            // Cuerpo no JSON: se ignora y se reporta solo el código HTTP.
        }

        return null;
    }

    private static async Task<string> SafeReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    /// <summary>
    /// Piston cambia la forma de la respuesta entre versiones, así que se acepta
    /// cualquiera de las dos:
    ///   v2 -> "language": { "name": ..., "version": ... }, "compile": { ... }
    ///   v3 -> "language": "python", "version": "3.10.0"      (sin bloque "compile")
    /// </summary>
    private sealed class PistonExecuteResponse
    {
        public JsonElement? Language { get; set; }

        public string? Version { get; set; }

        public PistonStageResult? Run { get; set; }

        public PistonStageResult? Compile { get; set; }
    }

    private sealed class PistonStageResult
    {
        public string? Stdout { get; set; }
        public string? Stderr { get; set; }
        public string? Output { get; set; }

        /// <summary>Numérico en ejecuciones correctas, textual ("RE") en algunos fallos de v3.</summary>
        public JsonElement? Code { get; set; }

        public string? Signal { get; set; }

        /// <summary>Código de estado de Piston: "TO" cuando se agota el tiempo de ejecución.</summary>
        public string? Status { get; set; }

        public string? Message { get; set; }
    }

    /// <summary>
    /// Piston corta por tiempo con status "TO" y deja code en null. Se comprueba el
    /// status y, como respaldo, el patrón code null + SIGKILL por si el status cambia.
    /// </summary>
    private static bool IsTimeout(PistonStageResult? stage)
        => stage is not null
            && (string.Equals(stage.Status, "TO", StringComparison.OrdinalIgnoreCase)
                || (stage.Code is null && string.Equals(stage.Signal, "SIGKILL", StringComparison.OrdinalIgnoreCase)));

    private static string? ResolveLanguageVersion(PistonExecuteResponse response)
    {
        if (!string.IsNullOrWhiteSpace(response.Version))
        {
            return response.Version;
        }

        // Solo en v2 la versión viaja anidada dentro del objeto "language".
        if (response.Language is { ValueKind: JsonValueKind.Object } language
            && language.TryGetProperty("version", out var nested)
            && nested.ValueKind == JsonValueKind.String)
        {
            return nested.GetString();
        }

        return null;
    }

    private static int? ResolveExitCode(PistonStageResult? stage)
    {
        if (stage?.Code is not { } code)
        {
            return null;
        }

        return code.ValueKind switch
        {
            JsonValueKind.Number when code.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(code.GetString(), out var parsed) => parsed,
            _ => null
        };
    }
}
