using Microsoft.EntityFrameworkCore;
using TodoApi.Data;

namespace TodoApi.Tests.Infrastructure;

// One throwaway LocalDB database per test run, migrated with the real migrations and dropped afterwards.
// Tests isolate themselves by creating their own users, so they can share the database.
public sealed class ApiFixture : IAsyncLifetime
{
    public const string TestSigningKey = "integration-tests-signing-key-at-least-32-bytes-long";

    public string ConnectionString { get; } =
        $@"Server=(localdb)\MSSQLLocalDB;Database=TodoApp_Tests_{Guid.NewGuid():N};Trusted_Connection=True;MultipleActiveResultSets=true";

    public TodoApiFactory Factory { get; private set; } = null!;

    public TodoDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<TodoDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using (var db = CreateDbContext())
        {
            await db.Database.MigrateAsync();
        }

        Factory = new TodoApiFactory(ConnectionString);
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await using var db = CreateDbContext();
        await db.Database.EnsureDeletedAsync();
    }
}

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "Database";
}
