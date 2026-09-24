using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookingService.DTOs;

namespace BookingService.Services;

public class IdentityService : IIdentityService
{
    private readonly HttpClient _httpClient;

    public IdentityService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BookingCustomerResponse?> GetBookingCustomerAsync(
        Guid customerId,
        string accessToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/users/{customerId}/booking-profile");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        using var response =
            await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<BookingCustomerResponse>();
    }
}