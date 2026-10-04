namespace PSAcademyBack.Entities;

public class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>
    /// SHA-256 del token entregado al cliente, en base64.
    ///
    /// Nunca se guarda el token en claro: con acceso de solo lectura a la tabla (un dump,
    /// una copia de seguridad, un SELECT del soporte) bastaria para robar la sesion de
    /// todos los usuarios. El token solo existe en el navegador del cliente.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsRevoked { get; set; } = false;

    public DateTime? RevokedAtUtc { get; set; }
}
