namespace NotificationService.Events;

public class ReviewSubmittedEvent
{
    public Guid? EventId { get; set; }

    public Guid ReviewId { get; set; }

    public Guid BookingId { get; set; }

    public Guid ListingId { get; set; }

    public Guid? ProviderUserId { get; set; }

    public int Rating { get; set; }

    public string ReviewText { get; set; } = "";
    
    public DateTime SubmittedAtUtc { get; set; }

    private DateTime _legacySubmittedAt;

    // Booking-service emits SubmittedAtUtc; older producers emitted SubmittedAt.
    public DateTime SubmittedAt
    {
        get => SubmittedAtUtc != default ? SubmittedAtUtc : _legacySubmittedAt;
        set => _legacySubmittedAt = value;
    }
}
