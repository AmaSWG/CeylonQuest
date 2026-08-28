using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IdentityService.Tests;

public class PasswordResetEmailTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IConfiguration CreateConfiguration(string baseUrl = "http://localhost:5173/reset-password", string env = "Development")
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Email:ResetPasswordBaseUrl", baseUrl },
            { "Email:Host", "" },
            { "Email:Port", "587" },
            { "Email:Username", "" },
            { "Email:Password", "" },
            { "Email:From", "noreply@ceylonquest.com" },
            { "Email:FromName", "CeylonQuest" },
            { "ASPNETCORE_ENVIRONMENT", env }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    private static User SeedUser(ApplicationDbContext db, string email = "user@example.com")
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
            Role = UserRole.Visitor,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, "OldPassword123!");
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task InitiateForgotPasswordAsync_RegisteredEmail_SendsPasswordResetEmail()
    {
        // Arrange
        using var db = CreateDbContext();
        var config = CreateConfiguration();
        var tokenService = new PasswordResetTokenService(db);
        var emailService = new FakeEmailService();
        var logger = new CapturingLogger<PasswordResetService>();

        var registeredEmail = "registered@ceylonquest.com";
        SeedUser(db, registeredEmail);

        var service = new PasswordResetService(db, tokenService, emailService, config, logger);

        // Act
        var (result, token) = await service.InitiateForgotPasswordAsync(registeredEmail);

        // Assert
        Assert.True(result);
        Assert.NotNull(token);
        Assert.Equal(1, emailService.SentCount);
        Assert.Equal(registeredEmail, emailService.LastRecipient);
        Assert.NotNull(emailService.LastResetLink);
        Assert.StartsWith("http://localhost:5173/reset-password?token=", emailService.LastResetLink);
        Assert.Contains(Uri.EscapeDataString(token), emailService.LastResetLink);
    }

    [Fact]
    public async Task InitiateForgotPasswordAsync_UnknownEmail_DoesNotSendEmail()
    {
        // Arrange
        using var db = CreateDbContext();
        var config = CreateConfiguration();
        var tokenService = new PasswordResetTokenService(db);
        var emailService = new FakeEmailService();
        var logger = new CapturingLogger<PasswordResetService>();

        var service = new PasswordResetService(db, tokenService, emailService, config, logger);

        // Act
        var (result, token) = await service.InitiateForgotPasswordAsync("unknown@nonexistent.com");

        // Assert
        Assert.True(result); // Returns true to prevent user enumeration
        Assert.Null(token); // No token generated for unknown email
        Assert.Equal(0, emailService.SentCount);
    }

    [Theory]
    [InlineData("existing@ceylonquest.com", true)]
    [InlineData("notfound@ceylonquest.com", false)]
    public async Task InitiateForgotPasswordAsync_ReturnsGenericSuccess_RegardlessOfAccountExistence(string email, bool shouldSeed)
    {
        // Arrange
        using var db = CreateDbContext();
        var config = CreateConfiguration();
        var tokenService = new PasswordResetTokenService(db);
        var emailService = new FakeEmailService();
        var logger = new CapturingLogger<PasswordResetService>();

        if (shouldSeed)
        {
            SeedUser(db, email);
        }

        var service = new PasswordResetService(db, tokenService, emailService, config, logger);

        // Act
        var (result, _) = await service.InitiateForgotPasswordAsync(email);

        // Assert: Both return true (same generic outcome)
        Assert.True(result);
    }

    [Fact]
    public async Task InitiateForgotPasswordAsync_ConstructsCorrectResetUrlWithConfiguredBaseUrl()
    {
        // Arrange
        using var db = CreateDbContext();
        var customBaseUrl = "https://app.ceylonquest.com/auth/reset";
        var config = CreateConfiguration(customBaseUrl);
        var tokenService = new PasswordResetTokenService(db);
        var emailService = new FakeEmailService();
        var logger = new CapturingLogger<PasswordResetService>();

        var user = SeedUser(db, "customurl@test.com");

        var service = new PasswordResetService(db, tokenService, emailService, config, logger);

        // Act
        await service.InitiateForgotPasswordAsync(user.Email);

        // Assert
        Assert.NotNull(emailService.LastResetLink);
        Assert.StartsWith($"{customBaseUrl}?token=", emailService.LastResetLink);
        var tokenParam = emailService.LastResetLink.Substring($"{customBaseUrl}?token=".Length);
        Assert.False(string.IsNullOrWhiteSpace(tokenParam));
    }

    [Fact]
    public async Task EmailService_DevelopmentFallback_LogsResetLinkWithoutCrashingWhenSmtpNotConfigured()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Email:Host", "" },
            { "Email:Username", "" },
            { "Email:Password", "" },
            { "ASPNETCORE_ENVIRONMENT", "Development" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var logger = new CapturingLogger<EmailService>();
        var env = new FakeHostEnvironment { EnvironmentName = "Development" };

        var emailService = new EmailService(config, logger, env);
        var recipient = "devuser@example.com";
        var resetLink = "http://localhost:5173/reset-password?token=testtoken123";

        // Act - should execute without throwing exception
        var exception = await Record.ExceptionAsync(() => emailService.SendPasswordResetEmailAsync(recipient, resetLink));

        // Assert
        Assert.Null(exception);
        Assert.Contains(logger.LogEntries, entry => entry.Message.Contains("[DEV] Reset link:") && entry.Message.Contains(resetLink));
    }

    [Fact]
    public async Task EmailService_ProductionMode_DoesNotLogResetLinkWhenSmtpMissing()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Email:Host", "" },
            { "Email:Username", "" },
            { "Email:Password", "" },
            { "ASPNETCORE_ENVIRONMENT", "Production" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var logger = new CapturingLogger<EmailService>();
        var env = new FakeHostEnvironment { EnvironmentName = "Production" };

        var emailService = new EmailService(config, logger, env);
        var recipient = "produser@example.com";
        var resetLink = "https://ceylonquest.com/reset-password?token=secrettoken456";

        // Act
        await emailService.SendPasswordResetEmailAsync(recipient, resetLink);

        // Assert: Verify [DEV] log was NEVER called and resetLink is NOT in logs
        Assert.DoesNotContain(logger.LogEntries, entry => entry.Message.Contains("[DEV]"));
        Assert.DoesNotContain(logger.LogEntries, entry => entry.Message.Contains(resetLink));
    }
}

public class FakeEmailService : IEmailService
{
    public int SentCount { get; private set; }
    public string? LastRecipient { get; private set; }
    public string? LastResetLink { get; private set; }

    public Task SendPasswordResetEmailAsync(string recipientEmail, string resetLink, CancellationToken cancellationToken = default)
    {
        SentCount++;
        LastRecipient = recipientEmail;
        LastResetLink = resetLink;
        return Task.CompletedTask;
    }
}

public class CapturingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> LogEntries { get; } = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var msg = formatter(state, exception);
        LogEntries.Add((logLevel, msg));
    }
}

public class FakeHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "IdentityService";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}
