using PSAcademyBack.Enums;

namespace PSAcademyBack.Dtos.Categories;

public class ExerciseSummaryResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public Difficulty Difficulty { get; set; }

    /// <summary>
    /// Progreso del usuario autenticado: "attempted" o "completed". Llega null si la
    /// petición no lleva JWT o el alumno todavía no ha enviado el ejercicio.
    /// </summary>
    public string? UserStatus { get; set; }

    /// <summary>Slug de lenguaje ya completado por el usuario, si existe.</summary>
    public string? CompletedInLanguageSlug { get; set; }
}
