using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos.Tags;
using TodoApi.Entities;

namespace TodoApi.Services;

public class TagService(TodoDbContext db) : ITagService
{
    private static readonly Expression<Func<Tag, TagDto>> ToDto = t => new TagDto(t.Id, t.Name, t.Color);

    private static readonly Func<Tag, TagDto> ToDtoCompiled = ToDto.Compile();

    public async Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.Tags
            .AsNoTracking()
            .OrderBy(t => t.Name).ThenBy(t => t.Id)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

    public async Task<TagDto> CreateAsync(CreateTagRequest request, CancellationToken cancellationToken)
    {
        var tag = new Tag { Name = request.Name.Trim(), Color = request.Color.ToUpperInvariant() };
        db.Tags.Add(tag);
        await db.SaveChangesAsync(cancellationToken);
        return ToDtoCompiled(tag);
    }

    public async Task<TagDto?> UpdateAsync(int tagId, UpdateTagRequest request, CancellationToken cancellationToken)
    {
        var tag = await db.Tags.SingleOrDefaultAsync(t => t.Id == tagId, cancellationToken);
        if (tag is null)
        {
            return null;
        }

        tag.Name = request.Name.Trim();
        tag.Color = request.Color.ToUpperInvariant();
        await db.SaveChangesAsync(cancellationToken);
        return ToDtoCompiled(tag);
    }

    public async Task<bool> DeleteAsync(int tagId, CancellationToken cancellationToken)
    {
        // Task assignments are removed by the database cascade.
        var deleted = await db.Tags.Where(t => t.Id == tagId).ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    public async Task<bool> AssignAsync(int userId, int taskId, int tagId, CancellationToken cancellationToken)
    {
        if (!await TaskAndTagExistAsync(userId, taskId, tagId, cancellationToken))
        {
            return false;
        }

        if (await db.TaskTags.AnyAsync(tt => tt.TaskId == taskId && tt.TagId == tagId, cancellationToken))
        {
            return true;
        }

        db.TaskTags.Add(new TaskTag { TaskId = taskId, TagId = tagId });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // A concurrent request assigned the same tag after the check above; the outcome is the same.
        }

        return true;
    }

    public async Task<bool> UnassignAsync(int userId, int taskId, int tagId, CancellationToken cancellationToken)
    {
        if (!await TaskAndTagExistAsync(userId, taskId, tagId, cancellationToken))
        {
            return false;
        }

        await db.TaskTags
            .Where(tt => tt.TaskId == taskId && tt.TagId == tagId)
            .ExecuteDeleteAsync(cancellationToken);
        return true;
    }

    private async Task<bool> TaskAndTagExistAsync(
        int userId, int taskId, int tagId, CancellationToken cancellationToken) =>
        await db.Tasks.AnyAsync(t => t.Id == taskId && t.TodoList.UserId == userId, cancellationToken)
        && await db.Tags.AnyAsync(t => t.Id == tagId, cancellationToken);
}
