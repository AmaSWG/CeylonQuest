using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NotificationService.Controllers;
using NotificationService.Data;
using NotificationService.DTOs;
using NotificationService.Models;
using Handler = NotificationService.Services.NotificationService;
using Xunit;

namespace NotificationService.Tests;

// SQLite executes the real SQL updates; each test owns an isolated database.
public class NotificationReadTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private NotificationDbContext db = null!;
    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        db = new(new DbContextOptionsBuilder<NotificationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
    }
    public async Task DisposeAsync() { await db.DisposeAsync(); await connection.DisposeAsync(); }
    private NotificationController Controller(string? identity, string claimType = ClaimTypes.NameIdentifier) => new(new Handler(db))
    {
        ControllerContext = new() { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(identity == null ? Array.Empty<Claim>() : new[] { new Claim(claimType, identity) }, "test")) } }
    };
    private static Notification Item(Guid user, bool read = false) => new() { RecipientUserId = user, EventKey = Guid.NewGuid().ToString(), IsRead = read };

    [Theory]
    [InlineData("list")]
    [InlineData("unread")]
    [InlineData("read")]
    [InlineData("read-all")]
    public async Task CanceledReadOperations_PropagateAndPreserveUnreadState(string operation)
    {
        var user = Guid.NewGuid(); var item = Item(user);
        db.Notifications.Add(item); await db.SaveChangesAsync();
        var service = new Handler(db); using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var ct = cancellation.Token;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            switch (operation)
            {
                case "list": await service.ListAsync(user, 1, 20, ct); break;
                case "unread": await service.UnreadAsync(user, ct); break;
                case "read": await service.ReadAsync(user, item.Id, ct); break;
                default: await service.ReadAllAsync(user, ct); break;
            }
        });
        db.ChangeTracker.Clear(); var saved = await db.Notifications.SingleAsync();
        Assert.False(saved.IsRead); Assert.Null(saved.ReadAtUtc);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadUpdate_DatabaseRejectsStatement_PropagatesAndPreservesAllRows(bool readAll)
    {
        var user = Guid.NewGuid(); var first = Item(user); var second = Item(user); var foreign = Item(Guid.NewGuid());
        db.Notifications.AddRange(first, second, foreign); await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER RejectRead BEFORE UPDATE ON Notifications
            BEGIN SELECT RAISE(ABORT, 'Simulated read persistence failure'); END;
            """);
        var service = new Handler(db);
        var error = await Assert.ThrowsAsync<SqliteException>(async () =>
        {
            if (readAll) await service.ReadAllAsync(user, default);
            else await service.ReadAsync(user, first.Id, default);
        });
        Assert.Contains("Simulated read persistence failure", error.Message);
        db.ChangeTracker.Clear(); var saved = await db.Notifications.ToListAsync();
        Assert.Equal(3, saved.Count); Assert.All(saved, n => { Assert.False(n.IsRead); Assert.Null(n.ReadAtUtc); });
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectRead;");
        var retried = readAll ? await service.ReadAllAsync(user, default) : await service.ReadAsync(user, first.Id, default);
        Assert.NotNull(retried); Assert.Equal(readAll ? 2 : 1, retried!.UpdatedCount);
        Assert.Equal(readAll ? 0 : 1, retried.UnreadCount);
        db.ChangeTracker.Clear();
        Assert.False((await db.Notifications.SingleAsync(n => n.Id == foreign.Id)).IsRead);
    }

    [Fact]
    public async Task ReadAll_ExecuteUpdate_WorksInsideTransactionAndCanBeRolledBack()
    {
        var user = Guid.NewGuid(); db.Notifications.AddRange(Item(user), Item(user)); await db.SaveChangesAsync();
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            var result = await new Handler(db).ReadAllAsync(user, default);
            Assert.Equal(2, result.UpdatedCount); Assert.Equal(0, result.UnreadCount);
            Assert.Equal(2, await db.Notifications.CountAsync(n => n.IsRead && n.ReadAtUtc != null));
            await transaction.RollbackAsync();
        }
        db.ChangeTracker.Clear();
        Assert.All(await db.Notifications.ToListAsync(), n => { Assert.False(n.IsRead); Assert.Null(n.ReadAtUtc); });
        Assert.Equal(2, await new Handler(db).UnreadAsync(user, default));
    }

    [Fact]
    public async Task IndividualRead_PersistsTimestamp_IsIdempotent_AndDoesNotReadOtherItems()
    {
        var user = Guid.NewGuid(); var first = Item(user); var second = Item(user); var foreign = Item(Guid.NewGuid());
        db.Notifications.AddRange(first, second, foreign); await db.SaveChangesAsync();
        var controller = Controller(user.ToString());
        var response = Assert.IsType<MarkReadResponse>(Assert.IsType<OkObjectResult>(await controller.Read(first.Id.ToString(), default)).Value);
        Assert.Equal(1, response.UpdatedCount); Assert.Equal(1, response.UnreadCount);
        db.ChangeTracker.Clear();
        var saved = await db.Notifications.SingleAsync(n => n.Id == first.Id);
        Assert.True(saved.IsRead); Assert.NotNull(saved.ReadAtUtc);
        var timestamp = saved.ReadAtUtc;
        response = Assert.IsType<MarkReadResponse>(Assert.IsType<OkObjectResult>(await controller.Read(first.Id.ToString(), default)).Value);
        Assert.Equal(0, response.UpdatedCount);
        db.ChangeTracker.Clear();
        Assert.Equal(timestamp, (await db.Notifications.SingleAsync(n => n.Id == first.Id)).ReadAtUtc);
        Assert.Equal(2, await db.Notifications.CountAsync(n => !n.IsRead));
        Assert.IsType<NotFoundResult>(await controller.Read(foreign.Id.ToString(), default));
        Assert.IsType<NotFoundResult>(await controller.Read(Guid.NewGuid().ToString(), default));
    }

    [Fact]
    public async Task ReadAll_UpdatesOnlyOwnersUnreadItems_AndRepeatedCallChangesNothing()
    {
        var user = Guid.NewGuid(); var alreadyRead = Item(user, true); alreadyRead.ReadAtUtc = DateTime.UtcNow.AddDays(-1);
        var original = alreadyRead.ReadAtUtc;
        db.Notifications.AddRange(Item(user), Item(user), alreadyRead, Item(Guid.NewGuid())); await db.SaveChangesAsync();
        var controller = Controller(user.ToString());
        var result = Assert.IsType<MarkReadResponse>(Assert.IsType<OkObjectResult>(await controller.ReadAll(default)).Value);
        Assert.Equal(2, result.UpdatedCount); Assert.Equal(0, result.UnreadCount);
        db.ChangeTracker.Clear();
        Assert.Equal(original, (await db.Notifications.SingleAsync(n => n.Id == alreadyRead.Id)).ReadAtUtc);
        Assert.Single(await db.Notifications.Where(n => !n.IsRead).ToListAsync());
        result = Assert.IsType<MarkReadResponse>(Assert.IsType<OkObjectResult>(await controller.ReadAll(default)).Value);
        Assert.Equal(0, result.UpdatedCount);
    }

    [Theory]
    [InlineData(null)] [InlineData("invalid")] [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task InvalidIdentity_AllActionsRejectIt(string? identity)
    {
        var controller = Controller(identity);
        Assert.IsType<UnauthorizedResult>(await controller.Get());
        Assert.IsType<UnauthorizedResult>(await controller.Unread(default));
        Assert.IsType<UnauthorizedResult>(await controller.Read(Guid.NewGuid().ToString(), default));
        Assert.IsType<UnauthorizedResult>(await controller.ReadAll(default));
    }

    [Theory]
    [InlineData(0,20)] [InlineData(-1,20)] [InlineData(1,0)] [InlineData(1,-1)] [InlineData(1,101)] [InlineData(int.MaxValue,20)]
    public async Task InvalidPagination_IsRejected(int page, int size) =>
        Assert.IsType<BadRequestObjectResult>(await Controller(Guid.NewGuid().ToString()).Get(page, size));

    [Fact]
    public async Task SubClaim_ListPaginatesNewestFirst_WithGlobalUnreadCount()
    {
        var user = Guid.NewGuid(); var now = DateTime.UtcNow;
        var items = Enumerable.Range(0,5).Select(i => { var n = Item(user, i == 0); n.CreatedAtUtc = now.AddMinutes(i); return n; }).ToList();
        db.Notifications.AddRange(items); db.Notifications.Add(Item(Guid.NewGuid())); await db.SaveChangesAsync();
        var controller = Controller(user.ToString(), "sub");
        var list = Assert.IsType<NotificationListResponse>(Assert.IsType<OkObjectResult>(await controller.Get(2,2)).Value);
        Assert.Equal(new[] { items[2].Id, items[1].Id }, list.Items.Select(n => n.Id));
        Assert.Equal(5,list.TotalCount); Assert.Equal(4,list.UnreadCount); Assert.Equal(2,list.Page);
        var count = Assert.IsType<OkObjectResult>(await controller.Unread(default));
        Assert.Equal(4, count.Value!.GetType().GetProperty("unreadCount")!.GetValue(count.Value));
        list = Assert.IsType<NotificationListResponse>(Assert.IsType<OkObjectResult>(await controller.Get(10,2)).Value);
        Assert.Empty(list.Items); Assert.Equal(5,list.TotalCount);
    }

    [Fact]
    public async Task EmptyAccount_HasNoNotificationsOrUnreadUpdates()
    {
        var user = Guid.NewGuid(); var service = new Handler(db);
        var list = await service.ListAsync(user,1,20,default);
        Assert.Empty(list.Items); Assert.Equal(0,list.TotalCount); Assert.Equal(0,list.UnreadCount);
        Assert.Equal(0,(await service.ReadAllAsync(user,default)).UpdatedCount);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("not-a-guid")]
    public async Task Read_MalformedId_ReturnsBadRequest(string id)
    {
        var controller = Controller(Guid.NewGuid().ToString());

        var result = await controller.Read(id, default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Read_UnknownValidGuid_ReturnsNotFound()
    {
        var result = await Controller(Guid.NewGuid().ToString()).Read(Guid.NewGuid().ToString(), default);
        Assert.IsType<NotFoundResult>(result);
        Assert.Empty(await db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task Read_OtherUsersNotification_ReturnsNotFoundAndPreservesUnreadState()
    {
        var foreign = Item(Guid.NewGuid());
        db.Notifications.Add(foreign);
        await db.SaveChangesAsync();

        var result = await Controller(Guid.NewGuid().ToString()).Read(foreign.Id.ToString(), default);

        Assert.IsType<NotFoundResult>(result);
        db.ChangeTracker.Clear();
        var saved = await db.Notifications.SingleAsync(n => n.Id == foreign.Id);
        Assert.False(saved.IsRead);
        Assert.Null(saved.ReadAtUtc);
    }
}
