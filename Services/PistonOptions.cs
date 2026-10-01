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
    public string BaseUrl { get; set; } = "https://emkc.org/api/v2/piston/";

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
