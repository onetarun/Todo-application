using System.Net.Http.Json;
using System.Text.Json;
using TodoApi.Dtos.Lists;
using TodoApi.Dtos.Tasks;
using TodoApi.Tests.Infrastructure;

namespace TodoApi.Tests.Integration;

// Dates read back from SQL Server must serialize as UTC ("...Z") so the client does not read them as local time.
[Collection(DatabaseCollection.Name)]
public class DateSerializationTests(ApiFixture fixture)
{
    [Fact]
    public async Task DatesReadFromDatabase_SerializeAsUtc()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();
        var list = await (await client.PostAsJsonAsync("/api/lists", new { title = "Dates" }, ApiClient.Json))
            .ReadAsync<TodoListDto>();
        var task = await (await client.PostAsJsonAsync(
            $"/api/lists/{list.Id}/tasks", new { title = "Due", dueDate = "2026-11-01T08:00:00+02:00" }, ApiClient.Json))
            .ReadAsync<TaskDto>();
        // No offset: taken as UTC.
        await client.PostAsJsonAsync($"/api/tasks/{task.Id}/reminders", new { remindAt = "2026-10-31T09:00:00" }, ApiClient.Json);
        await client.PostAsJsonAsync($"/api/tasks/{task.Id}/notes", new { content = "n" }, ApiClient.Json);

        using var detail = JsonDocument.Parse(await client.GetStringAsync($"/api/tasks/{task.Id}"));
        using var lists = JsonDocument.Parse(await client.GetStringAsync("/api/lists"));
        var root = detail.RootElement;

        Assert.Equal("2026-11-01T06:00:00Z", root.GetProperty("dueDate").GetString());
        Assert.Equal("2026-10-31T09:00:00Z", root.GetProperty("reminders")[0].GetProperty("remindAt").GetString());
        Assert.EndsWith("Z", root.GetProperty("createdAt").GetString());
        Assert.EndsWith("Z", root.GetProperty("updatedAt").GetString());
        Assert.EndsWith("Z", root.GetProperty("notes")[0].GetProperty("createdAt").GetString());
        Assert.All(lists.RootElement.EnumerateArray(), l => Assert.EndsWith("Z", l.GetProperty("createdAt").GetString()));
    }
}
