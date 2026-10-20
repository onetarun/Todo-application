namespace TodoApi.Entities;

public class TaskTag
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int TagId { get; set; }

    public TodoTask Task { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
