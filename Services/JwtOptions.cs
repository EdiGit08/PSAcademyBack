namespace PSAcademyBack.Services;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "PSAcademyBack";

    public string Audience { get; set; } = "PSAcademyBackClient";

    /// <summary>
    /// Clave de firma. NO debe tener un valor por defecto: si falta, la aplicación
    /// falla al arrancar en lugar de firmar con una clave conocida y publicada.
    /// </summary>
    public string? SecretKey { get; set; }

    public int ExpirationMinutes { get; set; } = 60;
}
