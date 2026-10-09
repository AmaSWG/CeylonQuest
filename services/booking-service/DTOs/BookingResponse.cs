namespace BookingService.DTOs;

public class CatalogListingResponse
{
    public Guid ProviderId { get; set; }
    public string Duration { get; set; } = string.Empty;
    public Guid Id { get; set; }

    public Guid? ProviderUserId { get; set; }

    public string ProviderBusinessName { get; set; } = string.Empty;

    // Experience fields
    public string Title { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Unit { get; set; } = string.Empty;

    public int MaxParticipants { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    // Restaurant fields
    public string Name { get; set; } = string.Empty;

    public int SeatingCapacity { get; set; }

    // Common field
    public bool IsActive { get; set; }
}
