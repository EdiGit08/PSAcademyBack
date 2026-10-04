using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PSAcademyBack.Data;
using PSAcademyBack.Entities;

namespace PSAcademyBack.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly JwtOptions _options;

    public RefreshTokenService(ApplicationDbContext dbContext, IOptions<JwtOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task<(string Token, DateTime ExpiresAtUtc)> CreateRefreshTokenAsync(User user, CancellationToken cancellationToken)
    {
        var expiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays);
        var token = GenerateToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(token),
            ExpiresAtUtc = expiresAt,
            CreatedAtUtc = DateTime.UtcNow,
            IsRevoked = false
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await PurgeStaleTokensAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (token, expiresAt);
    }

    public async Task<User?> RotateAsync(string token, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(token);

        var stored = await _dbContext.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        if (stored is null || stored.IsRevoked || stored.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return null;
        }

        // UPDATE condicional: el `!r.IsRevoked` es lo que decide la carrera. Dos peticiones
        // simultaneas leen la misma fila valida, pero solo una logra revocar; la otra
        // actualiza cero filas y devuelve null, asi que no se emiten dos sesiones con el
        // mismo token. Con un `SaveChanges` normal, las dos crearian un token nuevo.
        var revoked = await _dbContext.RefreshTokens
            .Where(r => r.Id == stored.Id && !r.IsRevoked && r.ExpiresAtUtc > DateTime.UtcNow)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.IsRevoked, true)
                    .SetProperty(r => r.RevokedAtUtc, DateTime.UtcNow),
                cancellationToken);

        if (revoked == 0)
        {
            _dbContext.ChangeTracker.Clear();
            return null;
        }

        _dbContext.ChangeTracker.Clear();
        return stored.User;
    }

    public async Task<bool> RevokeAsync(string token, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(token);
        var rt = await _dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);
        if (rt == null) return false;
        if (rt.IsRevoked) return true;
        rt.IsRevoked = true;
        rt.RevokedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Borra los tokens que ya no sirven para nada.
    ///
    /// Cada renovacion deja un token revocado y con la fecha de revocacion puesta, asi que
    /// sin esta limpieza la tabla recibiria una fila por cada renovacion (aproximadamente
    /// una cada 50 minutos por usuario activo). Se conservan los revocados recientes por si
    /// hay que investigar un cierre de sesion.
    /// </summary>
    private async Task PurgeStaleTokensAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);

        var stale = await _dbContext.RefreshTokens
            .Where(r => r.ExpiresAtUtc <= DateTime.UtcNow
                        || (r.IsRevoked && r.RevokedAtUtc != null && r.RevokedAtUtc <= cutoff))
            .ToListAsync(cancellationToken);

        if (stale.Count == 0) return;

        _dbContext.RefreshTokens.RemoveRange(stale);
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// SHA-256 en base64: 64 caracteres, indice unico y comparacion directa en la base de
    /// datos. No se usa PBKDF2 porque el token ya es aleatorio de 512 bits: no hay nada
    /// que atacar por fuerza bruta, solo que no quede escrito en la tabla.
    /// </summary>
    private static string HashToken(string token)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
