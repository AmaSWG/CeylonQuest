using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CeylonQuest.Tests.Sprint4AdminActions.Infrastructure;

public sealed class AdminApiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http = new();

    public async Task<(string Token, string Role)> LoginAsync(string email, string password)
    {
        var body = JsonSerializer.Serialize(new { email, password });
        using var resp = await _http.PostAsync(
            $"{AdminTestConfig.IdentityApiBaseUrl}/api/auth/login",
            new StringContent(body, Encoding.UTF8, "application/json"));

        var content = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Login failed for {email} (HTTP {(int)resp.StatusCode}): {content}");
        }

        using var doc = JsonDocument.Parse(content);
        var token = doc.RootElement.GetProperty("accessToken").GetString()!;
        var role = doc.RootElement.GetProperty("role").GetString()!;
        return (token, role);
    }

    public async Task<HttpResponseMessage> LoginRawAsync(string email, string password)
    {
        var body = JsonSerializer.Serialize(new { email, password });
        return await _http.PostAsync(
            $"{AdminTestConfig.IdentityApiBaseUrl}/api/auth/login",
            new StringContent(body, Encoding.UTF8, "application/json"));
    }

    public async Task<Guid> RegisterUserAsync(string firstName, string lastName, string email, string password, string role = "Visitor")
    {
        var body = JsonSerializer.Serialize(new
        {
            firstName,
            lastName,
            email,
            password,
            confirmPassword = password,
            phoneNumber = "+94771234567",
            nationality = "Sri Lanka",
            role
        });

        using var resp = await _http.PostAsync(
            $"{AdminTestConfig.IdentityApiBaseUrl}/api/auth/register",
            new StringContent(body, Encoding.UTF8, "application/json"));

        var content = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Register failed for {email} (HTTP {(int)resp.StatusCode}): {content}");
        }

        using var doc = JsonDocument.Parse(content);
        if (doc.RootElement.TryGetProperty("userId", out var idProp))
        {
            return Guid.Parse(idProp.GetString()!);
        }

        // If userId not returned directly, fetch via admin users list
        var (adminToken, _) = await LoginAsync(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);
        var users = await GetUsersAsync(adminToken);
        var created = users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Registered user {email} was not found in admin user list.");
        return created.Id;
    }

    public async Task<List<AdminUserRecord>> GetUsersAsync(string? token = null, string? role = null, bool? isActive = null)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{AdminTestConfig.IdentityApiBaseUrl}/api/admin/users?role={role}&isActive={isActive}");
        if (!string.IsNullOrEmpty(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        var content = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<AdminUserRecord>>(content, JsonOpts) ?? new();
    }

    public async Task<HttpResponseMessage> UpdateUserStatusRawAsync(string? token, string userId, bool isActive)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, $"{AdminTestConfig.IdentityApiBaseUrl}/api/admin/users/{userId}/status");
        if (!string.IsNullOrEmpty(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = JsonSerializer.Serialize(new { isActive });
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");
        return await _http.SendAsync(req);
    }

    public async Task<HttpResponseMessage> RemoveListingRawAsync(string? token, string listingType, string listingId, bool confirmRemoval, string? reason)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{AdminTestConfig.CatalogApiBaseUrl}/api/catalog/admin/listings/{listingType}/{listingId}/remove");
        if (!string.IsNullOrEmpty(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = JsonSerializer.Serialize(new { confirmRemoval, reason });
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");
        return await _http.SendAsync(req);
    }

    public async Task<List<AdminListingRecord>> GetAdminListingsAsync(string? token = null)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{AdminTestConfig.CatalogApiBaseUrl}/api/catalog/reports/admin-listings");
        if (!string.IsNullOrEmpty(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        var content = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<AdminListingRecord>>(content, JsonOpts) ?? new();
    }

    public async Task<HttpResponseMessage> GetAdminListingsRawAsync(string? token = null)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{AdminTestConfig.CatalogApiBaseUrl}/api/catalog/reports/admin-listings");
        if (!string.IsNullOrEmpty(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _http.SendAsync(req);
    }

    public async Task<Guid> CreateExperienceListingAsync(string providerToken, string title, decimal price = 2500m, int maxParticipants = 10)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{AdminTestConfig.CatalogApiBaseUrl}/api/catalog/activity-listings");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", providerToken);

        var body = JsonSerializer.Serialize(new
        {
            title,
            description = "QA Automated Test Experience Listing Description",
            price,
            unit = "Person",
            location = "Galle",
            maxParticipants,
            duration = "2 Hours",
            availableDays = "Daily",
            timeSlots = "09:00 - 11:00, 14:00 - 16:00",
            validFrom = DateTime.UtcNow.Date,
            validUntil = DateTime.UtcNow.Date.AddDays(90),
            images = (string?)null
        });
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req);
        var content = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Create experience failed (HTTP {(int)resp.StatusCode}): {content}");
        }

        using var doc = JsonDocument.Parse(content);
        return Guid.Parse(doc.RootElement.GetProperty("id").GetString()!);
    }

    public async Task<HttpResponseMessage> GetPublicActivityByIdRawAsync(Guid id)
    {
        return await _http.GetAsync($"{AdminTestConfig.CatalogApiBaseUrl}/api/catalog/activity-listings/public/{id}");
    }

    public async Task<HttpResponseMessage> SearchActivitiesRawAsync(string keyword)
    {
        return await _http.GetAsync($"{AdminTestConfig.CatalogApiBaseUrl}/api/catalog/search?q={Uri.EscapeDataString(keyword)}");
    }

    public async Task<HttpResponseMessage> GetPublicActivitiesRawAsync(string keyword)
    {
        return await _http.GetAsync($"{AdminTestConfig.CatalogApiBaseUrl}/api/catalog/activity-listings/public?search={Uri.EscapeDataString(keyword)}");
    }
    public async Task<HttpResponseMessage> GetAdminAuditLogsRawAsync(string? token, string? queryParams = null)
    {
        var url = $"{AdminTestConfig.IdentityApiBaseUrl}/api/admin/audit-logs" +
            (string.IsNullOrWhiteSpace(queryParams) ? "" : $"?{queryParams.TrimStart('?')}");
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _http.SendAsync(req);
    }

    public async Task<AdminAuditLogsResponse> GetAdminAuditLogsAsync(string token, string? queryParams = null)
    {
        using var resp = await GetAdminAuditLogsRawAsync(token, queryParams);
        resp.EnsureSuccessStatusCode();
        var content = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AdminAuditLogsResponse>(content, JsonOpts) ?? new();
    }

    public async Task<HttpResponseMessage> GetCatalogAuditLogsRawAsync(string? token, string? queryParams = null)
    {
        var url = $"{AdminTestConfig.CatalogApiBaseUrl}/api/catalog/admin/audit-logs" +
            (string.IsNullOrWhiteSpace(queryParams) ? "" : $"?{queryParams.TrimStart('?')}");
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _http.SendAsync(req);
    }
}

public record AdminUserRecord(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? Nationality,
    string Role,
    bool IsActive,
    DateTime CreatedAt
);

public record AdminListingRecord(
    Guid Id,
    string BookingType,
    string ServiceName,
    Guid ProviderId,
    string ProviderName,
    bool IsActive,
    bool IsRemoved,
    string Status
);

public record AdminAuditEntryRecord(
    Guid Id,
    string ActionType,
    DateTime TimestampUtc,
    Guid AdministratorId,
    Guid TargetId,
    string TargetType,
    string PreviousStatus,
    string NewStatus,
    string? Reason
);

public record AdminAuditLogsResponse(
    List<AdminAuditEntryRecord> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
)
{
    public AdminAuditLogsResponse() : this(new(), 0, 1, 25, 0) { }
}
