using System;

namespace PaymentNotificationService.Events;

public class BookingCreatedEvent
{
    public Guid? EventId { get; set; }
    public Guid? ProviderUserId { get; set; }
    public Guid BookingId { get; set; }

    public Guid VisitorId { get; set; }

    public Guid ListingId { get; set; }

    public string ListingType { get; set; } = string.Empty;

    public string BookingDate { get; set; } = string.Empty; // "YYYY-MM-DD"

    public string TimeSlot { get; set; } = string.Empty;

    // Capacity quantity: experience participants, one accommodation unit, or restaurant party size.
    public int ParticipantCount { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }
}
