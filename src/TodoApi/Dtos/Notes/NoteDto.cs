namespace TodoApi.Dtos.Notes;

public record NoteDto(int Id, int TaskId, string Content, DateTime CreatedAt);
