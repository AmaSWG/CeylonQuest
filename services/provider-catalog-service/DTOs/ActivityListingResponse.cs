namespace ProviderCatalogService.DTOs;

public class ActivityListingResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Unit { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public int MaxParticipants { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Duration { get; set; } = string.Empty;

    public string AvailableDays { get; set; } = string.Empty;

    public string TimeSlots { get; set; } = "[]";

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }
    public string? Images { get; set; }
}