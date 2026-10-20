using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos.Lists;
using TodoApi.Entities;

namespace TodoApi.Services;

public class TodoListService(TodoDbContext db, TimeProvider timeProvider) : ITodoListService
{
    private static readonly Expression<Func<TodoList, TodoListDto>> ToDto =
        l => new TodoListDto(l.Id, l.Title, l.Description, l.CreatedAt);

    private static readonly Func<TodoList, TodoListDto> ToDtoCompiled = ToDto.Compile();

    public async Task<IReadOnlyList<TodoListDto>> GetAllAsync(int userId, CancellationToken cancellationToken) =>
        await db.TodoLists
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.CreatedAt).ThenBy(l => l.Id)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

    public Task<TodoListDto?> GetAsync(int userId, int listId, CancellationToken cancellationToken) =>
        db.TodoLists
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.Id == listId)
            .Select(ToDto)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<TodoListDto> CreateAsync(int userId, CreateTodoListRequest request, CancellationToken cancellationToken)
    {
        var list = new TodoList
        {
            UserId = userId,
            Title = request.Title.Trim(),
            Description = request.Description,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        db.TodoLists.Add(list);
        await db.SaveChangesAsync(cancellationToken);

        return ToDtoCompiled(list);
    }

    public async Task<TodoListDto?> UpdateAsync(
        int userId, int listId, UpdateTodoListRequest request, CancellationToken cancellationToken)
    {
        var list = await FindAsync(userId, listId, cancellationToken);
        if (list is null)
        {
            return null;
        }

        list.Title = request.Title.Trim();
        list.Description = request.Description;
        await db.SaveChangesAsync(cancellationToken);

        return ToDtoCompiled(list);
    }

    public async Task<bool> DeleteAsync(int userId, int listId, CancellationToken cancellationToken)
    {
        // Tasks and their children are removed by the database cascade.
        var deleted = await db.TodoLists
            .Where(l => l.UserId == userId && l.Id == listId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    private Task<TodoList?> FindAsync(int userId, int listId, CancellationToken cancellationToken) =>
        db.TodoLists.SingleOrDefaultAsync(l => l.UserId == userId && l.Id == listId, cancellationToken);
}
