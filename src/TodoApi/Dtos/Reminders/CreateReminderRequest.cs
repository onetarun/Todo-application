using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos.Reminders;

public record CreateReminderRequest
{
    [Required]
    public DateTime? RemindAt { get; init; }
}
