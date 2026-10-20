using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TodoApi.Dtos.Notes;
using TodoApi.Services;
using TodoApi.Tests.Infrastructure;

namespace TodoApi.Tests.Services;

[Collection(DatabaseCollection.Name)]
public class NoteServiceTests(ApiFixture fixture)
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 20, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task CreateAndUpdate_SetsCreatedAtAndKeepsIt()
    {
        await using var db = fixture.CreateDbContext();
        var user = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, user.Id);
        var task = await TestData.CreateTaskAsync(db, list.Id);
        var service = new NoteService(db, _clock);

        var created = await service.CreateAsync(user.Id, task.Id, new CreateNoteRequest { Content = " First " }, default);
        _clock.Advance(TimeSpan.FromMinutes(5));
        var later = await service.CreateAsync(user.Id, task.Id, new CreateNoteRequest { Content = "Second" }, default);
        var updated = await service.UpdateAsync(user.Id, created!.Id, new UpdateNoteRequest { Content = "Edited" }, default);
        var all = await service.GetForTaskAsync(user.Id, task.Id, default);

        Assert.Equal("First", created.Content);
        Assert.Equal(new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc), created.CreatedAt);
        Assert.Equal(created.CreatedAt, updated!.CreatedAt);
        Assert.Equal(["Edited", "Second"], all!.Select(n => n.Content));
        Assert.Equal(later!.Id, all![1].Id);
    }

    [Fact]
    public async Task OtherUsersTask_IsInvisible()
    {
        await using var db = fixture.CreateDbContext();
        var owner = await TestData.CreateUserAsync(db);
        var intruder = await TestData.CreateUserAsync(db);
        var list = await TestData.CreateListAsync(db, owner.Id);
        var task = await TestData.CreateTaskAsync(db, list.Id);
        var service = new NoteService(db, _clock);
        var note = await service.CreateAsync(owner.Id, task.Id, new CreateNoteRequest { Content = "Private" }, default);

        Assert.Null(await service.GetForTaskAsync(intruder.Id, task.Id, default));
        Assert.Null(await service.CreateAsync(intruder.Id, task.Id, new CreateNoteRequest { Content = "x" }, default));
        Assert.Null(await service.UpdateAsync(intruder.Id, note!.Id, new UpdateNoteRequest { Content = "x" }, default));
        Assert.False(await service.DeleteAsync(intruder.Id, note.Id, default));

        Assert.Equal("Private", (await db.Notes.AsNoTracking().SingleAsync(n => n.Id == note.Id)).Content);
        Assert.True(await service.DeleteAsync(owner.Id, note.Id, default));
        Assert.False(await db.Notes.AnyAsync(n => n.Id == note.Id));
    }
}
