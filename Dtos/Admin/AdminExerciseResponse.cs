using PSAcademyBack.Dtos.Exercises;
using PSAcademyBack.Enums;

namespace PSAcademyBack.Dtos.Admin;

/// <summary>Vista de administración: incluye campos que el catálogo público no expone.</summary>
public class AdminExerciseResponse
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Difficulty Difficulty { get; set; }

    public string ExpectedOutput { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<AdminTemplateResponse> Templates { get; set; } = new();

    /// <summary>Valores del "Leer" en el orden en que se entregarán por stdin.</summary>
    public List<ExerciseInputResponse> Inputs { get; set; } = new();

    /// <summary>Pasos del tutorial. Vacío si el ejercicio no es de tutorial.</summary>
    public List<TutorialStepResponse> TutorialSteps { get; set; } = new();
}

public class AdminTemplateResponse
{
    public int Id { get; set; }

    public int LanguageId { get; set; }

    public string LanguageName { get; set; } = string.Empty;

    public string LanguageSlug { get; set; } = string.Empty;

    public string StarterCode { get; set; } = string.Empty;
}
