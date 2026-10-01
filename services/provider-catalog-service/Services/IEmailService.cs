using System.Threading;
using System.Threading.Tasks;

namespace ProviderCatalogService.Services;

public interface IEmailService
{
    /// <summary>
    /// Sends a provider application rejection notification to the applicant
    /// with the supplied business name and rejection reason
    /// </summary>
    Task SendApplicationRejectionEmailAsync(
        string recipientEmail,
        string businessName,
        string rejectionReason,
        CancellationToken cancellationToken = default);
}