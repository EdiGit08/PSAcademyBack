using PSAcademyBack.Entities;

namespace PSAcademyBack.Services;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user);
}
