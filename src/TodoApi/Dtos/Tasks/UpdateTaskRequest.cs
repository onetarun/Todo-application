using System.ComponentModel.DataAnnotations;
using TodoApi.Entities;

namespace TodoApi.Dtos.Tasks;

// Full replacement of the task's editable fields (PUT semantics).
public record UpdateTaskRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    [Required, EnumDataType(typeof(TodoTaskStatus))]
    public TodoTaskStatus? Status { get; init; }

    [Required, Range(TodoTask.MinPriority, TodoTask.MaxPriority)]
    public int? Priority { get; init; }

    public DateTime? DueDate { get; init; }
}
