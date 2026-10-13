using BookingService.DTOs;

namespace BookingService.Services;

public interface IReviewService
{
    Task<ReviewResponse> CreateAsync(Guid visitorId, CreateReviewRequest request, CancellationToken token = default);
    Task<PagedReviewsResponse> GetAsync(Guid listingId, ReviewQuery query, CancellationToken token = default);
    Task<ReviewSummaryResponse> GetSummaryAsync(Guid listingId, string? bookingType, CancellationToken token = default);
    Task<PlatformReviewResponse> CreatePlatformAsync(Guid visitorId, CreatePlatformReviewRequest request, CancellationToken token = default);
    Task<PagedPlatformReviewsResponse> GetPlatformAsync(ReviewQuery query, CancellationToken token = default);

    Task<ReviewEligibilityResponse> GetEligibilityAsync(
    Guid visitorId,
    Guid listingId,
    string bookingType,
    CancellationToken token = default);
}

public class ReviewException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
