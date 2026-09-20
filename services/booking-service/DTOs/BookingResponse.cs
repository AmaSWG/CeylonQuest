namespace BookingService.DTOs;

public class CatalogListingResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Unit { get; set; } = string.Empty;

    public int MaxParticipants { get; set; }

    public bool IsActive { get; set; }
}