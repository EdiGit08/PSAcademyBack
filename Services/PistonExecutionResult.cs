namespace PSAcademyBack.Services;

/// <summary>Resultado de una ejecución remota, normalizado y sin depender del JSON de Piston.</summary>
public sealed record PistonExecutionResult
{
    public bool Succeeded { get; init; }

    public string Stdout { get; init; } = string.Empty;

    public string Stderr { get; init; } = string.Empty;

    public int? ExitCode { get; init; }

    public string? LanguageVersion { get; init; }

    /// <summary>Mensaje de fallo de infraestructura (timeout, red, lenguaje no soportado).</summary>
    public string? FailureReason { get; init; }

    public bool HasError => !string.IsNullOrWhiteSpace(Stderr);

    public static PistonExecutionResult UnsupportedLanguage(string slug) => new()
    {
        Succeeded = false,
        FailureReason = $"El lenguaje '{slug}' no está soportado por el ejecutor remoto."
    };

    public static PistonExecutionResult Failure(string reason) => new()
    {
        Succeeded = false,
        FailureReason = reason
    };
}
