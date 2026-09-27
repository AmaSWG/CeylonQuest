using BookingService.DTOs;

namespace BookingService.Services;

public interface IAdminBookingsRevenueReportService
{
    Task<AdminBookingsRevenueReportResponse> GenerateAsync(string accessToken, AdminBookingsRevenueReportQuery query);
}