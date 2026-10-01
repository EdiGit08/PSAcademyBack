namespace PSAcademyBack;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
}

public static class RateLimitingPolicies
{
    /// <summary>Límite para la ejecución de código, por usuario autenticado.</summary>
    public const string CodeExecution = "CodeExecution";
}
