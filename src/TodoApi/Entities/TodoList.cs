namespace TodoApi.Entities;

public class TodoList
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public List<TodoTask> Tasks { get; set; } = [];
}
