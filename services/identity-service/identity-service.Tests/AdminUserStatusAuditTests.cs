using System;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityService.Controllers;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IdentityService.Tests;

public class AdminUserStatusAuditTests
{
    private static ApplicationDbContext CreateDb() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static AdminController Controller(ApplicationDbContext db, string adminId,
        string claimType = ClaimTypes.NameIdentifier) => new(
        db, new AdminReportService(db, NullLogger<AdminReportService>.Instance))
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(claimType, adminId),
                    new Claim(ClaimTypes.Role, "Admin")
                }, "Test"))
            }
        }
    };

    [Theory]
    [InlineData(true, "UserReactivated", "Inactive", "Active")]
    [InlineData(false, "UserSuspended", "Active", "Inactive")]
    public async Task StatusChangePersistsUserAndAccurateAudit(
        bool active, string action, string previous, string next)
    {
        using var db = CreateDb();
        var admin = Guid.NewGuid();
        var user = new User { Id = Guid.NewGuid(), IsActive = !active,
            RequiresPasswordChange = true };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var before = DateTime.UtcNow;

        var result = await Controller(db, admin.ToString()).UpdateUserStatus(
            user.Id, new UpdateUserStatusRequest { IsActive = active });

        Assert.IsType<OkObjectResult>(result);
        db.ChangeTracker.Clear();
        var saved = await db.Users.SingleAsync();
        Assert.Equal(active, saved.IsActive);
        Assert.Equal(!active, saved.RequiresPasswordChange);
        var audit = await db.AdminAuditLogs.SingleAsync();
        Assert.NotEqual(Guid.Empty, audit.Id);
        Assert.Equal(action, audit.ActionType);
        Assert.Equal(admin, audit.AdministratorId);
        Assert.Equal(user.Id, audit.TargetId);
        Assert.Equal("User", audit.TargetType);
        Assert.Equal(previous, audit.PreviousStatus);
        Assert.Equal(next, audit.NewStatus);
        Assert.InRange(audit.TimestampUtc, before, DateTime.UtcNow);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RepeatedStatusRequestDoesNotDuplicateAudit(bool active)
    {
        using var db = CreateDb();
        var user = new User { Id = Guid.NewGuid(), IsActive = !active };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var controller = Controller(db, Guid.NewGuid().ToString());
        var request = new UpdateUserStatusRequest { IsActive = active };

        Assert.IsType<OkObjectResult>(await controller.UpdateUserStatus(user.Id, request));
        Assert.IsType<OkObjectResult>(await controller.UpdateUserStatus(user.Id, request));
        Assert.Single(await db.AdminAuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UnchangedStatusDoesNotCreateAudit(bool active)
    {
        using var db = CreateDb();
        var user = new User { Id = Guid.NewGuid(), IsActive = active };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        Assert.IsType<OkObjectResult>(await Controller(db, Guid.NewGuid().ToString())
            .UpdateUserStatus(user.Id, new UpdateUserStatusRequest { IsActive = active }));
        Assert.Empty(await db.AdminAuditLogs.ToListAsync());
    }

    [Fact]
    public async Task MissingUserDoesNotCreateAudit()
    {
        using var db = CreateDb();
        Assert.IsType<NotFoundObjectResult>(await Controller(db, Guid.NewGuid().ToString())
            .UpdateUserStatus(Guid.NewGuid(), new UpdateUserStatusRequest { IsActive = true }));
        Assert.Empty(await db.AdminAuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-admin")]
    public async Task InvalidAdministratorCannotChangeUserOrCreateAudit(string admin)
    {
        using var db = CreateDb();
        var user = new User { Id = Guid.NewGuid(), IsActive = false };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        Assert.IsType<UnauthorizedObjectResult>(await Controller(db, admin)
            .UpdateUserStatus(user.Id, new UpdateUserStatusRequest { IsActive = true }));
        db.ChangeTracker.Clear();
        Assert.False((await db.Users.SingleAsync()).IsActive);
        Assert.Empty(await db.AdminAuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData("sub")]
    [InlineData("nameid")]
    public async Task AlternateAdministratorClaimIsRecorded(string claimType)
    {
        using var db = CreateDb();
        var admin = Guid.NewGuid();
        var user = new User { Id = Guid.NewGuid(), IsActive = false };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        Assert.IsType<OkObjectResult>(await Controller(db, admin.ToString(), claimType)
            .UpdateUserStatus(user.Id, new UpdateUserStatusRequest { IsActive = true }));
        Assert.Equal(admin, (await db.AdminAuditLogs.SingleAsync()).AdministratorId);
    }
}
