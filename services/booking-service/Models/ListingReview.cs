namespace BookingService.Models;

public class ListingReview
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public string BookingType { get; set; } = string.Empty;
    public Guid VisitorId { get; set; }

    public string ReviewerDisplayName { get; set; } = "Visitor";
    public Guid ListingId { get; set; }
    public Guid ProviderId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
