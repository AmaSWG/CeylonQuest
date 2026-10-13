namespace ProviderCatalogService.DTOs;

public class ListingRemovalResponse
{
    public Guid ListingId { get; set; }
    public string ListingType { get; set; } = string.Empty;
    public string Status { get; set; } = "Removed";
    public bool IsActive { get; set; }
    public Guid AuditId { get; set; }
    public DateTime RemovedAtUtc { get; set; }
}
