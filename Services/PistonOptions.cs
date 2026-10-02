namespace PSAcademyBack.Services;

public class PistonOptions
{
    public const string SectionName = "Piston";

    /// <summary>
    /// URL base de la instancia de Piston, incluyendo el sufijo /api/v2/.
    ///
    /// En desarrollo apunta al contenedor local definido en docker-compose.yml.
    /// En producción DEBE sobrescribirse con "Piston__BaseUrl": una instancia
    /// propia (VPS) o la instancia pública con clave autorizada.
    /// </summary>
    public string BaseUrl
    {
        get => _baseUrl;
        set => _baseUrl = value?.Trim() ?? string.Empty;
    }

    private string _baseUrl = "https://emkc.org/api/v2/piston/";

    /// <summary>
    /// Instancia Piston normalizada, como se le pasa a <see cref="HttpClient.BaseAddress"/>.
    /// </summary>
    /// <remarks>
    /// Las llamadas se hacen con "execute" como ruta relativa, así que el valor
    /// tiene que terminar en "/": sin ella System.Uri descarta el último segmento
    /// y ".../api/v2" acaba pidiendo ".../api/execute". Y si solo se pega el host
    /// (que es lo que anuncia el túnel) falta el sufijo entero, con lo que la
    /// petición cae en la raíz. Cualquiera de los dos deslices acaba en un 404 de
    /// Piston que el cliente reporta como un 502 "el ejecutor no está disponible",
    /// muy lejos de la causa real, así que se corrige aquí en vez de obligar a
    /// quien despliega a recordar el formato exacto.
    /// </remarks>
    public string NormalizedBaseUrl
    {
        get
        {
            var url = BaseUrl.Trim();

            if (url.Length == 0)
            {
                return url;
            }

            var hasApiPath =
                Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                !string.IsNullOrEmpty(uri.AbsolutePath.Trim('/'));

            var withSuffix = hasApiPath ? url : $"{url.TrimEnd('/')}{ApiSuffix}";

            return withSuffix.EndsWith('/') ? withSuffix : $"{withSuffix}/";
        }
    }

    /// <summary>Ruta de la API v2 de Piston, que es donde vive "execute".</summary>
    private const string ApiSuffix = "/api/v2/";

    /// <summary>
    /// Clave de autorización para instancias que la exigen.
    ///
    /// La instancia pública de Piston dejó de ser abierta el 15/02/2026 y pasa a
    /// exigir autorización previa para proyectos educativos no comerciales. Sin
    /// esta clave esa instancia responde 401 y /execute devuelve 502 a todos los
    /// alumnos. Una instancia autoalojada no la necesita: se deja vacía.
    ///
    /// Se envía como cabecera "Authorization" con el valor tal cual, que es lo que
    /// espera la implementación de referencia.
    /// </summary>
    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>Tope de caracteres de stdout/stderr que se devuelven al cliente.</summary>
    public int MaxOutputLength { get; set; } = 8_000;
}
