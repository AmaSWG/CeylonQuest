namespace BookingService.DTOs;

public class PaymentResponse
{
    public Guid TransactionId { get; set; }

    public Guid BookingId { get; set; }

    public string ListingTitle { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string TransactionReference { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public string BookingStatus { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime? ProcessedAt { get; set; }
}