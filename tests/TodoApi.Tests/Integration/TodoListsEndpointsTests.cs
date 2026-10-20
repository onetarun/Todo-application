using System.Net;
using System.Net.Http.Json;
using TodoApi.Dtos.Lists;
using TodoApi.Tests.Infrastructure;

namespace TodoApi.Tests.Integration;

[Collection(DatabaseCollection.Name)]
public class TodoListsEndpointsTests(ApiFixture fixture)
{
    [Fact]
    public async Task Endpoints_RequireAuthentication()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/lists");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Crud_Flow()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/api/lists", new { title = "Home", description = "Chores" }, ApiClient.Json);
        var created = await create.ReadAsync<TodoListDto>();
        var get = await client.GetAsync(create.Headers.Location);
        var update = await client.PutAsJsonAsync($"/api/lists/{created.Id}", new { title = "House" }, ApiClient.Json);
        var all = await (await client.GetAsync("/api/lists")).ReadAsync<List<TodoListDto>>();
        var delete = await client.DeleteAsync($"/api/lists/{created.Id}");
        var afterDelete = await client.GetAsync($"/api/lists/{created.Id}");

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal($"/api/lists/{created.Id}", create.Headers.Location?.AbsolutePath);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("House", (await update.ReadAsync<TodoListDto>()).Title);
        Assert.Equal("House", Assert.Single(all).Title);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task OtherUsersList_IsNotFound()
    {
        var owner = await fixture.Factory.CreateAuthenticatedClientAsync();
        var intruder = await fixture.Factory.CreateAuthenticatedClientAsync();
        var list = await (await owner.PostAsJsonAsync("/api/lists", new { title = "Private" }, ApiClient.Json))
            .ReadAsync<TodoListDto>();

        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/lists/{list.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await intruder.PutAsJsonAsync($"/api/lists/{list.Id}", new { title = "Mine now" }, ApiClient.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.DeleteAsync($"/api/lists/{list.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/lists/{list.Id}/tasks")).StatusCode);
        Assert.Empty(await (await intruder.GetAsync("/api/lists")).ReadAsync<List<TodoListDto>>());
    }

    [Fact]
    public async Task Create_InvalidTitle_ReturnsBadRequest()
    {
        var client = await fixture.Factory.CreateAuthenticatedClientAsync();

        var blank = await client.PostAsJsonAsync("/api/lists", new { title = "   " }, ApiClient.Json);
        var tooLong = await client.PostAsJsonAsync("/api/lists", new { title = new string('x', 201) }, ApiClient.Json);

        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
    }
}
