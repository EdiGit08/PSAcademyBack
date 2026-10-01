namespace PSAcademyBack.Enums;

/// <summary>
/// Tipo declarado de un valor de entrada ("valor del leer"). No cambia cómo viaja
/// el texto por stdin, pero define cómo se muestra y valida en el panel de admin.
/// </summary>
public enum InputValueType
{
    /// <summary>Valor numérico (entero o decimal).</summary>
    Number,

    /// <summary>Valor de texto.</summary>
    Text
}
