using System.ComponentModel.DataAnnotations;
using PSAcademyBack.Enums;

namespace PSAcademyBack.Dtos.Admin;

public class UpsertCategoryDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 120 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "La descripción no puede superar los 1000 caracteres.")]
    public string? Description { get; set; }

    [Range(0, 10_000, ErrorMessage = "OrderIndex debe estar entre 0 y 10000.")]
    public int OrderIndex { get; set; }
}

public class UpsertExerciseDto
{
    [Required(ErrorMessage = "La categoría es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La categoría es obligatoria.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "El título debe tener entre 3 y 200 caracteres.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    public string Description { get; set; } = string.Empty;

    [EnumDataType(typeof(Difficulty), ErrorMessage = "Dificultad no válida. Valores: Easy, Medium, Hard.")]
    public Difficulty Difficulty { get; set; } = Difficulty.Easy;

    [Required(ErrorMessage = "La salida esperada es obligatoria.")]
    public string ExpectedOutput { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>Plantillas por lenguaje. Reemplazan por completo las existentes.</summary>
    public List<UpsertTemplateDto> Templates { get; set; } = new();

    /// <summary>
    /// Valores del "Leer", en orden. El índice del arreglo define la secuencia de
    /// lectura: el primero es la primera entrada, el segundo la segunda, etc.
    /// Reemplazan por completo los existentes.
    /// </summary>
    public List<UpsertInputDto> Inputs { get; set; } = new();
}

public class UpsertInputDto
{
    [Required(ErrorMessage = "El valor de entrada es obligatorio.")]
    [StringLength(500, ErrorMessage = "El valor no puede superar los 500 caracteres.")]
    public string Value { get; set; } = string.Empty;

    [EnumDataType(typeof(InputValueType), ErrorMessage = "Tipo de valor no válido. Valores: Number, Text.")]
    public InputValueType ValueType { get; set; } = InputValueType.Number;
}

public class UpsertTemplateDto
{
    [Required(ErrorMessage = "El lenguaje es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El lenguaje es obligatorio.")]
    public int LanguageId { get; set; }

    [Required(ErrorMessage = "El código inicial es obligatorio.")]
    public string StarterCode { get; set; } = string.Empty;
}
