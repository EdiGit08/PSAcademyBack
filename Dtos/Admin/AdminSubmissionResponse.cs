namespace PSAcademyBack.Dtos.Admin;

public class AdminSubmissionResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public int ExerciseId { get; set; }
    public string ExerciseTitle { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ActualOutput { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Feedback { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }
    public string? GradedByEmail { get; set; }
}
