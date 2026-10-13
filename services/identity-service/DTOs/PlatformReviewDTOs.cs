using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class CreatePlatformReviewRequest
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}

public record PlatformReviewResponse(
    Guid Id,
    string ReviewerName,
    string? ProfilePictureUrl,
    int Rating,
    string Content,
    DateTime CreatedAtUtc);

public record PlatformReviewPageResponse(
    IReadOnlyList<PlatformReviewResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);