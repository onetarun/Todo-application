using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos.Tags;

public record UpdateTagRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Required, RegularExpression(TagDto.ColorPattern, ErrorMessage = "Color must be a hex color such as #1E90FF.")]
    public string Color { get; init; } = string.Empty;
}
