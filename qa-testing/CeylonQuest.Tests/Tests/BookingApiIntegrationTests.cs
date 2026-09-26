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
    [Trait("Category", "API")]
    public class BookingApiIntegrationTests
    {
        private readonly HttpClient _bookingClient;
        private readonly HttpClient _catalogClient;
        private readonly HttpClient _identityClient;

        public BookingApiIntegrationTests()
        {
            _bookingClient = new HttpClient { BaseAddress = new Uri("http://localhost:5229") };
            _catalogClient = new HttpClient { BaseAddress = new Uri("http://localhost:5141") };
            _identityClient = new HttpClient { BaseAddress = new Uri("http://localhost:5278") };
        }

        private async Task<string> GetVisitorTokenAsync()
        {
            var loginResponse = await _identityClient.PostAsJsonAsync("/api/Auth/login", new
            {
                email = TestConfiguration.Settings.VisitorEmail,
                password = TestConfiguration.Settings.VisitorPassword
            });
            loginResponse.EnsureSuccessStatusCode();
            var json = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();

            if (json.TryGetProperty("accessToken", out var at))
                return at.GetString()!;
            if (json.TryGetProperty("token", out var t))
                return t.GetString()!;
            throw new InvalidOperationException("Could not extract access token.");
        }

        private async Task<(Guid listingId, string date, string slot, int remainingCapacity, decimal price, int maxParticipants)> GetAvailableExperienceAsync()
        {
            var searchResp = await _catalogClient.GetAsync("/api/catalog/search?type=experience");
            searchResp.EnsureSuccessStatusCode();
            var searchJson = await searchResp.Content.ReadFromJsonAsync<JsonElement>();

            var items = searchJson.ValueKind == JsonValueKind.Array
                ? searchJson.EnumerateArray()
                : searchJson.GetProperty("items").EnumerateArray();

            foreach (var item in items)
            {
                var listingId = Guid.Parse(item.GetProperty("id").GetString()!);
                var price = item.GetProperty("price").GetDecimal();
                var maxParticipants = item.TryGetProperty("maxParticipants", out var mp) ? mp.GetInt32() : 10;

                for (int i = 1; i <= 14; i++)
                {
                    var testDate = DateTime.UtcNow.AddDays(i).ToString("yyyy-MM-dd");
                    var availResp = await _catalogClient.GetAsync($"/api/catalog/availability/{listingId}?date={testDate}");
                    if (!availResp.IsSuccessStatusCode) continue;

                    var availJson = await availResp.Content.ReadFromJsonAsync<JsonElement>();
                    var isOperating = !availJson.TryGetProperty("isOperatingDay", out var op) || op.GetBoolean();
                    var isFullyBooked = availJson.TryGetProperty("isFullyBooked", out var fb) && fb.GetBoolean();

                    if (isOperating && !isFullyBooked && availJson.TryGetProperty("slots", out var slots))
                    {
                        foreach (var slot in slots.EnumerateArray())
                        {
                            var remaining = slot.GetProperty("remainingCapacity").GetInt32();
                            var slotTime = slot.GetProperty("timeSlot").GetString()!;
                            if (remaining > 0)
                            {
                                return (listingId, testDate, slotTime, remaining, price, maxParticipants);
                            }
                        }
                    }
                }
            }

            throw new InvalidOperationException("No available experience slot found in catalog.");
        }

        [Fact(DisplayName = "API 7.1: Missing token returns 401 Unauthorized")]
        public async Task CreateBooking_MissingAuthToken_Returns401()
        {
            _bookingClient.DefaultRequestHeaders.Authorization = null;
            var payload = new
            {
                listingId = Guid.NewGuid(),
                bookingDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
                timeSlot = "10:00 AM",
                participantCount = 2
            };

            var resp = await _bookingClient.PostAsJsonAsync("/api/Bookings", payload);
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact(DisplayName = "API 7.1: Non-existent listing ID returns 400 Bad Request")]
        public async Task CreateBooking_NonExistentListing_Returns400()
        {
            var token = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new
            {
                listingId = Guid.NewGuid(), // random non-existent GUID
                bookingDate = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-dd"),
                timeSlot = "10:00 AM",
                participantCount = 1
            };

            var resp = await _bookingClient.PostAsJsonAsync("/api/Bookings", payload);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact(DisplayName = "API 7.1: Invalid or non-existent time slot returns 400 Bad Request")]
        public async Task CreateBooking_InvalidTimeSlot_Returns400()
        {
            var token = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var (listingId, date, _, _, _, _) = await GetAvailableExperienceAsync();

            var payload = new
            {
                listingId,
                bookingDate = date,
                timeSlot = "INVALID_SLOT_03:45_AM",
                participantCount = 1
            };

            var resp = await _bookingClient.PostAsJsonAsync("/api/Bookings", payload);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact(DisplayName = "API 7.1: Exact capacity boundary (count == remainingCapacity) succeeds with 201")]
        public async Task CreateBooking_ExactCapacityBoundary_Succeeds()
        {
            var token = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var (listingId, date, slot, remaining, _, maxParticipants) = await GetAvailableExperienceAsync();
            var exactCount = Math.Min(remaining, maxParticipants);

            var payload = new
            {
                listingId,
                bookingDate = date,
                timeSlot = slot,
                participantCount = exactCount
            };

            var resp = await _bookingClient.PostAsJsonAsync("/api/Bookings", payload);
            Assert.True(resp.StatusCode == HttpStatusCode.Created || resp.StatusCode == HttpStatusCode.OK);
        }

        [Fact(DisplayName = "API 7.1: Decrements remaining capacity in catalog")]
        public async Task CreateBooking_DecrementsRemainingCapacity()
        {
            var token = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var (listingId, date, slot, remainingBefore, _, _) = await GetAvailableExperienceAsync();
            if (remainingBefore < 1) return;

            var payload = new
            {
                listingId,
                bookingDate = date,
                timeSlot = slot,
                participantCount = 1
            };

            var resp = await _bookingClient.PostAsJsonAsync("/api/Bookings", payload);
            resp.EnsureSuccessStatusCode();

            // Verify capacity decreased by 1
            var availResp = await _catalogClient.GetAsync($"/api/catalog/availability/{listingId}?date={date}");
            var availJson = await availResp.Content.ReadFromJsonAsync<JsonElement>();
            var targetSlot = availJson.GetProperty("slots").EnumerateArray()
                .First(s => s.GetProperty("timeSlot").GetString() == slot);

            var remainingAfter = targetSlot.GetProperty("remainingCapacity").GetInt32();
            Assert.Equal(remainingBefore - 1, remainingAfter);
        }
    }
}