using Microsoft.EntityFrameworkCore;
using PSAcademyBack.Data;
using PSAcademyBack.Entities;

namespace PSAcademyBack.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _dbContext;

    public NotificationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(int userId, Enums.NotificationType type, string title, string message, string? data = null, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!exists) return;

        _dbContext.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            Data = data,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
