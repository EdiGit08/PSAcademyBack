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
    private const string TutorialCategoryName = "Tutorial";

    private readonly ApplicationDbContext _dbContext;
    private readonly IPistonExecutionService _pistonExecutionService;
    private readonly ExerciseGradingService _gradingService;
    private readonly ILogger<ExercisesController> _logger;

    public ExercisesController(
        ApplicationDbContext dbContext,
        IPistonExecutionService pistonExecutionService,
        ExerciseGradingService gradingService,
        ILogger<ExercisesController> logger)
    {
        _dbContext = dbContext;
        _pistonExecutionService = pistonExecutionService;
        _gradingService = gradingService;
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
                        Stdin = s.Stdin,
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

        // La entrada de un paso se resuelve en memoria y no dentro del Select: el
        // recorte de saltos de línea no es traducible a SQL y fallaría en tiempo de
        // ejecución. Se devuelve ya resuelta para que el panel del tutorial le muestre
        // al alumno los mismos datos que el backend le va a entregar al ejecutar.
        var exerciseStdin = JoinStdinLines(exercise.Inputs.OrderBy(i => i.OrderIndex).Select(i => i.Value));

        foreach (var step in exercise.TutorialSteps)
        {
            step.Stdin = ResolveStepStdin(step.Stdin) ?? exerciseStdin;
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

            // Solo se muestra la justificación si el último envío fue devuelto: es la
            // única información que el alumno necesita para entender qué corregir.
            exercise.Feedback = await _dbContext.UserProgress
                .AsNoTracking()
                .Where(p => p.UserId == userId.Value && p.ExerciseId == id && p.Status == ProgressStatus.Incorrect)
                .OrderByDescending(p => p.UpdatedAt)
                .Select(p => p.Feedback)
                .FirstOrDefaultAsync(cancellationToken);

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
            .Include(e => e.Category)
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

        // Cada paso puede traer sus propios datos de entrada, y si no los trae hereda
        // los del ejercicio. El orden importa: la lección 4 explica la suma de dos
        // números con 15 y 25, pero su reto final lee tres (12, 8 y 5). Sin esta
        // precedencia el paso se ejecutaría con los datos del reto, su salida nunca
        // coincidiría con la esperada y el alumno quedaría atascado ahí.
        var stepStdin = ResolveStepStdin(step?.Stdin);
        var stdin = stepStdin ?? JoinStdinLines(exercise.Inputs.OrderBy(i => i.OrderIndex).Select(i => i.Value));

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

        // Los ejercicios normales NO se completan al acertar la salida: quedan en espera
        // de que el admin los revise. El tutorial es la excepción y mantiene su
        // autocorrección, porque sus pasos son prácticas guiadas y no entregas.
        var isTutorialExercise = step is not null
            || string.Equals(exercise.Category?.Name, TutorialCategoryName, StringComparison.OrdinalIgnoreCase);

        if (isTutorialExercise)
        {
            var tutorialStatus = isCorrect && (step is null || isLastStep)
                ? ProgressStatus.Completed
                : ProgressStatus.Attempted;

            await UpsertProgressAsync(userId.Value, id, tutorialStatus, request.Code, language.Id, cancellationToken);

            return Ok(new ExecuteCodeResponse
            {
                IsCorrect = isCorrect,
                ActualOutput = execution.Stdout,
                ExpectedOutput = OutputNormalizer.Normalize(expectedOutput),
                ErrorOutput = string.IsNullOrWhiteSpace(execution.Stderr) ? null : execution.Stderr,
                HasError = execution.HasError,
                UserStatus = tutorialStatus.ToString().ToLowerInvariant()
            });
        }

        // Ejercicio normal: ejecutar no completa el ejercicio, pero si la salida es
        // correcta ENVIA la solucion a calificacion en la misma llamada. Acertar y enviar
        // son el mismo gesto para el alumno; pedir un segundo "Enviar" obligaria a
        // ejecutar el codigo dos veces contra el ejecutor, que es la parte lenta, y dejaria
        // margen para que el alumno enviara algo distinto de lo que acaba de ver pasar.
        var alreadyPending = await _dbContext.Submissions
            .AnyAsync(
                s => s.UserId == userId.Value && s.ExerciseId == id && s.Status == SubmissionStatus.Pending,
                cancellationToken);

        // Un ejercicio ya completado no se vuelve a enviar. El alumno puede seguir
        // probando variantes, pero sin generar trabajo para el admin ni risking perder el
        // aprobado anterior por una nueva decision.
        var isAlreadyCompleted = await _dbContext.UserProgress
            .AnyAsync(
                p => p.UserId == userId.Value && p.ExerciseId == id && p.Status == ProgressStatus.Completed,
                cancellationToken);

        Submission? submission = null;

        if (isCorrect && !alreadyPending && !isAlreadyCompleted)
        {
            submission = await _gradingService.SubmitAsync(
                userId.Value, id, language.Id, request.Code,
                execution.Stdout, OutputNormalizer.Normalize(expectedOutput), cancellationToken);
        }

        // Con un envio en cola el estado es PendingReview y sigue asi aunque el alumno
        // vuelva a ejecutar: no puede esquivar la calificacion pendiente.
        var awaitingReview = alreadyPending || submission is not null;

        var resultingStatus = isAlreadyCompleted
            ? ProgressStatus.Completed
            : awaitingReview
                ? ProgressStatus.PendingReview
                : ProgressStatus.Attempted;

        await UpsertProgressAsync(
            userId.Value, id, resultingStatus, request.Code, language.Id, cancellationToken);

        return Ok(new ExecuteCodeResponse
        {
            IsCorrect = isCorrect,
            ActualOutput = execution.Stdout,
            ExpectedOutput = OutputNormalizer.Normalize(expectedOutput),
            ErrorOutput = string.IsNullOrWhiteSpace(execution.Stderr) ? null : execution.Stderr,
            HasError = execution.HasError,
            UserStatus = resultingStatus.ToString().ToLowerInvariant(),
            AwaitingReview = awaitingReview,
            SubmissionId = submission?.Id
        });
    }

    /// <summary>
    /// Envía la solución a calificación de forma explícita.
    ///
    /// El camino normal ya no es este: <c>/execute</c> envía en cuanto la salida es
    /// correcta. Se mantiene para reenviar sin volver a ejecutar (y para integraciones que
    /// no pasan por el workspace). El backend NO confía en el cliente, así que vuelve a
    /// ejecutar el código y rechaza el envío si la salida no coincide con la esperada.
    /// </summary>
    [HttpPost("{id:int}/submit")]
    [Authorize]
    [EnableRateLimiting(RateLimitingPolicies.CodeExecution)]
    [ProducesResponseType(typeof(SubmitCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<SubmitCodeResponse>> Submit(
        int id,
        [FromBody] SubmitCodeRequest request,
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
            .Include(e => e.Category)
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

        if (string.Equals(exercise.Category?.Name, TutorialCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "El tutorial no se califica",
                Detail = "Los ejercicios del tutorial se corrigen automáticamente al acertar la salida esperada.",
                Status = StatusCodes.Status400BadRequest
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

        // Un envío en cola bloquea los siguientes: sin esto el alumno podría acumular
        // dozens de submissions idénticas en la bandeja del admin.
        var alreadyPending = await _dbContext.Submissions
            .AnyAsync(
                s => s.UserId == userId.Value
                     && s.ExerciseId == id
                     && s.Status == SubmissionStatus.Pending,
                cancellationToken);

        if (alreadyPending)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Ya hay un envío en espera",
                Detail = "Tu solución anterior todavía está pendiente de calificación. Espera la respuesta del administrador antes de enviar otra.",
                Status = StatusCodes.Status409Conflict
            });
        }

        // Igual que en /execute: un ejercicio ya completado no se vuelve a enviar.
        var isAlreadyCompleted = await _dbContext.UserProgress
            .AnyAsync(
                p => p.UserId == userId.Value && p.ExerciseId == id && p.Status == ProgressStatus.Completed,
                cancellationToken);

        if (isAlreadyCompleted)
        {
            return Conflict(new ProblemDetails
            {
                Title = "El ejercicio ya está completado",
                Detail = "Este ejercicio ya fue aprobado, así que no hace falta enviarlo otra vez a calificación.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var stdin = JoinStdinLines(exercise.Inputs.OrderBy(i => i.OrderIndex).Select(i => i.Value));

        var execution = await _pistonExecutionService.ExecuteAsync(
            language.Slug, request.Code, stdin, cancellationToken);

        if (!execution.Succeeded)
        {
            _logger.LogWarning(
                "Fallo del ejecutor al enviar ejercicio {ExerciseId} usuario {UserId}: {Reason}",
                id, userId.Value, execution.FailureReason);

            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Title = "El ejecutor no está disponible",
                Detail = execution.FailureReason,
                Status = StatusCodes.Status502BadGateway
            });
        }

        // El cliente puede mentir sobre el resultado, así que la comprobación se repite
        // aquí contra la salida esperada antes de crear la submission.
        if (execution.HasError || !OutputNormalizer.AreEqual(exercise.ExpectedOutput, execution.Stdout))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "La solución todavía no es correcta",
                Detail = "La salida de tu código no coincide con la esperada. Corrige el ejercicio y vuelve a intentar antes de enviarlo a calificación.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var submission = await _gradingService.SubmitAsync(
            userId.Value, id, language.Id, request.Code,
            execution.Stdout, OutputNormalizer.Normalize(exercise.ExpectedOutput), cancellationToken);

        return Ok(new SubmitCodeResponse
        {
            SubmissionId = submission.Id,
            Status = SubmissionStatus.Pending.ToString()
        });
    }

    /// <summary>
    /// Normaliza la entrada estándar guardada en un paso y devuelve null cuando el
    /// paso no declara ninguna, para que el llamante aplique la del ejercicio.
    ///
    /// Se unifica el salto de línea porque el admin lo escribe en un textarea del
    /// navegador (que entrega CRLF en Windows) y Piston entrega las líneas tal cual:
    /// un "\r" pegado al valor haría que `Leer` leyera "Ana\r" y la comparación de
    /// la salida fallara siempre.
    /// </summary>
    private static string? ResolveStepStdin(string? stepStdin)
    {
        if (string.IsNullOrWhiteSpace(stepStdin))
        {
            return null;
        }

        var normalized = stepStdin.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n');

        return normalized.Length == 0 ? null : normalized + "\n";
    }

    /// <summary>
    /// Entrada estándar a partir de los "valores del leer": una línea por valor, en el
    /// orden en que el programa los consume.
    /// </summary>
    private static string? JoinStdinLines(IEnumerable<string> values)
    {
        var lines = values.ToList();

        return lines.Count == 0
            ? null
            : string.Join("\n", lines) + "\n";
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

        // Al mandar una solución nueva a calificación se borra la justificación anterior:
        // esa ya se resolvió con el envío que está en la cola y el alumno no debe ver el
        // aviso de "devuelto" mientras espera la respuesta a su reintento.
        var shouldClearFeedback = status == ProgressStatus.PendingReview;

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
            // Un intento posterior nunca degrada un ejercicio ya completado ni uno que
            // esta esperando calificacion: el alumno puede seguir probando, pero el
            // estado que ve (completado / en espera) tiene que permanecer.
            var keepCurrent = progress.Status is ProgressStatus.Completed or ProgressStatus.PendingReview;

            if (!keepCurrent)
            {
                progress.Status = status;
            }

            progress.LastSubmittedCode = code;

            // El lenguaje solo cambia cuando el ejercicio se supera: así se conserva
            // con qué lenguaje lo resolvió el alumno aunque después falle otro intento.
            if (!keepCurrent || status == ProgressStatus.Completed)
            {
                progress.LastSubmittedLanguageId = languageId;
            }

            if (shouldClearFeedback)
            {
                progress.Feedback = null;
            }

            progress.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
