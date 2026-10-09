using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.DTOs;

namespace NotificationService.Services;

public class NotificationService(NotificationDbContext db)
{
    public Task<int> UnreadAsync(Guid user, CancellationToken ct) =>
        db.Notifications.CountAsync(x => x.RecipientUserId == user && !x.IsRead, ct);

    public async Task<NotificationListResponse> ListAsync(Guid user, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Notifications.AsNoTracking().Where(x => x.RecipientUserId == user);
        var total = await query.CountAsync(ct);
        var unread = await UnreadAsync(user, ct);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new NotificationDto(x.Id, x.EventType, x.Title, x.Message,
                x.BookingId, x.ListingId, x.PaymentId, x.ReviewId, x.Amount, x.Currency,
                x.Rating, x.RefundStatus, x.RefundAmount, x.CreatedAtUtc, x.OccurredAtUtc,
                x.IsRead, x.ReadAtUtc)).ToListAsync(ct);
        return new(items, page, pageSize, total, unread);
    }

    public async Task<MarkReadResponse?> ReadAsync(Guid user, Guid id, CancellationToken ct)
    {
        var query = db.Notifications.Where(x => x.Id == id && x.RecipientUserId == user);
        if (!await query.AnyAsync(ct)) return null;
        var now = DateTime.UtcNow;
        var count = await query.Where(x => !x.IsRead).ExecuteUpdateAsync(s =>
            s.SetProperty(x => x.IsRead, true).SetProperty(x => x.ReadAtUtc, now), ct);
        return new(count, await UnreadAsync(user, ct));
    }

    public async Task<MarkReadResponse> ReadAllAsync(Guid user, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var count = await db.Notifications.Where(x => x.RecipientUserId == user && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true)
                .SetProperty(x => x.ReadAtUtc, now), ct);
        return new(count, await UnreadAsync(user, ct));
    }
}
