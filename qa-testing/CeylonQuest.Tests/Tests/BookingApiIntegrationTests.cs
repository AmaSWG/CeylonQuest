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

            // Supports both 'accessToken' and 'token' properties
            if (json.TryGetProperty("accessToken", out var at))
                return at.GetString()!;
            if (json.TryGetProperty("token", out var t))
                return t.GetString()!;
            throw new InvalidOperationException("Could not extract access token from auth response.");
        }

        private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url, object payload, string token)
        {
            var request = new HttpRequestMessage(method, url)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        private async Task<(Guid listingId, string date, string slot, int remainingCapacity, decimal price, int maxParticipants)> GetAvailableExperienceAsync()
        {
            // 1. Search for active experiences using the correct query param: type=experience
            var searchResp = await _catalogClient.GetAsync("/api/catalog/search?type=experience");
            searchResp.EnsureSuccessStatusCode();
            var searchJson = await searchResp.Content.ReadFromJsonAsync<JsonElement>();
            var items = searchJson.GetProperty("items").EnumerateArray()
                .Where(it => it.TryGetProperty("type", out var t) && t.GetString() == "Experience");

            foreach (var item in items)
            {
                var listingId = Guid.Parse(item.GetProperty("id").GetString()!);
                var price = item.GetProperty("price").GetDecimal();
                var maxParticipants = item.TryGetProperty("maxParticipants", out var mp) && mp.ValueKind == JsonValueKind.Number
                    ? mp.GetInt32()
                    : 10;

                // 2. Find the first date and slot with open capacity in the next 14 days
                for (int d = 1; d <= 14; d++)
                {
                    var candidateDate = DateTime.Today.AddDays(d).ToString("yyyy-MM-dd");
                    var availResp = await _catalogClient.GetAsync($"/api/catalog/availability/{listingId}?date={candidateDate}");
                    if (!availResp.IsSuccessStatusCode) continue;

                    var availJson = await availResp.Content.ReadFromJsonAsync<JsonElement>();
                    if (!availJson.GetProperty("isOperatingDay").GetBoolean()) continue;
                    if (availJson.GetProperty("isFullyBooked").GetBoolean()) continue;

                    var slots = availJson.GetProperty("slots").EnumerateArray();
                    foreach (var slot in slots)
                    {
                        var remaining = slot.GetProperty("remainingCapacity").GetInt32();
                        if (remaining > 0)
                        {
                            return (listingId, candidateDate, slot.GetProperty("timeSlot").GetString()!, remaining, price, maxParticipants);
                        }
                    }
                }
            }

            throw new InvalidOperationException("No operational experience with open capacity found in test catalog.");
        }
        private async Task<(Guid listingId, string date, string slot, int remaining)>
            FindListingWithKnownCapacityAsync()
        {
            var (listingId, date, slot, remaining, _, _) = await GetAvailableExperienceAsync();
            return (listingId, date, slot, remaining);
        }


        [Fact(DisplayName = "API: CreateBooking returns 401 Unauthorized when token is missing")]
        [Trait("Category", "API")]
        public async Task CreateBooking_WithoutToken_ReturnsUnauthorized()
        {
            var payload = new
            {
                listingId = Guid.NewGuid(),
                bookingDate = DateTime.Today.AddDays(2).ToString("yyyy-MM-dd"),
                timeSlot = "09:00 AM - 11:00 AM",
                participantCount = 1
            };

            var response = await _bookingClient.PostAsJsonAsync("/api/Bookings", payload);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "API: CreateBooking returns 400 Bad Request when date is in the past")]
        [Trait("Category", "API")]
        public async Task CreateBooking_DateInPast_ReturnsBadRequest()
        {
            var token = await GetVisitorTokenAsync();
            var payload = new
            {
                listingId = Guid.NewGuid(),
                bookingDate = DateTime.Today.AddDays(-2).ToString("yyyy-MM-dd"),
                timeSlot = "09:00 AM - 11:00 AM",
                participantCount = 1
            };

            var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/Bookings", payload, token);
            var response = await _bookingClient.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "API: CreateBooking returns 400 Bad Request when participant count is zero")]
        [Trait("Category", "API")]
        public async Task CreateBooking_ZeroGuests_ReturnsBadRequest()
        {
            var token = await GetVisitorTokenAsync();
            var payload = new
            {
                listingId = Guid.NewGuid(),
                bookingDate = DateTime.Today.AddDays(2).ToString("yyyy-MM-dd"),
                timeSlot = "09:00 AM - 11:00 AM",
                participantCount = 0
            };

            var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/Bookings", payload, token);
            var response = await _bookingClient.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "API: CreateBooking returns 400/409 when slot is fully booked")]
        [Trait("Category", "API")]
        public async Task CreateBooking_FullyBookedSlot_ReturnsConflict()
        {
            var token = await GetVisitorTokenAsync();
            var (listingId, date, slot, remaining, _, maxParticipants) = await GetAvailableExperienceAsync();
            int guestsToFill = Math.Min(remaining, maxParticipants);
            // 1. Fill available capacity
            var fillRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/api/Bookings", new
            {
                listingId,
                bookingDate = date,
                timeSlot = slot,
                participantCount = guestsToFill
            }, token);
            await _bookingClient.SendAsync(fillRequest);
            // 2. Try to book 1 more seat on the slot
            var extraRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/api/Bookings", new
            {
                listingId,
                bookingDate = date,
                timeSlot = slot,
                participantCount = 1
            }, token);
            var response = await _bookingClient.SendAsync(extraRequest);
            // 3. Assert rejection
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict,
                $"Expected 400 Bad Request or 409 Conflict for exhausted slot, but got {response.StatusCode}");
        }

        [Fact(DisplayName = "API: CreateBooking decrements remaining capacity in catalog")]
        [Trait("Category", "API")]
        public async Task CreateBooking_DecrementsRemainingCapacity()
        {
            var token = await GetVisitorTokenAsync();
            var (listingId, date, slot, initialRemaining, _, _) = await GetAvailableExperienceAsync();
            int guestsToBook = Math.Min(2, initialRemaining);
            var payload = new
            {
                listingId = listingId,
                bookingDate = date,
                timeSlot = slot,
                participantCount = guestsToBook
            };
            // 1. Create Booking
            var createReq = CreateAuthenticatedRequest(HttpMethod.Post, "/api/Bookings", payload, token);
            var createResp = await _bookingClient.SendAsync(createReq);

            createResp.EnsureSuccessStatusCode();
            // 2. Query Availability after booking
            var afterResp = await _catalogClient.GetAsync($"/api/catalog/availability/{listingId}?date={date}");
            afterResp.EnsureSuccessStatusCode();
            var afterJson = await afterResp.Content.ReadFromJsonAsync<JsonElement>();
            var updatedSlot = afterJson.GetProperty("slots").EnumerateArray()
                .First(s => s.GetProperty("timeSlot").GetString() == slot);
            var updatedRemaining = updatedSlot.GetProperty("remainingCapacity").GetInt32();
            // 3. Assert capacity dropped exactly by guestsToBook
            Assert.Equal(initialRemaining - guestsToBook, updatedRemaining);
        }

        [Fact(DisplayName = "API: Overbooking capacity returns 409 Conflict")]
        [Trait("Category", "API")]
        public async Task CreateBooking_ExceedingAvailableCapacity_Returns409Conflict()
        {
            var token = await GetVisitorTokenAsync();
            var (listingId, date, slot, remainingCapacity, _, _) = await GetAvailableExperienceAsync();
            // Attempt to book 1 place more than currently remaining in the slot
            var payload = new
            {
                listingId = listingId,
                bookingDate = date,
                timeSlot = slot,
                participantCount = remainingCapacity + 1
            };
            var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/Bookings", payload, token);
            var response = await _bookingClient.SendAsync(request);
            // Must reject with Conflict (409) or Bad Request (400)
            Assert.True(response.StatusCode == HttpStatusCode.Conflict || response.StatusCode == HttpStatusCode.BadRequest,
                $"Expected 409 Conflict or 400 Bad Request for overbooking, but got {response.StatusCode}");
        }
        [Fact(DisplayName = "API: Persisted booking has Pending Payment status and correct TotalAmount")]
        [Trait("Category", "API")]
        public async Task CreateBooking_PersistsCorrectStatusAndAmount()
        {
            var token = await GetVisitorTokenAsync();
            var (listingId, date, slot, remaining, basePrice, _) = await GetAvailableExperienceAsync();
            int guests = 1;
            var payload = new
            {
                listingId = listingId,
                bookingDate = date,
                timeSlot = slot,
                participantCount = guests
            };
            var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/Bookings", payload, token);
            var response = await _bookingClient.SendAsync(request);

            response.EnsureSuccessStatusCode();
            var booking = await response.Content.ReadFromJsonAsync<JsonElement>();
            // Assert Booking Status
            var status = booking.GetProperty("status").GetString();
            Assert.True(status == "PendingPayment" || status == "Pending Payment", $"Expected status Pending Payment, got {status}");
            // Assert Payment Status
            var paymentStatus = booking.GetProperty("paymentStatus").GetString();
            Assert.Equal("Unpaid", paymentStatus);
            // Assert Total Amount calculation
            var totalAmount = booking.GetProperty("totalAmount").GetDecimal();
            Assert.Equal(basePrice * guests, totalAmount);
        }
        [Fact(DisplayName = "API: participantCount > maxParticipants returns 400 Bad Request")]
        [Trait("Category", "API")]
        public async Task CreateBooking_ParticipantCountExceedsListingMax_Returns400BadRequest()
        {
            var token = await GetVisitorTokenAsync();
            var (listingId, date, slot, _, _, maxParticipants) = await GetAvailableExperienceAsync();
            var payload = new
            {
                listingId = listingId,
                bookingDate = date,
                timeSlot = slot,
                participantCount = maxParticipants + 5 // Exceeds experience limit
            };
            var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/Bookings", payload, token);
            var response = await _bookingClient.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var errorBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("Maximum participants", errorBody, StringComparison.OrdinalIgnoreCase);
        }

    }
}