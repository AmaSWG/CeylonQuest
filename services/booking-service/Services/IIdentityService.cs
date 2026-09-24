using BookingService.DTOs;

namespace BookingService.Services;

public interface IIdentityService
{
    Task<BookingCustomerResponse?> GetBookingCustomerAsync(
        Guid customerId,
        string accessToken);
}