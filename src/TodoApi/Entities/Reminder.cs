namespace TodoApi.Entities;

public class Reminder
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public DateTime RemindAt { get; set; }

    public TodoTask Task { get; set; } = null!;
}
