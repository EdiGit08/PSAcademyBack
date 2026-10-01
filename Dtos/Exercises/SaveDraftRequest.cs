using System.ComponentModel.DataAnnotations;

namespace PSAcademyBack.Dtos.Exercises;

/// <summary>
/// Cuerpo de <c>PUT /api/exercises/{id}/draft</c>. Guarda el código que el alumno
/// lleva escrito para un lenguaje, sin ejecutarlo.
/// </summary>
public class SaveDraftRequest
{
    [Required(ErrorMessage = "El lenguaje es obligatorio.")]
    public string LanguageSlug { get; set; } = string.Empty;

    /// <summary>Puede ser vacío a propósito (el alumno borró todo el editor).</summary>
    public string Code { get; set; } = string.Empty;
}
