using System;
using System.Threading.Tasks;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdentityService.Tests;

public class UserProfileServiceTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static User SeedUser(ApplicationDbContext db, UserRole role = UserRole.Visitor)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Kamal",
            LastName = "Silva",
            Email = "kamal.silva@example.com",
            PhoneNumber = "0771234567",
            Nationality = "Sri Lankan",
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task GetProfileAsync_ExistingUser_ReturnsUserProfileResponse()
    {
        using var db = CreateDbContext();
        var user = SeedUser(db, UserRole.Visitor);
        var service = new UserProfileService(db);

        var profile = await service.GetProfileAsync(user.Id);

        Assert.NotNull(profile);
        Assert.Equal(user.Id, profile.Id);
        Assert.Equal("Kamal", profile.FirstName);
        Assert.Equal("Silva", profile.LastName);
        Assert.Equal("kamal.silva@example.com", profile.Email);
        Assert.Equal("0771234567", profile.PhoneNumber);
        Assert.Equal("Sri Lankan", profile.Nationality);
        Assert.Equal("Visitor", profile.Role);
        Assert.Equal(user.CreatedAt, profile.CreatedAt);
    }

    [Fact]
    public async Task GetProfileAsync_NonExistentUser_ThrowsUserNotFoundException()
    {
        using var db = CreateDbContext();
        var service = new UserProfileService(db);

        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            service.GetProfileAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateProfileAsync_ValidRequest_UpdatesEditableFieldsAndPreservesEmailAndRole()
    {
        using var db = CreateDbContext();
        var user = SeedUser(db, UserRole.Visitor);
        var originalEmail = user.Email;
        var originalRole = user.Role;
        var originalCreatedAt = user.CreatedAt;

        var service = new UserProfileService(db);

        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Nimal",
            LastName = "Perera",
            PhoneNumber = "0719876543",
            Nationality = "Australian"
        };

        var updated = await service.UpdateProfileAsync(user.Id, updateRequest);

        Assert.Equal("Nimal", updated.FirstName);
        Assert.Equal("Perera", updated.LastName);
        Assert.Equal("0719876543", updated.PhoneNumber);
        Assert.Equal("Australian", updated.Nationality);
        // Non-editable fields remain unchanged
        Assert.Equal(originalEmail, updated.Email);
        Assert.Equal("Visitor", updated.Role);
        Assert.Equal(originalCreatedAt, updated.CreatedAt);

        // Verify in DB directly
        var dbUser = await db.Users.FindAsync(user.Id);
        Assert.NotNull(dbUser);
        Assert.Equal("Nimal", dbUser.FirstName);
        Assert.Equal("Perera", dbUser.LastName);
        Assert.Equal("0719876543", dbUser.PhoneNumber);
        Assert.Equal("Australian", dbUser.Nationality);
        Assert.Equal(originalEmail, dbUser.Email);
        Assert.Equal(originalRole, dbUser.Role);
    }

    [Fact]
    public async Task UpdateProfileAsync_TrimsWhitespaceFromEditableFields()
    {
        using var db = CreateDbContext();
        var user = SeedUser(db);
        var service = new UserProfileService(db);

        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "  Sunil  ",
            LastName = "  Fernando  ",
            PhoneNumber = "  0785556666  ",
            Nationality = "  British  "
        };

        var updated = await service.UpdateProfileAsync(user.Id, updateRequest);

        Assert.Equal("Sunil", updated.FirstName);
        Assert.Equal("Fernando", updated.LastName);
        Assert.Equal("0785556666", updated.PhoneNumber);
        Assert.Equal("British", updated.Nationality);
    }

    [Fact]
    public async Task UpdateProfileAsync_NonExistentUser_ThrowsUserNotFoundException()
    {
        using var db = CreateDbContext();
        var service = new UserProfileService(db);

        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "NewName",
            LastName = "NewLast",
            PhoneNumber = "0770000000",
            Nationality = "Sri Lankan"
        };

        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            service.UpdateProfileAsync(Guid.NewGuid(), updateRequest));
    }
}
