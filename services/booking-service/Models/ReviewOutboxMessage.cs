namespace BookingService.Models;

public class ReviewOutboxMessage
{
    public Guid Id { get; set; }
    public string Topic { get; set; } = "review.submitted";
    public string Payload { get; set; } = string.Empty;
    public DateTime? PublishedAtUtc { get; set; }
}
