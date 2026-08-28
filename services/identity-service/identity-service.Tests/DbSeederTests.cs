using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityService.Data;
using IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IdentityService.Tests;

public class DbSeederTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IConfiguration CreateConfiguration(string? email = null, string? password = null)
    {
        var dict = new Dictionary<string, string?>();
        if (email != null) dict["AdminSeed:Email"] = email;
        if (password != null) dict["AdminSeed:Password"] = password;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }

    [Fact]
    public async Task SeedAdminUserAsync_EmptyDb_CreatesAdminWithHashedPassword()
    {
        using var db = CreateDbContext();
        var config = CreateConfiguration();
        var logger = NullLogger.Instance;

        await DbSeeder.SeedAdminUserAsync(db, config, logger);

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == DbSeeder.DefaultAdminEmail);
        Assert.NotNull(admin);
        Assert.Equal(UserRole.Admin, admin.Role);
        Assert.True(admin.IsActive);
        Assert.False(admin.RequiresPasswordChange);

        var hasher = new PasswordHasher<User>();
        var verifyResult = hasher.VerifyHashedPassword(admin, admin.PasswordHash, DbSeeder.DefaultAdminPassword);
        Assert.Equal(PasswordVerificationResult.Success, verifyResult);
    }

    [Fact]
    public async Task SeedAdminUserAsync_ExistingAdmin_DoesNotDuplicate()
    {
        using var db = CreateDbContext();
        var config = CreateConfiguration();
        var logger = NullLogger.Instance;

        await DbSeeder.SeedAdminUserAsync(db, config, logger);
        await DbSeeder.SeedAdminUserAsync(db, config, logger);

        var count = await db.Users.CountAsync(u => u.Email == DbSeeder.DefaultAdminEmail);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SeedAdminUserAsync_CustomConfigurationCredentials_SeedsConfiguredAdmin()
    {
        using var db = CreateDbContext();
        var config = CreateConfiguration("customadmin@ceylonquest.com", "CustomSecret2026!");
        var logger = NullLogger.Instance;

        await DbSeeder.SeedAdminUserAsync(db, config, logger);

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == "customadmin@ceylonquest.com");
        Assert.NotNull(admin);
        Assert.Equal(UserRole.Admin, admin.Role);

        var hasher = new PasswordHasher<User>();
        var verifyResult = hasher.VerifyHashedPassword(admin, admin.PasswordHash, "CustomSecret2026!");
        Assert.Equal(PasswordVerificationResult.Success, verifyResult);
    }
}
