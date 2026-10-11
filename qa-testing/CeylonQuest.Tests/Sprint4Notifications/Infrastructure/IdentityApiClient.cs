using System.Net.Http.Json;

namespace CeylonQuest.Tests.Sprint4Notifications.Infrastructure;

/// <summary>
/// Registers and logs in real identity-service accounts for use as test actors. Visitor
/// registration (unlike provider onboarding) needs no OTP/email activation, so this is the only
/// account type the suite can reliably self-provision - see README.md for why "provider"
/// notification recipients in this suite are a second plain visitor account used as a stand-in.
/// </summary>
public sealed class IdentityApiClient
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(TestConfig.IdentityApiBaseUrl) };

    public async Task<TestUser> CreateVisitorAsync(string label, CancellationToken ct = default)
    {
        var email = $"qa.{label}.{Guid.NewGuid():N}@ceylonquest-tests.local";
        const string password = "QaTest!2026";

        var registerResponse = await _http.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "QA",
            lastName = label,
            email,
            phoneNumber = "0771234567",
            nationality = "Sri Lankan",
            password,
            confirmPassword = password
        }, ct);
        registerResponse.EnsureSuccessStatusCode();
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(JsonDefaults.Options, ct);

        var token = await LoginAsync(email, password, ct);
        return new TestUser(registered!.UserId, email, password, token);
    }

    public async Task<string> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/login", new { email, password }, ct);
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResult>(JsonDefaults.Options, ct);
        return login!.AccessToken;
    }

    private sealed record RegisterResponse(string Message, Guid UserId);
    private sealed record LoginResult(string AccessToken, string TokenType, string Role);
}
