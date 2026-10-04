using PSAcademyBack.Dtos.Admin;
using PSAcademyBack.Entities;

namespace PSAcademyBack.Services;

public interface IExerciseGradingService
{
    Task<Submission> SubmitAsync(
        int userId, int exerciseId, int languageId, string code,
        string actualOutput, string expectedOutput, CancellationToken cancellationToken);

    Task<AdminSubmissionResponse?> GradeAsync(
        int submissionId, bool correct, string? feedback, int graderId,
        CancellationToken cancellationToken);
}
