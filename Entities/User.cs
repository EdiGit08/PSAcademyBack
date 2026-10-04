using PSAcademyBack.Enums;

namespace PSAcademyBack.Entities;

public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserProgress> Progress { get; set; } = new List<UserProgress>();

    public ICollection<UserCodeDraft> Drafts { get; set; } = new List<UserCodeDraft>();

    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
