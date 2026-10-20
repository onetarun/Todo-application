using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos.Subtasks;

// Full replacement of the subtask's editable fields (PUT semantics).
public record UpdateSubtaskRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [Required]
    public bool? IsCompleted { get; init; }
}
