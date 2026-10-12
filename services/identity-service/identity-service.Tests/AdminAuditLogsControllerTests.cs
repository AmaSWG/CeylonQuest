using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IdentityService.Controllers;
using IdentityService.Data;
using IdentityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdentityService.Tests;

public class AdminAuditLogsControllerTests
{
    private static ApplicationDbContext CreateDb() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class CatalogStub : HttpMessageHandler, IHttpClientFactory
    {
        public int Calls;
        public string Authorization;
        public Uri RequestUri;
        public string Body = "{\"items\":[],\"totalCount\":0}";
        public HttpStatusCode Status = HttpStatusCode.OK;
        public Exception Failure;

        public HttpClient CreateClient(string name)
        {
            Assert.Equal("AuditCatalog", name);
            return new HttpClient(this, false) { BaseAddress = new Uri("http://catalog/") };
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            RequestUri = request.RequestUri;
            Authorization = request.Headers.Authorization?.ToString();
            if (Failure != null) throw Failure;
            return Task.FromResult(new HttpResponseMessage(Status)
            {
                Content = new StringContent(Body, Encoding.UTF8, "application/json")
            });
        }
    }

    private static AdminAuditLogsController Controller(ApplicationDbContext db, CatalogStub stub)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer test-token";
        return new AdminAuditLogsController(db, stub)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static JsonElement Payload(IActionResult result) =>
        JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(result).Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static AdminAuditLog Entry(DateTime time, string action = "UserReactivated") => new()
    {
        TimestampUtc = time, ActionType = action, AdministratorId = Guid.NewGuid(),
        TargetId = Guid.NewGuid(), TargetType = "User",
        PreviousStatus = "Inactive", NewStatus = "Active"
    };

    [Theory]
    [InlineData(0, 25)]
    [InlineData(101, 25)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task InvalidPaginationReturnsBadRequestWithoutCatalogCall(int page, int pageSize)
    {
        using var db = CreateDb();
        using var stub = new CatalogStub();
        Assert.IsType<BadRequestObjectResult>(await Controller(db, stub).Get(
            null, null, null, null, null, page, pageSize));
        Assert.Equal(0, stub.Calls);
    }

    [Fact]
    public async Task InvalidActionAndReversedDatesAreRejected()
    {
        using var db = CreateDb();
        using var stub = new CatalogStub();
        var controller = Controller(db, stub);
        Assert.IsType<BadRequestObjectResult>(await controller.Get(
            "Unknown", null, null, null, null));
        Assert.IsType<BadRequestObjectResult>(await controller.Get(
            null, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-1)));
        Assert.Equal(0, stub.Calls);
    }

    [Fact]
    public async Task UserFiltersApplyBeforeCountingAndSkipCatalog()
    {
        using var db = CreateDb();
        using var stub = new CatalogStub();
        var time = new DateTime(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc);
        var matching = Entry(time);
        var wrongAction = Entry(time, "UserSuspended");
        wrongAction.AdministratorId = matching.AdministratorId;
        wrongAction.TargetId = matching.TargetId;
        var tooOld = Entry(time.AddDays(-1));
        tooOld.AdministratorId = matching.AdministratorId;
        tooOld.TargetId = matching.TargetId;
        var wrongAdmin = Entry(time);
        wrongAdmin.TargetId = matching.TargetId;
        var wrongTarget = Entry(time);
        wrongTarget.AdministratorId = matching.AdministratorId;
        db.AdminAuditLogs.AddRange(matching, wrongAction, tooOld, wrongAdmin, wrongTarget);
        await db.SaveChangesAsync();

        var body = Payload(await Controller(db, stub).Get("UserReactivated",
            matching.AdministratorId, matching.TargetId,
            new DateTimeOffset(time), new DateTimeOffset(time)));
        Assert.Equal(1, body.GetProperty("totalCount").GetInt32());
        Assert.Equal(matching.Id, body.GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.Equal(0, stub.Calls);
    }

    [Fact]
    public async Task CombinedRecordsAreMergedBeforePaginationAndTokenIsForwarded()
    {
        using var db = CreateDb();
        using var stub = new CatalogStub();
        var time = new DateTime(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc);
        var newest = Entry(time.AddMinutes(3));
        var oldest = Entry(time);
        db.AdminAuditLogs.AddRange(newest, oldest);
        await db.SaveChangesAsync();
        var listingId = Guid.NewGuid();
        stub.Body = JsonSerializer.Serialize(new
        {
            items = new[] { new { id = listingId, actionType = "ListingRemoved",
                timestampUtc = time.AddMinutes(2), administratorId = Guid.NewGuid(),
                targetId = Guid.NewGuid(), targetType = "Experience",
                previousStatus = "Active", newStatus = "Removed", reason = "Policy violation" } },
            totalCount = 1
        });

        var controller = Controller(db, stub);
        var first = Payload(await controller.Get(null, null, null, null, null, 1, 2));
        Assert.Equal(3, first.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, first.GetProperty("totalPages").GetInt32());
        Assert.Equal(newest.Id, first.GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.Equal(listingId, first.GetProperty("items")[1].GetProperty("id").GetGuid());
        Assert.Equal("Policy violation", first.GetProperty("items")[1].GetProperty("reason").GetString());
        Assert.EndsWith("Z", first.GetProperty("items")[0].GetProperty("timestampUtc").GetString());
        var second = Payload(await controller.Get(null, null, null, null, null, 2, 2));
        Assert.Equal(oldest.Id, second.GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.Equal(1, second.GetProperty("items").GetArrayLength());
        Assert.Equal("Bearer test-token", stub.Authorization);
        Assert.Contains("take=4", stub.RequestUri.Query);
    }

    [Fact]
    public async Task ListingOnlyFilterForwardsFiltersAndExcludesUserRecords()
    {
        using var db = CreateDb();
        using var stub = new CatalogStub();
        db.AdminAuditLogs.Add(Entry(DateTime.UtcNow));
        await db.SaveChangesAsync();
        var admin = Guid.NewGuid();
        var target = Guid.NewGuid();
        var from = new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);
        var to = from.AddHours(1);
        var body = Payload(await Controller(db, stub).Get("ListingRemoved", admin, target, from, to));
        Assert.Equal(0, body.GetProperty("totalCount").GetInt32());
        var query = Uri.UnescapeDataString(stub.RequestUri.Query);
        Assert.Contains($"administratorId={admin}", query);
        Assert.Contains($"targetId={target}", query);
        Assert.Contains($"dateFrom={from:O}", query);
        Assert.Contains($"dateTo={to:O}", query);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(500)]
    public async Task CatalogFailureDoesNotReturnPartialSuccess(int status)
    {
        using var db = CreateDb();
        using var stub = new CatalogStub { Status = (HttpStatusCode)status };
        db.AdminAuditLogs.Add(Entry(DateTime.UtcNow));
        await db.SaveChangesAsync();
        var result = Assert.IsType<ObjectResult>(await Controller(db, stub).Get(null, null, null, null, null));
        Assert.Equal(502, result.StatusCode);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("invalid json")]
    public async Task EmptyOrMalformedCatalogResponseReturnsBadGateway(string body)
    {
        using var db = CreateDb();
        using var stub = new CatalogStub { Body = body };
        Assert.Equal(502, Assert.IsType<ObjectResult>(await Controller(db, stub)
            .Get(null, null, null, null, null)).StatusCode);
    }

    [Fact]
    public async Task NetworkFailureAndTimeoutHaveDistinctErrors()
    {
        using var db = CreateDb();
        using var stub = new CatalogStub { Failure = new HttpRequestException("Offline") };
        var controller = Controller(db, stub);
        Assert.Equal(502, Assert.IsType<ObjectResult>(await controller.Get(null, null, null, null, null)).StatusCode);
        stub.Failure = new TaskCanceledException("Timeout");
        Assert.Equal(504, Assert.IsType<ObjectResult>(await controller.Get(null, null, null, null, null)).StatusCode);
    }

    [Fact]
    public async Task CallerCancellationPropagates()
    {
        using var db = CreateDb();
        using var stub = new CatalogStub { Failure = new TaskCanceledException() };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Controller(db, stub)
            .Get("ListingRemoved", null, null, null, null, cancellationToken: cancellation.Token));
    }

    [Fact]
    public void AuditAndStatusControllersDeclareAdminAuthorization()
    {
        // Direct controller unit tests do not execute ASP.NET authorization middleware.
        foreach (var type in new[] { typeof(AdminAuditLogsController), typeof(AdminController) })
        {
            var authorization = Assert.Single(type.GetCustomAttributes<AuthorizeAttribute>());
            Assert.Equal("Admin", authorization.Roles);
        }
    }
}
