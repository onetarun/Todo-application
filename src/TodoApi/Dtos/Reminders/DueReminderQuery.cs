using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos.Reminders;

public record DueReminderQuery
{
    /// <summary>Upper bound (UTC). A value without an offset is taken as UTC.</summary>
    [Required]
    public DateTime? Before { get; init; }
}
