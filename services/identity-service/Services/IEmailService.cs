using System.Threading;
using System.Threading.Tasks;

namespace IdentityService.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(
        string recipientEmail,
        string resetLink,
        CancellationToken cancellationToken = default);
}
