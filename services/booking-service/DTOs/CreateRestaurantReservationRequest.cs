using System.ComponentModel.DataAnnotations;

namespace BookingService.DTOs;

public class CreateRestaurantReservationRequest
{
    [Required]
    public Guid RestaurantId { get; set; }

    [Required]
    public DateOnly ReservationDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string TimeSlot { get; set; } = string.Empty;

    [Required]
    [Range(1, 100)]
    public int PartySize { get; set; }
}