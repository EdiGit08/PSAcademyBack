using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PSAcademyBack.Data;
using PSAcademyBack.Dtos.Admin;
using PSAcademyBack.Entities;
using PSAcademyBack.Enums;

namespace PSAcademyBack.Services;

/// <summary>
/// Calificación de ejercicios con revisión del administrador.
///
/// El flujo es: el alumno ejecuta y, si la salida coincide, envía la solución. Esta
/// queda <see cref="SubmissionStatus.Pending"/> y el progreso pasa a
/// <see cref="ProgressStatus.PendingReview"/>. El admin recibe una notificación y decide:
/// si aprueba, el progreso pasa a <see cref="ProgressStatus.Completed"/>; si rechaza, pasa
/// a <see cref="ProgressStatus.Incorrect"/> y guarda la justificación que el alumno verá.
///
/// La única excepción son los ejercicios de la categoría "Tutorial": se corrigen solos
/// al acertar la salida, sin intervención del admin.
/// </summary>
public class ExerciseGradingService : IExerciseGradingService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public ExerciseGradingService(ApplicationDbContext dbContext, INotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public async Task<Submission> SubmitAsync(
        int userId,
        int exerciseId,
        int languageId,
        string code,
        string actualOutput,
        string expectedOutput,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var submission = new Submission
        {
            UserId = userId,
            ExerciseId = exerciseId,
            LanguageId = languageId,
            Code = code,
            ActualOutput = actualOutput,
            ExpectedOutput = expectedOutput,
            Status = SubmissionStatus.Pending,
            SubmittedAt = now
        };

        _dbContext.Submissions.Add(submission);

        var progress = await _dbContext.UserProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ExerciseId == exerciseId, cancellationToken);

        if (progress is null)
        {
            _dbContext.UserProgress.Add(new UserProgress
            {
                UserId = userId,
                ExerciseId = exerciseId,
                Status = ProgressStatus.PendingReview,
                LastSubmittedCode = code,
                LastSubmittedLanguageId = languageId,
                UpdatedAt = now
            });
        }
        else
        {
            progress.Status = ProgressStatus.PendingReview;
            progress.LastSubmittedCode = code;
            progress.LastSubmittedLanguageId = languageId;
            // La justificación anterior se borra al reenviar: el alumno ya la leyó y está
            // intentando corregir. Dejarla mezclaría la nota vieja con la nueva.
            progress.Feedback = null;
            progress.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var adminIds = await _dbContext.Users
            .Where(u => u.Role == UserRole.Admin)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var payload = JsonSerializer.Serialize(new { submissionId = submission.Id, exerciseId });

        foreach (var adminId in adminIds)
        {
            await _notificationService.CreateAsync(
                adminId,
                NotificationType.SubmissionPending,
                "Nueva calificación pendiente",
                "Un alumno envió una solución que espera calificación.",
                payload,
                cancellationToken);
        }

        return submission;
    }

    public async Task<AdminSubmissionResponse?> GradeAsync(
        int submissionId,
        bool correct,
        string? feedback,
        int graderId,
        CancellationToken cancellationToken)
    {
        var submission = await _dbContext.Submissions
            .Include(s => s.User)
            .Include(s => s.Exercise)
            .Include(s => s.Language)
            .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);

        if (submission is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;

        // La reserva del envio se hace con un UPDATE condicional en una sola sentencia:
        // si dos admins abren la misma fila y califican a la vez, solo uno gana. Con una
        // lectura y un guardado en dos pasos, el segundo sobrescribiria al primero y el
        // alumno recibiria dos notificaciones contradictorias.
        var claimed = await _dbContext.Submissions
            .Where(s => s.Id == submissionId && s.Status == SubmissionStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(s => s.Status, correct ? SubmissionStatus.Correct : SubmissionStatus.Incorrect)
                    .SetProperty(s => s.Feedback, correct ? null : feedback)
                    .SetProperty(s => s.GradedAt, now)
                    .SetProperty(s => s.GradedById, graderId),
                cancellationToken);

        if (claimed == 0)
        {
            // Ya estaba calificado: se avisa con 409 para que el admin no piense que su
            // decision se guardo cuando en realidad gano otra.
            throw new SubmissionAlreadyGradedException(submissionId);
        }

        var progress = await _dbContext.UserProgress
            .FirstOrDefaultAsync(p => p.UserId == submission.UserId && p.ExerciseId == submission.ExerciseId, cancellationToken);

        if (progress is not null)
        {
            progress.Status = correct ? ProgressStatus.Completed : ProgressStatus.Incorrect;
            progress.Feedback = correct ? null : feedback;
            progress.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // El UPDATE condicional se ejecuto fuera del ChangeTracker: se recarga para que
        // la respuesta refleje lo que hay ahora en la base de datos.
        await _dbContext.Entry(submission).ReloadAsync(cancellationToken);

        var graderEmail = await _dbContext.Users
            .Where(u => u.Id == graderId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        await NotifyStudentAsync(submission, correct, cancellationToken);

        return new AdminSubmissionResponse
        {
            Id = submission.Id,
            UserId = submission.UserId,
            UserEmail = submission.User.Email,
            ExerciseId = submission.ExerciseId,
            ExerciseTitle = submission.Exercise.Title,
            LanguageName = submission.Language.Name,
            Code = submission.Code,
            ActualOutput = submission.ActualOutput,
            ExpectedOutput = submission.ExpectedOutput,
            Status = submission.Status.ToString(),
            Feedback = submission.Feedback,
            SubmittedAt = submission.SubmittedAt,
            GradedAt = submission.GradedAt,
            GradedByEmail = graderEmail
        };
    }

    /// <summary>
    /// Avisa al alumno del resultado. El id del ejercicio viaja en <c>Data</c> para que
    /// el frontend pueda enlazar directamente al workspace sin tener que interpretarlo.
    /// </summary>
    private Task NotifyStudentAsync(Submission submission, bool correct, CancellationToken cancellationToken)
    {
        var title = correct ? "Ejercicio aprobado" : "Ejercicio devuelto";
        var message = correct
            ? $"Tu solución de \"{submission.Exercise.Title}\" fue aprobada."
            : $"Tu solución de \"{submission.Exercise.Title}\" fue devuelta. Lee la justificación y vuelve a intentarlo.";

        var data = JsonSerializer.Serialize(new
        {
            submissionId = submission.Id,
            exerciseId = submission.ExerciseId
        });

        return _notificationService.CreateAsync(
            submission.UserId,
            NotificationType.SubmissionGraded,
            title,
            message,
            data,
            cancellationToken);
    }
}
