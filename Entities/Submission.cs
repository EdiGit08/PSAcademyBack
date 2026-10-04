using PSAcademyBack.Enums;

namespace PSAcademyBack.Entities;

public class Submission
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int ExerciseId { get; set; }

    public Exercise Exercise { get; set; } = null!;

    public int LanguageId { get; set; }

    public Language Language { get; set; } = null!;

    public string Code { get; set; } = string.Empty;

    public string ActualOutput { get; set; } = string.Empty;

    public string ExpectedOutput { get; set; } = string.Empty;

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

    public string? Feedback { get; set; }

    public int? GradedById { get; set; }

    public User? GradedBy { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? GradedAt { get; set; }
}
