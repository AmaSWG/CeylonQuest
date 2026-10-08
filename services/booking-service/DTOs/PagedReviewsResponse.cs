namespace BookingService.DTOs;

public record PagedReviewsResponse(IReadOnlyList<ReviewResponse> Items,
    int Page, int PageSize, int TotalCount, int TotalPages,
    double AverageRating, int ReviewCount);
