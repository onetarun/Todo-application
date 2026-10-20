using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos.Auth;

public record RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}
