using System.ComponentModel.DataAnnotations;

namespace TodoApi.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinSigningKeyBytes = 32;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    // Supplied via user secrets (development) or the Jwt__SigningKey environment variable — never appsettings.
    [Required]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 7;
}
