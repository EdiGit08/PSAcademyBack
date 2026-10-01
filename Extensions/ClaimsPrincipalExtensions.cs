using System.Security.Claims;
using PSAcademyBack.Enums;

namespace PSAcademyBack.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Devuelve el Id del usuario autenticado, o null si el claim no es válido.</summary>
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue("sub");

        return int.TryParse(raw, out var id) ? id : null;
    }

    public static bool IsInRole(this ClaimsPrincipal principal, UserRole role)
        => principal.IsInRole(role.ToString());
}
