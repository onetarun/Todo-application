using TodoApi.Dtos.Reminders;

namespace TodoApi.Services;

// Every method is scoped to userId through the parent task's list; another user's data behaves as if it does not exist.
// Reminders are only stored; delivery is still an open decision (docs/plan.md, decision 7).
public interface IReminderService
{
    /// <returns>null when the task does not exist for this user.</returns>
    Task<IReadOnlyList<ReminderDto>?> GetForTaskAsync(int userId, int taskId, CancellationToken cancellationToken);

    /// <returns>null when the task does not exist for this user.</returns>
    Task<ReminderDto?> CreateAsync(
        int userId, int taskId, CreateReminderRequest request, CancellationToken cancellationToken);

    /// <returns>false when the reminder does not exist for this user.</returns>
    Task<bool> DeleteAsync(int userId, int reminderId, CancellationToken cancellationToken);
}
