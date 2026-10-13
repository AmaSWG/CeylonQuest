using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using NotificationService.Data;
using NotificationService.Services;
using Xunit;

namespace NotificationService.Tests;

public class ReviewTimestampContractTests
{
    [Theory]
    [InlineData("SubmittedAtUtc")]
    [InlineData("SubmittedAt")]
    public async Task ProducerTimestampCreatesProviderNotificationAndReplayDoesNotDuplicate(string field)
    {
        await using var db = new NotificationDbContext(
            new DbContextOptionsBuilder<NotificationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var provider = Guid.NewGuid();
        var review = Guid.NewGuid();
        var timestamp = new DateTime(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc);
        var payload = new JsonObject
        {
            ["EventId"] = Guid.NewGuid().ToString(),
            ["EventVersion"] = 1,
            ["ReviewId"] = review.ToString(),
            ["BookingId"] = Guid.NewGuid().ToString(),
            ["ListingId"] = Guid.NewGuid().ToString(),
            ["ProviderUserId"] = provider.ToString(),
            ["Rating"] = 5,
            ["ReviewText"] = "Excellent experience",
            [field] = timestamp.ToString("O")
        }.ToJsonString();
        var processor = new NotificationEventProcessor(db,
            new RecipientResolver(db, new ConfigurationBuilder().Build()));

        await processor.ProcessAsync("review.submitted", payload, default);
        await processor.ProcessAsync("review.submitted", payload, default);

        var notification = Assert.Single(await db.Notifications.ToListAsync());
        Assert.Equal(provider, notification.RecipientUserId);
        Assert.Equal(review, notification.ReviewId);
        Assert.Equal(timestamp, notification.OccurredAtUtc);
        Assert.Equal("New review", notification.Title);
        Assert.Contains("Excellent experience", notification.Message);
        Assert.Single(await db.ProcessedEvents.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanonicalTimestampWinsRegardlessOfJsonPropertyOrder(bool canonicalFirst)
    {
        var canonical = "2026-10-12T10:00:00Z";
        var legacy = "2026-10-11T10:00:00Z";
        var payload = canonicalFirst
            ? $"{{\"SubmittedAtUtc\":\"{canonical}\",\"SubmittedAt\":\"{legacy}\"}}"
            : $"{{\"SubmittedAt\":\"{legacy}\",\"SubmittedAtUtc\":\"{canonical}\"}}";
        var result = JsonSerializer.Deserialize<NotificationService.Events.ReviewSubmittedEvent>(payload);
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc), result.SubmittedAt);
    }
}
