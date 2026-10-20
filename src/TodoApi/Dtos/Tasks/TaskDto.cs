using TodoApi.Dtos.Tags;
using TodoApi.Entities;

namespace TodoApi.Dtos.Tasks;

public record TaskDto(
    int Id,
    int TodoListId,
    string Title,
    string? Description,
    TodoTaskStatus Status,
    int Priority,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<TagDto> Tags);
