using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PSAcademyBack.Data;
using PSAcademyBack.Dtos.Exercises;
using PSAcademyBack.Entities;
using PSAcademyBack.Enums;
using PSAcademyBack.Extensions;
using PSAcademyBack.Services;

namespace PSAcademyBack.Controllers;

[ApiController]
[Route("api/exercises")]
public class ExercisesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPistonExecutionService _pistonExecutionService;
    private readonly ILogger<ExercisesController> _logger;

    public ExercisesController(
        ApplicationDbContext dbContext,
        IPistonExecutionService pistonExecutionService,
        ILogger<ExercisesController> logger)
    {
        _dbContext = dbContext;
        _pistonExecutionService = pistonExecutionService;
        _logger = logger;
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ExerciseDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseDetailResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var exercise = await _dbContext.Exercises
            .AsNoTracking()
            .Where(e => e.Id == id && e.IsActive)
            .Select(e => new ExerciseDetailResponse
            {
                Id = e.Id,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name,
                Title = e.Title,
                Description = e.Description,
                Difficulty = e.Difficulty,
                ExpectedOutput = e.ExpectedOutput,
                Templates = e.Templates
                    .Where(t => t.Language.IsActive)
                    .OrderBy(t => t.Language.Name)
                    .Select(t => new ExerciseTemplateResponse
                    {
                        LanguageId = t.LanguageId,
                        LanguageName = t.Language.Name,
                        LanguageSlug = t.Language.Slug,
                        StarterCode = t.StarterCode
                    })
                    .ToList(),
                Inputs = e.Inputs
                    .OrderBy(i => i.OrderIndex)
                    .Select(i => new ExerciseInputResponse
                    {
                        OrderIndex = i.OrderIndex,
                        Value = i.Value,
                        ValueType = i.ValueType
                    })
                    .ToList(),
                TutorialSteps = e.TutorialSteps
                    .OrderBy(s => s.OrderIndex)
                    .Select(s => new TutorialStepResponse
                    {
                        Id = s.Id,
                        OrderIndex = s.OrderIndex,
                        Title = s.Title,
                        Body = s.Body,
                        Task = s.Task,
                        CodeSnippet = s.CodeSnippet,
                        ExpectedOutput = s.ExpectedOutput,
                        Tip = s.Tip
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (exercise is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ejercicio no encontrado",
                Detail = $"No existe un ejercicio activo con id {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var userId = User.GetUserId();
        if (userId.HasValue)
        {
            var status = await _dbContext.UserProgress
                .AsNoTracking()
                .Where(p => p.UserId == userId.Value && p.ExerciseId == id)
                .Select(p => (ProgressStatus?)p.Status)
                .SingleOrDefaultAsync(cancellationToken);

            exercise.UserStatus = status?.ToString().ToLowerInvariant();

            // Solo con identidad válida se devuelve el código guardado del alumno.
            exercise.Drafts = await _dbContext.UserCodeDrafts
                .AsNoTracking()
                .Where(d => d.UserId == userId.Value && d.ExerciseId == id)
                .OrderBy(d => d.Language.Name)
                .Select(d => new ExerciseDraftResponse
                {
                    LanguageSlug = d.Language.Slug,
                    Code = d.Code,
                    UpdatedAt = d.UpdatedAt
                })
                .ToListAsync(cancellationToken);
        }

        return Ok(exercise);
    }

    [HttpPost("{id:int}/execute")]
    [Authorize]
    [EnableRateLimiting(RateLimitingPolicies.CodeExecution)]
    [ProducesResponseType(typeof(ExecuteCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ExecuteCodeResponse>> Execute(
        int id,
        [FromBody] ExecuteCodeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Token inválido",
                Detail = "El token no contiene un identificador de usuario legible.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var exercise = await _dbContext.Exercises
            .AsNoTracking()
            .Include(e => e.Inputs)
            .Include(e => e.TutorialSteps)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive, cancellationToken);

        if (exercise is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ejercicio no encontrado",
                Detail = $"No existe un ejercicio activo con id {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var slug = request.LanguageSlug.Trim();

        var language = await _dbContext.Languages
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Slug.ToLower() == slug.ToLower(), cancellationToken);

        if (language is null || !language.IsActive)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Lenguaje no disponible",
                Detail = $"El lenguaje '{slug}' no está registrado o está inactivo.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (!_pistonExecutionService.IsSupported(language.Slug))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Lenguaje no ejecutable",
                Detail = $"El lenguaje '{language.Name}' no puede ejecutarse en el entorno de evaluación.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // En el tutorial la salida se valida contra la del paso, no contra la del
        // ejercicio: cada paso es una práctica independiente. Solo el último paso
        // marca el ejercicio como completado, de modo que aprobar el primero no
        // regale el progreso de la lección entera.
        var step = exercise.TutorialSteps
            .OrderBy(s => s.OrderIndex)
            .FirstOrDefault(s => s.Id == request.TutorialStepId);

        if (request.TutorialStepId.HasValue && step is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Paso no encontrado",
                Detail = $"El paso {request.TutorialStepId} no pertenece al ejercicio {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var expectedOutput = step?.ExpectedOutput ?? exercise.ExpectedOutput;
        var isLastStep = step is not null && step.OrderIndex == exercise.TutorialSteps.Max(s => s.OrderIndex);

        // Los "valores del leer" se entregan como líneas de stdin, en orden.
        var stdin = exercise.Inputs.Count == 0
            ? null
            : string.Join("\n", exercise.Inputs
                .OrderBy(i => i.OrderIndex)
                .Select(i => i.Value)) + "\n";

        var execution = await _pistonExecutionService.ExecuteAsync(
            language.Slug,
            request.Code,
            stdin,
            cancellationToken);

        if (!execution.Succeeded)
        {
            // Fallo de infraestructura (red, timeout, ejecutor caído): no se guarda progreso,
            // porque no fue un intento real del alumno.
            _logger.LogWarning(
                "Fallo del ejecutor en ejercicio {ExerciseId} usuario {UserId}: {Reason}",
                id, userId.Value, execution.FailureReason);

            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Title = "El ejecutor no está disponible",
                Detail = execution.FailureReason,
                Status = StatusCodes.Status502BadGateway
            });
        }

        var isCorrect = !execution.HasError && OutputNormalizer.AreEqual(expectedOutput, execution.Stdout);
        var status = isCorrect && (step is null || isLastStep) ? ProgressStatus.Completed : ProgressStatus.Attempted;

        await UpsertProgressAsync(userId.Value, id, status, request.Code, language.Id, cancellationToken);

        return Ok(new ExecuteCodeResponse
        {
            IsCorrect = isCorrect,
            ActualOutput = execution.Stdout,
            ExpectedOutput = OutputNormalizer.Normalize(expectedOutput),
            ErrorOutput = string.IsNullOrWhiteSpace(execution.Stderr) ? null : execution.Stderr,
            HasError = execution.HasError,
            UserStatus = status.ToString().ToLowerInvariant()
        });
    }

    /// <summary>
    /// Guarda (o reemplaza) el borrador de código del alumno para un lenguaje, sin
    /// ejecutarlo. Permite continuar más tarde exactamente donde se dejó.
    /// </summary>
    [HttpPut("{id:int}/draft")]
    [Authorize]
    [ProducesResponseType(typeof(ExerciseDraftResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseDraftResponse>> SaveDraft(
        int id,
        [FromBody] SaveDraftRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Token inválido",
                Detail = "El token no contiene un identificador de usuario legible.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        if (!await _dbContext.Exercises.AnyAsync(e => e.Id == id && e.IsActive, cancellationToken))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Ejercicio no encontrado",
                Detail = $"No existe un ejercicio activo con id {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var slug = request.LanguageSlug.Trim();

        var language = await _dbContext.Languages
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Slug.ToLower() == slug.ToLower(), cancellationToken);

        if (language is null || !language.IsActive)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Lenguaje no disponible",
                Detail = $"El lenguaje '{slug}' no está registrado o está inactivo.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var draft = await _dbContext.UserCodeDrafts
            .FirstOrDefaultAsync(
                d => d.UserId == userId.Value && d.ExerciseId == id && d.LanguageId == language.Id,
                cancellationToken);

        if (draft is null)
        {
            draft = new UserCodeDraft
            {
                UserId = userId.Value,
                ExerciseId = id,
                LanguageId = language.Id,
                Code = request.Code,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.UserCodeDrafts.Add(draft);
        }
        else
        {
            draft.Code = request.Code;
            draft.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ExerciseDraftResponse
        {
            LanguageSlug = language.Slug,
            Code = draft.Code,
            UpdatedAt = draft.UpdatedAt
        });
    }

    private async Task UpsertProgressAsync(
        int userId,
        int exerciseId,
        ProgressStatus status,
        string code,
        int languageId,
        CancellationToken cancellationToken)
    {
        var progress = await _dbContext.UserProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ExerciseId == exerciseId, cancellationToken);

        if (progress is null)
        {
            _dbContext.UserProgress.Add(new UserProgress
            {
                UserId = userId,
                ExerciseId = exerciseId,
                Status = status,
                LastSubmittedCode = code,
                LastSubmittedLanguageId = languageId,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            // Un intento fallido posterior nunca debe degradar un ejercicio ya completado.
            var wasCompleted = progress.Status == ProgressStatus.Completed;

            if (!wasCompleted)
            {
                progress.Status = status;
            }

            progress.LastSubmittedCode = code;

            // El lenguaje solo cambia cuando el ejercicio se supera: así se conserva
            // con qué lenguaje lo resolvió el alumno aunque después falle otro intento.
            if (!wasCompleted || status == ProgressStatus.Completed)
            {
                progress.LastSubmittedLanguageId = languageId;
            }

            progress.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
