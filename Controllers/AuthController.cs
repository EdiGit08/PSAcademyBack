using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSAcademyBack.Data;
using PSAcademyBack.Dtos.Auth;
using PSAcademyBack.Entities;
using PSAcademyBack.Enums;
using PSAcademyBack.Services;

namespace PSAcademyBack.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);

        // Mismo mensaje tanto si el usuario no existe como si la contraseña es
        // incorrecta, para no filtrar qué correos están registrados.
        static ProblemDetails InvalidCredentials() => new()
        {
            Title = "Credenciales inválidas",
            Detail = "El correo o la contraseña son incorrectos.",
            Status = StatusCodes.Status401Unauthorized
        };

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogInformation("Intento de login fallido para {Email}", email);
            return Unauthorized(InvalidCredentials());
        }

        var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user);
        var (refreshToken, refreshExpiresAtUtc) = await _refreshTokenService.CreateRefreshTokenAsync(user, cancellationToken);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiresAtUtc,
            User = new AuthUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role.ToString().ToLowerInvariant()
            }
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResponse>> Register(
        [FromBody] RegisterDto request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _dbContext.Users
            .AnyAsync(u => u.Email.ToLower() == email, cancellationToken);

        if (emailExists)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Correo ya registrado",
                Detail = "Ya existe una cuenta con ese correo.",
                Status = StatusCodes.Status409Conflict
            });
        }

        // El registro público nunca puede pedir un rol: el cliente no elige. El único
        // caso en el que se concede Admin es la PRIMERA alta de la vida de la base de
        // datos, para que exista alguien que pueda entrar al panel y cargar el curso.
        //
        // En producción no se crea ningún admin por semilla (DbSeeder está bloqueado a
        // Development porque genera admin@psacademy.com con contraseña conocida), así
        // que "el primero que se registra es el admin" es el mecanismo de arranque.
        //
        // La comprobación es un SELECT sin bloqueo: dos registros simultáneos sobre una
        // base vacía podrían obtener los dos rol Admin. Se acepta a propósito, porque
        // la consecuencia es inocua (dos administradores) y el escenario exige que dos
        // personas se registren en el mismo instante sobre una base recién creada.
        var isFirstUser = !await _dbContext.Users.AnyAsync(cancellationToken);

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 11),
            Role = isFirstUser ? UserRole.Admin : UserRole.User,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (isFirstUser)
        {
            _logger.LogWarning(
                "Primer usuario de la base de datos ({UserId}) registrado con rol Admin. " +
                "Las siguientes altas se crean con rol User.",
                user.Id);
        }

        _logger.LogInformation("Usuario registrado con id {UserId}", user.Id);

        var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user);
        var (refreshToken, refreshExpiresAtUtc) = await _refreshTokenService.CreateRefreshTokenAsync(user, cancellationToken);

        return Created($"/api/auth/users/{user.Id}", new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiresAtUtc,
            User = new AuthUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role.ToString().ToLowerInvariant()
            }
        });
    }

    /// <summary>
    /// Canjea el refresh token por un JWT nuevo. El refresh token se rota en cada uso
    /// (el anterior queda revocado): si alguien lo robó, el próximo canje del atacante
    /// falla y delata el uso indebido. El frontend llama a este endpoint automáticamente
    /// cuando detecta que el token de acceso está por expirar o una petición dio 401,
    /// que es lo que mantiene viva la sesión mientras el alumno sigue trabajando.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RefreshResponse>> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Refresh token ausente",
                Detail = "No se recibió el refresh token.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Rotación: el token usado se revoca dentro de la propia validación y se emite
        // uno nuevo. Si el canje falla es porque el token no existía, ya se había canjeado
        // o había caducado, y en los tres casos la sesión ya no es renovable.
        var user = await _refreshTokenService.RotateAsync(request.RefreshToken, cancellationToken);

        if (user is null)
        {
            _logger.LogInformation("Intento de refresh con token invalido, expirado, revocado o ya rotado.");

            return Unauthorized(new ProblemDetails
            {
                Title = "Sesión expirada",
                Detail = "El token de renovación no es válido. Vuelve a iniciar sesión.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var (refreshToken, refreshExpiresAtUtc) =
            await _refreshTokenService.CreateRefreshTokenAsync(user, cancellationToken);

        var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user);

        return Ok(new RefreshResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiresAtUtc
        });
    }

    /// <summary>
    /// Revoca el refresh token del cliente. Responde 204 exista o no el token: confirmar
    /// el cierre de sesion no debe convertirse en un oraculo sobre que tokens existen.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            await _refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken);
        }

        return NoContent();
    }
}
