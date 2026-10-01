using System;

namespace ProviderCatalogService.DTOs;

public class ReserveCapacityRequest
{
    public Guid ListingId { get; set; }

    public string Date { get; set; } = string.Empty;

    public string TimeSlot { get; set; } = string.Empty;

    public int GuestCount { get; set; }
}