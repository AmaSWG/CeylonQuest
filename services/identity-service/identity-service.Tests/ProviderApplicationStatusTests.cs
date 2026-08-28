using System;
using System.Threading.Tasks;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdentityService.Tests;

public class ProviderApplicationStatusTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetStatusByEmailAsync_PendingApplication_ReturnsPendingStatus()
    {
        using var db = CreateDbContext();
        var service = new ProviderApplicationService(db);

        var app = new ProviderApplication
        {
            Id = Guid.NewGuid(),
            FirstName = "Kasun",
            LastName = "Silva",
            Email = "kasun.safari@example.com",
            PhoneNumber = "0771122334",
            BusinessName = "Yala Safari Expeditions",
            ServiceType = "Safari Tour Operator",
            Location = "Yala, Sri Lanka",
            Description = "Safari tours across Yala National Park.",
            Status = ProviderApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.ProviderApplications.Add(app);
        await db.SaveChangesAsync();

        var result = await service.GetStatusByEmailAsync("kasun.safari@example.com");

        Assert.NotNull(result);
        Assert.Equal("kasun.safari@example.com", result.Email);
        Assert.Equal("Yala Safari Expeditions", result.BusinessName);
        Assert.Equal("Safari Tour Operator", result.ServiceType);
        Assert.Equal("Pending", result.Status);
        Assert.Null(result.RejectionReason);
        Assert.Contains("under review", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetStatusByEmailAsync_ApprovedApplication_ReturnsApprovedStatus()
    {
        using var db = CreateDbContext();
        var service = new ProviderApplicationService(db);

        var app = new ProviderApplication
        {
            Id = Guid.NewGuid(),
            FirstName = "Nimal",
            LastName = "Fernando",
            Email = "nimal.guide@example.com",
            PhoneNumber = "0719876543",
            BusinessName = "Ceylon Heritage Walks",
            ServiceType = "Tour Guide",
            Location = "Kandy, Sri Lanka",
            Description = "Cultural and heritage tours in Kandy.",
            Status = ProviderApplicationStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };
        db.ProviderApplications.Add(app);
        await db.SaveChangesAsync();

        var result = await service.GetStatusByEmailAsync("NIMAL.GUIDE@EXAMPLE.COM");

        Assert.NotNull(result);
        Assert.Equal("nimal.guide@example.com", result.Email);
        Assert.Equal("Ceylon Heritage Walks", result.BusinessName);
        Assert.Equal("Approved", result.Status);
        Assert.Null(result.RejectionReason);
        Assert.Contains("approved", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetStatusByEmailAsync_RejectedApplication_ReturnsRejectionReason()
    {
        using var db = CreateDbContext();
        var service = new ProviderApplicationService(db);

        var app = new ProviderApplication
        {
            Id = Guid.NewGuid(),
            FirstName = "Saman",
            LastName = "Kumara",
            Email = "saman.hotel@example.com",
            PhoneNumber = "0755554433",
            BusinessName = "Southern Coral Inn",
            ServiceType = "Accommodation",
            Location = "Mirissa, Sri Lanka",
            Description = "Boutique hotel near beach.",
            Status = ProviderApplicationStatus.Rejected,
            RejectionReason = "Provided business registration license has expired. Please re-submit with a valid SLTDA certificate.",
            CreatedAt = DateTime.UtcNow
        };
        db.ProviderApplications.Add(app);
        await db.SaveChangesAsync();

        var result = await service.GetStatusByEmailAsync("saman.hotel@example.com");

        Assert.NotNull(result);
        Assert.Equal("saman.hotel@example.com", result.Email);
        Assert.Equal("Southern Coral Inn", result.BusinessName);
        Assert.Equal("Rejected", result.Status);
        Assert.Equal("Provided business registration license has expired. Please re-submit with a valid SLTDA certificate.", result.RejectionReason);
        Assert.Contains("not approved", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetStatusByEmailAsync_NonExistentEmail_ReturnsNull()
    {
        using var db = CreateDbContext();
        var service = new ProviderApplicationService(db);

        var result = await service.GetStatusByEmailAsync("nonexistent.provider@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetStatusByEmailAsync_EmptyOrWhitespaceEmail_ReturnsNull()
    {
        using var db = CreateDbContext();
        var service = new ProviderApplicationService(db);

        var result1 = await service.GetStatusByEmailAsync("");
        var result2 = await service.GetStatusByEmailAsync("   ");

        Assert.Null(result1);
        Assert.Null(result2);
    }

    [Fact]
    public async Task GetStatusByEmailAsync_MultipleApplications_ReturnsLatestOne()
    {
        using var db = CreateDbContext();
        var service = new ProviderApplicationService(db);

        var appOld = new ProviderApplication
        {
            Id = Guid.NewGuid(),
            Email = "repeat.applicant@example.com",
            BusinessName = "Old Business Name",
            ServiceType = "Tour Guide",
            Status = ProviderApplicationStatus.Rejected,
            RejectionReason = "Incomplete documentation",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        var appNew = new ProviderApplication
        {
            Id = Guid.NewGuid(),
            Email = "repeat.applicant@example.com",
            BusinessName = "New Business Name",
            ServiceType = "Tour Guide",
            Status = ProviderApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.ProviderApplications.AddRange(appOld, appNew);
        await db.SaveChangesAsync();

        var result = await service.GetStatusByEmailAsync("repeat.applicant@example.com");

        Assert.NotNull(result);
        Assert.Equal("New Business Name", result.BusinessName);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task GetStatusByEmailAsync_ExistingProviderUserInUsersTable_ReturnsApprovedStatus()
    {
        using var db = CreateDbContext();
        var service = new ProviderApplicationService(db);

        var provider = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Anura",
            LastName = "Bandara",
            Email = "anura.tours@example.com",
            PhoneNumber = "0779988776",
            Nationality = "Sri Lankan",
            PasswordHash = "hashed_pass",
            Role = UserRole.Provider, // Role = 1
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(provider);
        await db.SaveChangesAsync();

        var result = await service.GetStatusByEmailAsync("anura.tours@example.com");

        Assert.NotNull(result);
        Assert.Equal("anura.tours@example.com", result.Email);
        Assert.Equal("Approved", result.Status);
        Assert.Contains("approved", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
