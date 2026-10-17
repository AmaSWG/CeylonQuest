using System;
using System.Threading;
using System.Threading.Tasks;
using IdentityService.Events;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using static IdentityService.Tests.ProviderActivationTestSupport;

namespace IdentityService.Tests;

public class ProviderAccountActivationServiceTests
{
    private static ProviderApproved Approval() => new()
    {
        ApplicationId = Guid.NewGuid(), Email = "  PROVIDER@EXAMPLE.COM  ",
        BusinessName = "  Approved Tours  ", ApprovedAt = DateTime.UtcNow
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActivateApprovedProviderAsync_NewOrExistingAccount_PersistsPendingProviderAndSendsOtp(bool existing)
    {
        using var db = CreateDb();
        var existingId = Guid.Empty;
        if (existing)
        {
            var user = await SeedProvider(db);
            existingId = user.Id;
            user.Role = UserRole.Visitor;
            user.IsActive = true;
            user.RequiresPasswordChange = false;
            await db.SaveChangesAsync();
        }
        var approved = Approval();
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var expectedEmail = existing ? "Provider@example.com" : approved.Email.Trim();
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        email.Setup(e => e.SendProviderOtpEmailAsync(expectedEmail, approved.BusinessName, It.IsAny<string>(), token))
            .Returns(Task.CompletedTask);
        var service = new ProviderAccountActivationService(db, email.Object, new OtpService(),
            NullLogger<ProviderAccountActivationService>.Instance);
        var before = DateTime.UtcNow;

        await service.ActivateApprovedProviderAsync(approved, token);

        var after = DateTime.UtcNow;
        db.ChangeTracker.Clear();
        var saved = await db.Users.SingleAsync();
        AssertPendingOtp(saved, before, after);
        Assert.Equal(UserRole.Provider, saved.Role);
        Assert.Equal(expectedEmail, saved.Email);
        Assert.NotEqual(Guid.Empty, saved.Id);
        Assert.Equal(existing ? "Island Tours" : "Approved Tours", saved.FirstName);
        Assert.Equal(existing ? "original-hash" : "", saved.PasswordHash);
        if (existing) Assert.Equal(existingId, saved.Id);
        email.Verify(e => e.SendProviderOtpEmailAsync(expectedEmail, approved.BusinessName, saved.OtpCode, token), Times.Once);
        email.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActivateApprovedProviderAsync_EmailThrows_PropagatesWithAccountAlreadyPersisted(bool existing)
    {
        using var db = CreateDb();
        if (existing) await SeedProvider(db);
        var approved = Approval();
        var expectedEmail = existing ? "Provider@example.com" : approved.Email.Trim();
        var failure = new InvalidOperationException("Simulated email failure");
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        email.Setup(e => e.SendProviderOtpEmailAsync(expectedEmail, approved.BusinessName,
            It.IsAny<string>(), CancellationToken.None)).ThrowsAsync(failure);
        var service = new ProviderAccountActivationService(db, email.Object, new OtpService(),
            NullLogger<ProviderAccountActivationService>.Instance);
        var before = DateTime.UtcNow;

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ActivateApprovedProviderAsync(approved));

        Assert.Same(failure, thrown);
        db.ChangeTracker.Clear();
        var saved = await db.Users.SingleAsync();
        Assert.Equal(UserRole.Provider, saved.Role);
        Assert.Equal(expectedEmail, saved.Email);
        AssertPendingOtp(saved, before, DateTime.UtcNow);
        email.Verify(e => e.SendProviderOtpEmailAsync(expectedEmail, approved.BusinessName,
            saved.OtpCode, CancellationToken.None), Times.Once);
        email.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ActivateApprovedProviderAsync_SaveFails_DoesNotCreateAccountOrSendEmail()
    {
        var interceptor = new FailingSaveInterceptor { Enabled = true };
        using var db = CreateDb(interceptor);
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        var service = new ProviderAccountActivationService(db, email.Object, new OtpService(),
            NullLogger<ProviderAccountActivationService>.Instance);

        var thrown = await Assert.ThrowsAsync<DbUpdateException>(() => service.ActivateApprovedProviderAsync(Approval()));

        Assert.Same(interceptor.Failure, thrown);
        db.ChangeTracker.Clear();
        Assert.Empty(await db.Users.ToListAsync());
        VerifyNoEmail(email);
    }
}
