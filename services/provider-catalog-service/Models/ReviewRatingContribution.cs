namespace ProviderCatalogService.Models;

// Durable inbox and rebuild source. No review text or visitor details are copied.
public class ReviewRatingContribution
{
    public Guid ReviewId { get; set; }
    public Guid EventId { get; set; }
    public Guid ListingId { get; set; }
    public string BookingType { get; set; } = string.Empty;
    public int Rating { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
}
