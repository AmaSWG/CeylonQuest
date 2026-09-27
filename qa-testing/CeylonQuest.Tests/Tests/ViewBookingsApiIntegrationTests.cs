using System;
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
    public class ViewBookingsApiIntegrationTests
    {
        private readonly HttpClient _bookingClient;
        private readonly HttpClient _identityClient;

        public ViewBookingsApiIntegrationTests()
        {
            _bookingClient = new HttpClient { BaseAddress = new Uri("http://localhost:5229") };
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

            if (json.TryGetProperty("accessToken", out var at))
                return at.GetString()!;
            if (json.TryGetProperty("token", out var t))
                return t.GetString()!;
            throw new InvalidOperationException("Could not extract token.");
        }

        [Fact(DisplayName = "CQVB-05: GET /api/user-bookings/my without token returns 401 Unauthorized")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQVB-05")]
        public async Task CQVB_05_GetUserBookings_Unauthenticated_Returns401()
        {
            _bookingClient.DefaultRequestHeaders.Authorization = null;
            var resp = await _bookingClient.GetAsync("/api/user-bookings/my");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact(DisplayName = "CQVB-06: GET /api/user-bookings/my with valid token returns 200 and booking array")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQVB-06")]
        public async Task CQVB_06_GetUserBookings_Authenticated_Returns200AndValidStructure()
        {
            var token = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await _bookingClient.GetAsync("/api/user-bookings/my");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var items = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Array, items.ValueKind);

            foreach (var item in items.EnumerateArray())
            {
                // Verify unified contract fields
                Assert.True(item.TryGetProperty("id", out _));
                Assert.True(item.TryGetProperty("bookingType", out var bt));
                Assert.True(item.TryGetProperty("serviceName", out _));
                Assert.True(item.TryGetProperty("status", out _));
                Assert.True(item.TryGetProperty("peopleCount", out _));
                Assert.True(item.TryGetProperty("totalAmount", out _));

                var typeStr = bt.GetString();
                Assert.True(
                    typeStr == "Experience Booking" ||
                    typeStr == "Restaurant Reservation" ||
                    typeStr == "Accommodation Booking",
                    $"Unexpected booking type: {typeStr}"
                );
            }
        }

        [Fact(DisplayName = "CQVB-07: GET /api/provider-bookings/my without token returns 401 Unauthorized")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQVB-07")]
        public async Task CQVB_07_GetProviderBookings_Unauthenticated_Returns401()
        {
            _bookingClient.DefaultRequestHeaders.Authorization = null;
            var resp = await _bookingClient.GetAsync("/api/provider-bookings/my");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact(DisplayName = "CQVB-08: GET /api/provider-bookings/my with visitor token returns 403 Forbidden")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQVB-08")]
        public async Task CQVB_08_GetProviderBookings_VisitorRole_Returns403Forbidden()
        {
            var visitorToken = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", visitorToken);

            var resp = await _bookingClient.GetAsync("/api/provider-bookings/my");
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }
    }
}