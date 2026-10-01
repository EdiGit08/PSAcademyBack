namespace PSAcademyBack.Entities;

/// <summary>
/// Borrador de código que el alumno va escribiendo en el workspace. Se guarda por
/// (usuario, ejercicio, lenguaje) para que pueda cerrar el navegador y continuar
/// después justo donde lo dejó, sin haber ejecutado necesariamente.
/// </summary>
public class UserCodeDraft
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int ExerciseId { get; set; }

    public Exercise Exercise { get; set; } = null!;

    public int LanguageId { get; set; }

    public Language Language { get; set; } = null!;

    public string Code { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
