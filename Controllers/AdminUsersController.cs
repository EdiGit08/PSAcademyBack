using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSAcademyBack.Data;
using PSAcademyBack.Dtos.Admin;
using PSAcademyBack.Entities;
using PSAcademyBack.Enums;
using PSAcademyBack.Extensions;

namespace PSAcademyBack.Controllers;

/// <summary>
/// Administración de cuentas: listar, editar, cambiar rol y borrar.
///
/// Son operaciones sobre personas, así que todo lo que no sea listar lleva dos
/// salvaguardas que viven aquí y no en la UI (que es solo una fachada): un admin no
/// puede tocar su propia cuenta, y no se puede dejar la plataforma sin ningún
/// administrador. Sin la segunda, degradar o borrar al último admin deja el panel
/// inaccesible para siempre y ya no hay forma de arreglarlo por la API.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminUsersController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(ApplicationDbContext dbContext, ILogger<AdminUsersController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // ------------------------------------------------------------------ Listado

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AdminUserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AdminUserResponse>>> GetUsers(CancellationToken cancellationToken)
    {
        var selfId = User.GetUserId();

        var users = await _dbContext.Users
            .AsNoTracking()
            .Select(u => new AdminUserResponse
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                AttemptedCount = u.Progress.Count,
                CompletedCount = u.Progress.Count(p => p.Status == ProgressStatus.Completed),
                DraftCount = u.Drafts.Count,
                LastActivityAt = u.Progress
                    .Select(p => (DateTime?)p.UpdatedAt)
                    .Concat(u.Drafts.Select(d => (DateTime?)d.UpdatedAt))
                    .Max()
            })
            .ToListAsync(cancellationToken);

        // El orden se resuelve en memoria porque "última actividad" es una agregación
        // sobre dos colecciones y las cuentas recién creadas la traen a null. Ordenar
        // por ahí deja arriba a los alumnos activos, que es lo que el admin consulta.
        foreach (var user in users)
        {
            user.IsSelf = user.Id == selfId;
        }

        return Ok(users
            .OrderByDescending(u => u.LastActivityAt ?? DateTime.MinValue)
            .ThenBy(u => u.Email, StringComparer.OrdinalIgnoreCase));
    }

    // ------------------------------------------------------------------ Edición

    /// <summary>
    /// Edita una cuenta. El recurso se manda completo: el correo y el rol siempre se
    /// reescriben, y la contraseña solo si viene informada (si no, se conserva la que
    /// ya tenía: el admin nunca ve el hash, así que no puede "reenviarla").
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AdminUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminUserResponse>> UpdateUser(
        int id,
        [FromBody] UpsertUserDto request,
        CancellationToken cancellationToken)
    {
        var selfId = User.GetUserId();
        var email = request.Email.Trim();

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Usuario no encontrado",
                Detail = $"No existe un usuario con id {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        // El admin se cambia a sí mismo con el formulario ya abierto en otra pestaña,
        // o por un clic accidental. Cualquiera de los dos casos lo deja fuera del panel.
        if (id == selfId)
        {
            return Forbid();
        }

        if (await _dbContext.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == email.ToLower(), cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Correo duplicado",
                Detail = $"Ya existe una cuenta registrada con '{email}'.",
                Status = StatusCodes.Status409Conflict
            });
        }

        // Degradar al único admin dejaría el panel sin nadie que lo administre.
        if (user.Role == UserRole.Admin
            && request.Role != UserRole.Admin
            && !await ExistsOtherAdminAsync(id, cancellationToken))
        {
            return Conflict(LastAdminProblem());
        }

        user.Email = email;
        user.Role = request.Role;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            // Mismo factor de coste que en el registro: si el admin fija una contraseña
            // nueva, esa cuenta queda con la misma protección que si se hubiera
            // registrado sola.
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword.Trim(), workFactor: 11);

            _logger.LogInformation(
                "El admin {AdminId} reinició la contraseña del usuario {UserId}", selfId, id);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "El admin {AdminId} actualizó la cuenta {UserId} (rol {Role})", selfId, id, request.Role);

        return Ok(await BuildResponseAsync(id, selfId, cancellationToken));
    }

    // -------------------------------------------------------------------- Borrado

    /// <summary>
    /// Borra la cuenta y todo lo suyo: progreso y borradores caen en cascada desde la
    /// base de datos. No hay papelera; la UI pide confirmación escribiendo el correo.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        var selfId = User.GetUserId();

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Usuario no encontrado",
                Detail = $"No existe un usuario con id {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (id == selfId)
        {
            return Forbid();
        }

        if (user.Role == UserRole.Admin && !await ExistsOtherAdminAsync(id, cancellationToken))
        {
            return Conflict(LastAdminProblem());
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("El admin {AdminId} borró la cuenta {UserId}", selfId, id);

        return NoContent();
    }

    // ---------------------------------------------------------------- Auxiliares

    /// <summary>
    /// True si existe algún administrador distinto del id indicado. Se cuenta con
    /// <c>!= id</c> en lugar de "total de admins mayor que uno" porque excluye la
    /// cuenta que se está a punto de degradar o borrar.
    /// </summary>
    private Task<bool> ExistsOtherAdminAsync(int id, CancellationToken cancellationToken)
        => _dbContext.Users
            .AnyAsync(u => u.Id != id && u.Role == UserRole.Admin, cancellationToken);

    private static ProblemDetails LastAdminProblem() => new()
    {
        Title = "No se puede quitar el último administrador",
        Detail = "Es el único administrador de la plataforma. Crea o designa otro "
                 + "administrador antes de cambiarle el rol o de borrar la cuenta.",
        Status = StatusCodes.Status409Conflict
    };

    /// <summary>
    /// Recarga una cuenta ya guardada para devolverla con los mismos contadores que
    /// devuelve el listado. Se construye desde la base y no desde la entidad en
    /// memoria para no arrastrar a la respuesta datos que no están en el DTO.
    /// </summary>
    private async Task<AdminUserResponse> BuildResponseAsync(int id, int? selfId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new AdminUserResponse
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                AttemptedCount = u.Progress.Count,
                CompletedCount = u.Progress.Count(p => p.Status == ProgressStatus.Completed),
                DraftCount = u.Drafts.Count,
                LastActivityAt = u.Progress
                    .Select(p => (DateTime?)p.UpdatedAt)
                    .Concat(u.Drafts.Select(d => (DateTime?)d.UpdatedAt))
                    .Max(),
                IsSelf = u.Id == selfId
            })
            .SingleAsync(cancellationToken);

        return user;
    }
}
