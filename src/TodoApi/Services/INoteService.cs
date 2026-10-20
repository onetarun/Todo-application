using TodoApi.Dtos.Notes;

namespace TodoApi.Services;

// Every method is scoped to userId through the parent task's list; another user's data behaves as if it does not exist.
public interface INoteService
{
    /// <returns>null when the task does not exist for this user.</returns>
    Task<IReadOnlyList<NoteDto>?> GetForTaskAsync(int userId, int taskId, CancellationToken cancellationToken);

    /// <returns>null when the task does not exist for this user.</returns>
    Task<NoteDto?> CreateAsync(int userId, int taskId, CreateNoteRequest request, CancellationToken cancellationToken);

    /// <returns>null when the note does not exist for this user.</returns>
    Task<NoteDto?> UpdateAsync(int userId, int noteId, UpdateNoteRequest request, CancellationToken cancellationToken);

    /// <returns>false when the note does not exist for this user.</returns>
    Task<bool> DeleteAsync(int userId, int noteId, CancellationToken cancellationToken);
}
