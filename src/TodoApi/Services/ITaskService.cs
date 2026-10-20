using TodoApi.Dtos.Tasks;
using TodoApi.Entities;

namespace TodoApi.Services;

// Every method is scoped to userId through the task's list; another user's task behaves as if it does not exist.
public interface ITaskService
{
    /// <returns>null when the list does not exist for this user.</returns>
    Task<IReadOnlyList<TaskDto>?> GetForListAsync(int userId, int listId, TaskQuery query, CancellationToken cancellationToken);

    /// <returns>null when the list does not exist for this user.</returns>
    Task<TaskDto?> CreateAsync(int userId, int listId, CreateTaskRequest request, CancellationToken cancellationToken);

    Task<TaskDetailDto?> GetDetailAsync(int userId, int taskId, CancellationToken cancellationToken);

    /// <returns>null when the task does not exist for this user.</returns>
    Task<TaskDto?> UpdateAsync(int userId, int taskId, UpdateTaskRequest request, CancellationToken cancellationToken);

    /// <returns>false when the task does not exist for this user.</returns>
    Task<bool> UpdateStatusAsync(int userId, int taskId, TodoTaskStatus status, CancellationToken cancellationToken);

    /// <returns>false when the task does not exist for this user.</returns>
    Task<bool> DeleteAsync(int userId, int taskId, CancellationToken cancellationToken);
}
