using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NotificationService.Data;

namespace NotificationService.Tests;

// Keeps the connection open for the lifetime of an isolated relational database.
internal sealed class SqliteNotificationTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    internal NotificationDbContext Db { get; private set; } = null!;

    internal static async Task<SqliteNotificationTestDatabase> CreateAsync(params IInterceptor[] interceptors)
    {
        var fixture = new SqliteNotificationTestDatabase();
        await fixture.connection.OpenAsync();
        fixture.Db = new(new DbContextOptionsBuilder<NotificationDbContext>()
            .UseSqlite(fixture.connection).AddInterceptors(interceptors).Options);
        await fixture.Db.Database.EnsureCreatedAsync();
        return fixture;
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await connection.DisposeAsync();
    }
}
