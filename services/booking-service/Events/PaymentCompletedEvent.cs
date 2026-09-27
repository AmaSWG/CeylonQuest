namespace BookingService.Events;

public class PaymentCompletedEvent
{
    public Guid PaymentId { get; set; }

    public Guid BookingId { get; set; }

    public Guid VisitorId { get; set; }

    public decimal Amount { get; set; }

    public string TransactionReference { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public string BookingStatus { get; set; } = string.Empty;

    public DateTime CompletedAt { get; set; }
}