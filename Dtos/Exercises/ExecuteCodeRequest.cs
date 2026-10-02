using System.ComponentModel.DataAnnotations;

namespace PSAcademyBack.Dtos.Exercises;

public class ExecuteCodeRequest
{
    /// <summary>Slug del lenguaje, p. ej. "python". Debe existir en Languages y estar activo.</summary>
    [Required(ErrorMessage = "El lenguaje es obligatorio.")]
    public string LanguageSlug { get; set; } = string.Empty;

    [Required(ErrorMessage = "El código es obligatorio.")]
    [MaxLength(20_000, ErrorMessage = "El código supera el máximo permitido de 20000 caracteres.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Paso del tutorial que se está validando. Si viene informado, la salida se
    /// compara contra la del paso y no contra la del ejercicio, de modo que el alumno
    /// puede practicar sin que un fallo en un paso intermedio le cierre el ejercicio.
    /// Solo el último paso da el ejercicio por completado.
    /// </summary>
    public int? TutorialStepId { get; set; }
}
