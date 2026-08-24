using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IdentityService.Tests;

public class AuthServiceTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IConfiguration CreateConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Key", "unit_test_secret_key_1234567890_long_enough_32_bytes" },
            { "Jwt:Issuer", "CeylonQuestTest" },
            { "Jwt:Audience", "CeylonQuestTestAudience" },
            { "Jwt:ExpiryMinutes", "60" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    private static User SeedUser(
        ApplicationDbContext db,
        string email = "user@example.com",
        string password = "Password123!",
        UserRole role = UserRole.Visitor,
        bool isActive = true,
        bool requiresPasswordChange = false)
    {
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            Email = email,
            PhoneNumber = "0771234567",
            Nationality = "Sri Lankan",
            Role = role,
            IsActive = isActive,
            RequiresPasswordChange = requiresPasswordChange,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task AuthenticateAsync_ValidVisitorCredentials_ReturnsTokenAndVisitorRole()
    {
        using var db = CreateDbContext();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        SeedUser(db, "visitor@example.com", "Password123!", UserRole.Visitor);

        var req = new LoginRequest { Email = "visitor@example.com", Password = "Password123!" };
        var response = await service.AuthenticateAsync(req);

        Assert.NotNull(response);
        Assert.Equal("Visitor", response.Role);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(response.AccessToken);
        Assert.Equal("Visitor", jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value);
        Assert.Equal("visitor@example.com", jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value);
    }

    [Fact]
    public async Task AuthenticateAsync_ValidProviderCredentials_ReturnsTokenAndProviderRole()
    {
        using var db = CreateDbContext();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        SeedUser(db, "provider@example.com", "SecurePass123!", UserRole.Provider);

        var req = new LoginRequest { Email = "provider@example.com", Password = "SecurePass123!" };
        var response = await service.AuthenticateAsync(req);

        Assert.NotNull(response);
        Assert.Equal("Provider", response.Role);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(response.AccessToken);
        Assert.Equal("Provider", jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value);
    }

    [Fact]
    public async Task AuthenticateAsync_EmailTrimmingAndCaseInsensitive_AuthenticatesSuccessfully()
    {
        using var db = CreateDbContext();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        SeedUser(db, "user@example.com", "Password123!");

        var req = new LoginRequest { Email = "  USER@EXAMPLE.COM  ", Password = "Password123!" };
        var response = await service.AuthenticateAsync(req);

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
    }

    [Fact]
    public async Task AuthenticateAsync_NonExistentEmail_ThrowsUnauthorizedAccessException()
    {
        using var db = CreateDbContext();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        var req = new LoginRequest { Email = "nonexistent@example.com", Password = "Password123!" };
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AuthenticateAsync(req));
        Assert.Equal("Invalid credentials", ex.Message);
    }

    [Fact]
    public async Task AuthenticateAsync_IncorrectPassword_ThrowsUnauthorizedAccessException()
    {
        using var db = CreateDbContext();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        SeedUser(db, "user@example.com", "Password123!");

        var req = new LoginRequest { Email = "user@example.com", Password = "WrongPassword999!" };
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AuthenticateAsync(req));
        Assert.Equal("Invalid credentials", ex.Message);
    }

    [Fact]
    public async Task AuthenticateAsync_InactiveAccount_ThrowsUnauthorizedAccessException()
    {
        using var db = CreateDbContext();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        SeedUser(db, "inactive@example.com", "Password123!", isActive: false);

        var req = new LoginRequest { Email = "inactive@example.com", Password = "Password123!" };
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AuthenticateAsync(req));
        Assert.Equal("Account is not active", ex.Message);
    }

    [Fact]
    public async Task AuthenticateAsync_RequiresPasswordActivation_ThrowsUnauthorizedAccessException()
    {
        using var db = CreateDbContext();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        SeedUser(db, "unactivated@example.com", "Password123!", isActive: true, requiresPasswordChange: true);

        var req = new LoginRequest { Email = "unactivated@example.com", Password = "Password123!" };
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AuthenticateAsync(req));
        Assert.Equal("Account requires activation before login", ex.Message);
    }
}
