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

        [Fact(DisplayName = "API: Reserve Table returns 200 OK with Confirmed status")]
        public async Task CreateReservation_ValidDetails_Returns200WithConfirmedStatus()
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

            var resp = await _bookingClient.PostAsJsonAsync("/api/Reservations", payload);
            var body = await resp.Content.ReadAsStringAsync();
            Assert.True(resp.IsSuccessStatusCode, $"Expected 200 OK, got {(int)resp.StatusCode}: {body}");

            var doc = JsonDocument.Parse(body);
            Assert.Equal("Confirmed", doc.RootElement.GetProperty("status").GetString());
            Assert.True(doc.RootElement.GetProperty("totalPrice").GetDecimal() > 0);
        }

        [Fact(DisplayName = "API: Party size exceeding capacity returns 400 Bad Request")]
        public async Task CreateReservation_ExceedingCapacity_Returns400BadRequest()
        {
            var token = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var (restaurantId, date, slot, remaining) = await GetAvailableRestaurantSlotAsync();

            var payload = new
            {
                restaurantId,
                reservationDate = date,
                timeSlot = slot,
                partySize = remaining + 50
            };

            var resp = await _bookingClient.PostAsJsonAsync("/api/Reservations", payload);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact(DisplayName = "API: Past reservation date returns 400 Bad Request")]
        public async Task CreateReservation_PastDate_Returns400BadRequest()
        {
            var token = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var (restaurantId, _, slot, _) = await GetAvailableRestaurantSlotAsync();

            var payload = new
            {
                restaurantId,
                reservationDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
                timeSlot = slot,
                partySize = 2
            };

            var resp = await _bookingClient.PostAsJsonAsync("/api/Reservations", payload);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact(DisplayName = "API: Unauthenticated request returns 401 Unauthorized")]
        public async Task CreateReservation_NoAuth_Returns401Unauthorized()
        {
            _bookingClient.DefaultRequestHeaders.Authorization = null;

            var payload = new
            {
                restaurantId = Guid.NewGuid(),
                reservationDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
                timeSlot = "19:00",
                partySize = 2
            };

            var resp = await _bookingClient.PostAsJsonAsync("/api/Reservations", payload);
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact(DisplayName = "API: GetMyReservations returns 200 OK with list")]
        public async Task GetMyReservations_AuthenticatedUser_Returns200WithList()
        {
            var token = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await _bookingClient.GetAsync("/api/Reservations/my");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        }
    }
}