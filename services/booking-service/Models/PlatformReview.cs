namespace BookingService.Models;

public class PlatformReview
{
    public Guid Id { get; set; }
    public Guid VisitorId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
