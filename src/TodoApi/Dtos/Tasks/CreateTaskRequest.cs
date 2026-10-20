using System.ComponentModel.DataAnnotations;
using TodoApi.Entities;

namespace TodoApi.Dtos.Tasks;

public record CreateTaskRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    [EnumDataType(typeof(TodoTaskStatus))]
    public TodoTaskStatus Status { get; init; } = TodoTaskStatus.Todo;

    [Range(TodoTask.MinPriority, TodoTask.MaxPriority)]
    public int Priority { get; init; } = 2;

    public DateTime? DueDate { get; init; }
}
