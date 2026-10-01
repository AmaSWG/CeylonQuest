using System.ComponentModel.DataAnnotations;

namespace BookingService.DTOs;

public class CancelAccommodationBookingRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}