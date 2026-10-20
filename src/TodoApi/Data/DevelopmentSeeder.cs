using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoApi.Entities;

namespace TodoApi.Data;

// Development-only sample data. Idempotent: does nothing once any user exists.
// Both sync and async entry points are required — EF tooling (dotnet ef database update) calls the sync one.
public static class DevelopmentSeeder
{
    public const string DemoEmail = "demo@todo.local";
    public const string DemoPassword = "Demo123!";

    public static void Seed(DbContext context, bool _)
    {
        if (context.Set<User>().Any())
        {
            return;
        }

        context.Add(CreateDemoUser());
        context.AddRange(CreateTags());
        context.SaveChanges();
        LinkTags(context);
        context.SaveChanges();
    }

    public static async Task SeedAsync(DbContext context, bool _, CancellationToken cancellationToken)
    {
        if (await context.Set<User>().AnyAsync(cancellationToken))
        {
            return;
        }

        context.Add(CreateDemoUser());
        context.AddRange(CreateTags());
        await context.SaveChangesAsync(cancellationToken);
        LinkTags(context);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static User CreateDemoUser()
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Name = "Demo User",
            Email = DemoEmail,
            PasswordHash = string.Empty,
            CreatedAt = now,
            TodoLists =
            [
                new TodoList
                {
                    Title = "Work",
                    Description = "Tasks for the day job",
                    CreatedAt = now,
                    Tasks =
                    [
                        new TodoTask
                        {
                            Title = "Prepare sprint demo",
                            Description = "Slides and a short live demo",
                            Status = TodoTaskStatus.InProgress,
                            Priority = 3,
                            DueDate = now.Date.AddDays(2),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Subtasks =
                            [
                                new Subtask { Title = "Draft slides", IsCompleted = true },
                                new Subtask { Title = "Rehearse demo" }
                            ],
                            Notes = [new Note { Content = "Keep it under 10 minutes.", CreatedAt = now }],
                            Reminders = [new Reminder { RemindAt = now.Date.AddDays(1).AddHours(9) }]
                        },
                        new TodoTask
                        {
                            Title = "Review pull requests",
                            Status = TodoTaskStatus.Todo,
                            Priority = 2,
                            CreatedAt = now,
                            UpdatedAt = now
                        }
                    ]
                },
                new TodoList
                {
                    Title = "Personal",
                    CreatedAt = now,
                    Tasks =
                    [
                        new TodoTask
                        {
                            Title = "Buy groceries",
                            Status = TodoTaskStatus.Todo,
                            Priority = 1,
                            DueDate = now.Date.AddDays(1),
                            CreatedAt = now,
                            UpdatedAt = now,
                            Subtasks =
                            [
                                new Subtask { Title = "Milk" },
                                new Subtask { Title = "Bread" }
                            ]
                        },
                        new TodoTask
                        {
                            Title = "Renew library card",
                            Status = TodoTaskStatus.Done,
                            Priority = 1,
                            CreatedAt = now,
                            UpdatedAt = now
                        }
                    ]
                }
            ]
        };

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, DemoPassword);
        return user;
    }

    private static Tag[] CreateTags() =>
    [
        new Tag { Name = "Urgent", Color = "#E5484D" },
        new Tag { Name = "Work", Color = "#3E63DD" },
        new Tag { Name = "Home", Color = "#30A46C" }
    ];

    // Runs after the first save so task and tag ids exist.
    private static void LinkTags(DbContext context)
    {
        var tags = context.Set<Tag>().Local.ToDictionary(t => t.Name);
        var tasks = context.Set<TodoTask>().Local.ToDictionary(t => t.Title);

        context.AddRange(
            new TaskTag { TaskId = tasks["Prepare sprint demo"].Id, TagId = tags["Urgent"].Id },
            new TaskTag { TaskId = tasks["Prepare sprint demo"].Id, TagId = tags["Work"].Id },
            new TaskTag { TaskId = tasks["Review pull requests"].Id, TagId = tags["Work"].Id },
            new TaskTag { TaskId = tasks["Buy groceries"].Id, TagId = tags["Home"].Id });
    }
}
