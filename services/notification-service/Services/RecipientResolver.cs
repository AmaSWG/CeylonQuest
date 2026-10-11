using NotificationService.Data;
using System.Net;
using System.Net.Http.Json;

namespace NotificationService.Services;

// Configuration mappings are an explicit integration fallback, not catalog provider IDs.
public class RecipientResolver(NotificationDbContext db, IConfiguration config, HttpClient? catalogClient = null) : IRecipientResolver
{
    public async Task<Guid> ProviderAsync(Guid listingId, Guid? suppliedUserId, CancellationToken ct)
    {
        var id = suppliedUserId.GetValueOrDefault();
        if (id == Guid.Empty)
            Guid.TryParse(config[$"Notifications:ListingProviders:{listingId}"], out id);
        if (id == Guid.Empty && catalogClient != null)
            id = await LookupProviderAsync(listingId, ct);
        if (id == Guid.Empty)
            throw new InvalidOperationException($"Provider identity user ID is unavailable for listing {listingId}.");
        return id;
    }

    private async Task<Guid> LookupProviderAsync(Guid listingId, CancellationToken ct)
    {
        using var activity = await catalogClient!.GetAsync($"/api/catalog/activity-listings/public/{listingId}", ct);
        if (activity.StatusCode != HttpStatusCode.NotFound)
        {
            activity.EnsureSuccessStatusCode();
            var listing = await activity.Content.ReadFromJsonAsync<CatalogListing>(cancellationToken: ct);
            return listing?.ProviderUserId ?? Guid.Empty;
        }

        foreach (var type in new[] { "restaurant", "accommodation" })
        {
            using var response = await catalogClient.GetAsync($"/api/catalog/{type}-listings/public", ct);
            response.EnsureSuccessStatusCode();
            var listings = await response.Content.ReadFromJsonAsync<List<CatalogListing>>(cancellationToken: ct);
            var listing = listings?.FirstOrDefault(l => l.Id == listingId);
            if (listing != null) return listing.ProviderUserId ?? Guid.Empty;
        }
        return Guid.Empty;
    }

    private sealed class CatalogListing
    {
        public Guid Id { get; set; }
        public Guid? ProviderUserId { get; set; }
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
