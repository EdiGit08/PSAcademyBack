using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PSAcademyBack.Data;
using PSAcademyBack.Dtos.Admin;
using PSAcademyBack.Entities;
using PSAcademyBack.Enums;
using PSAcademyBack.Extensions;
using PSAcademyBack.Services;

namespace PSAcademyBack.Controllers;

/// <summary>
/// Bandeja de calificacion del administrador.
///
/// La lista se alimenta de las submissions creadas por los alumnos, no del progreso:
/// asi el admin ve exactamente lo que cada alumno mando, con su codigo y sus salidas,
/// y puede decidir sin depender de que el progreso se haya calculado bien.
/// </summary>
[ApiController]
[Route("api/admin/submissions")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminSubmissionsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IExerciseGradingService _gradingService;
    private readonly ILogger<AdminSubmissionsController> _logger;

    public AdminSubmissionsController(
        ApplicationDbContext dbContext,
        IExerciseGradingService gradingService,
        ILogger<AdminSubmissionsController> logger)
    {
        _dbContext = dbContext;
        _gradingService = gradingService;
        _logger = logger;
    }

    /// <summary>
    /// Lista las submissions. Por defecto solo las pendientes, que es la cola de trabajo;
    /// con <c>status=Correct|Incorrect|All</c> se consulta el historial ya calificado.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AdminSubmissionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AdminSubmissionResponse>>> List(
        [FromQuery] string? status, CancellationToken cancellationToken)
    {
        IQueryable<Submission> query = _dbContext.Submissions
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Exercise)
            .Include(s => s.Language);

        var statusFilter = string.IsNullOrWhiteSpace(status) ? "Pending" : status.Trim();
        if (statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            // Sin filtro.
        }
        else if (statusFilter.Equals("Correct", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.Status == SubmissionStatus.Correct);
        }
        else if (statusFilter.Equals("Incorrect", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.Status == SubmissionStatus.Incorrect);
        }
        else
        {
            query = query.Where(s => s.Status == SubmissionStatus.Pending);
        }

        var submissions = await query
            .OrderBy(s => s.Status == SubmissionStatus.Pending ? 0 : 1)
            .ThenBy(s => s.Status == SubmissionStatus.Pending ? s.SubmittedAt : s.GradedAt ?? s.SubmittedAt)
            .ToListAsync(cancellationToken);

        var adminById = await _dbContext.Users
            .Where(u => u.Role == UserRole.Admin)
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        var results = submissions
            .Select(s => new AdminSubmissionResponse
            {
                Id = s.Id,
                UserId = s.UserId,
                UserEmail = s.User.Email,
                ExerciseId = s.ExerciseId,
                ExerciseTitle = s.Exercise.Title,
                LanguageName = s.Language.Name,
                Code = s.Code,
                ActualOutput = s.ActualOutput,
                ExpectedOutput = s.ExpectedOutput,
                Status = s.Status.ToString(),
                Feedback = s.Feedback,
                SubmittedAt = s.SubmittedAt,
                GradedAt = s.GradedAt,
                GradedByEmail = s.GradedById.HasValue && adminById.TryGetValue(s.GradedById.Value, out var email)
                    ? email
                    : null
            })
            .ToList();

        return Ok(results);
    }

    /// <summary>
    /// Califica una submission. Si <c>correct=false</c> la justificacion es obligatoria:
    /// es lo unico que le dice al alumno que corregir, y el frontend lo pide antes de
    /// llamar, pero el backend no confía en esa UI.
    /// </summary>
    [HttpPut("{id:int}/grade")]
    [ProducesResponseType(typeof(AdminSubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminSubmissionResponse>> Grade(
        int id,
        [FromBody] GradeSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var feedback = request.Feedback?.Trim();

        if (!request.Correct && string.IsNullOrWhiteSpace(feedback))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Falta la justificación",
                Detail = "Al devolver un ejercicio hay que escribirle al alumno qué debe corregir.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var adminId = User.GetUserId();
        if (adminId is null)
        {
            return Unauthorized();
        }

        AdminSubmissionResponse? result;
        try
        {
            result = await _gradingService.GradeAsync(
                id, request.Correct, request.Correct ? null : feedback, adminId.Value, cancellationToken);
        }
        catch (SubmissionAlreadyGradedException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Envío ya calificado",
                Detail = "Otro administrador llego antes. Actualiza la bandeja para ver su decisión.",
                Status = StatusCodes.Status409Conflict
            });
        }

        if (result is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Envío no encontrado",
                Detail = $"No existe un envío con id {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        _logger.LogInformation(
            "El admin {AdminId} califico el envio {SubmissionId} como {Correct}",
            adminId.Value, id, request.Correct);

        return Ok(result);
    }
}
