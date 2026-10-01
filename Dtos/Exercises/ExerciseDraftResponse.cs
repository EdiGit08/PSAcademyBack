namespace PSAcademyBack.Dtos.Exercises;

/// <summary>
/// Borrador de código guardado por el alumno para un lenguaje concreto. Permite
/// restaurar el workspace al volver, aunque no se haya ejecutado nada.
/// </summary>
public class ExerciseDraftResponse
{
    public string LanguageSlug { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }
}
