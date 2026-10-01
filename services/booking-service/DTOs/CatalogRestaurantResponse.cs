namespace BookingService.DTOs;

public class CatalogRestaurantResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal PricePerPerson { get; set; }

    public int SeatingCapacity { get; set; }

    public bool IsActive { get; set; }
}