using System.Threading;
using System.Threading.Tasks;

namespace ProviderCatalogService.Services;

public interface IEmailService
{
    Task SendApplicationRejectionEmailAsync(
        string recipientEmail,
        string businessName,
        string rejectionReason,
        CancellationToken cancellationToken = default);
}