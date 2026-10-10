namespace BookingService.DTOs;

public record ReviewResponse(
    Guid Id,
    Guid ListingId,
    string BookingType,
    int Rating,
    string Comment,
    DateTime CreatedAtUtc,
    string ReviewerDisplayName = "Visitor");
public record ReviewSummaryResponse(
    Guid ListingId,
    string? BookingType,
    double AverageRating,
    int ReviewCount
    );
