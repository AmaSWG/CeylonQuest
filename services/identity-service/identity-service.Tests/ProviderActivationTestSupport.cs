#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using IdentityService.Data;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace IdentityService.Tests;

internal static class ProviderActivationTestSupport
{
    internal static ApplicationDbContext CreateDb(params IInterceptor[] interceptors) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).AddInterceptors(interceptors).Options);

    internal sealed class FailingSaveInterceptor : SaveChangesInterceptor
    {
        internal bool Enabled { get; set; }
        internal DbUpdateException Failure { get; } = new("Simulated persistence failure");

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Enabled) throw Failure;
            return new ValueTask<InterceptionResult<int>>(result);
        }
    }

    internal static async Task<User> SeedProvider(ApplicationDbContext db)
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Email = "Provider@example.com", FirstName = "Island Tours",
            Role = UserRole.Provider, IsActive = false, RequiresPasswordChange = true,
            PasswordHash = "original-hash", OtpCode = "123456",
            OtpExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    internal static void VerifyNoEmail(Mock<IEmailService> email)
    {
        email.Verify(e => e.SendProviderOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        email.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        email.VerifyNoOtherCalls();
    }

    internal static void AssertPendingOtp(User user, DateTime before, DateTime after)
    {
        Assert.False(user.IsActive);
        Assert.True(user.RequiresPasswordChange);
        Assert.Matches("^[0-9]{6}$", user.OtpCode!);
        Assert.NotNull(user.OtpExpiresAt);
        Assert.InRange(user.OtpExpiresAt!.Value,
            before.AddMinutes(OtpService.DefaultExpiryMinutes),
            after.AddMinutes(OtpService.DefaultExpiryMinutes));
    }
}
