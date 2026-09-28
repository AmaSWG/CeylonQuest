namespace ProviderCatalogService.Models;

// Persisted in the same transaction as the inventory update.
public class BookingCapacityRelease
{
    public Guid BookingId { get; set; }
    public DateTime ReleasedAt { get; set; }
}
