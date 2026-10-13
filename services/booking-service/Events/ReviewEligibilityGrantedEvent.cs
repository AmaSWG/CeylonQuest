namespace BookingService.Events;

public record ReviewEligibilityGrantedEvent(Guid EventId, Guid BookingId, string BookingType,
    Guid ListingId, Guid VisitorId, string ListingTitle, DateTime VerifiedAtUtc);
