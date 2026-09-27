using System.ComponentModel.DataAnnotations;

namespace BookingService.Models;

public class Booking
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid VisitorId { get; set; }

    [Required]
    public Guid ListingId { get; set; }

    [Required]
    [MaxLength(200)]
    public string ListingTitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ListingType { get; set; } = "Experience";

    [Required]
    public DateOnly BookingDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string TimeSlot { get; set; } = string.Empty;

    [Required]
    public int ParticipantCount { get; set; }

    [Required]
    public decimal UnitPrice { get; set; }

    [Required]
    public decimal TotalAmount { get; set; }

    [Required]
    public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;

    [Required]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    [MaxLength(200)]
    public string? PaymentReference { get; set; }

    // Cancellation details
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    // Refund details
    public decimal RefundAmount { get; set; } = 0;

    public decimal RefundPercentage { get; set; } = 0;

    public DateTime? RefundedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}