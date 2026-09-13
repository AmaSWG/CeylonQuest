using System;

namespace ProviderCatalogService.Models;

public class AvailabilitySlot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ListingId { get; set; }

    public string ListingType { get; set; } = "Experience"; // "Experience" | "Restaurant"

    public DateOnly Date { get; set; }

    public string TimeSlot { get; set; } = string.Empty; // e.g. "08:00 AM - 10:00 AM"

    public int TotalCapacity { get; set; }

    public int RemainingCapacity { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}