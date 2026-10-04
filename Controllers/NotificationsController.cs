using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSAcademyBack.Data;
using PSAcademyBack.Dtos;
using PSAcademyBack.Extensions;

namespace PSAcademyBack.Controllers;

/// <summary>
/// Notificaciones dentro de la aplicacion.
///
/// Son de solo lectura para el receptor (listar y marcar como leida); el sistema las
/// crea el backend al.calificar o al recibir un envio. No hay push ni email: el frontend
/// las sondea cada 30 s, que para este volumen es mas que suficiente y evita depender de
/// un servicio externo.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public NotificationsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Notificaciones del usuario autenticado, mas recientes primero. El contador de
    /// no leidas viaja en la cabecera X-Unread-Count para que la campana no tenga que
    /// pedir una segunda peticion en cada sondeo.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<NotificationResponse>>> List(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var unreadCount = await _dbContext.Notifications
            .CountAsync(n => n.UserId == userId.Value && !n.IsRead, cancellationToken);

        Response.Headers["X-Unread-Count"] = unreadCount.ToString();

        var notifications = await _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId.Value)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationResponse
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                Title = n.Title,
                Message = n.Message,
                Data = n.Data,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(notifications);
    }

    /// <summary>
    /// Marca una notificacion como leida. Se limita a las del propio usuario: el id se
    /// filtra por UserId en la consulta, no se comprueba despues.
    /// </summary>
    [HttpPatch("{id:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId.Value, cancellationToken);

        if (notification is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Notificación no encontrada",
                Detail = $"No existe la notificación {id} para esta cuenta.",
                Status = StatusCodes.Status404NotFound
            });
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }
}
