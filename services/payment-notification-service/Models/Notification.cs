namespace PaymentNotificationService.Models;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipientUserId { get; set; }
    public string EventKey { get; set; } = "";
    public string EventType { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public Guid BookingId { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? ReviewId { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public int? Rating { get; set; }
    public string? RefundStatus { get; set; }
    public decimal? RefundAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime OccurredAtUtc { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}
