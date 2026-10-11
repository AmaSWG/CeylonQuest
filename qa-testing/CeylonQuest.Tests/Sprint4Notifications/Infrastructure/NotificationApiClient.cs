using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CeylonQuest.Tests.Sprint4Notifications.Infrastructure;

/// <summary>Thin wrapper over the real notification-service REST API (services/notification-service/Controllers/NotificationController.cs).</summary>
public sealed class NotificationApiClient
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(TestConfig.NotificationApiBaseUrl) };

    private static HttpRequestMessage Request(HttpMethod method, string path, string? token)
    {
        var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public Task<HttpResponseMessage> GetListRawAsync(string? token, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        _http.SendAsync(Request(HttpMethod.Get, $"/api/notifications?page={page}&pageSize={pageSize}", token), ct);

    public async Task<NotificationList> GetListAsync(string token, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var response = await GetListRawAsync(token, page, pageSize, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NotificationList>(JsonDefaults.Options, ct))!;
    }

    public Task<HttpResponseMessage> GetUnreadCountRawAsync(string? token, CancellationToken ct = default) =>
        _http.SendAsync(Request(HttpMethod.Get, "/api/notifications/unread-count", token), ct);

    public async Task<int> GetUnreadCountAsync(string token, CancellationToken ct = default)
    {
        var response = await GetUnreadCountRawAsync(token, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<UnreadCountResponse>(JsonDefaults.Options, ct);
        return body!.UnreadCount;
    }

    public Task<HttpResponseMessage> MarkReadRawAsync(string? token, string id, CancellationToken ct = default) =>
        _http.SendAsync(Request(HttpMethod.Patch, $"/api/notifications/{id}/read", token), ct);

    public async Task<MarkReadResult> MarkReadAsync(string token, Guid id, CancellationToken ct = default)
    {
        var response = await MarkReadRawAsync(token, id.ToString(), ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MarkReadResult>(JsonDefaults.Options, ct))!;
    }

    public Task<HttpResponseMessage> MarkAllReadRawAsync(string? token, CancellationToken ct = default) =>
        _http.SendAsync(Request(HttpMethod.Patch, "/api/notifications/read-all", token), ct);

    public async Task<MarkReadResult> MarkAllReadAsync(string token, CancellationToken ct = default)
    {
        var response = await MarkAllReadRawAsync(token, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MarkReadResult>(JsonDefaults.Options, ct))!;
    }

    /// <summary>Polls GET /api/notifications until an item matching <paramref name="predicate"/> appears (Kafka consumption is async).</summary>
    public async Task<NotificationItem> WaitForNotificationAsync(string token, Func<NotificationItem, bool> predicate,
        TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TestConfig.EventualConsistencyTimeout);
        while (true)
        {
            var list = await GetListAsync(token, 1, 50, ct);
            var match = list.Items.FirstOrDefault(predicate);
            if (match != null) return match;
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException("Expected notification did not arrive within the eventual-consistency timeout.");
            await Task.Delay(TestConfig.PollInterval, ct);
        }
    }

    /// <summary>Polls GET /api/notifications/unread-count until it reaches at least <paramref name="minimum"/>.</summary>
    public async Task<int> WaitForUnreadAtLeastAsync(string token, int minimum, CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow + TestConfig.EventualConsistencyTimeout;
        var count = 0;
        while (true)
        {
            count = await GetUnreadCountAsync(token, ct);
            if (count >= minimum) return count;
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException($"Unread count never reached {minimum} (last seen {count}).");
            await Task.Delay(TestConfig.PollInterval, ct);
        }
    }
}
