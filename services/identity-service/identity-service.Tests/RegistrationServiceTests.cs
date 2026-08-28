using System;
using System.Threading.Tasks;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdentityService.Tests;

public class RegistrationServiceTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static RegisterRequest ValidRequest(string email = "new.user@example.com") => new()
    {
        FirstName = "Ann",
        LastName = "Perera",
        Email = email,
        PhoneNumber = "0771234567",
        Nationality = "Sri Lankan",
        Password = "Str0ng!Pass",
        ConfirmPassword = "Str0ng!Pass",
        RegistrationType = RegistrationType.Visitor
    };

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesVisitorWithHashedPassword()
    {
        using var db = CreateDbContext();
        var service = new RegistrationService(db);

        var user = await service.RegisterAsync(ValidRequest());

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(UserRole.Visitor, user.Role);
        Assert.NotEqual("Str0ng!Pass", user.PasswordHash);

        var hasher = new PasswordHasher<User>();
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, user.PasswordHash, "Str0ng!Pass"));
        Assert.Single(db.Users);
    }

    [Fact]
    public async Task RegisterAsync_TrimsWhitespaceFromTextFields()
    {
        using var db = CreateDbContext();
        var service = new RegistrationService(db);

        var request = ValidRequest();
        request.FirstName = "  Ann  ";
        request.LastName = "  Perera  ";
        request.Email = "  spaced.user@example.com  ";

        var user = await service.RegisterAsync(request);

        Assert.Equal("Ann", user.FirstName);
        Assert.Equal("Perera", user.LastName);
        Assert.Equal("spaced.user@example.com", user.Email);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsDuplicateEmailException()
    {
        using var db = CreateDbContext();
        var service = new RegistrationService(db);

        await service.RegisterAsync(ValidRequest("dup@example.com"));

        await Assert.ThrowsAsync<DuplicateEmailException>(
            () => service.RegisterAsync(ValidRequest("dup@example.com")));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_IsCaseInsensitive()
    {
        using var db = CreateDbContext();
        var service = new RegistrationService(db);

        await service.RegisterAsync(ValidRequest("Case@Example.com"));

        await Assert.ThrowsAsync<DuplicateEmailException>(
            () => service.RegisterAsync(ValidRequest("case@example.com")));
    }
}
