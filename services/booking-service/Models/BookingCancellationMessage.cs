namespace BookingService.Models;

public class BookingCancellationMessage
{
    public Guid BookingId { get; set; }
    public string Payload { get; set; } = "";
    public DateTime? PublishedAt { get; set; }
}
