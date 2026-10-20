using System.Net;
using System.Net.Http.Json;
using System.Text;
using TodoApi.Dtos.Lists;
using TodoApi.Dtos.Tasks;
using TodoApi.Entities;
using TodoApi.Tests.Infrastructure;

namespace TodoApi.Tests.Integration;

[Collection(DatabaseCollection.Name)]
public class TasksEndpointsTests(ApiFixture fixture)
{
    private static async Task<(HttpClient Client, int ListId)> CreateClientWithListAsync(TodoApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var list = await (await client.PostAsJsonAsync("/api/lists", new { title = "Work" }, ApiClient.Json))
            .ReadAsync<TodoListDto>();
        return (client, list.Id);
    }

    private static Task<HttpResponseMessage> PostRawJsonAsync(HttpClient client, string path, string json) =>
        client.PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"));

    [Fact]
    public async Task Create_ReturnsCreatedWithStatusAsString()
    {
        var (client, listId) = await CreateClientWithListAsync(fixture.Factory);

        var response = await client.PostAsJsonAsync(
            $"/api/lists/{listId}/tasks",
            new { title = "Ship it", status = "InProgress", priority = 3, dueDate = "2026-11-01T00:00:00Z" },
            ApiClient.Json);
        var json = await response.Content.ReadAsStringAsync();
        var task = await response.ReadAsync<TaskDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/tasks/{task.Id}", response.Headers.Location?.AbsolutePath);
        Assert.Contains("\"status\":\"InProgress\"", json);
        Assert.Equal(listId, task.TodoListId);
        Assert.Equal(3, task.Priority);
    }

    [Theory]
    [InlineData("""{"title":"x","priority":4}""")]
    [InlineData("""{"title":"x","priority":0}""")]
    [InlineData("""{"title":"x","status":"Paused"}""")]
    [InlineData("""{"title":"x","status":1}""")]
    [InlineData("""{"title":""}""")]
    public async Task Create_InvalidInput_ReturnsBadRequest(string json)
    {
        var (client, listId) = await CreateClientWithListAsync(fixture.Factory);

        var response = await PostRawJsonAsync(client, $"/api/lists/{listId}/tasks", json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_InMissingList_ReturnsNotFound()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/lists/999999999/tasks", new { title = "Lost" }, ApiClient.Json);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTasks_FiltersByQuery()
    {
        var (client, listId) = await CreateClientWithListAsync(fixture.Factory);
        await client.PostAsJsonAsync($"/api/lists/{listId}/tasks", new { title = "A", status = "Done", priority = 1 }, ApiClient.Json);
        await client.PostAsJsonAsync($"/api/lists/{listId}/tasks", new { title = "B", status = "Todo", priority = 3 }, ApiClient.Json);
        await client.PostAsJsonAsync($"/api/lists/{listId}/tasks", new { title = "C", status = "Done", priority = 3 }, ApiClient.Json);

        var done = await (await client.GetAsync($"/api/lists/{listId}/tasks?status=Done")).ReadAsync<List<TaskDto>>();
        var doneHigh = await (await client.GetAsync($"/api/lists/{listId}/tasks?status=Done&priority=3"))
            .ReadAsync<List<TaskDto>>();
        var badPriority = await client.GetAsync($"/api/lists/{listId}/tasks?priority=9");

        Assert.Equal(["A", "C"], done.Select(t => t.Title));
        Assert.Equal("C", Assert.Single(doneHigh).Title);
        Assert.Equal(HttpStatusCode.BadRequest, badPriority.StatusCode);
    }

    [Fact]
    public async Task UpdatePatchDelete_Flow()
    {
        var (client, listId) = await CreateClientWithListAsync(fixture.Factory);
        var task = await (await client.PostAsJsonAsync($"/api/lists/{listId}/tasks", new { title = "Draft" }, ApiClient.Json))
            .ReadAsync<TaskDto>();

        var put = await client.PutAsJsonAsync(
            $"/api/tasks/{task.Id}", new { title = "Final", status = "InProgress", priority = 1 }, ApiClient.Json);
        var patch = await client.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", new { status = "Done" }, ApiClient.Json);
        var detail = await (await client.GetAsync($"/api/tasks/{task.Id}")).ReadAsync<TaskDetailDto>();
        var delete = await client.DeleteAsync($"/api/tasks/{task.Id}");
        var afterDelete = await client.GetAsync($"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal("Final", (await put.ReadAsync<TaskDto>()).Title);
        Assert.Equal(HttpStatusCode.NoContent, patch.StatusCode);
        Assert.Equal(TodoTaskStatus.Done, detail.Status);
        Assert.Equal(1, detail.Priority);
        Assert.Empty(detail.Subtasks);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Put_MissingRequiredFields_ReturnsBadRequest()
    {
        var (client, listId) = await CreateClientWithListAsync(fixture.Factory);
        var task = await (await client.PostAsJsonAsync($"/api/lists/{listId}/tasks", new { title = "Draft" }, ApiClient.Json))
            .ReadAsync<TaskDto>();

        var put = await client.PutAsJsonAsync($"/api/tasks/{task.Id}", new { title = "No status or priority" }, ApiClient.Json);
        var patch = await client.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", new { }, ApiClient.Json);

        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, patch.StatusCode);
    }

    [Fact]
    public async Task OtherUsersTask_IsNotFound()
    {
        var (owner, listId) = await CreateClientWithListAsync(fixture.Factory);
        var intruder = await fixture.Factory.CreateAuthenticatedClientAsync();
        var task = await (await owner.PostAsJsonAsync($"/api/lists/{listId}/tasks", new { title = "Private" }, ApiClient.Json))
            .ReadAsync<TaskDto>();

        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/tasks/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.PutAsJsonAsync(
            $"/api/tasks/{task.Id}", new { title = "Mine", status = "Todo", priority = 1 }, ApiClient.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.PatchAsJsonAsync(
            $"/api/tasks/{task.Id}/status", new { status = "Done" }, ApiClient.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.DeleteAsync($"/api/tasks/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.PostAsJsonAsync(
            $"/api/lists/{listId}/tasks", new { title = "Injected" }, ApiClient.Json)).StatusCode);
    }
}
