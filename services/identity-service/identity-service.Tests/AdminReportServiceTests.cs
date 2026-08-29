using System;
using System.Threading.Tasks;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IdentityService.Tests;

/// <summary>
/// Unit tests for AdminReportService.
/// These tests use the InMemory EF Core provider, so the service's LINQ fallback path is exercised.
/// The ADO.NET path is validated by the integration/manual test against the real MySQL database.
/// </summary>
public class AdminReportServiceTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static AdminReportService CreateService(ApplicationDbContext db)
        => new AdminReportService(db, NullLogger<AdminReportService>.Instance);

    // ── Seed helpers ─────────────────────────────────────────────────────────

    private static void SeedUser(
        ApplicationDbContext db,
        string email,
        UserRole role      = UserRole.Visitor,
        bool isActive      = true,
        DateTime? createdAt = null)
    {
        db.Users.Add(new User
        {
            Id           = Guid.NewGuid(),
            FirstName    = "Test",
            LastName     = "User",
            Email        = email,
            PhoneNumber  = "0771234567",
            Nationality  = "Sri Lankan",
            PasswordHash = "hash",
            Role         = role,
            IsActive     = isActive,
            CreatedAt    = createdAt ?? DateTime.UtcNow
        });
        db.SaveChanges();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Registration Summary Tests
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetReportAsync_NoFilters_ReturnsCorrectTotals()
    {
        using var db = CreateDbContext();
        var svc = CreateService(db);

        SeedUser(db, "visitor1@test.com",  UserRole.Visitor,  isActive: true);
        SeedUser(db, "visitor2@test.com",  UserRole.Visitor,  isActive: false);
        SeedUser(db, "provider@test.com",  UserRole.Provider, isActive: true);
        SeedUser(db, "admin@test.com",     UserRole.Admin,    isActive: true);

        var report = await svc.GetReportAsync(new ReportQueryParams());

        Assert.Equal(4, report.Registrations.TotalUsers);
        Assert.Equal(2, report.Registrations.TotalVisitors);
        Assert.Equal(1, report.Registrations.TotalProviders);
        Assert.Equal(1, report.Registrations.TotalAdmins);
        Assert.Equal(3, report.Registrations.ActiveUsers);
        Assert.Equal(1, report.Registrations.InactiveUsers);
    }

    [Fact]
    public async Task GetReportAsync_RoleFilter_ReturnsOnlyMatchingRole()
    {
        using var db = CreateDbContext();
        var svc = CreateService(db);

        SeedUser(db, "visitor1@test.com", UserRole.Visitor);
        SeedUser(db, "visitor2@test.com", UserRole.Visitor);
        SeedUser(db, "provider@test.com", UserRole.Provider);

        var report = await svc.GetReportAsync(new ReportQueryParams { Role = "Visitor" });

        Assert.Equal(2, report.Registrations.TotalUsers);
        Assert.Equal(2, report.Registrations.TotalVisitors);
        Assert.Equal(0, report.Registrations.TotalProviders);
    }

    [Fact]
    public async Task GetReportAsync_DateFromFilter_ExcludesRecordsBeforeDate()
    {
        using var db = CreateDbContext();
        var svc = CreateService(db);

        var past   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var recent = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        SeedUser(db, "old@test.com",    createdAt: past);
        SeedUser(db, "recent@test.com", createdAt: recent);

        var report = await svc.GetReportAsync(new ReportQueryParams
        {
            DateFrom = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Only the record from August should be included
        Assert.Equal(1, report.Registrations.TotalUsers);
    }

    [Fact]
    public async Task GetReportAsync_DateToFilter_ExcludesRecordsAfterDate()
    {
        using var db = CreateDbContext();
        var svc = CreateService(db);

        var old    = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var future = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);

        SeedUser(db, "old@test.com",    createdAt: old);
        SeedUser(db, "future@test.com", createdAt: future);

        var report = await svc.GetReportAsync(new ReportQueryParams
        {
            DateTo = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc)
        });

        // Only the old record should be included
        Assert.Equal(1, report.Registrations.TotalUsers);
    }

    [Fact]
    public async Task GetReportAsync_DateRangeFilter_ReturnsOnlyRecordsInRange()
    {
        using var db = CreateDbContext();
        var svc = CreateService(db);

        SeedUser(db, "jan@test.com", createdAt: new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));
        SeedUser(db, "mar@test.com", createdAt: new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc));
        SeedUser(db, "aug@test.com", createdAt: new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc));

        var report = await svc.GetReportAsync(new ReportQueryParams
        {
            DateFrom = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTo   = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc)
        });

        // Only March record falls in Feb–Jul range
        Assert.Equal(1, report.Registrations.TotalUsers);
    }

    [Fact]
    public async Task GetReportAsync_NoUsers_ReturnsZeroTotals()
    {
        using var db = CreateDbContext();
        var svc = CreateService(db);

        var report = await svc.GetReportAsync(new ReportQueryParams());

        Assert.Equal(0, report.Registrations.TotalUsers);
        Assert.Equal(0, report.Registrations.ActiveUsers);
        Assert.Equal(0, report.Registrations.InactiveUsers);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // General Report Tests
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetReportAsync_GeneratedAt_IsRecentUtcTimestamp()
    {
        using var db = CreateDbContext();
        var svc = CreateService(db);
        var before = DateTime.UtcNow.AddSeconds(-1);

        var report = await svc.GetReportAsync(new ReportQueryParams());

        Assert.True(report.GeneratedAt >= before);
        Assert.True(report.GeneratedAt <= DateTime.UtcNow.AddSeconds(2));
    }

    [Fact]
    public async Task GetReportAsync_FiltersEchoedBackInResponse()
    {
        using var db = CreateDbContext();
        var svc = CreateService(db);

        var filters = new ReportQueryParams
        {
            DateFrom          = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTo            = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Role              = "Visitor",
            ApplicationStatus = "Pending"
        };

        var report = await svc.GetReportAsync(filters);

        Assert.Equal(filters.DateFrom,          report.AppliedFilters.DateFrom);
        Assert.Equal(filters.DateTo,            report.AppliedFilters.DateTo);
        Assert.Equal(filters.Role,              report.AppliedFilters.Role);
        Assert.Equal(filters.ApplicationStatus, report.AppliedFilters.ApplicationStatus);
    }
}
