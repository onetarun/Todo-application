using Microsoft.AspNetCore.Mvc;
using TodoApi.Auth;
using TodoApi.Dtos.Reminders;
using TodoApi.Services;

namespace TodoApi.Controllers;

[ApiController]
[Route("api")]
public class RemindersController(IReminderService reminderService, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("tasks/{taskId:int}/reminders")]
    [ProducesResponseType<IReadOnlyList<ReminderDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ReminderDto>>> GetForTask(int taskId, CancellationToken cancellationToken)
    {
        var reminders = await reminderService.GetForTaskAsync(currentUser.RequiredUserId, taskId, cancellationToken);
        return reminders is null ? NotFound() : Ok(reminders);
    }

    // There is no single-reminder GET in the contract, so Location points at the task's reminder collection.
    [HttpPost("tasks/{taskId:int}/reminders")]
    [ProducesResponseType<ReminderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReminderDto>> Create(
        int taskId, CreateReminderRequest request, CancellationToken cancellationToken)
    {
        var reminder = await reminderService.CreateAsync(currentUser.RequiredUserId, taskId, request, cancellationToken);
        return reminder is null ? NotFound() : CreatedAtAction(nameof(GetForTask), new { taskId }, reminder);
    }

    [HttpDelete("reminders/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await reminderService.DeleteAsync(currentUser.RequiredUserId, id, cancellationToken) ? NoContent() : NotFound();
}
