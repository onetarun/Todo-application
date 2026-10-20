namespace TodoApi.Entities;

// Named TodoTask (table "Tasks") to avoid colliding with System.Threading.Tasks.Task.
public class TodoTask
{
    public const int MinPriority = 1;
    public const int MaxPriority = 3;

    public int Id { get; set; }
    public int TodoListId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public TodoTaskStatus Status { get; set; }
    public int Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public TodoList TodoList { get; set; } = null!;
    public List<Subtask> Subtasks { get; set; } = [];
    public List<Note> Notes { get; set; } = [];
    public List<Reminder> Reminders { get; set; } = [];
    public List<TaskTag> TaskTags { get; set; } = [];
}
