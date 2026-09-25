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
    public decimal PricePerPerson { get; set; }

    [Required]
    public decimal TotalPrice { get; set; }

    [Required]
    public ReservationStatus Status { get; set; } =
        ReservationStatus.Confirmed;

    // =========================================================
    // Cancellation information
    // =========================================================

    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    // =========================================================
    // Refund information
    // =========================================================

    public decimal RefundPercentage { get; set; } = 0m;

    public decimal RefundAmount { get; set; } = 0m;

    public DateTime? RefundedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}