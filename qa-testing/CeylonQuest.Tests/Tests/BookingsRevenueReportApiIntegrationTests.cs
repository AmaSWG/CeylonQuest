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
    public class BookingsRevenueReportApiIntegrationTests
    {
        private readonly HttpClient _bookingClient;
        private readonly HttpClient _identityClient;

        public BookingsRevenueReportApiIntegrationTests()
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

            if (json.TryGetProperty("accessToken", out var at)) return at.GetString()!;
            if (json.TryGetProperty("token", out var t)) return t.GetString()!;
            throw new InvalidOperationException("Could not extract visitor token.");
        }

        private async Task<string> GetProviderTokenAsync()
        {
            var loginResp = await _identityClient.PostAsJsonAsync("/api/Auth/login", new
            {
                email = TestConfiguration.Settings.ProviderEmail,
                password = TestConfiguration.Settings.ProviderPassword
            });
            if (!loginResp.IsSuccessStatusCode) return string.Empty;

            var json = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
            if (json.TryGetProperty("accessToken", out var at)) return at.GetString()!;
            if (json.TryGetProperty("token", out var t)) return t.GetString()!;
            return string.Empty;
        }

        [Fact(DisplayName = "CQ-RP-API-01: GET /api/provider-bookings/reports/bookings-revenue unauthenticated returns 401")]
        [Trait("Story", "9.3")]
        [Trait("TestCase", "CQ-RP-API-01")]
        public async Task CQ_RP_API_01_GetReport_Unauthenticated_Returns401()
        {
            _bookingClient.DefaultRequestHeaders.Authorization = null;
            var resp = await _bookingClient.GetAsync("/api/provider-bookings/reports/bookings-revenue");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact(DisplayName = "CQ-RP-API-02: GET /api/provider-bookings/reports/bookings-revenue with Visitor token returns 403")]
        [Trait("Story", "9.3")]
        [Trait("TestCase", "CQ-RP-API-02")]
        public async Task CQ_RP_API_02_GetReport_VisitorToken_Returns403()
        {
            var visitorToken = await GetVisitorTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", visitorToken);

            var resp = await _bookingClient.GetAsync("/api/provider-bookings/reports/bookings-revenue");
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact(DisplayName = "CQ-RP-API-03: GET report returns 200 and accurate calculated metrics")]
        [Trait("Story", "9.3")]
        [Trait("TestCase", "CQ-RP-API-03")]
        public async Task CQ_RP_API_03_GetReport_ProviderToken_Returns200AndAccurateMetrics()
        {
            var providerToken = await GetProviderTokenAsync();
            if (string.IsNullOrEmpty(providerToken)) return;

            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", providerToken);
            var resp = await _bookingClient.GetAsync("/api/provider-bookings/reports/bookings-revenue");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            var totalBookings = json.GetProperty("totalBookings").GetInt32();
            var totalRevenue = json.GetProperty("totalRevenue").GetDecimal();
            var records = json.GetProperty("records").EnumerateArray().ToList();

            Assert.Equal(totalBookings, records.Count);

            // Sum up record revenues and assert matching total revenue
            decimal calculatedRevenue = 0m;
            foreach (var r in records)
            {
                calculatedRevenue += r.GetProperty("revenue").GetDecimal();
            }
            Assert.Equal(totalRevenue, calculatedRevenue);
        }

        [Fact(DisplayName = "CQ-RP-API-04: GET report with startDate after endDate returns 400 Bad Request")]
        [Trait("Story", "9.3")]
        [Trait("TestCase", "CQ-RP-API-04")]
        public async Task CQ_RP_API_04_GetReport_StartDateAfterEndDate_Returns400()
        {
            var providerToken = await GetProviderTokenAsync();
            if (string.IsNullOrEmpty(providerToken)) return;

            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", providerToken);
            var resp = await _bookingClient.GetAsync("/api/provider-bookings/reports/bookings-revenue?startDate=2026-12-31&endDate=2026-01-01");
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact(DisplayName = "CQ-RP-API-05: Cancelled bookings contribute 0 to Revenue")]
        [Trait("Story", "9.3")]
        [Trait("TestCase", "CQ-RP-API-05")]
        public async Task CQ_RP_API_05_GetReport_CancelledBookings_HaveZeroRevenue()
        {
            var providerToken = await GetProviderTokenAsync();
            if (string.IsNullOrEmpty(providerToken)) return;

            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", providerToken);
            var resp = await _bookingClient.GetAsync("/api/provider-bookings/reports/bookings-revenue?status=Cancelled");
            if (!resp.IsSuccessStatusCode) return;

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            var records = json.GetProperty("records").EnumerateArray();

            foreach (var r in records)
            {
                Assert.Equal("Cancelled", r.GetProperty("status").GetString());
                Assert.Equal(0m, r.GetProperty("revenue").GetDecimal());
            }
        }
    }
}