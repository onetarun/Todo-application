namespace TodoApi.Dtos.Lists;

public record TodoListDto(int Id, string Title, string? Description, DateTime CreatedAt);
