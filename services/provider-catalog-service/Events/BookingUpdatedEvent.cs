using System;

namespace ProviderCatalogService.Events;

public class BookingUpdatedEvent
{
    public Guid BookingId { get; set; }

    public Guid ListingId { get; set; }

    public string ListingType { get; set; } = "Experience";

    public string OldBookingDate { get; set; } = string.Empty; // "YYYY-MM-DD"

    public string OldTimeSlot { get; set; } = string.Empty;

    public int OldParticipantCount { get; set; } = 1;

    public string NewBookingDate { get; set; } = string.Empty; // "YYYY-MM-DD"

    public string NewTimeSlot { get; set; } = string.Empty;

    public int NewParticipantCount { get; set; } = 1;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}