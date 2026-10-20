using Microsoft.EntityFrameworkCore;
using TodoApi.Dtos.Reminders;
using TodoApi.Services;
using TodoApi.Tests.Infrastructure;

namespace TodoApi.Tests.Services;

[Collection(DatabaseCollection.Name)]
public class ReminderServiceTests(ApiFixture fixture)
{
    [Fact]
    public async Task Create_ListsInRemindAtOrder()
    {
        await using var db = fixture.CreateDbContext();
        var user = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, user.Id);
        var task = await TestData.CreateTaskAsync(db, list.Id);
        var service = new ReminderService(db);
        var later = new DateTime(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc);
        var sooner = new DateTime(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

        await service.CreateAsync(user.Id, task.Id, new CreateReminderRequest { RemindAt = later }, default);
        await service.CreateAsync(user.Id, task.Id, new CreateReminderRequest { RemindAt = sooner }, default);
        var all = await service.GetForTaskAsync(user.Id, task.Id, default);

        Assert.Equal([sooner, later], all!.Select(r => r.RemindAt));
    }

    [Fact]
    public async Task OtherUsersTask_IsInvisible()
    {
        await using var db = fixture.CreateDbContext();
        var owner = await TestData.CreateUserAsync(db);
        var intruder = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, owner.Id);
        var task = await TestData.CreateTaskAsync(db, list.Id);
        var service = new ReminderService(db);
        var request = new CreateReminderRequest { RemindAt = DateTime.UtcNow.AddDays(1) };
        var reminder = await service.CreateAsync(owner.Id, task.Id, request, default);

        Assert.Null(await service.GetForTaskAsync(intruder.Id, task.Id, default));
        Assert.Null(await service.CreateAsync(intruder.Id, task.Id, request, default));
        Assert.False(await service.DeleteAsync(intruder.Id, reminder!.Id, default));
        Assert.True(await service.DeleteAsync(owner.Id, reminder.Id, default));
        Assert.False(await db.Reminders.AnyAsync(r => r.Id == reminder.Id));
    }
}
