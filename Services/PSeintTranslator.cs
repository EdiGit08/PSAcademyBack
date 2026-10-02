using System.Text;
using System.Text.RegularExpressions;

namespace PSAcademyBack.Services;

/// <summary>
/// Error de traducción atribuible al código del alumno (constructo no soportado o
/// sintaxis inválida). Se reporta como salida de error normal, no como fallo de
/// infraestructura.
/// </summary>
public sealed class PSeintTranslationException : Exception
{
    public int LineNumber { get; }

    public PSeintTranslationException(int lineNumber, string message)
        : base($"Línea {lineNumber}: {message}")
    {
        LineNumber = lineNumber;
    }
}

/// <summary>
/// Traduce un subconjunto de PSeint a Python ejecutable en Piston.
///
/// Soportado: Proceso/Algoritmo, Definir, Leer, Escribir (con "Sin Saltar"),
/// asignación (&lt;- y =), Si/Sino/FinSi, Mientras/FinMientras, Repetir/Hasta Que,
/// Para/Con Paso/FinPara, operadores lógicos y aritméticos, y funciones básicas.
///
/// No soportado (se informa con línea): arreglos (Dimension y accesos [i]),
/// Segun, SubProceso/Funcion y bloques en una sola línea.
/// </summary>
public static class PSeintTranslator
{
    private const string NewLine = "\n";

    public static string ToPython(string pseintCode)
    {
        var normalized = pseintCode.Replace("\r\n", "\n").Replace('\r', '\n');
        var sourceLines = normalized.Split('\n');

        var python = new List<string>
        {
            "import math",
            "import random",
            string.Empty,
            "def __leer(tipo):",
            "    valor = input()",
            "    if tipo == \"int\":",
            "        try:",
            "            return int(valor)",
            "        except ValueError:",
            "            return int(float(valor))",
            "    if tipo == \"float\":",
            "        return float(valor)",
            "    if tipo == \"bool\":",
            "        return valor.strip().lower() in (\"verdadero\", \"true\", \"1\")",
            "    if tipo == \"auto\":",
            "        t = valor.strip()",
            "        try:",
            "            return int(t)",
            "        except ValueError:",
            "            pass",
            "        try:",
            "            return float(t)",
            "        except ValueError:",
            "            return valor",
            "    return valor",
            string.Empty,
            "def __mayusculas(valor):",
            "    return str(valor).upper()",
            string.Empty,
            "def __minusculas(valor):",
            "    return str(valor).lower()",
            string.Empty,
            "def __longitud(valor):",
            "    return len(valor)",
            string.Empty,
            "# --- Código traducido desde PSeint ---"
        };

        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var frames = new Stack<Frame>();
        var indent = 0;

        void Pad(string text)
        {
            python.Add(new string(' ', indent * 4) + text);
        }

        void Append(string text)
        {
            Pad(text);
            if (frames.Count > 0)
            {
                frames.Peek().HasBody = true;
            }
        }

        void OpenBlock(string header)
        {
            Append(header);
            frames.Push(new Frame { Indent = indent });
            indent++;
        }

        void CloseBlock()
        {
            var frame = frames.Pop();
            if (!frame.HasBody)
            {
                python.Add(new string(' ', indent * 4) + "pass");
            }

            indent = frame.Indent;
        }

        for (var index = 0; index < sourceLines.Length; index++)
        {
            var lineNumber = index + 1;
            var line = StripComment(sourceLines[index]).Trim().TrimEnd(';').Trim();

            if (line.Length == 0)
            {
                continue;
            }

            // Cabeceras y cierres del algoritmo: no generan código.
            if (Regex.IsMatch(line, @"^(Proceso|Algoritmo|FinProceso|FinAlgoritmo)\b", RegexOptions.IgnoreCase))
            {
                continue;
            }

            var match = Regex.Match(line, @"^Definir\s+(.+?)\s+Como\s+(.+)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var cast = MapType(match.Groups[2].Value, lineNumber);
                foreach (var name in SplitTopLevel(match.Groups[1].Value, ','))
                {
                    var trimmed = name.Trim();
                    if (trimmed.Length > 0)
                    {
                        variables[trimmed] = cast;
                    }
                }

                continue;
            }

            match = Regex.Match(line, @"^Leer\s+(.+)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                foreach (var name in SplitTopLevel(match.Groups[1].Value, ','))
                {
                    var variable = name.Trim();
                    if (variable.Length == 0)
                    {
                        continue;
                    }

                    var cast = variables.TryGetValue(variable, out var declared) ? declared : "auto";
                    Append($"{variable} = __leer(\"{cast}\")");
                }

                continue;
            }

            match = Regex.Match(line, @"^Escribir\b(.*)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                Append(TranslateWrite(match.Groups[1].Value));
                continue;
            }

            match = Regex.Match(line, @"^Si\s+(.+?)\s+Entonces\b(.*)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EnsureNoInlineBody(match.Groups[2].Value, lineNumber);
                OpenBlock($"if {TranslateExpression(match.Groups[1].Value)}:");
                continue;
            }

            if (Regex.IsMatch(line, @"^Sino\b", RegexOptions.IgnoreCase))
            {
                var extra = Regex.Replace(line, @"^Sino\b", string.Empty, RegexOptions.IgnoreCase).Trim();
                if (extra.Length > 0)
                {
                    throw new PSeintTranslationException(
                        lineNumber, "no se soporta 'Sino Si' ni instrucciones en la misma línea que 'Sino'.");
                }

                EmitElse();
                continue;
            }

            if (Regex.IsMatch(line, @"^FinSi\b", RegexOptions.IgnoreCase))
            {
                CloseBlock();
                continue;
            }

            match = Regex.Match(line, @"^Mientras\s+(.+?)\s+Hacer\b(.*)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EnsureNoInlineBody(match.Groups[2].Value, lineNumber);
                OpenBlock($"while {TranslateExpression(match.Groups[1].Value)}:");
                continue;
            }

            if (Regex.IsMatch(line, @"^FinMientras\b", RegexOptions.IgnoreCase))
            {
                CloseBlock();
                continue;
            }

            if (Regex.IsMatch(line, @"^Repetir\b", RegexOptions.IgnoreCase))
            {
                OpenBlock("while True:");
                continue;
            }

            match = Regex.Match(line, @"^Hasta\s+Que\s+(.+)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (frames.Count == 0)
                {
                    throw new PSeintTranslationException(lineNumber, "'Hasta Que' sin un 'Repetir' abierto.");
                }

                var frame = frames.Peek();
                Pad($"if {TranslateExpression(match.Groups[1].Value)}: break");
                frame.HasBody = true;
                indent = frame.Indent;
                frames.Pop();
                continue;
            }

            match = Regex.Match(
                line,
                @"^Para\s+([A-Za-z_]\w*)\s*(?:<-|=)\s*(.+?)\s+Hasta\s+(.+?)(?:\s+Con\s+Paso\s+(.+?))?\s+Hacer\b(.*)$",
                RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EnsureNoInlineBody(match.Groups[5].Value, lineNumber);
                var variable = match.Groups[1].Value;
                var start = TranslateExpression(match.Groups[2].Value);
                var end = TranslateExpression(match.Groups[3].Value);
                var step = match.Groups[4].Success ? TranslateExpression(match.Groups[4].Value) : null;

                var range = step is null
                    ? $"range({start}, ({end}) + 1)"
                    : $"range({start}, ({end}) + (1 if ({step}) >= 0 else -1), {step})";

                OpenBlock($"for {variable} in {range}:");
                continue;
            }

            if (Regex.IsMatch(line, @"^FinPara\b", RegexOptions.IgnoreCase))
            {
                CloseBlock();
                continue;
            }

            if (Regex.IsMatch(line, @"^(Dimension|Segun|FinSegun|SubProceso|FinSubProceso|Funcion|FinFuncion)\b", RegexOptions.IgnoreCase))
            {
                throw new PSeintTranslationException(
                    lineNumber, "constructo no soportado por el traductor (arreglos, Segun o subprocesos).");
            }

            match = Regex.Match(line, @"^([A-Za-z_]\w*)\s*(?:<-|=)\s*(.+)$");
            if (match.Success)
            {
                var variable = match.Groups[1].Value;
                Append($"{variable} = {TranslateExpression(match.Groups[2].Value)}");
                continue;
            }

            throw new PSeintTranslationException(lineNumber, $"instrucción no reconocida: '{line}'.");
        }

        while (frames.Count > 0)
        {
            // Cierres implícitos al final del archivo (tolerancia a bloques sin cerrar).
            CloseBlock();
        }

        return string.Join(NewLine, python);

        void EmitElse()
        {
            var frame = frames.Peek();

            if (!frame.HasBody)
            {
                python.Add(new string(' ', indent * 4) + "pass");
            }

            indent = frame.Indent;
            Pad("else:");
            indent = frame.Indent + 1;
            frame.HasBody = false;
        }
    }

    private static void EnsureNoInlineBody(string remainder, int lineNumber)
    {
        if (!string.IsNullOrWhiteSpace(remainder))
        {
            throw new PSeintTranslationException(
                lineNumber, "no se admiten instrucciones en la misma línea que el bloque (usa varias líneas).");
        }
    }

    private static string MapType(string pseintType, int lineNumber)
    {
        var normalized = RemoveDiacritics(pseintType.Trim().ToLowerInvariant());

        return normalized switch
        {
            "entero" => "int",
            "real" or "numero" => "float",
            "caracter" or "cadena" or "texto" => "str",
            "logico" or "booleano" => "bool",
            _ => throw new PSeintTranslationException(lineNumber, $"tipo de dato no soportado: '{pseintType.Trim()}'.")
        };
    }

    private static string TranslateWrite(string arguments)
    {
        var withoutNoNewLine = Regex.Replace(arguments, @"\bSin\s+Saltar\b", string.Empty, RegexOptions.IgnoreCase);
        var end = withoutNoNewLine.Length != arguments.Length ? ", end=\"\"" : string.Empty;

        var parts = SplitTopLevel(withoutNoNewLine, ',')
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .Select(TranslateExpression)
            .ToList();

        if (parts.Count == 0)
        {
            return "print()";
        }

        return $"print({string.Join(", ", parts)}, sep=\"\"{end})";
    }

    /// <summary>
    /// Traduce una expresión o condición de PSeint a Python. Los literales de texto
    /// se aíslan antes de reemplazar operadores para no tocarlos.
    /// </summary>
    private static string TranslateExpression(string expression)
    {
        var literals = new List<string>();

        expression = Regex.Replace(expression, "\"([^\"]*)\"", match =>
        {
            literals.Add("\"" + match.Groups[1].Value + "\"");
            return "\u0001" + (literals.Count - 1) + "\u0001";
        });

        expression = Regex.Replace(
            expression,
            @"\bPotencia\s*\(\s*([^,()]+)\s*,\s*([^,()]+)\s*\)",
            "(($1) ** ($2))",
            RegexOptions.IgnoreCase);
        expression = Regex.Replace(
            expression,
            @"\bMod\s*\(\s*([^,()]+)\s*,\s*([^,()]+)\s*\)",
            "(($1) % ($2))",
            RegexOptions.IgnoreCase);

        expression = expression.Replace("<>", "!=");
        expression = Regex.Replace(expression, @"(?<![<>=!])=(?!=)", "==");
        expression = expression.Replace("^", "**");

        // Los operadores logicos se distinguen por mayuscula a proposito: en PSeint se
        // escriben en mayuscula (Si a > 0 Y b < 10) y, sin distinguir mayusculas, una
        // variable llamada 'y' se convertiria en 'and' (x + y -> x + and). Solo la
        // forma en mayuscula puede ser el operador.
        expression = Regex.Replace(expression, @"\bNo\b", "not");
        expression = Regex.Replace(expression, @"\bY\b", "and");
        expression = Regex.Replace(expression, @"\bO\b", "or");
        expression = Regex.Replace(expression, @"\bMod\b", "%");
        expression = Regex.Replace(expression, @"\bVerdadero\b", "True", RegexOptions.IgnoreCase);
        expression = Regex.Replace(expression, @"\bFalso\b", "False", RegexOptions.IgnoreCase);

        expression = Regex.Replace(expression, @"\bRaiz\s*\(", "math.sqrt(", RegexOptions.IgnoreCase);
        expression = Regex.Replace(expression, @"\bAbs\s*\(", "abs(", RegexOptions.IgnoreCase);
        expression = Regex.Replace(expression, @"\bTrunc\s*\(", "math.trunc(", RegexOptions.IgnoreCase);
        expression = Regex.Replace(expression, @"\bRedon\s*\(", "round(", RegexOptions.IgnoreCase);
        expression = Regex.Replace(expression, @"\bLongitud\s*\(", "__longitud(", RegexOptions.IgnoreCase);
        expression = Regex.Replace(expression, @"\bMayusculas\s*\(", "__mayusculas(", RegexOptions.IgnoreCase);
        expression = Regex.Replace(expression, @"\bMinusculas\s*\(", "__minusculas(", RegexOptions.IgnoreCase);
        expression = Regex.Replace(expression, @"\bAleatorio\s*\(", "random.randint(", RegexOptions.IgnoreCase);

        for (var i = 0; i < literals.Count; i++)
        {
            expression = expression.Replace("\u0001" + i + "\u0001", literals[i]);
        }

        return expression;
    }

    private static string StripComment(string line)
    {
        var inString = false;

        for (var i = 0; i < line.Length; i++)
        {
            var current = line[i];

            if (current == '"')
            {
                inString = !inString;
            }
            else if (!inString && current == '/' && i + 1 < line.Length && line[i + 1] == '/')
            {
                return line[..i];
            }
        }

        return line;
    }

    private static List<string> SplitTopLevel(string value, char separator)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var depth = 0;
        var inString = false;

        foreach (var character in value)
        {
            if (character == '"')
            {
                inString = !inString;
            }

            if (!inString)
            {
                if (character == '(')
                {
                    depth++;
                }
                else if (character == ')')
                {
                    depth--;
                }
                else if (character == separator && depth == 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                    continue;
                }
            }

            current.Append(character);
        }

        parts.Add(current.ToString());
        return parts;
    }

    private static string RemoveDiacritics(string value)
        => value
            .Replace("á", "a").Replace("é", "e").Replace("í", "i")
            .Replace("ó", "o").Replace("ú", "u").Replace("ü", "u");

    private sealed class Frame
    {
        public int Indent { get; init; }

        public bool HasBody { get; set; }
    }
}
