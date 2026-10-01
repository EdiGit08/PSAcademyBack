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
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDbContext dbContext,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
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

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
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

        // El registro público nunca puede asignar roles: el valor es fijo "user".
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 11),
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Usuario registrado con id {UserId}", user.Id);

        var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user);

        return Created($"/api/auth/users/{user.Id}", new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            User = new AuthUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role.ToString().ToLowerInvariant()
            }
        });
    }
}
