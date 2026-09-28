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
    public class AdminBookingsRevenueReportApiTests
    {
        private readonly HttpClient _bookingClient;
        private readonly HttpClient _identityClient;

        public AdminBookingsRevenueReportApiTests()
        {
            _bookingClient = new HttpClient { BaseAddress = new Uri("http://localhost:5229") };
            _identityClient = new HttpClient { BaseAddress = new Uri("http://localhost:5278") };
        }

        private async Task<string> GetAdminTokenAsync()
        {
            var loginResp = await _identityClient.PostAsJsonAsync("/api/Auth/login", new
            {
                email = "adminceylonquest@gmail.com",
                password = "AdminPassword123!"
            });
            loginResp.EnsureSuccessStatusCode();
            var json = await loginResp.Content.ReadFromJsonAsync<JsonElement>();

            if (json.TryGetProperty("accessToken", out var at)) return at.GetString()!;
            if (json.TryGetProperty("token", out var t)) return t.GetString()!;
            throw new InvalidOperationException("Could not extract admin token.");
        }

        [Fact(DisplayName = "CQ-ADM-RP-API-01: GET /api/admin/reports/bookings-revenue without token returns 401")]
        [Trait("Story", "9.3")]
        [Trait("TestCase", "CQ-ADM-RP-API-01")]
        public async Task CQ_ADM_RP_API_01_AdminReport_Unauthenticated_Returns401()
        {
            _bookingClient.DefaultRequestHeaders.Authorization = null;
            var resp = await _bookingClient.GetAsync("/api/admin/reports/bookings-revenue");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact(DisplayName = "CQ-ADM-RP-API-02: Admin GET /api/admin/reports/bookings-revenue returns 200 and system-wide totals")]
        [Trait("Story", "9.3")]
        [Trait("TestCase", "CQ-ADM-RP-API-02")]
        public async Task CQ_ADM_RP_API_02_AdminReport_Authenticated_Returns200()
        {
            var adminToken = await GetAdminTokenAsync();
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var resp = await _bookingClient.GetAsync("/api/admin/reports/bookings-revenue");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.TryGetProperty("totalBookings", out var tb) && tb.GetInt32() >= 0);
            Assert.True(json.TryGetProperty("totalRevenue", out var tr) && tr.GetDecimal() >= 0);
        }
    }
}