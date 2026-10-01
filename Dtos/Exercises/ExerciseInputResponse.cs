using PSAcademyBack.Enums;

namespace PSAcademyBack.Dtos.Exercises;

/// <summary>
/// Valor de entrada ("valor del leer") expuesto al cliente. El orden del arreglo
/// es la secuencia en que el programa recibe cada línea por stdin.
/// </summary>
public class ExerciseInputResponse
{
    public int OrderIndex { get; set; }

    public string Value { get; set; } = string.Empty;

    public InputValueType ValueType { get; set; }
}
