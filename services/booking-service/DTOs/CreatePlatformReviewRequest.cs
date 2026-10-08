using System.ComponentModel.DataAnnotations;

namespace BookingService.DTOs;

public class CreatePlatformReviewRequest
{
    [Range(1, 5)]
    public int Rating { get; set; }
    [Required, StringLength(2000)]
    public string Comment { get; set; } = string.Empty;
}

public record PlatformReviewResponse(Guid Id, int Rating, string Comment, DateTime CreatedAtUtc);
public record PagedPlatformReviewsResponse(IReadOnlyList<PlatformReviewResponse> Items,
    int Page, int PageSize, int TotalCount, int TotalPages, double AverageRating, int ReviewCount);
