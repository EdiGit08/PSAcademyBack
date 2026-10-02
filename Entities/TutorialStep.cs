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

    /// <summary>
    /// Valores que <see cref="CodeSnippet"/> recibe por entrada estándar, uno por línea.
    /// Si es null o vacío, el paso hereda los "valores del leer" del ejercicio.
    ///
    /// Existe porque un paso suele practicar una idea con datos distintos de los del
    /// reto final: la lección 4 pide dos números (15 y 25) mientras que el ejercicio
    /// completo lee tres (12, 8 y 5). Sin este campo el alumno vería por consola datos
    /// que no cuadran con la explicación del paso y ese paso sería imposible de superar.
    /// </summary>
    public string? Stdin { get; set; }

    /// <summary>Pista para desbloqueados (opcional).</summary>
    public string? Tip { get; set; }
}