using System.Text.Json;
using CeylonQuest.Tests.Sprint4Notifications;
using CeylonQuest.Tests.Sprint4Notifications.Infrastructure;
using CeylonQuest.Tests.Sprint4Reviews.Infrastructure;

namespace CeylonQuest.Tests.Sprint4Reviews.Api;

public sealed class ReviewApiFixture : IAsyncLifetime
{
    public IdentityApiClient Identity { get; } = new();
    public CatalogApiClient Catalog { get; } = new();
    public BookingApiClient Booking { get; } = new();
    public ReviewApiClient Reviews { get; } = new();
    public NotificationApiClient Notifications { get; } = new();

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    public async Task<(TestUser Visitor, Guid BookingId, Guid ListingId, string ListingTitle)> CreatePendingBookingAsync(string label)
    {
        var visitor = await Identity.CreateVisitorAsync(label);
        var (bookingId, listingId, listingTitle) = await CreatePendingBookingAsync(visitor);
        return (visitor, bookingId, listingId, listingTitle);
    }

    public async Task<(Guid BookingId, Guid ListingId, string ListingTitle)> CreatePendingBookingAsync(TestUser visitor)
    {
        var listing = await Catalog.FindBookableListingAsync("Experience")
            ?? throw new InvalidOperationException("No bookable Experience listing found.");
        var slot = await Catalog.FindAvailableSlotAsync(listing.Id)
            ?? throw new InvalidOperationException($"No available slot found for listing {listing.Id}.");

        HttpResponseMessage? response = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            response = await Booking.CreateExperienceBookingAsync(visitor.Token, listing.Id, slot.Date, slot.TimeSlot, 1);
            if (response.IsSuccessStatusCode) break;
            if (attempt < 2) await Task.Delay(1000);
        }

        if (response == null || !response.IsSuccessStatusCode)
        {
            var err = response != null ? await response.Content.ReadAsStringAsync() : "No response";
            throw new HttpRequestException($"Booking creation failed with status {response?.StatusCode}: {err}");
        }
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var bookingId = body.RootElement.GetProperty("id").GetGuid();

        return (bookingId, listing.Id, listing.Title);
    }

    public async Task<(TestUser Visitor, Guid BookingId, Guid ListingId, string ListingTitle)> CreateCompletedBookingAsync(string label, Guid? providerUserId = null)
    {
        var (visitor, bookingId, listingId, listingTitle) = await CreatePendingBookingAsync(label);
        await BookingDbHelper.MarkBookingCompletedAsync(bookingId, providerUserId);
        return (visitor, bookingId, listingId, listingTitle);
    }

    public async Task<string> GetAdminTokenAsync() =>
        await Identity.LoginAsync("adminceylonquest@gmail.com", "AdminPassword123!");

    public async Task<string> GetProviderTokenAsync() =>
        await Identity.LoginAsync(TestConfig.ProviderEmail, TestConfig.ProviderPassword);
}

[CollectionDefinition(nameof(ReviewApiCollection), DisableParallelization = true)]
public sealed class ReviewApiCollection : ICollectionFixture<ReviewApiFixture>
{
}
