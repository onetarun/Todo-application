using Microsoft.EntityFrameworkCore;
using TodoApi.Dtos.Tags;
using TodoApi.Services;
using TodoApi.Tests.Infrastructure;

namespace TodoApi.Tests.Services;

[Collection(DatabaseCollection.Name)]
public class TagServiceTests(ApiFixture fixture)
{
    [Fact]
    public async Task CreateAndUpdate_TrimsNameAndNormalizesColor()
    {
        await using var db = fixture.CreateDbContext();
        var service = new TagService(db);

        var created = await service.CreateAsync(new CreateTagRequest { Name = " Errands ", Color = "#a1b2c3" }, default);
        var updated = await service.UpdateAsync(created.Id, new UpdateTagRequest { Name = "Chores", Color = "#00ff00" }, default);

        Assert.Equal("Errands", created.Name);
        Assert.Equal("#A1B2C3", created.Color);
        Assert.Equal(new TagDto(created.Id, "Chores", "#00FF00"), updated);
        Assert.Contains(updated!, await service.GetAllAsync(default));
        Assert.Null(await service.UpdateAsync(int.MaxValue, new UpdateTagRequest { Name = "x", Color = "#000000" }, default));
    }

    [Fact]
    public async Task AssignAndUnassign_AreIdempotent()
    {
        await using var db = fixture.CreateDbContext();
        var user = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, user.Id);
        var task = await TestData.CreateTaskAsync(db, list.Id);
        var tag = await TestData.CreateTagAsync(db);
        var service = new TagService(db);

        Assert.True(await service.AssignAsync(user.Id, task.Id, tag.Id, default));
        Assert.True(await service.AssignAsync(user.Id, task.Id, tag.Id, default));
        Assert.Equal(1, await db.TaskTags.CountAsync(tt => tt.TaskId == task.Id));

        Assert.True(await service.UnassignAsync(user.Id, task.Id, tag.Id, default));
        Assert.True(await service.UnassignAsync(user.Id, task.Id, tag.Id, default));
        Assert.False(await db.TaskTags.AnyAsync(tt => tt.TaskId == task.Id));
    }

    [Fact]
    public async Task Assign_OtherUsersTaskOrMissingTag_ReturnsFalse()
    {
        await using var db = fixture.CreateDbContext();
        var owner = await TestData.CreateUserAsync(db);
        var intruder = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, owner.Id);
        var task = await TestData.CreateTaskAsync(db, list.Id);
        var tag = await TestData.CreateTagAsync(db);
        var service = new TagService(db);

        Assert.False(await service.AssignAsync(intruder.Id, task.Id, tag.Id, default));
        Assert.False(await service.AssignAsync(owner.Id, task.Id, int.MaxValue, default));
        Assert.False(await service.UnassignAsync(intruder.Id, task.Id, tag.Id, default));
        Assert.False(await db.TaskTags.AnyAsync(tt => tt.TaskId == task.Id));
    }

    [Fact]
    public async Task Delete_RemovesTagAndItsAssignments()
    {
        await using var db = fixture.CreateDbContext();
        var user = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, user.Id);
        var task = await TestData.CreateTaskAsync(db, list.Id);
        var tag = await TestData.CreateTagAsync(db);
        var service = new TagService(db);
        await service.AssignAsync(user.Id, task.Id, tag.Id, default);

        Assert.True(await service.DeleteAsync(tag.Id, default));
        Assert.False(await service.DeleteAsync(tag.Id, default));
        Assert.False(await db.TaskTags.AnyAsync(tt => tt.TagId == tag.Id));
        Assert.True(await db.Tasks.AnyAsync(t => t.Id == task.Id));
    }
}
