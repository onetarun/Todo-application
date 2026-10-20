namespace TodoApi.Dtos.Reminders;

public record ReminderDto(int Id, int TaskId, DateTime RemindAt);
