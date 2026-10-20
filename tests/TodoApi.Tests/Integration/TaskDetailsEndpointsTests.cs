using System.Net;
using System.Net.Http.Json;
using System.Text;
using TodoApi.Dtos.Lists;
using TodoApi.Dtos.Notes;
using TodoApi.Dtos.Reminders;
using TodoApi.Dtos.Subtasks;
using TodoApi.Dtos.Tags;
using TodoApi.Dtos.Tasks;
using TodoApi.Tests.Infrastructure;

namespace TodoApi.Tests.Integration;

[Collection(DatabaseCollection.Name)]
public class TaskDetailsEndpointsTests(ApiFixture fixture)
{
    private static async Task<(HttpClient Client, int TaskId)> CreateClientWithTaskAsync(TodoApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var list = await (await client.PostAsJsonAsync("/api/lists", new { title = "Work" }, ApiClient.Json))
            .ReadAsync<TodoListDto>();
        var task = await (await client.PostAsJsonAsync($"/api/lists/{list.Id}/tasks", new { title = "Task" }, ApiClient.Json))
            .ReadAsync<TaskDto>();
        return (client, task.Id);
    }

    private static async Task<TagDto> CreateTagAsync(HttpClient client) =>
        await (await client.PostAsJsonAsync(
            "/api/tags", new { name = $"Tag {Guid.NewGuid():N}"[..20], color = "#3E63DD" }, ApiClient.Json))
            .ReadAsync<TagDto>();

    private static Task<HttpResponseMessage> SendJsonAsync(HttpClient client, HttpMethod method, string path, string json) =>
        client.SendAsync(new HttpRequestMessage(method, path)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

    private static async Task<TaskDetailDto> GetDetailAsync(HttpClient client, int taskId) =>
        await (await client.GetAsync($"/api/tasks/{taskId}")).ReadAsync<TaskDetailDto>();

    [Fact]
    public async Task Subtasks_Flow()
    {
        var (client, taskId) = await CreateClientWithTaskAsync(fixture.Factory);

        var create = await client.PostAsJsonAsync($"/api/tasks/{taskId}/subtasks", new { title = "Step 1" }, ApiClient.Json);
        var subtask = await create.ReadAsync<SubtaskDto>();
        var put = await client.PutAsJsonAsync(
            $"/api/subtasks/{subtask.Id}", new { title = "Step one", isCompleted = false }, ApiClient.Json);
        var complete = await client.PatchAsync($"/api/subtasks/{subtask.Id}/complete", null);
        var list = await (await client.GetAsync($"/api/tasks/{taskId}/subtasks")).ReadAsync<List<SubtaskDto>>();
        var detail = await GetDetailAsync(client, taskId);
        var delete = await client.DeleteAsync($"/api/subtasks/{subtask.Id}");
        var deleteAgain = await client.DeleteAsync($"/api/subtasks/{subtask.Id}");

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal($"/api/tasks/{taskId}/subtasks", create.Headers.Location?.AbsolutePath);
        Assert.Equal(taskId, subtask.TaskId);
        Assert.False(subtask.IsCompleted);
        Assert.Equal("Step one", (await put.ReadAsync<SubtaskDto>()).Title);
        Assert.Equal(HttpStatusCode.NoContent, complete.StatusCode);
        Assert.Equal(new SubtaskDto(subtask.Id, taskId, "Step one", true), Assert.Single(list));
        Assert.True(Assert.Single(detail.Subtasks).IsCompleted);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteAgain.StatusCode);
    }

    [Fact]
    public async Task Notes_Flow()
    {
        var (client, taskId) = await CreateClientWithTaskAsync(fixture.Factory);

        var create = await client.PostAsJsonAsync($"/api/tasks/{taskId}/notes", new { content = "Call Sam" }, ApiClient.Json);
        var note = await create.ReadAsync<NoteDto>();
        var put = await client.PutAsJsonAsync($"/api/notes/{note.Id}", new { content = "Call Sam at 3" }, ApiClient.Json);
        var list = await (await client.GetAsync($"/api/tasks/{taskId}/notes")).ReadAsync<List<NoteDto>>();
        var delete = await client.DeleteAsync($"/api/notes/{note.Id}");

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal($"/api/tasks/{taskId}/notes", create.Headers.Location?.AbsolutePath);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal("Call Sam at 3", Assert.Single(list).Content);
        Assert.Equal(note.CreatedAt, list[0].CreatedAt);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Empty((await GetDetailAsync(client, taskId)).Notes);
    }

    [Fact]
    public async Task Reminders_Flow()
    {
        var (client, taskId) = await CreateClientWithTaskAsync(fixture.Factory);
        var remindAt = new DateTime(2026, 11, 1, 8, 30, 0, DateTimeKind.Utc);

        var create = await client.PostAsJsonAsync($"/api/tasks/{taskId}/reminders", new { remindAt }, ApiClient.Json);
        var reminder = await create.ReadAsync<ReminderDto>();
        var list = await (await client.GetAsync($"/api/tasks/{taskId}/reminders")).ReadAsync<List<ReminderDto>>();
        var delete = await client.DeleteAsync($"/api/reminders/{reminder.Id}");

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal($"/api/tasks/{taskId}/reminders", create.Headers.Location?.AbsolutePath);
        Assert.Equal(remindAt, reminder.RemindAt);
        Assert.Equal(reminder, Assert.Single(list));
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Empty((await GetDetailAsync(client, taskId)).Reminders);
    }

    [Fact]
    public async Task Tags_CrudAndAssignment_Flow()
    {
        var (client, taskId) = await CreateClientWithTaskAsync(fixture.Factory);

        var create = await client.PostAsJsonAsync("/api/tags", new { name = "Errands", color = "#30a46c" }, ApiClient.Json);
        var tag = await create.ReadAsync<TagDto>();
        var put = await client.PutAsJsonAsync($"/api/tags/{tag.Id}", new { name = "Chores", color = "#30A46C" }, ApiClient.Json);
        var all = await (await client.GetAsync("/api/tags")).ReadAsync<List<TagDto>>();
        var assign = await client.PutAsync($"/api/tasks/{taskId}/tags/{tag.Id}", null);
        var assignAgain = await client.PutAsync($"/api/tasks/{taskId}/tags/{tag.Id}", null);
        var tagged = await GetDetailAsync(client, taskId);
        var unassign = await client.DeleteAsync($"/api/tasks/{taskId}/tags/{tag.Id}");
        var untagged = await GetDetailAsync(client, taskId);
        var delete = await client.DeleteAsync($"/api/tags/{tag.Id}");
        var putDeleted = await client.PutAsJsonAsync($"/api/tags/{tag.Id}", new { name = "x", color = "#000000" }, ApiClient.Json);

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal("/api/tags", create.Headers.Location?.AbsolutePath);
        Assert.Equal("#30A46C", tag.Color);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Contains(new TagDto(tag.Id, "Chores", "#30A46C"), all);
        Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, assignAgain.StatusCode);
        Assert.Equal(tag.Id, Assert.Single(tagged.Tags).Id);
        Assert.Equal(HttpStatusCode.NoContent, unassign.StatusCode);
        Assert.Empty(untagged.Tags);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, putDeleted.StatusCode);
    }

    [Fact]
    public async Task DeletingTag_RemovesItFromTasks()
    {
        var (client, taskId) = await CreateClientWithTaskAsync(fixture.Factory);
        var tag = await CreateTagAsync(client);
        await client.PutAsync($"/api/tasks/{taskId}/tags/{tag.Id}", null);

        await client.DeleteAsync($"/api/tags/{tag.Id}");

        Assert.Empty((await GetDetailAsync(client, taskId)).Tags);
    }

    [Theory]
    [InlineData("POST", "/api/tasks/{task}/subtasks", """{"title":""}""")]
    [InlineData("POST", "/api/tasks/{task}/subtasks", """{"title":"   "}""")]
    [InlineData("POST", "/api/tasks/{task}/notes", """{}""")]
    [InlineData("POST", "/api/tasks/{task}/reminders", """{}""")]
    [InlineData("POST", "/api/tasks/{task}/reminders", """{"remindAt":"not a date"}""")]
    [InlineData("POST", "/api/tags", """{"name":"x","color":"blue"}""")]
    [InlineData("POST", "/api/tags", """{"name":"x","color":"#12345"}""")]
    [InlineData("POST", "/api/tags", """{"name":"","color":"#123456"}""")]
    [InlineData("POST", "/api/tags", """{"name":"012345678901234567890123456789012345678901234567890","color":"#123456"}""")]
    [InlineData("PUT", "/api/subtasks/{subtask}", """{"title":"No completion flag"}""")]
    [InlineData("PUT", "/api/notes/{note}", """{"content":""}""")]
    [InlineData("PUT", "/api/tags/{tag}", """{"name":"x"}""")]
    public async Task InvalidInput_ReturnsBadRequest(string method, string pathTemplate, string json)
    {
        var (client, taskId) = await CreateClientWithTaskAsync(fixture.Factory);
        var subtask = await (await client.PostAsJsonAsync($"/api/tasks/{taskId}/subtasks", new { title = "s" }, ApiClient.Json))
            .ReadAsync<SubtaskDto>();
        var note = await (await client.PostAsJsonAsync($"/api/tasks/{taskId}/notes", new { content = "n" }, ApiClient.Json))
            .ReadAsync<NoteDto>();
        var tag = await CreateTagAsync(client);
        var path = pathTemplate
            .Replace("{task}", taskId.ToString())
            .Replace("{subtask}", subtask.Id.ToString())
            .Replace("{note}", note.Id.ToString())
            .Replace("{tag}", tag.Id.ToString());

        var response = await SendJsonAsync(client, new HttpMethod(method), path, json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OtherUsersTaskDetails_AreNotFound()
    {
        var (owner, taskId) = await CreateClientWithTaskAsync(fixture.Factory);
        var intruder = await fixture.Factory.CreateAuthenticatedClientAsync();
        var subtask = await (await owner.PostAsJsonAsync($"/api/tasks/{taskId}/subtasks", new { title = "s" }, ApiClient.Json))
            .ReadAsync<SubtaskDto>();
        var note = await (await owner.PostAsJsonAsync($"/api/tasks/{taskId}/notes", new { content = "n" }, ApiClient.Json))
            .ReadAsync<NoteDto>();
        var reminder = await (await owner.PostAsJsonAsync(
            $"/api/tasks/{taskId}/reminders", new { remindAt = DateTime.UtcNow.AddDays(1) }, ApiClient.Json))
            .ReadAsync<ReminderDto>();
        var tag = await CreateTagAsync(owner);

        HttpResponseMessage[] responses =
        [
            await intruder.GetAsync($"/api/tasks/{taskId}/subtasks"),
            await intruder.PostAsJsonAsync($"/api/tasks/{taskId}/subtasks", new { title = "x" }, ApiClient.Json),
            await intruder.PutAsJsonAsync($"/api/subtasks/{subtask.Id}", new { title = "x", isCompleted = true }, ApiClient.Json),
            await intruder.PatchAsync($"/api/subtasks/{subtask.Id}/complete", null),
            await intruder.DeleteAsync($"/api/subtasks/{subtask.Id}"),
            await intruder.GetAsync($"/api/tasks/{taskId}/notes"),
            await intruder.PostAsJsonAsync($"/api/tasks/{taskId}/notes", new { content = "x" }, ApiClient.Json),
            await intruder.PutAsJsonAsync($"/api/notes/{note.Id}", new { content = "x" }, ApiClient.Json),
            await intruder.DeleteAsync($"/api/notes/{note.Id}"),
            await intruder.GetAsync($"/api/tasks/{taskId}/reminders"),
            await intruder.PostAsJsonAsync($"/api/tasks/{taskId}/reminders", new { remindAt = DateTime.UtcNow }, ApiClient.Json),
            await intruder.DeleteAsync($"/api/reminders/{reminder.Id}"),
            await intruder.PutAsync($"/api/tasks/{taskId}/tags/{tag.Id}", null),
            await intruder.DeleteAsync($"/api/tasks/{taskId}/tags/{tag.Id}")
        ];

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
        var detail = await GetDetailAsync(owner, taskId);
        Assert.Equal(new SubtaskDto(subtask.Id, taskId, "s", false), Assert.Single(detail.Subtasks));
        Assert.Equal("n", Assert.Single(detail.Notes).Content);
        Assert.Single(detail.Reminders);
        Assert.Empty(detail.Tags);
    }

    [Fact]
    public async Task MissingTaskOrTag_ReturnsNotFound()
    {
        var (client, taskId) = await CreateClientWithTaskAsync(fixture.Factory);
        var tag = await CreateTagAsync(client);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/tasks/999999999/subtasks")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsync($"/api/tasks/{taskId}/tags/999999999", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsync($"/api/tasks/999999999/tags/{tag.Id}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/tags/999999999")).StatusCode);
    }

    [Fact]
    public async Task Endpoints_RequireAuthentication()
    {
        var anonymous = fixture.Factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/tags")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/tasks/1/subtasks")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.DeleteAsync("/api/reminders/1")).StatusCode);
    }
}
