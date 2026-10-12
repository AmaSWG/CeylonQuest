using System.Net.Http.Headers;
using System.Net.Http.Json;
using CeylonQuest.Tests.Sprint4Notifications;
using CeylonQuest.Tests.Sprint4Notifications.Infrastructure;

namespace CeylonQuest.Tests.Sprint4Reviews.Infrastructure;

/// <summary>Thin wrapper over the real booking-service review API (services/booking-service/Controllers/ReviewsController.cs).</summary>
public sealed class ReviewApiClient
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(TestConfig.BookingApiBaseUrl) };

    public async Task<ReviewSummaryResponse?> GetSummaryAsync(Guid listingId, string? bookingType = "Experience", CancellationToken ct = default)
    {
        var url = $"/api/bookings/reviews/{listingId}/summary" + (bookingType != null ? $"?bookingType={bookingType}" : "");
        var response = await _http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ReviewSummaryResponse>(JsonDefaults.Options, ct);
    }

    public async Task<HttpResponseMessage> CreateReviewAsync(string? token, Guid bookingId, string bookingType, int rating, string comment, CancellationToken ct = default)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var res = await CreateReviewRawAsync(token, new { bookingId, bookingType, rating, comment }, ct);
            if (res.StatusCode != System.Net.HttpStatusCode.ServiceUnavailable || attempt == 2)
                return res;
            await Task.Delay(500, ct);
        }
        return await CreateReviewRawAsync(token, new { bookingId, bookingType, rating, comment }, ct);
    }

    public async Task<HttpResponseMessage> CreateReviewRawAsync(string? token, object payload, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/bookings/reviews");
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(payload, options: JsonDefaults.Options);
        return await _http.SendAsync(request, ct);
    }

    public async Task<HttpResponseMessage> GetReviewsRawAsync(Guid listingId, string? queryString = null, CancellationToken ct = default)
    {
        var url = $"/api/bookings/reviews/{listingId}" + (queryString != null ? $"?{queryString}" : "");
        return await _http.GetAsync(url, ct);
    }

    public async Task<PagedReviewsResponse?> GetReviewsAsync(Guid listingId, int? rating = null, int page = 1, int pageSize = 10, string? bookingType = "Experience", CancellationToken ct = default)
    {
        var q = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (rating.HasValue) q.Add($"rating={rating.Value}");
        if (!string.IsNullOrEmpty(bookingType)) q.Add($"bookingType={bookingType}");
        var response = await GetReviewsRawAsync(listingId, string.Join("&", q), ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PagedReviewsResponse>(JsonDefaults.Options, ct);
    }
}

public sealed record ReviewResponse(
    Guid Id,
    Guid ListingId,
    string BookingType,
    int Rating,
    string Comment,
    DateTime CreatedAtUtc,
    string ReviewerDisplayName,
    bool AttendanceVerified,
    string? ProviderResponse,
    DateTime? ProviderRespondedAtUtc);

public sealed record PagedReviewsResponse(
    IReadOnlyList<ReviewResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    double AverageRating,
    int ReviewCount);

public sealed record ReviewSummaryResponse(Guid ListingId, string? BookingType, double AverageRating, int ReviewCount);
