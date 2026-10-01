namespace PaymentNotificationService.DTOs;

public record NotificationDto(Guid Id, string EventType, string Title, string Message,
    Guid BookingId, Guid? ListingId, Guid? PaymentId, Guid? ReviewId, decimal? Amount,
    string? Currency, int? Rating, string? RefundStatus, decimal? RefundAmount,
    DateTime CreatedAtUtc, DateTime OccurredAtUtc, bool IsRead, DateTime? ReadAtUtc);

public record NotificationListResponse(IReadOnlyList<NotificationDto> Items, int Page,
    int PageSize, int TotalCount, int UnreadCount);

public record MarkReadResponse(int UpdatedCount, int UnreadCount);

