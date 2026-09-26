using System.ComponentModel.DataAnnotations;

namespace BookingService.Models;

public class AccommodationBooking
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid VisitorId { get; set; }

    [Required]
    public Guid AccommodationId { get; set; }

    [Required]
    [MaxLength(200)]
    public string AccommodationName { get; set; } = string.Empty;

    [Required]
    public DateOnly CheckInDate { get; set; }

    [Required]
    public DateOnly CheckOutDate { get; set; }

    [Required]
    public int NumberOfNights { get; set; }

    [Required]
    public int GuestCount { get; set; }

    [Required]
    public decimal PricePerNight { get; set; }

    [Required]
    public decimal TotalPrice { get; set; }

    [Required]
    public AccommodationBookingStatus Status { get; set; }
        = AccommodationBookingStatus.Confirmed;

    // =========================================================
    // CANCELLATION / REFUND
    // =========================================================

    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    public decimal RefundPercentage { get; set; } = 0m;

    public decimal RefundAmount { get; set; } = 0m;

    public DateTime? RefundedAt { get; set; }

    // =========================================================
    // AUDIT
    // =========================================================

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}