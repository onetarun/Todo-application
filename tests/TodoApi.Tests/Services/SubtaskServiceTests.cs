using Microsoft.EntityFrameworkCore;
using TodoApi.Dtos.Subtasks;
using TodoApi.Services;
using TodoApi.Tests.Infrastructure;

namespace TodoApi.Tests.Services;

[Collection(DatabaseCollection.Name)]
public class SubtaskServiceTests(ApiFixture fixture)
{
    [Fact]
    public async Task CreateUpdateComplete_OwnTask()
    {
        await using var db = fixture.CreateDbContext();
        var user = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, user.Id);
        var task = await TestData.CreateTaskAsync(db, list.Id);
        var service = new SubtaskService(db);

        var first = await service.CreateAsync(user.Id, task.Id, new CreateSubtaskRequest { Title = " Step 1 " }, default);
        var second = await service.CreateAsync(user.Id, task.Id, new CreateSubtaskRequest { Title = "Step 2" }, default);
        var renamed = await service.UpdateAsync(
            user.Id, first!.Id, new UpdateSubtaskRequest { Title = "Step one", IsCompleted = false }, default);
        var completed = await service.CompleteAsync(user.Id, second!.Id, default);
        var all = await service.GetForTaskAsync(user.Id, task.Id, default);

        Assert.Equal("Step 1", first.Title);
        Assert.False(first.IsCompleted);
        Assert.Equal("Step one", renamed!.Title);
        Assert.True(completed);
        Assert.Equal([("Step one", false), ("Step 2", true)], all!.Select(s => (s.Title, s.IsCompleted)));
    }

    [Fact]
    public async Task OtherUsersTask_IsInvisible()
    {
        await using var db = fixture.CreateDbContext();
        var owner = await TestData.CreateUserAsync(db);
        var intruder = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, owner.Id);
        var task = await TestData.CreateTaskAsync(db, list.Id);
        var service = new SubtaskService(db);
        var subtask = await service.CreateAsync(owner.Id, task.Id, new CreateSubtaskRequest { Title = "Private" }, default);

        Assert.Null(await service.GetForTaskAsync(intruder.Id, task.Id, default));
        Assert.Null(await service.CreateAsync(intruder.Id, task.Id, new CreateSubtaskRequest { Title = "x" }, default));
        Assert.Null(await service.UpdateAsync(
            intruder.Id, subtask!.Id, new UpdateSubtaskRequest { Title = "x", IsCompleted = true }, default));
        Assert.False(await service.CompleteAsync(intruder.Id, subtask.Id, default));
        Assert.False(await service.DeleteAsync(intruder.Id, subtask.Id, default));

        var stored = await db.Subtasks.AsNoTracking().SingleAsync(s => s.Id == subtask.Id);
        Assert.Equal("Private", stored.Title);
        Assert.False(stored.IsCompleted);
        Assert.True(await service.DeleteAsync(owner.Id, subtask.Id, default));
        Assert.False(await db.Subtasks.AnyAsync(s => s.Id == subtask.Id));
    }
}
