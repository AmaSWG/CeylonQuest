namespace BookingService.DTOs;

public record EligibleReviewBooking(
    Guid BookingId,
    DateOnly Date,
    DateTime ScheduledEndAtUtc);

public record ReviewEligibilityResponse(
    IReadOnlyList<EligibleReviewBooking> EligibleBookings,
    string? Message);