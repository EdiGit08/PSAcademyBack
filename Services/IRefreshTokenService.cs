using PSAcademyBack.Entities;

namespace PSAcademyBack.Services;

public interface IRefreshTokenService
{
    Task<(string Token, DateTime ExpiresAtUtc)> CreateRefreshTokenAsync(User user, CancellationToken cancellationToken);
    Task<bool> RevokeAsync(string token, CancellationToken cancellationToken);

    /// <summary>
    /// Valida el token, lo revoca y devuelve su propietario si el canje es válido.
    ///
    /// La revocación va dentro de la misma operación que la validación para que dos
    /// peticiones simultáneas con el mismo token (dos pestañas abiertas, o un atacante
    /// reutilizando uno robado) no puedan emitir dos tokens nuevos: solo la que logra
    /// revocar la fila recibe un JWT.
    /// </summary>
    Task<User?> RotateAsync(string token, CancellationToken cancellationToken);
}