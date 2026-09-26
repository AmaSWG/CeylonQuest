using CeylonQuest.Tests.Configuration;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace CeylonQuest.Tests.Tests
{
    [Trait("Category", "API")]
    public class RestaurantReservationApiTests
    {
        private readonly HttpClient _catalogClient;
        private readonly HttpClient _bookingClient;
        private readonly HttpClient _identityClient;

        public RestaurantReservationApiTests()
        {
            _catalogClient = new HttpClient { BaseAddress = new Uri("http://localhost:5141") };
            _bookingClient = new HttpClient { BaseAddress = new Uri("http://localhost:5229") };
            _identityClient = new HttpClient { BaseAddress = new Uri("http://localhost:5278") };
        }

        private async Task<string> GetVisitorTokenAsync()
        {
            var loginPayload = new
            {
                email = TestConfiguration.Settings.VisitorEmail,
                password = TestConfiguration.Settings.VisitorPassword
            };

            var resp = await _identityClient.PostAsJsonAsync("/api/Auth/login", loginPayload);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            if (json.TryGetProperty("accessToken", out var at)) return at.GetString()!;
            if (json.TryGetProperty("token", out var t)) return t.GetString()!;
            throw new InvalidOperationException("Could not extract token.");
        }

        private async Task<(Guid restaurantId, string date, string slot, int capacity)> GetAvailableRestaurantSlotAsync()
        {
            var searchResp = await _catalogClient.GetAsync("/api/catalog/search?pageSize=50");
            searchResp.EnsureSuccessStatusCode();

            var doc = await JsonDocument.ParseAsync(await searchResp.Content.ReadAsStreamAsync());
            var items = doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement.EnumerateArray()
                : doc.RootElement.GetProperty("items").EnumerateArray();

            var restaurant = items.FirstOrDefault(i =>
                i.TryGetProperty("type", out var t) &&
                string.Equals(t.GetString(), "Restaurant", StringComparison.OrdinalIgnoreCase));

            if (restaurant.ValueKind == JsonValueKind.Undefined)
                throw new InvalidOperationException("No Restaurant listings found in catalog.");

            var restaurantId = Guid.Parse(restaurant.GetProperty("id").GetString()!);

            for (int i = 1; i <= 14; i++)
            {
                var candidateDate = DateTime.UtcNow.AddDays(i).ToString("yyyy-MM-dd");
                var availResp = await _catalogClient.GetAsync($"/api/catalog/availability/{restaurantId}?date={candidateDate}");
                if (!availResp.IsSuccessStatusCode) continue;

                var availDoc = await JsonDocument.ParseAsync(await availResp.Content.ReadAsStreamAsync());
                var isOperating = !availDoc.RootElement.TryGetProperty("isOperatingDay", out var op) || op.GetBoolean();
                var isFullyBooked = availDoc.RootElement.TryGetProperty("isFullyBooked", out var fb) && fb.GetBoolean();

                if (isOperating && !isFullyBooked && availDoc.RootElement.TryGetProperty("slots", out var slots))
                {
                    foreach (var slot in slots.EnumerateArray())
                    {
                        var remaining = slot.GetProperty("remainingCapacity").GetInt32();
                        var slotTime = slot.GetProperty("timeSlot").GetString()!;
                        if (remaining > 0)
                        {
                            return (restaurantId, candidateDate, slotTime, remaining);
                        }
                    }
                }
            }

            throw new InvalidOperationException("No available slot found for restaurant.");
        }

        [Fact(DisplayName = "API 8.1: Zero or negative party size returns 400 Bad Request")]
        public async Task CreateReservation_ZeroPartySize_Returns400()
        {
            var token = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var (restaurantId, date, slot, _) = await GetAvailableRestaurantSlotAsync();

            var payload = new
            {
                restaurantId,
                reservationDate = date,
                timeSlot = slot,
                partySize = 0
            };

            var resp = await _bookingClient.PostAsJsonAsync("/api/Reservations", payload);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact(DisplayName = "API 8.1: Invalid or non-existent time slot returns 400 Bad Request")]
        public async Task CreateReservation_InvalidTimeSlot_Returns400()
        {
            var token = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var (restaurantId, date, _, _) = await GetAvailableRestaurantSlotAsync();

            var payload = new
            {
                restaurantId,
                reservationDate = date,
                timeSlot = "INVALID_11:99_PM",
                partySize = 2
            };

            var resp = await _bookingClient.PostAsJsonAsync("/api/Reservations", payload);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact(DisplayName = "API 8.1: Exact capacity boundary reservation succeeds")]
        public async Task CreateReservation_ExactCapacityBoundary_Succeeds()
        {
            var token = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var (restaurantId, date, slot, remaining) = await GetAvailableRestaurantSlotAsync();

            var payload = new
            {
                restaurantId,
                reservationDate = date,
                timeSlot = slot,
                partySize = remaining
            };

            var resp = await _bookingClient.PostAsJsonAsync("/api/Reservations", payload);
            Assert.True(resp.IsSuccessStatusCode, "Exact remaining capacity should be reserved successfully.");
        }

        [Fact(DisplayName = "API 8.1: Reservation decrements restaurant availability")]
        public async Task CreateReservation_DecrementsRemainingCapacity()
        {
            var token = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var (restaurantId, date, slot, remainingBefore) = await GetAvailableRestaurantSlotAsync();
            if (remainingBefore < 1) return;

            var payload = new
            {
                restaurantId,
                reservationDate = date,
                timeSlot = slot,
                partySize = 1
            };

            var resp = await _bookingClient.PostAsJsonAsync("/api/Reservations", payload);
            resp.EnsureSuccessStatusCode();

            // Verify updated capacity
            var availResp = await _catalogClient.GetAsync($"/api/catalog/availability/{restaurantId}?date={date}");
            var availDoc = await JsonDocument.ParseAsync(await availResp.Content.ReadAsStreamAsync());
            var updatedSlot = availDoc.RootElement.GetProperty("slots").EnumerateArray()
                .First(s => s.GetProperty("timeSlot").GetString() == slot);

            var remainingAfter = updatedSlot.GetProperty("remainingCapacity").GetInt32();
            Assert.Equal(remainingBefore - 1, remainingAfter);
        }

        [Fact(DisplayName = "API 8.1: Created reservation exists in GetMyReservations response")]
        public async Task CreateReservation_PersistsAndAppearsInMyReservations()
        {
            var token = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var (restaurantId, date, slot, _) = await GetAvailableRestaurantSlotAsync();

            var payload = new
            {
                restaurantId,
                reservationDate = date,
                timeSlot = slot,
                partySize = 2
            };

            var createResp = await _bookingClient.PostAsJsonAsync("/api/Reservations", payload);
            createResp.EnsureSuccessStatusCode();
            var createdDoc = await JsonDocument.ParseAsync(await createResp.Content.ReadAsStreamAsync());
            var reservationId = createdDoc.RootElement.GetProperty("id").GetString();

            // Query My Reservations
            var myResp = await _bookingClient.GetAsync("/api/Reservations/my");
            myResp.EnsureSuccessStatusCode();
            var myDoc = await JsonDocument.ParseAsync(await myResp.Content.ReadAsStreamAsync());

            var exists = myDoc.RootElement.EnumerateArray().Any(r =>
                r.GetProperty("id").GetString() == reservationId);

            Assert.True(exists, "Newly created reservation must be present in My Reservations history.");
        }
    }
}