namespace TodoApi.Entities;

public class Note
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }

    public TodoTask Task { get; set; } = null!;
}
