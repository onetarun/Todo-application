using Microsoft.AspNetCore.Mvc;
using TodoApi.Auth;
using TodoApi.Dtos.Subtasks;
using TodoApi.Services;

namespace TodoApi.Controllers;

[ApiController]
[Route("api")]
public class SubtasksController(ISubtaskService subtaskService, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("tasks/{taskId:int}/subtasks")]
    [ProducesResponseType<IReadOnlyList<SubtaskDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SubtaskDto>>> GetForTask(int taskId, CancellationToken cancellationToken)
    {
        var subtasks = await subtaskService.GetForTaskAsync(currentUser.RequiredUserId, taskId, cancellationToken);
        return subtasks is null ? NotFound() : Ok(subtasks);
    }

    // There is no single-subtask GET in the contract, so Location points at the task's subtask collection.
    [HttpPost("tasks/{taskId:int}/subtasks")]
    [ProducesResponseType<SubtaskDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubtaskDto>> Create(
        int taskId, CreateSubtaskRequest request, CancellationToken cancellationToken)
    {
        var subtask = await subtaskService.CreateAsync(currentUser.RequiredUserId, taskId, request, cancellationToken);
        return subtask is null ? NotFound() : CreatedAtAction(nameof(GetForTask), new { taskId }, subtask);
    }

    [HttpPut("subtasks/{id:int}")]
    [ProducesResponseType<SubtaskDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubtaskDto>> Update(
        int id, UpdateSubtaskRequest request, CancellationToken cancellationToken)
    {
        var subtask = await subtaskService.UpdateAsync(currentUser.RequiredUserId, id, request, cancellationToken);
        return subtask is null ? NotFound() : subtask;
    }

    [HttpPatch("subtasks/{id:int}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(int id, CancellationToken cancellationToken) =>
        await subtaskService.CompleteAsync(currentUser.RequiredUserId, id, cancellationToken) ? NoContent() : NotFound();

    [HttpDelete("subtasks/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await subtaskService.DeleteAsync(currentUser.RequiredUserId, id, cancellationToken) ? NoContent() : NotFound();
}
