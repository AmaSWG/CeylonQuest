using BookingService.DTOs;

namespace BookingService.Services;

public interface IProviderPerformanceReportService
{
    Task<ProviderPerformanceReportResponse> GenerateAsync(
        string accessToken,
        ProviderPerformanceReportQuery query,
        CancellationToken cancellationToken = default);
}