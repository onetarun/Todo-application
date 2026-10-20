using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoApi.Auth;
using TodoApi.Data;
using TodoApi.Dtos.Auth;
using TodoApi.Entities;

namespace TodoApi.Services;

public class AuthService(
    TodoDbContext db,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider) : IAuthService
{
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return null;
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = string.Empty,
            CreatedAt = UtcNow()
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);

        try
        {
            return await IssueTokensAsync(user, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // A concurrent registration with the same email won the race past the AnyAsync check above.
            return null;
        }
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var storedToken = await FindRefreshTokenAsync(refreshToken, cancellationToken);
        if (storedToken is null || storedToken.ExpiresAt <= UtcNow())
        {
            return null;
        }

        if (storedToken.RevokedAt is not null)
        {
            // A rotated-out token being replayed suggests it was stolen: end every session for this user.
            await RevokeAllAsync(storedToken.UserId, cancellationToken);
            return null;
        }

        storedToken.RevokedAt = UtcNow();
        return await IssueTokensAsync(storedToken.User, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var storedToken = await FindRefreshTokenAsync(refreshToken, cancellationToken);
        if (storedToken is null || storedToken.RevokedAt is not null)
        {
            return;
        }

        storedToken.RevokedAt = UtcNow();
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<UserDto?> GetUserAsync(int userId, CancellationToken cancellationToken) =>
        db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserDto(u.Id, u.Name, u.Email))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var (refreshToken, refreshExpiresAt) = tokenService.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            User = user,
            TokenHash = tokenService.HashRefreshToken(refreshToken),
            ExpiresAt = refreshExpiresAt,
            CreatedAt = UtcNow()
        });

        // Saved before creating the access token so a newly registered user has its database-generated id.
        await db.SaveChangesAsync(cancellationToken);

        var (accessToken, accessExpiresAt) = tokenService.CreateAccessToken(user);
        return new AuthResponse(
            accessToken,
            accessExpiresAt,
            refreshToken,
            refreshExpiresAt,
            new UserDto(user.Id, user.Name, user.Email));
    }

    private Task<RefreshToken?> FindRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashRefreshToken(refreshToken);
        return db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    // Tracked update rather than ExecuteUpdateAsync, so tokens already loaded in this context don't stay stale.
    private async Task RevokeAllAsync(int userId, CancellationToken cancellationToken)
    {
        var now = UtcNow();
        var activeTokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;
}
