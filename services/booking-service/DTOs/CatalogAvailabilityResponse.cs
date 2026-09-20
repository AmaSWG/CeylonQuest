namespace BookingService.DTOs;

public class CatalogAvailabilityResponse
{
    public Guid ListingId { get; set; }

    public string Date { get; set; } = string.Empty;

    public bool IsOperatingDay { get; set; }

    public bool IsFullyBooked { get; set; }

    public List<CatalogAvailabilitySlot> Slots { get; set; } = new();
}

public class CatalogAvailabilitySlot
{
    public string TimeSlot { get; set; } = string.Empty;

    public int TotalCapacity { get; set; }

    public int RemainingCapacity { get; set; }

    public bool IsFullyBooked { get; set; }
}