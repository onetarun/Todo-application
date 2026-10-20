using TodoApi.Entities;
using TodoApi.Services;
using TodoApi.Tests.Infrastructure;

namespace TodoApi.Tests.Services;

[Collection(DatabaseCollection.Name)]
public class DueReminderTests(ApiFixture fixture)
{
    private static readonly DateTime Now = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetDueAsync_ReturnsOnlyTheUsersRemindersInThe24HourWindow_NewestFirst()
    {
        await using var db = fixture.CreateDbContext();
        var user = await TestData.CreateUserAsync(db);
        var other = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, user.Id);
        var otherList = await TestData.CreateListAsync(db, other.Id);
        var task = await TestData.CreateTaskAsync(db, list.Id, "Write report");
        var otherTask = await TestData.CreateTaskAsync(db, otherList.Id);
        db.Reminders.AddRange(
            new Reminder { TaskId = task.Id, RemindAt = Now.AddMinutes(-5) },
            new Reminder { TaskId = task.Id, RemindAt = Now.AddHours(-23) },
            new Reminder { TaskId = task.Id, RemindAt = Now }, // inclusive upper bound
            new Reminder { TaskId = task.Id, RemindAt = Now.AddHours(-24) }, // exclusive lower bound
            new Reminder { TaskId = task.Id, RemindAt = Now.AddMinutes(1) }, // not due yet
            new Reminder { TaskId = otherTask.Id, RemindAt = Now.AddMinutes(-5) }); // another user's
        await db.SaveChangesAsync();

        var due = await new ReminderService(db).GetDueAsync(user.Id, Now, default);

        Assert.Equal([Now, Now.AddMinutes(-5), Now.AddHours(-23)], due.Select(r => r.RemindAt));
        Assert.All(due, r => Assert.Equal("Write report", r.TaskTitle));
        Assert.All(due, r => Assert.Equal(task.Id, r.TaskId));
    }

    [Fact]
    public async Task GetDueAsync_TreatsAnOffsetlessTimeAsUtc()
    {
        await using var db = fixture.CreateDbContext();
        var user = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, user.Id);
        var task = await TestData.CreateTaskAsync(db, list.Id);
        db.Reminders.Add(new Reminder { TaskId = task.Id, RemindAt = Now });
        await db.SaveChangesAsync();
        var service = new ReminderService(db);

        var unspecified = DateTime.SpecifyKind(Now, DateTimeKind.Unspecified);

        Assert.Single(await service.GetDueAsync(user.Id, unspecified, default));
        Assert.Empty(await service.GetDueAsync(user.Id, unspecified.AddSeconds(-1), default));
    }
}
