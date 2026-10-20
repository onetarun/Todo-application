using Microsoft.EntityFrameworkCore;
using TodoApi.Dtos.Lists;
using TodoApi.Services;
using TodoApi.Tests.Infrastructure;

namespace TodoApi.Tests.Services;

[Collection(DatabaseCollection.Name)]
public class TodoListServiceTests(ApiFixture fixture)
{
    private static TodoListService CreateService(Data.TodoDbContext db) => new(db, TimeProvider.System);

    [Fact]
    public async Task CreateAsync_TrimsTitleAndReturnsDto()
    {
        await using var db = fixture.CreateDbContext();
        var user = await TestData.CreateUserAsync(db);

        var list = await CreateService(db).CreateAsync(
            user.Id, new CreateTodoListRequest { Title = "  Groceries  ", Description = "Weekly" }, default);

        Assert.True(list.Id > 0);
        Assert.Equal("Groceries", list.Title);
        Assert.Equal("Weekly", list.Description);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyTheUsersLists()
    {
        await using var db = fixture.CreateDbContext();
        var user = await TestData.CreateUserAsync(db);
        var otherUser = await TestData.CreateUserAsync(db);
        await TestData.CreateListAsync(db, user.Id, "Mine");
        await TestData.CreateListAsync(db, otherUser.Id, "Theirs");

        var lists = await CreateService(db).GetAllAsync(user.Id, default);

        Assert.Equal(["Mine"], lists.Select(l => l.Title));
    }

    [Fact]
    public async Task GetUpdateDelete_OtherUsersList_BehaveAsNotFound()
    {
        await using var db = fixture.CreateDbContext();
        var owner = await TestData.CreateUserAsync(db);
        var intruder = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, owner.Id);
        var service = CreateService(db);

        Assert.Null(await service.GetAsync(intruder.Id, list.Id, default));
        Assert.Null(await service.UpdateAsync(intruder.Id, list.Id, new UpdateTodoListRequest { Title = "Hacked" }, default));
        Assert.False(await service.DeleteAsync(intruder.Id, list.Id, default));
        Assert.NotNull(await service.GetAsync(owner.Id, list.Id, default));
    }

    [Fact]
    public async Task UpdateAsync_ReplacesTitleAndDescription()
    {
        await using var db = fixture.CreateDbContext();
        var user = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, user.Id, "Old");

        var updated = await CreateService(db).UpdateAsync(
            user.Id, list.Id, new UpdateTodoListRequest { Title = "New", Description = null }, default);

        Assert.NotNull(updated);
        Assert.Equal("New", updated.Title);
        Assert.Null(updated.Description);
    }

    [Fact]
    public async Task DeleteAsync_RemovesListAndCascadesToTasks()
    {
        await using var db = fixture.CreateDbContext();
        var user = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, user.Id);
        var task = await TestData.CreateTaskAsync(db, list.Id);

        Assert.True(await CreateService(db).DeleteAsync(user.Id, list.Id, default));

        Assert.False(await db.TodoLists.AnyAsync(l => l.Id == list.Id));
        Assert.False(await db.Tasks.AnyAsync(t => t.Id == task.Id));
    }
}
