using TodoApi.Data;
using TodoApi.Entities;

namespace TodoApi.Tests.Infrastructure;

// Inserts rows directly for service tests, bypassing the API.
public static class TestData
{
    public static async Task<User> CreateUserAsync(TodoDbContext db)
    {
        var user = new User
        {
            Name = "Service Test",
            Email = ApiClient.UniqueEmail(),
            PasswordHash = "not-used",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static async Task<TodoList> CreateListAsync(TodoDbContext db, int userId, string title = "List")
    {
        var list = new TodoList { UserId = userId, Title = title, CreatedAt = DateTime.UtcNow };
        db.TodoLists.Add(list);
        await db.SaveChangesAsync();
        return list;
    }

    public static async Task<TodoTask> CreateTaskAsync(
        TodoDbContext db,
        int listId,
        string title = "Task",
        TodoTaskStatus status = TodoTaskStatus.Todo,
        int priority = 2)
    {
        var task = new TodoTask
        {
            TodoListId = listId,
            Title = title,
            Status = status,
            Priority = priority,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task;
    }

    public static async Task<Tag> CreateTagAsync(TodoDbContext db, string name = "Tag")
    {
        var tag = new Tag { Name = name, Color = "#123456" };
        db.Tags.Add(tag);
        await db.SaveChangesAsync();
        return tag;
    }
}
