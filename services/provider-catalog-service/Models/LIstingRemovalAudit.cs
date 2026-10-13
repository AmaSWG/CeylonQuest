namespace ProviderCatalogService.Models;

public class ListingRemovalAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ListingId { get; set; }
    public string ListingType { get; set; } = string.Empty;

    public Guid AdministratorId { get; set; }
    public DateTime RemovedAtUtc { get; set; }

    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = "Removed";
    public string Reason { get; set; } = string.Empty;
}