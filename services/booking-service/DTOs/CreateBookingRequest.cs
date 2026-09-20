using System.ComponentModel.DataAnnotations;

namespace BookingService.DTOs;

public class CreateBookingRequest
{
    [Required]
    public Guid ListingId { get; set; }

    [Required]
    public DateOnly BookingDate { get; set; }

    [Required]
    public string TimeSlot { get; set; } = string.Empty;

    [Range(1, 100)]
    public int ParticipantCount { get; set; }
}