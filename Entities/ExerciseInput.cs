using PSAcademyBack.Enums;

namespace PSAcademyBack.Entities;

/// <summary>
/// Un valor que el programa "lee" (stdin) cuando se ejecuta el ejercicio. Se guardan
/// ordenados y se concatenan como líneas de entrada, de modo que la primera lectura
/// recibe el primer valor, la segunda el segundo, etc.
/// </summary>
public class ExerciseInput
{
    public int Id { get; set; }

    public int ExerciseId { get; set; }

    public Exercise Exercise { get; set; } = null!;

    /// <summary>Posición en la secuencia de lecturas (0 para la primera).</summary>
    public int OrderIndex { get; set; }

    /// <summary>Valor tal como se enviará por stdin (línea de texto sin procesar).</summary>
    public string Value { get; set; } = string.Empty;

    public InputValueType ValueType { get; set; } = InputValueType.Number;
}
