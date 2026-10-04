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

    /// <summary>
    /// True cuando el ejercicio sigue en la cola del admin. El frontend usa esta
    /// bandera para explicar que no se puede enviar otra solución todavía, en lugar de
    /// dejar el botón activo y que la API responda 409.
    /// </summary>
    public bool AwaitingReview { get; set; }

    /// <summary>
    /// Id del envío creado por esta ejecución, cuando la salida resultó correcta y el
    /// ejercicio no tenía ya uno en cola. Es null en el tutorial (que se autocorrige) y
    /// cuando ya había un envío pendiente: en ese caso el progreso sigue siendo
    /// PendingReview y no se crea uno nuevo.
    /// </summary>
    public int? SubmissionId { get; set; }
}
