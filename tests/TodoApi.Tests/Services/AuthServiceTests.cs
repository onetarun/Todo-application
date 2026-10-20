using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using TodoApi.Auth;
using TodoApi.Data;
using TodoApi.Dtos.Auth;
using TodoApi.Entities;
using TodoApi.Services;
using TodoApi.Tests.Infrastructure;

namespace TodoApi.Tests.Services;

[Collection(DatabaseCollection.Name)]
public class AuthServiceTests(ApiFixture fixture)
{
    private const string Password = "Passw0rd!";

    private readonly FakeTimeProvider _clock = new(DateTimeOffset.UtcNow);

    private AuthService CreateService(TodoDbContext db)
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = ApiFixture.TestSigningKey,
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        });
        return new AuthService(db, new PasswordHasher<User>(), new TokenService(jwtOptions, _clock), _clock);
    }

    private async Task<AuthResponse> RegisterAsync(TodoDbContext db, string? email = null) =>
        (await CreateService(db).RegisterAsync(
            new RegisterRequest { Name = "Auth Test", Email = email ?? ApiClient.UniqueEmail(), Password = Password },
            default))!;

    [Fact]
    public async Task RegisterAsync_NormalizesEmailAndStoresOnlyHashes()
    {
        await using var db = fixture.CreateDbContext();
        var email = ApiClient.UniqueEmail();

        var response = await RegisterAsync(db, $"  {email.ToUpperInvariant()} ");

        Assert.Equal(email, response.User.Email);
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == response.User.Id);
        Assert.NotEqual(Password, user.PasswordHash);
        var storedToken = await db.RefreshTokens.AsNoTracking().SingleAsync(t => t.UserId == user.Id);
        Assert.NotEqual(response.RefreshToken, storedToken.TokenHash);
        Assert.Equal(64, storedToken.TokenHash.Length);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmailInAnyCase_ReturnsNull()
    {
        await using var db = fixture.CreateDbContext();
        var email = ApiClient.UniqueEmail();
        await RegisterAsync(db, email);

        await using var db2 = fixture.CreateDbContext();
        var duplicate = await CreateService(db2).RegisterAsync(
            new RegisterRequest { Name = "Again", Email = email.ToUpperInvariant(), Password = Password }, default);

        Assert.Null(duplicate);
    }

    [Fact]
    public async Task LoginAsync_ChecksPassword()
    {
        await using var db = fixture.CreateDbContext();
        var registered = await RegisterAsync(db);
        var service = CreateService(db);

        var wrong = await service.LoginAsync(
            new LoginRequest { Email = registered.User.Email, Password = "wrong-password" }, default);
        var right = await service.LoginAsync(
            new LoginRequest { Email = registered.User.Email, Password = Password }, default);
        var unknown = await service.LoginAsync(
            new LoginRequest { Email = ApiClient.UniqueEmail(), Password = Password }, default);

        Assert.Null(wrong);
        Assert.Null(unknown);
        Assert.NotNull(right);
        Assert.Equal(registered.User.Id, right.User.Id);
    }

    [Fact]
    public async Task RefreshAsync_RotatesToken()
    {
        await using var db = fixture.CreateDbContext();
        var registered = await RegisterAsync(db);

        var refreshed = await CreateService(db).RefreshAsync(registered.RefreshToken, default);

        Assert.NotNull(refreshed);
        Assert.NotEqual(registered.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(registered.User.Id, refreshed.User.Id);
    }

    [Fact]
    public async Task RefreshAsync_ReusedToken_RevokesAllUserTokens()
    {
        await using var db = fixture.CreateDbContext();
        var registered = await RegisterAsync(db);
        var service = CreateService(db);
        var rotated = await service.RefreshAsync(registered.RefreshToken, default);

        var replay = await service.RefreshAsync(registered.RefreshToken, default);
        var afterReplay = await service.RefreshAsync(rotated!.RefreshToken, default);

        Assert.Null(replay);
        Assert.Null(afterReplay);
        Assert.False(await db.RefreshTokens.AnyAsync(t => t.UserId == registered.User.Id && t.RevokedAt == null));
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ReturnsNull()
    {
        await using var db = fixture.CreateDbContext();
        var registered = await RegisterAsync(db);

        _clock.Advance(TimeSpan.FromDays(7) + TimeSpan.FromSeconds(1));

        Assert.Null(await CreateService(db).RefreshAsync(registered.RefreshToken, default));
    }

    [Fact]
    public async Task LogoutAsync_RevokesToken()
    {
        await using var db = fixture.CreateDbContext();
        var registered = await RegisterAsync(db);
        var service = CreateService(db);

        await service.LogoutAsync(registered.RefreshToken, default);

        Assert.Null(await service.RefreshAsync(registered.RefreshToken, default));
    }
}
