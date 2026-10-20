using TodoApi.Dtos.Subtasks;

namespace TodoApi.Services;

// Every method is scoped to userId through the parent task's list; another user's data behaves as if it does not exist.
public interface ISubtaskService
{
    /// <returns>null when the task does not exist for this user.</returns>
    Task<IReadOnlyList<SubtaskDto>?> GetForTaskAsync(int userId, int taskId, CancellationToken cancellationToken);

    /// <returns>null when the task does not exist for this user.</returns>
    Task<SubtaskDto?> CreateAsync(int userId, int taskId, CreateSubtaskRequest request, CancellationToken cancellationToken);

    /// <returns>null when the subtask does not exist for this user.</returns>
    Task<SubtaskDto?> UpdateAsync(int userId, int subtaskId, UpdateSubtaskRequest request, CancellationToken cancellationToken);

    /// <returns>false when the subtask does not exist for this user.</returns>
    Task<bool> CompleteAsync(int userId, int subtaskId, CancellationToken cancellationToken);

    /// <returns>false when the subtask does not exist for this user.</returns>
    Task<bool> DeleteAsync(int userId, int subtaskId, CancellationToken cancellationToken);
}
