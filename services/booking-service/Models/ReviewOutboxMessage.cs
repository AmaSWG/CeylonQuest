namespace BookingService.Models;

public class ReviewOutboxMessage
{
    public Guid Id { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime? PublishedAtUtc { get; set; }
}
