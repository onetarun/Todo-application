using TodoApi.Dtos.Auth;

namespace TodoApi.Services;

public interface IAuthService
{
    /// <returns>null when the email is already registered.</returns>
    Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    /// <returns>null when the email is unknown or the password is wrong.</returns>
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <returns>null when the refresh token is unknown, expired, or revoked.</returns>
    Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);

    Task<UserDto?> GetUserAsync(int userId, CancellationToken cancellationToken);
}
