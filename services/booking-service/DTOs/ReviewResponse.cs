namespace BookingService.DTOs;

public record ReviewResponse(
    Guid Id,
    Guid ListingId,
    string BookingType,
    int Rating,
    string Comment,
    DateTime CreatedAtUtc,
    string ReviewerDisplayName = "Visitor",
    bool AttendanceVerified = false,
    string? ProviderResponse = null,
    DateTime? ProviderRespondedAtUtc = null);
public record ReviewSummaryResponse(
    Guid ListingId,
    string? BookingType,
    double AverageRating,
    int ReviewCount
    );
