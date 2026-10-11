using System;

namespace NotificationService.Events;

public class BookingCanceledEvent
{
    public Guid? EventId { get; set; }
    public Guid? VisitorId { get; set; }
    public Guid? ProviderUserId { get; set; }
    public string? RefundStatus { get; set; }
    public decimal? RefundAmount { get; set; }
    public string? Currency { get; set; }
    public Guid BookingId { get; set; }

    public Guid ListingId { get; set; }

    public string ListingType { get; set; } = string.Empty;

    public string BookingDate { get; set; } = string.Empty; // "YYYY-MM-DD"

    public string TimeSlot { get; set; } = string.Empty;

    public int ParticipantCount { get; set; } = 1;

    public string? Reason { get; set; }

    public DateTime CanceledAt { get; set; }
}
