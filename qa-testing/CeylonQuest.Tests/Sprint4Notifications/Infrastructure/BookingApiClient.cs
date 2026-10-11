using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CeylonQuest.Tests.Sprint4Notifications.Infrastructure;

/// <summary>Thin wrapper over the real booking-service REST API, used only by the optional "real flow" bonus tests.</summary>
public sealed class BookingApiClient
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(TestConfig.BookingApiBaseUrl) };

    private static HttpRequestMessage Request(HttpMethod method, string path, string token)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public async Task<HttpResponseMessage> CreateExperienceBookingAsync(string token, Guid listingId, string date,
        string timeSlot, int participantCount, CancellationToken ct = default)
    {
        var request = Request(HttpMethod.Post, "/api/Bookings", token);
        request.Content = JsonContent.Create(new { listingId, bookingDate = date, timeSlot, participantCount }, options: JsonDefaults.Options);
        return await _http.SendAsync(request, ct);
    }

    public async Task<HttpResponseMessage> CancelBookingAsync(string token, Guid bookingId, string? reason, CancellationToken ct = default)
    {
        var request = Request(HttpMethod.Put, $"/api/Bookings/{bookingId}/cancel", token);
        request.Content = JsonContent.Create(new { reason }, options: JsonDefaults.Options);
        return await _http.SendAsync(request, ct);
    }
}
