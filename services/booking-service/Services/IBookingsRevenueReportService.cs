using BookingService.DTOs;

namespace BookingService.Services;

public interface IBookingsRevenueReportService
{
    Task<BookingsRevenueReportResponse> GenerateAsync(string accessToken, BookingsRevenueReportQuery query);
}
