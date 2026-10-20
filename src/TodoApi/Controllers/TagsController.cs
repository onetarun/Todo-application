using Microsoft.AspNetCore.Mvc;
using TodoApi.Auth;
using TodoApi.Dtos.Tags;
using TodoApi.Services;

namespace TodoApi.Controllers;

[ApiController]
[Route("api")]
public class TagsController(ITagService tagService, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("tags")]
    [ProducesResponseType<IReadOnlyList<TagDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await tagService.GetAllAsync(cancellationToken));

    // There is no single-tag GET in the contract, so Location points at the tag collection.
    [HttpPost("tags")]
    [ProducesResponseType<TagDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TagDto>> Create(CreateTagRequest request, CancellationToken cancellationToken)
    {
        var tag = await tagService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), null, tag);
    }

    [HttpPut("tags/{id:int}")]
    [ProducesResponseType<TagDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagDto>> Update(int id, UpdateTagRequest request, CancellationToken cancellationToken)
    {
        var tag = await tagService.UpdateAsync(id, request, cancellationToken);
        return tag is null ? NotFound() : tag;
    }

    [HttpDelete("tags/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await tagService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPut("tasks/{taskId:int}/tags/{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(int taskId, int tagId, CancellationToken cancellationToken) =>
        await tagService.AssignAsync(currentUser.RequiredUserId, taskId, tagId, cancellationToken)
            ? NoContent()
            : NotFound();

    [HttpDelete("tasks/{taskId:int}/tags/{tagId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unassign(int taskId, int tagId, CancellationToken cancellationToken) =>
        await tagService.UnassignAsync(currentUser.RequiredUserId, taskId, tagId, cancellationToken)
            ? NoContent()
            : NotFound();
}
