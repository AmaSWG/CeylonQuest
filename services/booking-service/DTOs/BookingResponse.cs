namespace BookingService.DTOs;

public class CatalogListingResponse
{
    public Guid Id { get; set; }

    // Experience fields
    public string Title { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Unit { get; set; } = string.Empty;

    public int MaxParticipants { get; set; }

    // Restaurant fields
    public string Name { get; set; } = string.Empty;

    public int SeatingCapacity { get; set; }

    // Common field
    public bool IsActive { get; set; }
}