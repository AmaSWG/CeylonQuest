using System.ComponentModel.DataAnnotations;

namespace BookingService.DTOs;

public class CancelRestaurantReservationRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}