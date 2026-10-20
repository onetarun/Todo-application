using TodoApi.Dtos.Notes;
using TodoApi.Dtos.Reminders;
using TodoApi.Dtos.Subtasks;
using TodoApi.Dtos.Tags;
using TodoApi.Entities;

namespace TodoApi.Dtos.Tasks;

public record TaskDetailDto(
    int Id,
    int TodoListId,
    string Title,
    string? Description,
    TodoTaskStatus Status,
    int Priority,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<TagDto> Tags,
    IReadOnlyList<SubtaskDto> Subtasks,
    IReadOnlyList<NoteDto> Notes,
    IReadOnlyList<ReminderDto> Reminders);
