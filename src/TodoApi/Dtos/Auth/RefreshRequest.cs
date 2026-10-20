using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos.Auth;

public record RefreshRequest
{
    [Required, StringLength(200)]
    public string RefreshToken { get; init; } = string.Empty;
}
