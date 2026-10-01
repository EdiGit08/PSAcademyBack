namespace PSAcademyBack.Dtos.Exercises;

public class ExecuteCodeResponse
{
    public bool IsCorrect { get; set; }

    public string ActualOutput { get; set; } = string.Empty;

    public string ExpectedOutput { get; set; } = string.Empty;

    /// <summary>Salida de error o de compilación, si la hubo.</summary>
    public string? ErrorOutput { get; set; }

    public bool HasError { get; set; }

    /// <summary>Progreso del usuario tras este envío.</summary>
    public string? UserStatus { get; set; }
}
