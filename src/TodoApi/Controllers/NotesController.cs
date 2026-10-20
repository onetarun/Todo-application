using Microsoft.AspNetCore.Mvc;
using TodoApi.Auth;
using TodoApi.Dtos.Notes;
using TodoApi.Services;

namespace TodoApi.Controllers;

[ApiController]
[Route("api")]
public class NotesController(INoteService noteService, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("tasks/{taskId:int}/notes")]
    [ProducesResponseType<IReadOnlyList<NoteDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<NoteDto>>> GetForTask(int taskId, CancellationToken cancellationToken)
    {
        var notes = await noteService.GetForTaskAsync(currentUser.RequiredUserId, taskId, cancellationToken);
        return notes is null ? NotFound() : Ok(notes);
    }

    // There is no single-note GET in the contract, so Location points at the task's note collection.
    [HttpPost("tasks/{taskId:int}/notes")]
    [ProducesResponseType<NoteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteDto>> Create(
        int taskId, CreateNoteRequest request, CancellationToken cancellationToken)
    {
        var note = await noteService.CreateAsync(currentUser.RequiredUserId, taskId, request, cancellationToken);
        return note is null ? NotFound() : CreatedAtAction(nameof(GetForTask), new { taskId }, note);
    }

    [HttpPut("notes/{id:int}")]
    [ProducesResponseType<NoteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteDto>> Update(int id, UpdateNoteRequest request, CancellationToken cancellationToken)
    {
        var note = await noteService.UpdateAsync(currentUser.RequiredUserId, id, request, cancellationToken);
        return note is null ? NotFound() : note;
    }

    [HttpDelete("notes/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await noteService.DeleteAsync(currentUser.RequiredUserId, id, cancellationToken) ? NoContent() : NotFound();
}
