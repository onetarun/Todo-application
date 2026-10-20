using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TodoApi.Entities;

namespace TodoApi.Auth;

public class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private const int RefreshTokenBytes = 64;

    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public static SymmetricSecurityKey CreateSigningKey(string signingKey) =>
        new(Encoding.UTF8.GetBytes(signingKey));

    public (string Token, DateTime ExpiresAt) CreateAccessToken(User user)
    {
        var expiresAt = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(_options.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAt,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.Name)
            ]),
            SigningCredentials = new SigningCredentials(
                CreateSigningKey(_options.SigningKey), SecurityAlgorithms.HmacSha256)
        };

        return (_tokenHandler.CreateToken(descriptor), expiresAt);
    }

    public (string Token, DateTime ExpiresAt) CreateRefreshToken()
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
        var expiresAt = timeProvider.GetUtcNow().UtcDateTime.AddDays(_options.RefreshTokenDays);
        return (token, expiresAt);
    }

    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
