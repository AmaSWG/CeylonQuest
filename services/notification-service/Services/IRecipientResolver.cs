namespace NotificationService.Services;

public interface IRecipientResolver
{
    Task<Guid> ProviderAsync(Guid listingId, Guid? suppliedUserId, CancellationToken ct);
    
    Task<(Guid VisitorId, Guid ProviderUserId)> BookingAsync(Guid bookingId, Guid listingId,
        Guid? visitorId, Guid? providerUserId, CancellationToken ct);
}
