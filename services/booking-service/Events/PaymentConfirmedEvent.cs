namespace BookingService.Events;

public class PaymentConfirmedEvent
{
    public Guid PaymentId { get; set; }

    public Guid BookingId { get; set; }

    public Guid VisitorId { get; set; }

    public Guid ListingId { get; set; }

    public string ListingTitle { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string TransactionReference { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public string BookingStatus { get; set; } = string.Empty;

    public DateOnly BookingDate { get; set; }

    public string TimeSlot { get; set; } = string.Empty;

    public DateTime ConfirmedAt { get; set; }
}