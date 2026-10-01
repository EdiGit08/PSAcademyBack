namespace PSAcademyBack.Services;

public interface IPistonExecutionService
{
    /// <summary>
    /// Ejecuta el código en el ejecutor remoto. Nunca lanza por fallos de infraestructura:
    /// esos casos se reportan en <see cref="PistonExecutionResult.FailureReason"/>.
    /// </summary>
    /// <param name="stdin">Líneas que el programa recibirá por entrada estándar (null = sin entrada).</param>
    Task<PistonExecutionResult> ExecuteAsync(
        string languageSlug,
        string code,
        string? stdin = null,
        CancellationToken cancellationToken = default);

    /// <summary>Indica si el ejecutor remoto puede interpretar el slug indicado.</summary>
    bool IsSupported(string languageSlug);
}
