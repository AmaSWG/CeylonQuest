#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using static IdentityService.Tests.ProviderActivationTestSupport;

namespace IdentityService.Tests;

public class ProviderActivationServiceTests
{
    private static ProviderActivateRequest Request(string otp = "123456") => new()
    {
        Email = "  PROVIDER@EXAMPLE.COM  ", Otp = otp, NewPassword = "NewPassword123!"
    };

    [Fact]
    public async Task ActivateAsync_ValidOtp_PersistsActivationAndHashedPassword_AndConsumesOtp()
    {
        using var db = CreateDb();
        await SeedProvider(db);
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        var service = new ProviderActivationService(db, new OtpService(), email.Object);
        var request = Request(" 123456 ");

        await service.ActivateAsync(request);

        db.ChangeTracker.Clear();
        var saved = await db.Users.SingleAsync();
        Assert.True(saved.IsActive);
        Assert.False(saved.RequiresPasswordChange);
        Assert.Null(saved.OtpCode);
        Assert.Null(saved.OtpExpiresAt);
        Assert.Equal(UserRole.Provider, saved.Role);
        Assert.NotEqual(request.NewPassword, saved.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(saved, saved.PasswordHash, request.NewPassword));
        await Assert.ThrowsAsync<InvalidOtpException>(() => service.ActivateAsync(request));
        VerifyNoEmail(email);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VerifyOtpAsync_ValidOtp_DoesNotConsumeOtpOrActivateAccount(bool withoutExpiry)
    {
        using var db = CreateDb();
        var user = await SeedProvider(db);
        if (withoutExpiry) user.OtpExpiresAt = null;
        await db.SaveChangesAsync();
        var expiry = user.OtpExpiresAt;
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        var service = new ProviderActivationService(db, new OtpService(), email.Object);

        await service.VerifyOtpAsync(" PROVIDER@EXAMPLE.COM ", " 123456 ");

        db.ChangeTracker.Clear();
        var saved = await db.Users.SingleAsync();
        Assert.Equal("123456", saved.OtpCode);
        Assert.Equal(expiry, saved.OtpExpiresAt);
        Assert.Equal("original-hash", saved.PasswordHash);
        Assert.False(saved.IsActive);
        Assert.True(saved.RequiresPasswordChange);
        VerifyNoEmail(email);
    }

    [Theory]
    [InlineData(false, "missing-user")]
    [InlineData(true, "missing-user")]
    [InlineData(false, "null-otp")]
    [InlineData(true, "null-otp")]
    [InlineData(false, "empty-otp")]
    [InlineData(true, "empty-otp")]
    [InlineData(false, "expired")]
    [InlineData(true, "expired")]
    [InlineData(false, "incorrect")]
    [InlineData(true, "incorrect")]
    [InlineData(false, "blank")]
    [InlineData(true, "blank")]
    public async Task OtpValidation_InvalidInput_RejectsWithoutChangingPersistedState(bool verifyOnly, string scenario)
    {
        using var db = CreateDb();
        User? user = null;
        if (scenario != "missing-user")
        {
            user = await SeedProvider(db);
            if (scenario == "null-otp") user.OtpCode = null;
            if (scenario == "empty-otp") user.OtpCode = "";
            if (scenario == "expired") user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(-10);
            await db.SaveChangesAsync();
        }
        var originalOtp = user?.OtpCode;
        var originalExpiry = user?.OtpExpiresAt;
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        var service = new ProviderActivationService(db, new OtpService(), email.Object);
        var otp = scenario == "incorrect" ? "654321" : scenario == "blank" ? "   " : "123456";
        Func<Task> act = () => verifyOnly
            ? service.VerifyOtpAsync(Request().Email, otp)
            : service.ActivateAsync(Request(otp));

        if (scenario == "expired") await Assert.ThrowsAsync<ExpiredOtpException>(act);
        else await Assert.ThrowsAsync<InvalidOtpException>(act);

        db.ChangeTracker.Clear();
        if (user == null) Assert.Empty(await db.Users.ToListAsync());
        else
        {
            var saved = await db.Users.SingleAsync();
            Assert.Equal(originalOtp, saved.OtpCode);
            Assert.Equal(originalExpiry, saved.OtpExpiresAt);
            Assert.Equal("original-hash", saved.PasswordHash);
            Assert.False(saved.IsActive);
            Assert.True(saved.RequiresPasswordChange);
        }
        VerifyNoEmail(email);
    }

    [Theory]
    [InlineData(UserRole.Visitor)]
    [InlineData(UserRole.Admin)]
    public async Task ResendOtpAsync_NonProvider_DoesNotChangeAccountOrSendEmail(UserRole role)
    {
        using var db = CreateDb();
        var user = await SeedProvider(db);
        user.Role = role;
        await db.SaveChangesAsync();
        var expiry = user.OtpExpiresAt;
        var email = new Mock<IEmailService>(MockBehavior.Strict);

        await new ProviderActivationService(db, new OtpService(), email.Object).ResendOtpAsync(user.Email);

        db.ChangeTracker.Clear();
        var saved = await db.Users.SingleAsync();
        Assert.Equal(role, saved.Role);
        Assert.Equal("123456", saved.OtpCode);
        Assert.Equal(expiry, saved.OtpExpiresAt);
        Assert.False(saved.IsActive);
        Assert.True(saved.RequiresPasswordChange);
        VerifyNoEmail(email);
    }

    [Fact]
    public async Task ResendOtpAsync_MissingUser_ReturnsWithoutCreatingAccountOrSendingEmail()
    {
        using var db = CreateDb();
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        await new ProviderActivationService(db, new OtpService(), email.Object).ResendOtpAsync("missing@example.com");
        Assert.Empty(await db.Users.ToListAsync());
        VerifyNoEmail(email);
    }

    [Fact]
    public async Task ResendOtpAsync_AlreadyActivated_RejectsWithoutChangingOtpOrSendingEmail()
    {
        using var db = CreateDb();
        var user = await SeedProvider(db);
        user.IsActive = true;
        user.RequiresPasswordChange = false;
        await db.SaveChangesAsync();
        var expiry = user.OtpExpiresAt;
        var email = new Mock<IEmailService>(MockBehavior.Strict);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ProviderActivationService(db, new OtpService(), email.Object).ResendOtpAsync(user.Email));

        db.ChangeTracker.Clear();
        var saved = await db.Users.SingleAsync();
        Assert.True(saved.IsActive);
        Assert.False(saved.RequiresPasswordChange);
        Assert.Equal("123456", saved.OtpCode);
        Assert.Equal(expiry, saved.OtpExpiresAt);
        VerifyNoEmail(email);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task ResendOtpAsync_PendingProvider_PersistsOtpAndEmailsExactCode(bool active, bool requiresChange)
    {
        using var db = CreateDb();
        var user = await SeedProvider(db);
        user.IsActive = active;
        user.RequiresPasswordChange = requiresChange;
        await db.SaveChangesAsync();
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        email.Setup(e => e.SendProviderOtpEmailAsync(user.Email, user.FirstName, It.IsAny<string>(), token))
            .Returns(Task.CompletedTask);
        var before = DateTime.UtcNow;

        await new ProviderActivationService(db, new OtpService(), email.Object)
            .ResendOtpAsync(" PROVIDER@EXAMPLE.COM ", token);

        var after = DateTime.UtcNow;
        db.ChangeTracker.Clear();
        var saved = await db.Users.SingleAsync();
        AssertPendingOtp(saved, before, after);
        Assert.Equal("original-hash", saved.PasswordHash);
        email.Verify(e => e.SendProviderOtpEmailAsync(saved.Email, saved.FirstName, saved.OtpCode!, token), Times.Once);
        email.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResendOtpAsync_EmailThrows_PropagatesExceptionWithOtpAlreadyPersisted()
    {
        using var db = CreateDb();
        var user = await SeedProvider(db);
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        var failure = new InvalidOperationException("Simulated email failure");
        email.Setup(e => e.SendProviderOtpEmailAsync(user.Email, user.FirstName,
            It.IsAny<string>(), CancellationToken.None)).ThrowsAsync(failure);
        var before = DateTime.UtcNow;

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ProviderActivationService(db, new OtpService(), email.Object).ResendOtpAsync(user.Email));

        Assert.Same(failure, thrown);
        db.ChangeTracker.Clear();
        var saved = await db.Users.SingleAsync();
        AssertPendingOtp(saved, before, DateTime.UtcNow);
        Assert.Equal("original-hash", saved.PasswordHash);
        email.Verify(e => e.SendProviderOtpEmailAsync(saved.Email, saved.FirstName, saved.OtpCode!, CancellationToken.None), Times.Once);
        email.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("", "123456", "Password123!", "Email")]
    [InlineData("invalid-email", "123456", "Password123!", "Email")]
    [InlineData("provider@example.com", "", "Password123!", "Otp")]
    [InlineData("provider@example.com", "123456", "", "NewPassword")]
    [InlineData("provider@example.com", "123456", "short", "NewPassword")]
    public void ProviderActivateRequest_InvalidFields_FailDeclaredValidation(string email, string otp, string password, string member)
    {
        // Data annotations are validated at the request boundary, not by ActivateAsync.
        var request = new ProviderActivateRequest { Email = email, Otp = otp, NewPassword = password };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        Assert.Contains(errors, error => System.Linq.Enumerable.Contains(error.MemberNames, member));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActivationOrResend_SaveFails_PropagatesWithoutPersistingChangesOrSendingEmail(bool resend)
    {
        var interceptor = new FailingSaveInterceptor();
        using var db = CreateDb(interceptor);
        var user = await SeedProvider(db);
        var expiry = user.OtpExpiresAt;
        interceptor.Enabled = true;
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        var service = new ProviderActivationService(db, new OtpService(), email.Object);

        var thrown = await Assert.ThrowsAsync<DbUpdateException>(() => resend
            ? service.ResendOtpAsync(user.Email) : service.ActivateAsync(Request()));

        Assert.Same(interceptor.Failure, thrown);
        db.ChangeTracker.Clear();
        var saved = await db.Users.SingleAsync();
        Assert.Equal("123456", saved.OtpCode);
        Assert.Equal(expiry, saved.OtpExpiresAt);
        Assert.Equal("original-hash", saved.PasswordHash);
        Assert.False(saved.IsActive);
        Assert.True(saved.RequiresPasswordChange);
        VerifyNoEmail(email);
    }
}
