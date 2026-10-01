using PaymentNotificationService.Data;

namespace PaymentNotificationService.Services;

// Configuration mappings are an explicit integration fallback, not catalog provider IDs.
public class RecipientResolver(PaymentNotificationDbContext db, IConfiguration config) : IRecipientResolver
{
    public Task<Guid> ProviderAsync(Guid listingId, Guid? suppliedUserId, CancellationToken ct)
    {
        var id = suppliedUserId.GetValueOrDefault();
        if (id == Guid.Empty)
            Guid.TryParse(config[$"Notifications:ListingProviders:{listingId}"], out id);
        if (id == Guid.Empty)
            throw new InvalidOperationException($"Provider identity user ID is unavailable for listing {listingId}.");
        return Task.FromResult(id);
    }

    public async Task<(Guid VisitorId, Guid ProviderUserId)> BookingAsync(Guid bookingId,
        Guid listingId, Guid? visitorId, Guid? providerUserId, CancellationToken ct)
    {
        var context = await db.BookingContexts.FindAsync(new object[] { bookingId }, ct);
        var visitor = visitorId.GetValueOrDefault();
        if (visitor == Guid.Empty) visitor = context?.VisitorId ?? Guid.Empty;
        if (visitor == Guid.Empty)
            Guid.TryParse(config[$"Notifications:BookingVisitors:{bookingId}"], out visitor);
        if (visitor == Guid.Empty)
            throw new InvalidOperationException($"Visitor identity user ID is unavailable for booking {bookingId}.");
        var provider = providerUserId.GetValueOrDefault();
        if (provider == Guid.Empty) provider = context?.ProviderUserId ?? Guid.Empty;
        return (visitor, await ProviderAsync(listingId, provider, ct));
    }
}
