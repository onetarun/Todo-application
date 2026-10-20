using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos.Subtasks;

public record CreateSubtaskRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;
}
