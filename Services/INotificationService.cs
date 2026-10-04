using PSAcademyBack.Entities;

namespace PSAcademyBack.Services;

public interface INotificationService
{
    Task CreateAsync(int userId, Enums.NotificationType type, string title, string message, string? data = null, CancellationToken cancellationToken = default);
}
