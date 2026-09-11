namespace ProviderCatalogService.Models;

public class Provider
{
    public Guid Id { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string ServiceType { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? LegalDocumentPath { get; set; }

    public string? LegalDocumentFileName { get; set; }

    public Guid? IdentityUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ActivityListing> ActivityListings { get; set; }
        = new List<ActivityListing>();
}