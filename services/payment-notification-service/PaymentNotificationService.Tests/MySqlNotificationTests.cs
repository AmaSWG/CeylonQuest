using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using PaymentNotificationService.Data;
using PaymentNotificationService.Models;
using PaymentNotificationService.Services;
using Xunit;

namespace PaymentNotificationService.Tests;

public class MySqlFactAttribute : FactAttribute
{
    public MySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NotificationsTestConnectionString")))
            Skip = "Set NotificationsTestConnectionString to a dedicated MySQL database ending in _test.";
    }
}

public class MySqlNotificationTests
{
    [MySqlFact]
    public async Task Migration_UniqueConstraint_Ownership_AndReadUpdates_WorkOnMySql()
    {
        var connection = Environment.GetEnvironmentVariable("NotificationsTestConnectionString")!;
        var parsed = new MySqlConnectionStringBuilder(connection);
        Assert.EndsWith("_test", parsed.Database);
        var options = new DbContextOptionsBuilder<PaymentNotificationDbContext>()
            .UseMySql(connection, new MySqlServerVersion(new Version(8, 0, 30))).Options;
        await using var db = new PaymentNotificationDbContext(options);
        await db.Database.MigrateAsync();
        // All test records and read updates are rolled back. The migrated schema remains.
        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = Guid.NewGuid(); var other = Guid.NewGuid(); var key = Guid.NewGuid().ToString();
        var first = new Notification { RecipientUserId = user, EventKey = key };
        var second = new Notification { RecipientUserId = user, EventKey = key + ":second" };
        var foreign = new Notification { RecipientUserId = other, EventKey = key };
        db.Notifications.AddRange(first, second, foreign);
        await db.SaveChangesAsync();
        var service = new NotificationService(db);
        Assert.Null(await service.ReadAsync(user, foreign.Id, default));
        Assert.Equal(1, (await service.ReadAsync(user, first.Id, default))!.UpdatedCount);
        var timestamp = (await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == first.Id)).ReadAtUtc;
        Assert.Equal(0, (await service.ReadAsync(user, first.Id, default))!.UpdatedCount);
        Assert.Equal(timestamp, (await db.Notifications.AsNoTracking().SingleAsync(n => n.Id == first.Id)).ReadAtUtc);
        Assert.Equal(1, (await service.ReadAllAsync(user, default)).UpdatedCount);
        Assert.Equal(0, await service.UnreadAsync(user, default));
        Assert.Equal(1, await service.UnreadAsync(other, default));
        db.Notifications.Add(new Notification { RecipientUserId = user, EventKey = key });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        await transaction.RollbackAsync();
    }
}
