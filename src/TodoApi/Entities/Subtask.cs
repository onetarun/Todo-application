namespace TodoApi.Entities;

public class Subtask
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public required string Title { get; set; }
    public bool IsCompleted { get; set; }

    public TodoTask Task { get; set; } = null!;
}
