using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos.Auth;

public record LoginRequest
{
    [Required, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string Password { get; init; } = string.Empty;
}
