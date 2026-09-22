using System.ComponentModel.DataAnnotations;

namespace BookingService.Models;

public class RestaurantReservation
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid VisitorId { get; set; }

    [Required]
    public Guid RestaurantId { get; set; }

    [Required]
    [MaxLength(200)]
    public string RestaurantName { get; set; } = string.Empty;

    [Required]
    public DateOnly ReservationDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string TimeSlot { get; set; } = string.Empty;

    [Required]
    public int PartySize { get; set; }

    [Required]
    public ReservationStatus Status { get; set; } =
        ReservationStatus.Confirmed;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}