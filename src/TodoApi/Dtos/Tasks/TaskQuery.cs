using System.ComponentModel.DataAnnotations;
using TodoApi.Entities;

namespace TodoApi.Dtos.Tasks;

// Optional filters for GET /api/lists/{listId}/tasks; all supplied filters must match.
public record TaskQuery
{
    [EnumDataType(typeof(TodoTaskStatus))]
    public TodoTaskStatus? Status { get; init; }

    [Range(TodoTask.MinPriority, TodoTask.MaxPriority)]
    public int? Priority { get; init; }

    [Range(1, int.MaxValue)]
    public int? TagId { get; init; }
}
