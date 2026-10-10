using System.Net.Http.Json;
using System.Text.Json;

namespace BookingService.Services;

public class ReviewerProfileClient(HttpClient client)
{
    public async Task<string> GetDisplayNameAsync(
        string authorization,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(authorization))
            throw new ReviewException(401, "Visitor authentication is required.");

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get, "/api/users/me");

            request.Headers.TryAddWithoutValidation(
                "Authorization", authorization);

            using var response = await client.SendAsync(request, token);

            if (!response.IsSuccessStatusCode)
                throw new ReviewException(
                    503, "Your profile could not be loaded. Please try again.");

            var profile = await response.Content
                .ReadFromJsonAsync<ReviewerProfile>(cancellationToken: token);

            var first = profile?.FirstName?.Trim();
            var last = profile?.LastName?.Trim();

            if (string.IsNullOrWhiteSpace(first))
                return "Visitor";

            // Public display name; do not publish email or account identifiers.
            var name = string.IsNullOrWhiteSpace(last)
                ? first
                : $"{first} {last[0]}.";

            return name.Length > 100 ? name[..100] : name;
        }
        catch (Exception ex) when (
            ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            token.ThrowIfCancellationRequested();

            throw new ReviewException(
                503, "Your profile could not be loaded. Please try again.");
        }
    }

    private sealed class ReviewerProfile
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}