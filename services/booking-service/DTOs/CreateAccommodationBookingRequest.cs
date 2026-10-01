using System.ComponentModel.DataAnnotations;

namespace BookingService.DTOs;

public class CreateAccommodationBookingRequest
{
    [Required]
    public Guid AccommodationId { get; set; }

    [Required]
    public DateOnly CheckInDate { get; set; }

    [Required]
    public DateOnly CheckOutDate { get; set; }

    [Required]
    [Range(1, 100)]
    public int GuestCount { get; set; }
}