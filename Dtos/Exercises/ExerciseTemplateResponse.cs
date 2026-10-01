namespace PSAcademyBack.Dtos.Exercises;

public class ExerciseTemplateResponse
{
    public int LanguageId { get; set; }

    public string LanguageName { get; set; } = string.Empty;

    public string LanguageSlug { get; set; } = string.Empty;

    public string StarterCode { get; set; } = string.Empty;
}
