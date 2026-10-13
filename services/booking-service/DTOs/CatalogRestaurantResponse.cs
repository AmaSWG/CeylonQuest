namespace BookingService.DTOs;

public class CatalogRestaurantResponse
{
    public Guid ProviderId { get; set; }
    public Guid Id { get; set; }

    public Guid? ProviderUserId { get; set; }

    public string ProviderBusinessName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal PricePerPerson { get; set; }

    public int SeatingCapacity { get; set; }

    public bool IsActive { get; set; }
}
