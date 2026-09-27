namespace BookingService.Events;

public class PaymentFailedEvent
{
    public Guid PaymentId { get; set; }

    public Guid BookingId { get; set; }

    public Guid VisitorId { get; set; }

    public decimal Amount { get; set; }

    public string TransactionReference { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public string FailureReason { get; set; } = string.Empty;

    public DateTime FailedAt { get; set; }
}