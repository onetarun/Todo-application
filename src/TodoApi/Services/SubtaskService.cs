using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos.Subtasks;
using TodoApi.Entities;

namespace TodoApi.Services;

public class SubtaskService(TodoDbContext db) : ISubtaskService
{
    private static readonly Expression<Func<Subtask, SubtaskDto>> ToDto =
        s => new SubtaskDto(s.Id, s.TaskId, s.Title, s.IsCompleted);

    private static readonly Func<Subtask, SubtaskDto> ToDtoCompiled = ToDto.Compile();

    public async Task<IReadOnlyList<SubtaskDto>?> GetForTaskAsync(
        int userId, int taskId, CancellationToken cancellationToken)
    {
        if (!await TaskExistsAsync(userId, taskId, cancellationToken))
        {
            return null;
        }

        return await db.Subtasks
            .AsNoTracking()
            .Where(s => s.TaskId == taskId)
            .OrderBy(s => s.Id)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<SubtaskDto?> CreateAsync(
        int userId, int taskId, CreateSubtaskRequest request, CancellationToken cancellationToken)
    {
        if (!await TaskExistsAsync(userId, taskId, cancellationToken))
        {
            return null;
        }

        var subtask = new Subtask { TaskId = taskId, Title = request.Title.Trim() };
        db.Subtasks.Add(subtask);
        await db.SaveChangesAsync(cancellationToken);
        return ToDtoCompiled(subtask);
    }

    public async Task<SubtaskDto?> UpdateAsync(
        int userId, int subtaskId, UpdateSubtaskRequest request, CancellationToken cancellationToken)
    {
        var subtask = await OwnedSubtasks(userId).SingleOrDefaultAsync(s => s.Id == subtaskId, cancellationToken);
        if (subtask is null)
        {
            return null;
        }

        // IsCompleted is [Required]; model validation guarantees it is present here.
        subtask.Title = request.Title.Trim();
        subtask.IsCompleted = request.IsCompleted!.Value;
        await db.SaveChangesAsync(cancellationToken);
        return ToDtoCompiled(subtask);
    }

    public async Task<bool> CompleteAsync(int userId, int subtaskId, CancellationToken cancellationToken)
    {
        var updated = await OwnedSubtasks(userId)
            .Where(s => s.Id == subtaskId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsCompleted, true), cancellationToken);
        return updated > 0;
    }

    public async Task<bool> DeleteAsync(int userId, int subtaskId, CancellationToken cancellationToken)
    {
        var deleted = await OwnedSubtasks(userId)
            .Where(s => s.Id == subtaskId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    private IQueryable<Subtask> OwnedSubtasks(int userId) =>
        db.Subtasks.Where(s => s.Task.TodoList.UserId == userId);

    private Task<bool> TaskExistsAsync(int userId, int taskId, CancellationToken cancellationToken) =>
        db.Tasks.AnyAsync(t => t.Id == taskId && t.TodoList.UserId == userId, cancellationToken);
}
