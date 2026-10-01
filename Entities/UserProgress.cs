using PSAcademyBack.Enums;

namespace PSAcademyBack.Entities;

public class UserProgress
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int ExerciseId { get; set; }

    public Exercise Exercise { get; set; } = null!;

    public ProgressStatus Status { get; set; } = ProgressStatus.Attempted;

    public string? LastSubmittedCode { get; set; }

    /// <summary>Lenguaje con el que se superó el ejercicio por última vez.</summary>
    public int? LastSubmittedLanguageId { get; set; }

    public Language? LastSubmittedLanguage { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
