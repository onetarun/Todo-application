using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos.Notes;
using TodoApi.Entities;

namespace TodoApi.Services;

public class NoteService(TodoDbContext db, TimeProvider timeProvider) : INoteService
{
    private static readonly Expression<Func<Note, NoteDto>> ToDto =
        n => new NoteDto(n.Id, n.TaskId, n.Content, n.CreatedAt);

    private static readonly Func<Note, NoteDto> ToDtoCompiled = ToDto.Compile();

    public async Task<IReadOnlyList<NoteDto>?> GetForTaskAsync(
        int userId, int taskId, CancellationToken cancellationToken)
    {
        if (!await TaskExistsAsync(userId, taskId, cancellationToken))
        {
            return null;
        }

        return await db.Notes
            .AsNoTracking()
            .Where(n => n.TaskId == taskId)
            .OrderBy(n => n.CreatedAt).ThenBy(n => n.Id)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<NoteDto?> CreateAsync(
        int userId, int taskId, CreateNoteRequest request, CancellationToken cancellationToken)
    {
        if (!await TaskExistsAsync(userId, taskId, cancellationToken))
        {
            return null;
        }

        var note = new Note
        {
            TaskId = taskId,
            Content = request.Content.Trim(),
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        db.Notes.Add(note);
        await db.SaveChangesAsync(cancellationToken);
        return ToDtoCompiled(note);
    }

    public async Task<NoteDto?> UpdateAsync(
        int userId, int noteId, UpdateNoteRequest request, CancellationToken cancellationToken)
    {
        var note = await OwnedNotes(userId).SingleOrDefaultAsync(n => n.Id == noteId, cancellationToken);
        if (note is null)
        {
            return null;
        }

        note.Content = request.Content.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return ToDtoCompiled(note);
    }

    public async Task<bool> DeleteAsync(int userId, int noteId, CancellationToken cancellationToken)
    {
        var deleted = await OwnedNotes(userId)
            .Where(n => n.Id == noteId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    private IQueryable<Note> OwnedNotes(int userId) =>
        db.Notes.Where(n => n.Task.TodoList.UserId == userId);

    private Task<bool> TaskExistsAsync(int userId, int taskId, CancellationToken cancellationToken) =>
        db.Tasks.AnyAsync(t => t.Id == taskId && t.TodoList.UserId == userId, cancellationToken);
}
