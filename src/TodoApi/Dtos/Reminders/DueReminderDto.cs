namespace TodoApi.Dtos.Reminders;

// A reminder that has come due, with its task's title so the client can notify without loading the task.
public record DueReminderDto(int Id, int TaskId, string TaskTitle, DateTime RemindAt);
