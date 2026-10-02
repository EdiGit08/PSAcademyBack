namespace PSAcademyBack.Entities;

/// <summary>
/// Un paso guiado del tutorial. Solo lo usan los ejercicios de la categoría
/// "Tutorial": cada paso explica un concepto, trae un programa completo de PSeint
/// que el alumno ejecuta en el navegador y una salida esperada propia.
///
/// El paso se valida contra <see cref="ExpectedOutput"/> y no contra la del
/// ejercicio, de modo que el alumno puede equivocarse en un paso sin que eso
/// invalide su progreso en el ejercicio completo.
/// </summary>
public class TutorialStep
{
    public int Id { get; set; }

    public int ExerciseId { get; set; }

    public Exercise Exercise { get; set; } = null!;

    /// <summary>Posición del paso dentro del tutorial (0, 1, 2...).</summary>
    public int OrderIndex { get; set; }

    /// <summary>Título corto del paso, p. ej. "Tu primer Leer".</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Explicación en Markdown: qué hace el código y por qué.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Reto que el alumno debe resolver en este paso (opcional).</summary>
    public string? Task { get; set; }

    /// <summary>Programa completo en PSeint que el alumno ejecuta en este paso.</summary>
    public string CodeSnippet { get; set; } = string.Empty;

    /// <summary>Salida que debe producir <see cref="CodeSnippet"/>.</summary>
    public string ExpectedOutput { get; set; } = string.Empty;

    /// <summary>Pista para desbloqueados (opcional).</summary>
    public string? Tip { get; set; }
}