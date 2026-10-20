namespace TodoApi.Dtos.Subtasks;

public record SubtaskDto(int Id, int TaskId, string Title, bool IsCompleted);
