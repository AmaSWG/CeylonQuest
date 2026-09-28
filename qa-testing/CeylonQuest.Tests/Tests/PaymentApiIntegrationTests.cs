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
    public class PaymentApiIntegrationTests
    {
        private readonly HttpClient _bookingClient;
        private readonly HttpClient _catalogClient;
        private readonly HttpClient _identityClient;

        public PaymentApiIntegrationTests()
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

        private async Task<Guid?> CreateTestBookingAsync(string token)
        {
            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var searchResp = await _catalogClient.GetAsync("/api/catalog/search?type=experience");
            if (!searchResp.IsSuccessStatusCode) return null;
            var searchJson = await searchResp.Content.ReadFromJsonAsync<JsonElement>();

            var items = searchJson.ValueKind == JsonValueKind.Array
                ? searchJson.EnumerateArray()
                : searchJson.GetProperty("items").EnumerateArray();

            foreach (var item in items)
            {
                var listingId = Guid.Parse(item.GetProperty("id").GetString()!);
                var testDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd");

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

        [Fact(DisplayName = "CQ-PAY-API-01: GET /api/Payments/booking/{id} without token returns 401 Unauthorized")]
        [Trait("Story", "10.1")]
        [Trait("TestCase", "CQ-PAY-API-01")]
        public async Task CQ_PAY_API_01_GetPaymentDetails_Unauthenticated_Returns401()
        {
            _bookingClient.DefaultRequestHeaders.Authorization = null;
            var resp = await _bookingClient.GetAsync($"/api/Payments/booking/{Guid.NewGuid()}?bookingType=Experience");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact(DisplayName = "CQ-PAY-API-02: GET /api/Payments/booking/{id} returns accurate booking payment details")]
        [Trait("Story", "10.1")]
        [Trait("TestCase", "CQ-PAY-API-02")]
        public async Task CQ_PAY_API_02_GetPaymentDetails_ValidBooking_Returns200AndAccurateAmount()
        {
            var token = await GetVisitorTokenAsync();
            var bookingId = await CreateTestBookingAsync(token);
            if (bookingId == null) return;

            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var resp = await _bookingClient.GetAsync($"/api/Payments/booking/{bookingId}?bookingType=Experience");
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(bookingId.Value, Guid.Parse(json.GetProperty("bookingId").GetString()!));
            Assert.Equal("LKR", json.GetProperty("currency").GetString());
            Assert.True(json.GetProperty("totalAmount").GetDecimal() > 0);
        }

        [Fact(DisplayName = "CQ-PAY-API-03: POST /api/Payments/create-checkout-session creates session with Stripe Sandbox")]
        [Trait("Story", "10.1")]
        [Trait("TestCase", "CQ-PAY-API-03")]
        public async Task CQ_PAY_API_03_CreateCheckoutSession_Returns200AndStripeUrl()
        {
            var token = await GetVisitorTokenAsync();
            var bookingId = await CreateTestBookingAsync(token);
            if (bookingId == null) return;

            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var resp = await _bookingClient.PostAsJsonAsync("/api/Payments/create-checkout-session", new
            {
                bookingId = bookingId.Value,
                bookingType = "Experience"
            });
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.TryGetProperty("checkoutUrl", out var checkoutUrl));
            Assert.False(string.IsNullOrWhiteSpace(checkoutUrl.GetString()));
            Assert.StartsWith("PAY-", json.GetProperty("transactionReference").GetString());
        }

        [Fact(DisplayName = "CQ-PAY-API-04: POST /api/Payments/cancel-checkout/{id} records payment cancellation")]
        [Trait("Story", "10.1")]
        [Trait("TestCase", "CQ-PAY-API-04")]
        public async Task CQ_PAY_API_04_CancelCheckout_UpdatesTransactionToFailed()
        {
            var token = await GetVisitorTokenAsync();
            var bookingId = await CreateTestBookingAsync(token);
            if (bookingId == null) return;

            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 1. Create checkout
            var sessionResp = await _bookingClient.PostAsJsonAsync("/api/Payments/create-checkout-session", new
            {
                bookingId = bookingId.Value,
                bookingType = "Experience"
            });
            sessionResp.EnsureSuccessStatusCode();
            var sessionJson = await sessionResp.Content.ReadFromJsonAsync<JsonElement>();
            var transactionId = Guid.Parse(sessionJson.GetProperty("transactionId").GetString()!);

            // 2. Cancel checkout
            var cancelResp = await _bookingClient.PostAsJsonAsync($"/api/Payments/cancel-checkout/{transactionId}", new { });
            cancelResp.EnsureSuccessStatusCode();

            var cancelJson = await cancelResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Failed", cancelJson.GetProperty("paymentStatus").GetString());
            Assert.Equal("PendingPayment", cancelJson.GetProperty("bookingStatus").GetString());
        }

        [Fact(DisplayName = "CQ-PAY-API-05: POST /api/Payments/create-checkout-session on cancelled booking returns 400")]
        [Trait("Story", "10.1")]
        [Trait("TestCase", "CQ-PAY-API-05")]
        public async Task CQ_PAY_API_05_CreateCheckoutSession_CancelledBooking_Returns400()
        {
            var token = await GetVisitorTokenAsync();
            var bookingId = await CreateTestBookingAsync(token);
            if (bookingId == null) return;

            _bookingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Cancel the booking first
            var cancelResp = await _bookingClient.PutAsJsonAsync($"/api/Bookings/{bookingId}/cancel", new { reason = "Test cancel" });
            cancelResp.EnsureSuccessStatusCode();

            // Attempt payment checkout on cancelled booking
            var payResp = await _bookingClient.PostAsJsonAsync("/api/Payments/create-checkout-session", new
            {
                bookingId = bookingId.Value,
                bookingType = "Experience"
            });
            Assert.Equal(HttpStatusCode.BadRequest, payResp.StatusCode);

            var errJson = await payResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains("cancelled booking", errJson.GetProperty("message").GetString()!);
        }
    }
}