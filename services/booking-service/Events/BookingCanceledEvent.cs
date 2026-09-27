using System;

namespace BookingService.Events;

public class BookingCanceledEvent
{
    public Guid BookingId { get; set; }

    public Guid ListingId { get; set; }

    public string ListingType { get; set; } = "Experience";

    public string BookingDate { get; set; } = string.Empty;

    public string TimeSlot { get; set; } = string.Empty;

    public int ParticipantCount { get; set; } = 1;

    public string? Reason { get; set; }

    public DateTime CanceledAt { get; set; } = DateTime.UtcNow;
}