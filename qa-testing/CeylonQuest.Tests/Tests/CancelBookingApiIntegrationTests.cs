using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using CeylonQuest.Tests.Configuration;
using Xunit;

namespace CeylonQuest.Tests.Tests
{
    public class CancelBookingApiIntegrationTests
    {
        private readonly HttpClient _bookingClient;
        private readonly HttpClient _catalogClient;
        private readonly HttpClient _identityClient;

        public CancelBookingApiIntegrationTests()
        {
            _bookingClient = new HttpClient { BaseAddress = new Uri("http://localhost:5229") };
            _catalogClient = new HttpClient { BaseAddress = new Uri("http://localhost:5141") };
            _identityClient = new HttpClient { BaseAddress = new Uri("http://localhost:5278") };
        }

        private async Task<string> GetVisitorTokenAsync()
        {
            var loginResp = await _identityClient.PostAsJsonAsync("/api/Auth/login", new
            {
                email = TestConfiguration.Settings.VisitorEmail,
                password = TestConfiguration.Settings.VisitorPassword
            });
            loginResp.EnsureSuccessStatusCode();
            var json = await loginResp.Content.ReadFromJsonAsync<JsonElement>();

            if (json.TryGetProperty("accessToken", out var at)) return at.GetString()!;
            if (json.TryGetProperty("token", out var t)) return t.GetString()!;
            throw new InvalidOperationException("Could not extract visitor token.");
        }

        private async Task<Guid?> CreateTestBookingAsync(string token, int daysInFuture = 7)
        {
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Fetch an available experience from catalog
            var searchResp = await _catalogClient.GetAsync("/api/catalog/search?type=experience");
            if (!searchResp.IsSuccessStatusCode) return null;
            var searchJson = await searchResp.Content.ReadFromJsonAsync<JsonElement>();

            var items = searchJson.ValueKind == JsonValueKind.Array
                ? searchJson.EnumerateArray()
                : searchJson.GetProperty("items").EnumerateArray();

            foreach (var item in items)
            {
                var listingId = Guid.Parse(item.GetProperty("id").GetString()!);
                var testDate = DateTime.UtcNow.AddDays(daysInFuture).ToString("yyyy-MM-dd");

                var availResp = await _catalogClient.GetAsync($"/api/catalog/availability/{listingId}?date={testDate}");
                if (!availResp.IsSuccessStatusCode) continue;

                var availJson = await availResp.Content.ReadFromJsonAsync<JsonElement>();
                if (availJson.TryGetProperty("slots", out var slots))
                {
                    foreach (var slot in slots.EnumerateArray())
                    {
                        if (slot.GetProperty("remainingCapacity").GetInt32() > 0)
                        {
                            var timeSlot = slot.GetProperty("timeSlot").GetString()!;
                            var createResp = await _bookingClient.PostAsJsonAsync("/api/Bookings", new
                            {
                                listingId,
                                bookingDate = testDate,
                                timeSlot,
                                participantCount = 1
                            });

                            if (createResp.IsSuccessStatusCode)
                            {
                                var createdJson = await createResp.Content.ReadFromJsonAsync<JsonElement>();
                                return Guid.Parse(createdJson.GetProperty("id").GetString()!);
                            }
                        }
                    }
                }
            }
            return null;
        }

        [Fact(DisplayName = "CQ-CN-API-01: PUT /api/Bookings/{id}/cancel without token returns 401 Unauthorized")]
        [Trait("Story", "9.2")]
        [Trait("TestCase", "CQ-CN-API-01")]
        public async Task CQ_CN_API_01_CancelBooking_Unauthenticated_Returns401()
        {
            _bookingClient.DefaultRequestHeaders.Authorization = null;
            var resp = await _bookingClient.PutAsJsonAsync($"/api/Bookings/{Guid.NewGuid()}/cancel", new { reason = "Test" });
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact(DisplayName = "CQ-CN-API-02: Non-owner visitor trying to cancel booking returns 404 Not Found")]
        [Trait("Story", "9.2")]
        [Trait("TestCase", "CQ-CN-API-02")]
        public async Task CQ_CN_API_02_CancelBooking_NonExistentOrNonOwner_Returns404()
        {
            var token = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await _bookingClient.PutAsJsonAsync($"/api/Bookings/{Guid.NewGuid()}/cancel", new { reason = "Test" });
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact(DisplayName = "CQ-CN-API-03: Cancel booking >48 hours in advance returns 200 and calculates 100% refund")]
        [Trait("Story", "9.2")]
        [Trait("TestCase", "CQ-CN-API-03")]
        public async Task CQ_CN_API_03_CancelBooking_MoreThan48Hours_Calculates100PercentRefund()
        {
            var token = await GetVisitorTokenAsync();
            var bookingId = await CreateTestBookingAsync(token, daysInFuture: 10);
            if (bookingId == null) return;

            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var cancelResp = await _bookingClient.PutAsJsonAsync($"/api/Bookings/{bookingId}/cancel", new
            {
                reason = "Family emergency"
            });
            cancelResp.EnsureSuccessStatusCode();

            var json = await cancelResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.TryGetProperty("refundPercentage", out var rp));
            Assert.Equal(100m, rp.GetDecimal());
            Assert.Equal("Cancelled", json.GetProperty("status").GetString());
        }

        [Fact(DisplayName = "CQ-CN-API-04: Cancelling an already cancelled booking returns 400 Bad Request")]
        [Trait("Story", "9.2")]
        [Trait("TestCase", "CQ-CN-API-04")]
        public async Task CQ_CN_API_04_CancelBooking_AlreadyCancelled_Returns400()
        {
            var token = await GetVisitorTokenAsync();
            var bookingId = await CreateTestBookingAsync(token, daysInFuture: 10);
            if (bookingId == null) return;

            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // First cancellation -> 200
            var firstCancel = await _bookingClient.PutAsJsonAsync($"/api/Bookings/{bookingId}/cancel", new { reason = "First" });
            firstCancel.EnsureSuccessStatusCode();

            // Duplicate cancellation -> 400
            var dupCancel = await _bookingClient.PutAsJsonAsync($"/api/Bookings/{bookingId}/cancel", new { reason = "Duplicate" });
            Assert.Equal(HttpStatusCode.BadRequest, dupCancel.StatusCode);

            var errJson = await dupCancel.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains("already been cancelled", errJson.GetProperty("message").GetString()!);
        }
    }
}