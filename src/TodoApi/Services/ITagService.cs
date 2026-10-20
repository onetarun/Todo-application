using TodoApi.Dtos.Tags;

namespace TodoApi.Services;

// Tags are global (docs/plan.md, decision 6): every signed-in user sees and manages the same set.
// Assigning a tag is scoped to userId through the task's list.
public interface ITagService
{
    Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<TagDto> CreateAsync(CreateTagRequest request, CancellationToken cancellationToken);

    /// <returns>null when the tag does not exist.</returns>
    Task<TagDto?> UpdateAsync(int tagId, UpdateTagRequest request, CancellationToken cancellationToken);

    /// <returns>false when the tag does not exist.</returns>
    Task<bool> DeleteAsync(int tagId, CancellationToken cancellationToken);

    /// <summary>Idempotent: assigning an already-assigned tag succeeds.</summary>
    /// <returns>false when the task does not exist for this user or the tag does not exist.</returns>
    Task<bool> AssignAsync(int userId, int taskId, int tagId, CancellationToken cancellationToken);

    /// <summary>Idempotent: removing a tag that is not assigned succeeds.</summary>
    /// <returns>false when the task does not exist for this user or the tag does not exist.</returns>
    Task<bool> UnassignAsync(int userId, int taskId, int tagId, CancellationToken cancellationToken);
}
