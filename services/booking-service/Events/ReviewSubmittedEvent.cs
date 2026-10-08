namespace BookingService.Events;

public record ReviewSubmittedEvent(Guid EventId, int EventVersion, Guid ReviewId,
    Guid BookingId, string BookingType, Guid ListingId, string ListingTitle,
    Guid VisitorId, Guid ProviderId, int Rating, DateTime SubmittedAtUtc);
