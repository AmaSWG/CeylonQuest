using System.Net.Http.Json;
using CeylonQuest.Tests.Sprint4Notifications;
using CeylonQuest.Tests.Sprint4Notifications.Infrastructure;

namespace CeylonQuest.Tests.Sprint4Reviews.Infrastructure;

/// <summary>Thin wrapper over the real booking-service review API (services/booking-service/Controllers/ReviewsController.cs).</summary>
public sealed class ReviewApiClient
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(TestConfig.BookingApiBaseUrl) };

    public async Task<ReviewSummaryResponse?> GetSummaryAsync(Guid listingId, string bookingType, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/bookings/reviews/{listingId}/summary?bookingType={bookingType}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ReviewSummaryResponse>(JsonDefaults.Options, ct);
    }

    public async Task<HttpResponseMessage> CreateReviewAsync(string token, Guid bookingId, string bookingType, int rating, string comment, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/bookings/reviews");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { bookingId, bookingType, rating, comment }, options: JsonDefaults.Options);
        return await _http.SendAsync(request, ct);
    }
}

public sealed record ReviewSummaryResponse(Guid ListingId, string? BookingType, double AverageRating, int ReviewCount);
