using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Controllers;
using ProviderCatalogService.Data;
using ProviderCatalogService.Models;
using Xunit;

namespace ProviderCatalogService.Tests;

public class AdminAuditLogsControllerTests
{
    private static CatalogDbContext CreateDb() => new(
        new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static JsonElement Payload(IActionResult result) =>
        JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(result).Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static ListingRemovalAudit Entry(DateTime time) => new()
    {
        ListingId = Guid.NewGuid(), ListingType = "Experience",
        AdministratorId = Guid.NewGuid(), RemovedAtUtc = time,
        PreviousStatus = "Active", NewStatus = "Removed", Reason = "Policy violation"
    };

    [Fact]
    public async Task RetrievalIncludesAuditDetailsAndCountsBeforeLimiting()
    {
        using var db = CreateDb();
        var time = new DateTime(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc);
        var newest = Entry(time);
        db.ListingRemovalAudits.AddRange(newest, Entry(time.AddHours(-1)));
        await db.SaveChangesAsync();
        var body = Payload(await new AdminAuditLogsController(db).Get(
            null, null, null, null, take: 1));
        Assert.Equal(2, body.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, body.GetProperty("items").GetArrayLength());
        var item = body.GetProperty("items")[0];
        Assert.Equal(newest.Id, item.GetProperty("id").GetGuid());
        Assert.Equal("ListingRemoved", item.GetProperty("actionType").GetString());
        Assert.Equal(newest.AdministratorId, item.GetProperty("administratorId").GetGuid());
        Assert.Equal(newest.ListingId, item.GetProperty("targetId").GetGuid());
        Assert.Equal("Experience", item.GetProperty("targetType").GetString());
        Assert.Equal("Active", item.GetProperty("previousStatus").GetString());
        Assert.Equal("Removed", item.GetProperty("newStatus").GetString());
        Assert.Equal(newest.Reason, item.GetProperty("reason").GetString());
        Assert.Equal(time, item.GetProperty("timestampUtc").GetDateTime());
    }

    [Fact]
    public async Task FiltersUseAdministratorTargetAndInclusiveDateBounds()
    {
        using var db = CreateDb();
        var time = new DateTime(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc);
        var matching = Entry(time);
        var old = Entry(time.AddMinutes(-1));
        old.AdministratorId = matching.AdministratorId;
        old.ListingId = matching.ListingId;
        var later = Entry(time.AddMinutes(1));
        later.AdministratorId = matching.AdministratorId;
        later.ListingId = matching.ListingId;
        var wrongAdmin = Entry(time);
        wrongAdmin.ListingId = matching.ListingId;
        var wrongTarget = Entry(time);
        wrongTarget.AdministratorId = matching.AdministratorId;
        db.ListingRemovalAudits.AddRange(matching, old, later, wrongAdmin, wrongTarget);
        await db.SaveChangesAsync();
        var body = Payload(await new AdminAuditLogsController(db).Get(
            matching.AdministratorId, matching.ListingId,
            new DateTimeOffset(time), new DateTimeOffset(time)));
        Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
        Assert.Equal(matching.Id, body.GetProperty("items")[0].GetProperty("id").GetGuid());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10001)]
    public async Task InvalidLimitReturnsBadRequest(int take)
    {
        using var db = CreateDb();
        Assert.IsType<BadRequestObjectResult>(await new AdminAuditLogsController(db)
            .Get(null, null, null, null, take));
    }

    [Fact]
    public async Task ReversedDatesReturnBadRequest()
    {
        using var db = CreateDb();
        var time = DateTimeOffset.UtcNow;
        Assert.IsType<BadRequestObjectResult>(await new AdminAuditLogsController(db)
            .Get(null, null, time, time.AddDays(-1)));
    }

    [Fact]
    public async Task NoMatchingRecordsReturnsEmptySuccess()
    {
        using var db = CreateDb();
        var body = Payload(await new AdminAuditLogsController(db).Get(null, null, null, null));
        Assert.Equal(0, body.GetProperty("totalCount").GetInt32());
        Assert.Equal(0, body.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public void ControllerDeclaresAdminOnlyAuthorization()
    {
        var authorization = Assert.Single(typeof(AdminAuditLogsController)
            .GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal("Admin", authorization.Roles);
    }
}
