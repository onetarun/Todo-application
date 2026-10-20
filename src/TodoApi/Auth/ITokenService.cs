using TodoApi.Entities;

namespace TodoApi.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);
    (string Token, DateTime ExpiresAt) CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
