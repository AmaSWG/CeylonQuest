namespace ProviderCatalogService.Models;

public class ActivityListing
{
    public Guid Id { get; set; }

    public Guid ProviderId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Unit { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public int MaxParticipants { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	
	public Provider Provider { get; set; } = null!;
}