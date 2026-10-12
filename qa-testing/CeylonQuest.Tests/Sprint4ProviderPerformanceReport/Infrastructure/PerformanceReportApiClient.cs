using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CeylonQuest.Tests.Sprint4ProviderPerformanceReport.Infrastructure;

public sealed class PerformanceReportApiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http = new();

    private static (string Token, string Role)? _cachedAdmin;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<(string Token, string Role)> LoginAsync(string email, string password)
    {
        if (email.Equals(PerformanceReportTestConfig.AdminEmail, StringComparison.OrdinalIgnoreCase) && _cachedAdmin.HasValue)
        {
            return _cachedAdmin.Value;
        }

        await _lock.WaitAsync();
        try
        {
            if (email.Equals(PerformanceReportTestConfig.AdminEmail, StringComparison.OrdinalIgnoreCase) && _cachedAdmin.HasValue)
            {
                return _cachedAdmin.Value;
            }

            var body = JsonSerializer.Serialize(new { email, password });
            using var resp = await _http.PostAsync(
                $"{PerformanceReportTestConfig.IdentityApiBaseUrl}/api/auth/login",
                new StringContent(body, Encoding.UTF8, "application/json"));

            var content = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Login failed for {email} (HTTP {(int)resp.StatusCode}): {content}");
            }

            using var doc = JsonDocument.Parse(content);
            var token = doc.RootElement.GetProperty("accessToken").GetString()!;
            var role = doc.RootElement.GetProperty("role").GetString()!;
            var result = (token, role);

            if (email.Equals(PerformanceReportTestConfig.AdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                _cachedAdmin = result;
            }

            return result;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<(string Email, string Password, string Token)> CreateVisitorAsync(string prefix = "QA-Visitor")
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var email = $"qa_visitor_{id}@ceylonquest.qa";
        var password = "PassWord123!";

        var body = JsonSerializer.Serialize(new
        {
            firstName = prefix,
            lastName = $"User-{id}",
            email,
            password,
            confirmPassword = password,
            phoneNumber = "+94771234567",
            nationality = "Sri Lanka",
            role = "Visitor"
        });

        using var resp = await _http.PostAsync(
            $"{PerformanceReportTestConfig.IdentityApiBaseUrl}/api/auth/register",
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (!resp.IsSuccessStatusCode)
        {
            var content = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Register failed for {email}: {content}");
        }

        var (token, _) = await LoginAsync(email, password);
        return (email, password, token);
    }

    public async Task<HttpResponseMessage> GetReportRawAsync(string? token, string? queryString = null)
    {
        var url = $"{PerformanceReportTestConfig.BookingApiBaseUrl}/api/admin/reports/provider-performance";
        if (!string.IsNullOrWhiteSpace(queryString))
        {
            url += $"?{queryString.TrimStart('?')}";
        }

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrWhiteSpace(token))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await _http.SendAsync(req);
    }

    public async Task<ProviderPerformanceReportModel> GetReportAsync(string token, string? queryString = null)
    {
        using var resp = await GetReportRawAsync(token, queryString);
        var content = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GetReport failed (HTTP {(int)resp.StatusCode}): {content}");
        }

        return JsonSerializer.Deserialize<ProviderPerformanceReportModel>(content, JsonOpts)
            ?? throw new InvalidOperationException("Failed to deserialize ProviderPerformanceReportResponse.");
    }
}

public class ProviderPerformanceReportModel
{
    public ProviderPerformanceFilters Filters { get; set; } = new();
    public List<ProviderPerformanceReportRowModel> Providers { get; set; } = new();
}

public class ProviderPerformanceFilters
{
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? Category { get; set; }
    public double? MinimumRating { get; set; }
    public Guid? ProviderId { get; set; }
    public string SortBy { get; set; } = "Revenue";
    public string SortDirection { get; set; } = "Desc";
}

public class ProviderPerformanceReportRowModel
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public int TotalReviewCount { get; set; }
    public double? AverageRating { get; set; }
    public StarRatingBreakdownModel StarRatingBreakdown { get; set; } = new();
    public bool NoReviews { get; set; }
    public bool LowRating { get; set; }
    public string RatingState { get; set; } = string.Empty;
}

public class StarRatingBreakdownModel
{
    public int OneStar { get; set; }
    public int TwoStars { get; set; }
    public int ThreeStars { get; set; }
    public int FourStars { get; set; }
    public int FiveStars { get; set; }
}
