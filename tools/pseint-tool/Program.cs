using PSAcademyBack.Services;

// Traductor de PSeint a Python: lee el algoritmo por stdin y escribe por stdout el
// Python que el backend entregaria a Piston. Si la traduccion falla, escribe el
// error en stderr y sale con codigo 1 para que el generador de contenidos lo detecte.
try
{
    var pseint = Console.In.ReadToEnd();
    Console.Out.Write(PSeintTranslator.ToPython(pseint));
    return 0;
}
catch (PSeintTranslationException exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}