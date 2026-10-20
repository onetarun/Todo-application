using Microsoft.AspNetCore.Mvc;
using TodoApi.Auth;
using TodoApi.Dtos.Lists;
using TodoApi.Dtos.Tasks;
using TodoApi.Services;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/lists")]
public class TodoListsController(
    ITodoListService listService,
    ITaskService taskService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TodoListDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TodoListDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await listService.GetAllAsync(currentUser.RequiredUserId, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<TodoListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoListDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var list = await listService.GetAsync(currentUser.RequiredUserId, id, cancellationToken);
        return list is null ? NotFound() : list;
    }

    [HttpPost]
    [ProducesResponseType<TodoListDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TodoListDto>> Create(CreateTodoListRequest request, CancellationToken cancellationToken)
    {
        var list = await listService.CreateAsync(currentUser.RequiredUserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = list.Id }, list);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<TodoListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoListDto>> Update(
        int id, UpdateTodoListRequest request, CancellationToken cancellationToken)
    {
        var list = await listService.UpdateAsync(currentUser.RequiredUserId, id, request, cancellationToken);
        return list is null ? NotFound() : list;
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await listService.DeleteAsync(currentUser.RequiredUserId, id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("{listId:int}/tasks")]
    [ProducesResponseType<IReadOnlyList<TaskDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> GetTasks(
        int listId, [FromQuery] TaskQuery query, CancellationToken cancellationToken)
    {
        var tasks = await taskService.GetForListAsync(currentUser.RequiredUserId, listId, query, cancellationToken);
        return tasks is null ? NotFound() : Ok(tasks);
    }

    [HttpPost("{listId:int}/tasks")]
    [ProducesResponseType<TaskDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> CreateTask(
        int listId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await taskService.CreateAsync(currentUser.RequiredUserId, listId, request, cancellationToken);
        return task is null
            ? NotFound()
            : CreatedAtAction(nameof(TasksController.GetById), "Tasks", new { id = task.Id }, task);
    }
}
