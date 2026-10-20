using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TodoApi.Data;
using TodoApi.Dtos.Tasks;
using TodoApi.Entities;
using TodoApi.Services;
using TodoApi.Tests.Infrastructure;

namespace TodoApi.Tests.Services;

[Collection(DatabaseCollection.Name)]
public class TaskServiceTests(ApiFixture fixture)
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 20, 9, 0, 0, TimeSpan.Zero));

    private TaskService CreateService(TodoDbContext db) => new(db, _clock);

    [Fact]
    public async Task CreateAsync_InOwnList_SetsTimestampsAndDefaults()
    {
        await using var db = fixture.CreateDbContext();
        var user = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, user.Id);

        var task = await CreateService(db).CreateAsync(
            user.Id, list.Id, new CreateTaskRequest { Title = " Write report " }, default);

        Assert.NotNull(task);
        Assert.Equal("Write report", task.Title);
        Assert.Equal(TodoTaskStatus.Todo, task.Status);
        Assert.Equal(2, task.Priority);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, task.CreatedAt);
        Assert.Equal(task.CreatedAt, task.UpdatedAt);
        Assert.Empty(task.Tags);
    }

    [Fact]
    public async Task CreateAsync_InOtherUsersList_ReturnsNull()
    {
        await using var db = fixture.CreateDbContext();
        var owner = await TestData.CreateUserAsync(db);
        var intruder = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, owner.Id);

        var task = await CreateService(db).CreateAsync(
            intruder.Id, list.Id, new CreateTaskRequest { Title = "Sneaky" }, default);

        Assert.Null(task);
        Assert.False(await db.Tasks.AnyAsync(t => t.TodoListId == list.Id));
    }

    [Fact]
    public async Task GetForListAsync_AppliesAllFilters()
    {
        await using var db = fixture.CreateDbContext();
        var user = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, user.Id);
        var tag = await TestData.CreateTagAsync(db, "Urgent");
        var match = await TestData.CreateTaskAsync(db, list.Id, "Match", TodoTaskStatus.InProgress, 3);
        await TestData.CreateTaskAsync(db, list.Id, "Wrong status", TodoTaskStatus.Done, 3);
        await TestData.CreateTaskAsync(db, list.Id, "Wrong priority", TodoTaskStatus.InProgress, 1);
        var untagged = await TestData.CreateTaskAsync(db, list.Id, "No tag", TodoTaskStatus.InProgress, 3);
        db.TaskTags.Add(new TaskTag { TaskId = match.Id, TagId = tag.Id });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var all = await service.GetForListAsync(user.Id, list.Id, new TaskQuery(), default);
        var filtered = await service.GetForListAsync(
            user.Id, list.Id,
            new TaskQuery { Status = TodoTaskStatus.InProgress, Priority = 3, TagId = tag.Id },
            default);

        Assert.Equal(4, all!.Count);
        var only = Assert.Single(filtered!);
        Assert.Equal(match.Id, only.Id);
        Assert.Equal("Urgent", Assert.Single(only.Tags).Name);
        Assert.DoesNotContain(filtered!, t => t.Id == untagged.Id);
    }

    [Fact]
    public async Task GetForListAsync_OtherUsersList_ReturnsNull()
    {
        await using var db = fixture.CreateDbContext();
        var owner = await TestData.CreateUserAsync(db);
        var intruder = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, owner.Id);

        Assert.Null(await CreateService(db).GetForListAsync(intruder.Id, list.Id, new TaskQuery(), default));
    }

    [Fact]
    public async Task GetDetailAsync_IncludesSubtasksNotesRemindersAndTags()
    {
        await using var db = fixture.CreateDbContext();
        var user = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, user.Id);
        var task = await TestData.CreateTaskAsync(db, list.Id);
        var tag = await TestData.CreateTagAsync(db);
        db.Subtasks.Add(new Subtask { TaskId = task.Id, Title = "Step 1" });
        db.Notes.Add(new Note { TaskId = task.Id, Content = "Remember this", CreatedAt = DateTime.UtcNow });
        db.Reminders.Add(new Reminder { TaskId = task.Id, RemindAt = DateTime.UtcNow.AddDays(1) });
        db.TaskTags.Add(new TaskTag { TaskId = task.Id, TagId = tag.Id });
        await db.SaveChangesAsync();

        var detail = await CreateService(db).GetDetailAsync(user.Id, task.Id, default);

        Assert.NotNull(detail);
        Assert.Equal("Step 1", Assert.Single(detail.Subtasks).Title);
        Assert.Equal("Remember this", Assert.Single(detail.Notes).Content);
        Assert.Single(detail.Reminders);
        Assert.Equal(tag.Id, Assert.Single(detail.Tags).Id);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesFieldsAndBumpsUpdatedAt()
    {
        await using var db = fixture.CreateDbContext();
        var user = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, user.Id);
        var service = CreateService(db);
        var created = await service.CreateAsync(user.Id, list.Id, new CreateTaskRequest { Title = "Draft" }, default);
        _clock.Advance(TimeSpan.FromHours(1));
        var dueDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

        var updated = await service.UpdateAsync(
            user.Id, created!.Id,
            new UpdateTaskRequest
            {
                Title = "Final", Description = "Done right", Status = TodoTaskStatus.Done, Priority = 3, DueDate = dueDate
            },
            default);

        Assert.NotNull(updated);
        Assert.Equal("Final", updated.Title);
        Assert.Equal("Done right", updated.Description);
        Assert.Equal(TodoTaskStatus.Done, updated.Status);
        Assert.Equal(3, updated.Priority);
        Assert.Equal(dueDate, updated.DueDate);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.Equal(created.CreatedAt.AddHours(1), updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateStatusAsync_ChangesStatusOnlyForOwner()
    {
        await using var db = fixture.CreateDbContext();
        var owner = await TestData.CreateUserAsync(db);
        var intruder = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, owner.Id);
        var task = await TestData.CreateTaskAsync(db, list.Id);
        var service = CreateService(db);

        Assert.False(await service.UpdateStatusAsync(intruder.Id, task.Id, TodoTaskStatus.Done, default));
        Assert.True(await service.UpdateStatusAsync(owner.Id, task.Id, TodoTaskStatus.Done, default));

        var stored = await db.Tasks.AsNoTracking().SingleAsync(t => t.Id == task.Id);
        Assert.Equal(TodoTaskStatus.Done, stored.Status);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, stored.UpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTaskAndChildren_OnlyForOwner()
    {
        await using var db = fixture.CreateDbContext();
        var owner = await TestData.CreateUserAsync(db);
        var intruder = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, owner.Id);
        var task = await TestData.CreateTaskAsync(db, list.Id);
        db.Subtasks.Add(new Subtask { TaskId = task.Id, Title = "Child" });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        Assert.False(await service.DeleteAsync(intruder.Id, task.Id, default));
        Assert.True(await service.DeleteAsync(owner.Id, task.Id, default));

        Assert.False(await db.Tasks.AnyAsync(t => t.Id == task.Id));
        Assert.False(await db.Subtasks.AnyAsync(s => s.TaskId == task.Id));
    }
}
