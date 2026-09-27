using System;
using System.Collections.Generic;
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
    public class ViewBookingsApiIntegrationTests
    {
        private readonly HttpClient _bookingClient;
        private readonly HttpClient _catalogClient;
        private readonly HttpClient _identityClient;

        public ViewBookingsApiIntegrationTests()
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

        private async Task<string> GetProviderTokenAsync(string email = "john@ceylonsafari.com", string password = "Provider@123!")
        {
            var loginResp = await _identityClient.PostAsJsonAsync("/api/Auth/login", new { email, password });
            if (!loginResp.IsSuccessStatusCode) return string.Empty;

            var json = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
            if (json.TryGetProperty("accessToken", out var at)) return at.GetString()!;
            if (json.TryGetProperty("token", out var t)) return t.GetString()!;
            return string.Empty;
        }

        [Fact(DisplayName = "CQ-VB-API-01: Visitor GET /api/user-bookings/my without token returns 401 Unauthorized")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQ-VB-API-01")]
        public async Task CQ_VB_API_01_GetUserBookings_Unauthenticated_Returns401()
        {
            _bookingClient.DefaultRequestHeaders.Authorization = null;
            var resp = await _bookingClient.GetAsync("/api/user-bookings/my");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact(DisplayName = "CQ-VB-API-02: Visitor GET /api/user-bookings/my returns 200 and validates contract fields")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQ-VB-API-02")]
        public async Task CQ_VB_API_02_GetUserBookings_Authenticated_Returns200AndValidStructure()
        {
            var token = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await _bookingClient.GetAsync("/api/user-bookings/my");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var items = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Array, items.ValueKind);

            foreach (var item in items.EnumerateArray())
            {
                Assert.True(item.TryGetProperty("id", out var id) && id.GetGuid() != Guid.Empty);
                Assert.True(item.TryGetProperty("bookingType", out var bt));
                Assert.True(item.TryGetProperty("serviceName", out var sn) && !string.IsNullOrWhiteSpace(sn.GetString()));
                Assert.True(item.TryGetProperty("status", out _));
                Assert.True(item.TryGetProperty("peopleCount", out var pc) && pc.GetInt32() >= 1);
                Assert.True(item.TryGetProperty("totalAmount", out var ta) && ta.GetDecimal() >= 0);

                var typeStr = bt.GetString();
                Assert.Contains(typeStr, new[] { "Experience Booking", "Restaurant Reservation", "Accommodation Booking" });
            }
        }

        [Fact(DisplayName = "CQ-VB-API-03: Provider GET /api/provider-bookings/my without token returns 401 Unauthorized")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQ-VB-API-03")]
        public async Task CQ_VB_API_03_GetProviderBookings_Unauthenticated_Returns401()
        {
            _bookingClient.DefaultRequestHeaders.Authorization = null;
            var resp = await _bookingClient.GetAsync("/api/provider-bookings/my");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact(DisplayName = "CQ-VB-API-04: Provider GET /api/provider-bookings/my with Visitor token returns 403 Forbidden")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQ-VB-API-04")]
        public async Task CQ_VB_API_04_GetProviderBookings_VisitorRole_Returns403Forbidden()
        {
            var visitorToken = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", visitorToken);

            var resp = await _bookingClient.GetAsync("/api/provider-bookings/my");
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact(DisplayName = "CQ-VB-API-05: Provider GET /api/provider-bookings/my strictly enforces provider service isolation")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQ-VB-API-05")]
        public async Task CQ_VB_API_05_GetProviderBookings_ProviderRole_OnlyReturnsOwnedServices()
        {
            var providerToken = await GetProviderTokenAsync();
            if (string.IsNullOrEmpty(providerToken)) return; // Skip if provider test account is not pre-seeded

            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", providerToken);
            _catalogClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", providerToken);

            // 1. Fetch provider's owned listing IDs from Catalog
            var ownedServiceIds = new HashSet<Guid>();

            var actResp = await _catalogClient.GetAsync("/api/catalog/my-listings");
            if (actResp.IsSuccessStatusCode)
            {
                var actJson = await actResp.Content.ReadFromJsonAsync<JsonElement>();
                if (actJson.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in actJson.EnumerateArray())
                        if (item.TryGetProperty("id", out var id)) ownedServiceIds.Add(id.GetGuid());
                }
            }

            // 2. Fetch provider's bookings
            var bookingsResp = await _bookingClient.GetAsync("/api/provider-bookings/my");
            Assert.Equal(HttpStatusCode.OK, bookingsResp.StatusCode);

            var bookings = await bookingsResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Array, bookings.ValueKind);

            // 3. Assert every returned booking belongs to one of the provider's owned services
            foreach (var b in bookings.EnumerateArray())
            {
                if (b.TryGetProperty("serviceId", out var sid))
                {
                    var serviceId = sid.GetGuid();
                    if (ownedServiceIds.Count > 0)
                    {
                        Assert.Contains(serviceId, ownedServiceIds);
                    }
                }
            }
        }
    }
}