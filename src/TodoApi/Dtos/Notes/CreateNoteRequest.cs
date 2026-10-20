using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos.Notes;

public record CreateNoteRequest
{
    [Required, StringLength(4000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;
}
