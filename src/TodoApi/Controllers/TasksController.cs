using Microsoft.AspNetCore.Mvc;
using TodoApi.Auth;
using TodoApi.Dtos.Tasks;
using TodoApi.Services;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController(ITaskService taskService, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType<TaskDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var task = await taskService.GetDetailAsync(currentUser.RequiredUserId, id, cancellationToken);
        return task is null ? NotFound() : task;
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<TaskDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> Update(int id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await taskService.UpdateAsync(currentUser.RequiredUserId, id, request, cancellationToken);
        return task is null ? NotFound() : task;
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        int id, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        // Status is [Required]; model validation guarantees it is present here.
        var updated = await taskService.UpdateStatusAsync(
            currentUser.RequiredUserId, id, request.Status!.Value, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await taskService.DeleteAsync(currentUser.RequiredUserId, id, cancellationToken) ? NoContent() : NotFound();
}
