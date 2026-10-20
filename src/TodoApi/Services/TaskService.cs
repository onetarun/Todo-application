using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos.Notes;
using TodoApi.Dtos.Reminders;
using TodoApi.Dtos.Subtasks;
using TodoApi.Dtos.Tags;
using TodoApi.Dtos.Tasks;
using TodoApi.Entities;

namespace TodoApi.Services;

public class TaskService(TodoDbContext db, TimeProvider timeProvider) : ITaskService
{
    private static readonly Expression<Func<TodoTask, TaskDto>> ToDto = t => new TaskDto(
        t.Id,
        t.TodoListId,
        t.Title,
        t.Description,
        t.Status,
        t.Priority,
        t.DueDate,
        t.CreatedAt,
        t.UpdatedAt,
        t.TaskTags
            .OrderBy(tt => tt.Tag.Name)
            .Select(tt => new TagDto(tt.Tag.Id, tt.Tag.Name, tt.Tag.Color))
            .ToList());

    private static readonly Expression<Func<TodoTask, TaskDetailDto>> ToDetailDto = t => new TaskDetailDto(
        t.Id,
        t.TodoListId,
        t.Title,
        t.Description,
        t.Status,
        t.Priority,
        t.DueDate,
        t.CreatedAt,
        t.UpdatedAt,
        t.TaskTags
            .OrderBy(tt => tt.Tag.Name)
            .Select(tt => new TagDto(tt.Tag.Id, tt.Tag.Name, tt.Tag.Color))
            .ToList(),
        t.Subtasks
            .OrderBy(s => s.Id)
            .Select(s => new SubtaskDto(s.Id, s.TaskId, s.Title, s.IsCompleted))
            .ToList(),
        t.Notes
            .OrderBy(n => n.CreatedAt).ThenBy(n => n.Id)
            .Select(n => new NoteDto(n.Id, n.TaskId, n.Content, n.CreatedAt))
            .ToList(),
        t.Reminders
            .OrderBy(r => r.RemindAt)
            .Select(r => new ReminderDto(r.Id, r.TaskId, r.RemindAt))
            .ToList());

    public async Task<IReadOnlyList<TaskDto>?> GetForListAsync(
        int userId, int listId, TaskQuery query, CancellationToken cancellationToken)
    {
        if (!await ListExistsAsync(userId, listId, cancellationToken))
        {
            return null;
        }

        var tasks = db.Tasks.AsNoTracking().Where(t => t.TodoListId == listId);

        if (query.Status is { } status)
        {
            tasks = tasks.Where(t => t.Status == status);
        }

        if (query.Priority is { } priority)
        {
            tasks = tasks.Where(t => t.Priority == priority);
        }

        if (query.TagId is { } tagId)
        {
            tasks = tasks.Where(t => t.TaskTags.Any(tt => tt.TagId == tagId));
        }

        return await tasks
            .OrderBy(t => t.CreatedAt).ThenBy(t => t.Id)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskDto?> CreateAsync(
        int userId, int listId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!await ListExistsAsync(userId, listId, cancellationToken))
        {
            return null;
        }

        var now = UtcNow();
        var task = new TodoTask
        {
            TodoListId = listId,
            Title = request.Title.Trim(),
            Description = request.Description,
            Status = request.Status,
            Priority = request.Priority,
            DueDate = request.DueDate,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        return await GetDtoAsync(task.Id, cancellationToken);
    }

    public Task<TaskDetailDto?> GetDetailAsync(int userId, int taskId, CancellationToken cancellationToken) =>
        OwnedTasks(userId)
            .AsNoTracking()
            .Where(t => t.Id == taskId)
            .AsSplitQuery()
            .Select(ToDetailDto)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<TaskDto?> UpdateAsync(
        int userId, int taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await OwnedTasks(userId).SingleOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        // Status and Priority are [Required]; model validation guarantees they are present here.
        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.Status = request.Status!.Value;
        task.Priority = request.Priority!.Value;
        task.DueDate = request.DueDate;
        task.UpdatedAt = UtcNow();
        await db.SaveChangesAsync(cancellationToken);

        return await GetDtoAsync(task.Id, cancellationToken);
    }

    public async Task<bool> UpdateStatusAsync(
        int userId, int taskId, TodoTaskStatus status, CancellationToken cancellationToken)
    {
        var now = UtcNow();
        var updated = await OwnedTasks(userId)
            .Where(t => t.Id == taskId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, status)
                .SetProperty(t => t.UpdatedAt, now),
                cancellationToken);
        return updated > 0;
    }

    public async Task<bool> DeleteAsync(int userId, int taskId, CancellationToken cancellationToken)
    {
        // Subtasks, notes, reminders and task-tags are removed by the database cascade.
        var deleted = await OwnedTasks(userId)
            .Where(t => t.Id == taskId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    private IQueryable<TodoTask> OwnedTasks(int userId) =>
        db.Tasks.Where(t => t.TodoList.UserId == userId);

    private Task<bool> ListExistsAsync(int userId, int listId, CancellationToken cancellationToken) =>
        db.TodoLists.AnyAsync(l => l.UserId == userId && l.Id == listId, cancellationToken);

    private async Task<TaskDto> GetDtoAsync(int taskId, CancellationToken cancellationToken) =>
        await db.Tasks
            .AsNoTracking()
            .Where(t => t.Id == taskId)
            .Select(ToDto)
            .SingleAsync(cancellationToken);

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;
}
