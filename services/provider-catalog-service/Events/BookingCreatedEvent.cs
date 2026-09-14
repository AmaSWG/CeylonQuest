using System;

namespace ProviderCatalogService.Events;

public class BookingCreatedEvent
{
    public Guid BookingId { get; set; }

    public Guid ListingId { get; set; }

    public string ListingType { get; set; } = "Experience";

    public string BookingDate { get; set; } = string.Empty; // "YYYY-MM-DD"

    public string TimeSlot { get; set; } = string.Empty;

    public int ParticipantCount { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}