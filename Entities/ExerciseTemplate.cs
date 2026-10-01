namespace PSAcademyBack.Entities;

public class ExerciseTemplate
{
    public int Id { get; set; }

    public int ExerciseId { get; set; }

    public Exercise Exercise { get; set; } = null!;

    public int LanguageId { get; set; }

    public Language Language { get; set; } = null!;

    public string StarterCode { get; set; } = string.Empty;
}
