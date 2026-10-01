using System.ComponentModel.DataAnnotations;

namespace BookingService.DTOs;

public class CancelBookingRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}