namespace PSAcademyBack.Services;

public class PistonOptions
{
    public const string SectionName = "Piston";

    public string BaseUrl { get; set; } = "https://emkc.org/api/v2/piston/";

    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>Tope de caracteres de stdout/stderr que se devuelven al cliente.</summary>
    public int MaxOutputLength { get; set; } = 8_000;
}
