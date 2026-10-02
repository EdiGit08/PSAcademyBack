namespace PSAcademyBack.Dtos.Exercises;

/// <summary>
/// Paso guiado de un ejercicio de tutorial. Lo consume la página /tutorial, que
/// muestra la explicación, carga el código en el editor y valida la salida del
/// alumno contra <see cref="ExpectedOutput"/>.
/// </summary>
public class TutorialStepResponse
{
    public int Id { get; set; }

    public int OrderIndex { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string? Task { get; set; }

    /// <summary>Programa completo en PSeint que el alumno ejecuta en este paso.</summary>
    public string CodeSnippet { get; set; } = string.Empty;

    public string ExpectedOutput { get; set; } = string.Empty;

    /// <summary>
    /// Valores que el snippet del paso recibe por entrada estándar, uno por línea.
    /// Llega resuelto (heredado del ejercicio si el paso no trae los suyos) para que
    /// el panel pueda mostrarle al alumno con qué datos se ejecuta el paso.
    /// </summary>
    public string? Stdin { get; set; }

    public string? Tip { get; set; }
}