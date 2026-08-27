using System.Threading;
using System.Threading.Tasks;

namespace IdentityService.Services;

/// <summary>
/// Service contract specifically for password-reset email notifications.
/// Does not handle OTPs, provider registration, or password hashing.
/// </summary>
public interface IEmailService
{
    Task SendPasswordResetEmailAsync(
        string recipientEmail,
        string resetLink,
        CancellationToken cancellationToken = default);
}
