using System.ComponentModel.DataAnnotations;

namespace BookingService.Models;

public class PaymentTransaction
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid BookingId { get; set; }

    [Required]
    public Guid VisitorId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(200)]
    public string TransactionReference { get; set; } = string.Empty;

    [Required]
    public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;

    [MaxLength(500)]
    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    // BookingId identifies an entity in the table selected by BookingType.
    [Required]
    [MaxLength(30)]
    public string BookingType { get; set; } = "Experience";
}
