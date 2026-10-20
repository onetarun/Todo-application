using TodoApi.Dtos.Lists;

namespace TodoApi.Services;

// Every method is scoped to userId; another user's list behaves as if it does not exist.
public interface ITodoListService
{
    Task<IReadOnlyList<TodoListDto>> GetAllAsync(int userId, CancellationToken cancellationToken);

    Task<TodoListDto?> GetAsync(int userId, int listId, CancellationToken cancellationToken);

    Task<TodoListDto> CreateAsync(int userId, CreateTodoListRequest request, CancellationToken cancellationToken);

    /// <returns>null when the list does not exist for this user.</returns>
    Task<TodoListDto?> UpdateAsync(int userId, int listId, UpdateTodoListRequest request, CancellationToken cancellationToken);

    /// <returns>false when the list does not exist for this user.</returns>
    Task<bool> DeleteAsync(int userId, int listId, CancellationToken cancellationToken);
}
