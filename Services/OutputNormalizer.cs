namespace PSAcademyBack.Services;

/// <summary>
/// Normaliza salidas antes de compararlas. Sin esto, un ExpectedOutput guardado con CRLF
/// en Windows nunca coincidiría con el stdout de Linux que devuelve Piston.
/// </summary>
public static class OutputNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();
    }

    public static bool AreEqual(string? expected, string? actual)
        => string.Equals(Normalize(expected), Normalize(actual), StringComparison.Ordinal);
}
