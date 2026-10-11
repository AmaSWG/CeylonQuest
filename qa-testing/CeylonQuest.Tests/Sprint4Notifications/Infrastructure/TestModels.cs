namespace CeylonQuest.Tests.Sprint4Notifications.Infrastructure;

/// <summary>A real, registered+logged-in identity-service account used as a test actor.</summary>
public sealed record TestUser(Guid Id, string Email, string Password, string Token);

// Mirrors services/notification-service/DTOs/NotificationResponses.cs (field names only - case
// insensitive JSON binding means the camelCase wire format still matches these PascalCase records).
public sealed record NotificationItem(
    Guid Id, string EventType, string Title, string Message, Guid BookingId,
    Guid? ListingId, Guid? PaymentId, Guid? ReviewId, decimal? Amount, string? Currency,
    int? Rating, string? RefundStatus, decimal? RefundAmount,
    DateTime CreatedAtUtc, DateTime OccurredAtUtc, bool IsRead, DateTime? ReadAtUtc);

public sealed record NotificationList(
    List<NotificationItem> Items, int Page, int PageSize, int TotalCount, int UnreadCount);

public sealed record UnreadCountResponse(int UnreadCount);

public sealed record MarkReadResult(int UpdatedCount, int UnreadCount);

// Mirrors services/provider-catalog-service/DTOs/SearchResultItemDto.cs (subset of fields we need).
public sealed record CatalogSearchItem(Guid Id, string Type, string Title, string ProviderBusinessName);

public sealed record CatalogSearchResponse(List<CatalogSearchItem> Items, int Page, int PageSize, int TotalCount);
