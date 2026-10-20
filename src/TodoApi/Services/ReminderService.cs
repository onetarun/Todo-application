using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos.Reminders;
using TodoApi.Entities;

namespace TodoApi.Services;

public class ReminderService(TodoDbContext db) : IReminderService
{
    private static readonly Expression<Func<Reminder, ReminderDto>> ToDto =
        r => new ReminderDto(r.Id, r.TaskId, r.RemindAt);

    private static readonly Func<Reminder, ReminderDto> ToDtoCompiled = ToDto.Compile();

    public async Task<IReadOnlyList<ReminderDto>?> GetForTaskAsync(
        int userId, int taskId, CancellationToken cancellationToken)
    {
        if (!await TaskExistsAsync(userId, taskId, cancellationToken))
        {
            return null;
        }

        return await db.Reminders
            .AsNoTracking()
            .Where(r => r.TaskId == taskId)
            .OrderBy(r => r.RemindAt).ThenBy(r => r.Id)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReminderDto?> CreateAsync(
        int userId, int taskId, CreateReminderRequest request, CancellationToken cancellationToken)
    {
        if (!await TaskExistsAsync(userId, taskId, cancellationToken))
        {
            return null;
        }

        // RemindAt is [Required]; model validation guarantees it is present here.
        var reminder = new Reminder { TaskId = taskId, RemindAt = request.RemindAt!.Value };
        db.Reminders.Add(reminder);
        await db.SaveChangesAsync(cancellationToken);
        return ToDtoCompiled(reminder);
    }

    public async Task<bool> DeleteAsync(int userId, int reminderId, CancellationToken cancellationToken)
    {
        var deleted = await db.Reminders
            .Where(r => r.Id == reminderId && r.Task.TodoList.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    private Task<bool> TaskExistsAsync(int userId, int taskId, CancellationToken cancellationToken) =>
        db.Tasks.AnyAsync(t => t.Id == taskId && t.TodoList.UserId == userId, cancellationToken);
}
