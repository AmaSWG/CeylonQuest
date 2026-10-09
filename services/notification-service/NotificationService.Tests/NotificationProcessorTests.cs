using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using NotificationService.Data;
using NotificationService.Models;
using NotificationService.Services;
using Xunit;

namespace NotificationService.Tests;

public class NotificationProcessorTests
{
    private static NotificationDbContext Database() => new(new DbContextOptionsBuilder<NotificationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
    private static NotificationEventProcessor Processor(NotificationDbContext db) => new(db, new RecipientResolver(db,new ConfigurationBuilder().Build()));
    private static JsonObject Event(string topic) => new()
    {
        ["EventId"] = Guid.NewGuid().ToString(), ["BookingId"] = Guid.NewGuid().ToString(),
        ["VisitorId"] = Guid.NewGuid().ToString(), ["ProviderUserId"] = Guid.NewGuid().ToString(),
        ["ListingId"] = Guid.NewGuid().ToString(), ["PaymentId"] = Guid.NewGuid().ToString(), ["ReviewId"] = Guid.NewGuid().ToString(),
        ["CreatedAt"] = "2026-10-10T10:00:00Z", ["CompletedAt"] = "2026-10-10T10:00:00Z", ["CanceledAt"] = "2026-10-10T10:00:00Z",
        ["SubmittedAt"] = "2026-10-10T10:00:00Z", ["BookingDate"] = "2026-10-12", ["ListingType"] = "Experience",
        ["ProviderBusinessName"] = "Test Business", ["Rating"] = 5, ["Amount"] = 100, ["Currency"] = "LKR",
        ["TransactionReference"] = "reference-123", ["ReviewText"] = "Excellent"
    };
    public static IEnumerable<object[]> InvalidFields()
    {
        foreach (var topic in new[] { "booking.created", "payment.completed", "booking.canceled", "review.submitted" })
        {
            yield return new object[] { topic, "EventId", "00000000-0000-0000-0000-000000000000" };
            yield return new object[] { topic, "BookingId", "00000000-0000-0000-0000-000000000000" };
            var fields = topic switch {
                "booking.created" => new[] { "VisitorId", "ListingId", "CreatedAt", "BookingDate" },
                "payment.completed" => new[] { "PaymentId", "VisitorId", "CompletedAt", "Amount" },
                "booking.canceled" => new[] { "ListingId", "CanceledAt", "RefundAmount" },
                _ => new[] { "ReviewId", "ListingId", "SubmittedAt", "Rating" } };
            foreach (var field in fields)
                yield return new object[] { topic, field, field.EndsWith("Id") ? "00000000-0000-0000-0000-000000000000" : field.EndsWith("At") ? "0001-01-01T00:00:00" : field == "BookingDate" ? " " : "-1" };
        }
        yield return new object[] { "review.submitted", "Rating", "6" };
    }
    [Theory, MemberData(nameof(InvalidFields))]
    public async Task InvalidEvent_IsRejectedWithoutSavingOrAcknowledging(string topic,string field,string value)
    {
        await using var db = Database(); var payload = Event(topic);
        payload[field] = field is "Amount" or "RefundAmount" or "Rating" ? JsonValue.Create(int.Parse(value)) : JsonValue.Create(value);
        await Assert.ThrowsAsync<JsonException>(() => Processor(db).ProcessAsync(topic,payload.ToJsonString(),default));
        Assert.Empty(db.Notifications); Assert.Empty(db.ProcessedEvents); Assert.Empty(db.BookingContexts);
    }
    [Theory]
    [InlineData("booking.created")] [InlineData("payment.completed")] [InlineData("booking.canceled")] [InlineData("review.submitted")]
    public async Task SuccessfulRetry_ResolvesFailure_AndReplayDoesNotDuplicate(string topic)
    {
        await using var db = Database(); var payload = Event(topic).ToJsonString();
        db.FailedEvents.Add(new() { EventKey = NotificationEventProcessor.PayloadKey(topic,payload), Topic = topic, Payload = payload });
        await db.SaveChangesAsync();
        var processor = Processor(db); await processor.ProcessAsync(topic,payload,default);
        var count = await db.Notifications.CountAsync(); Assert.True(count > 0);
        await processor.ProcessAsync(topic,payload,default);
        Assert.Equal(count,await db.Notifications.CountAsync()); Assert.Single(db.ProcessedEvents);
        Assert.True(Assert.Single(db.FailedEvents).Resolved);
        Assert.All(db.Notifications,n => { Assert.Equal(topic,n.EventType); Assert.False(n.IsRead); Assert.Null(n.ReadAtUtc); });
    }
    [Fact]
    public async Task IdenticalVisitorAndProvider_ReceivesOnlyOneNotification()
    {
        await using var db = Database(); var payload = Event("booking.created");
        payload["ProviderUserId"] = payload["VisitorId"]!.GetValue<string>();
        await Processor(db).ProcessAsync("booking.created",payload.ToJsonString(),default);
        Assert.Single(db.Notifications); Assert.Single(db.BookingContexts);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Cancellation_MapsRefundAndDefaultReason(bool refund)
    {
        await using var db = Database(); var payload = Event("booking.canceled");
        if(refund) { payload["RefundAmount"] = 50; payload["RefundStatus"] = "Pending"; }
        await Processor(db).ProcessAsync("booking.canceled",payload.ToJsonString(),default);
        Assert.All(db.Notifications,n => {
            Assert.Contains("Not supplied",n.Message);
            if(refund) { Assert.Equal(50m,n.RefundAmount); Assert.Equal("Pending",n.RefundStatus); Assert.Contains("Refund amount",n.Message); }
            else { Assert.Null(n.RefundAmount); Assert.DoesNotContain("Refund amount",n.Message); }
        });
    }
    [Theory]
    [InlineData("unknown", "{}")] [InlineData("payment.completed", "null")] [InlineData("review.submitted", "not-json")]
    public async Task UnsupportedOrMalformedPayload_IsRejected(string topic,string payload)
    {
        await using var db = Database();
        await Assert.ThrowsAsync<JsonException>(() => Processor(db).ProcessAsync(topic,payload,default));
        Assert.Empty(db.Notifications); Assert.Empty(db.ProcessedEvents);
    }
    [Fact]
    public void FailureKeys_AreStable_AndDistinguishTopicsAndPayloads()
    {
        var key = NotificationEventProcessor.PayloadKey("booking.created","{}");
        Assert.Equal(key,NotificationEventProcessor.PayloadKey("booking.created","{}"));
        Assert.NotEqual(key,NotificationEventProcessor.PayloadKey("payment.completed","{}"));
        Assert.NotEqual(key,NotificationEventProcessor.PayloadKey("booking.created","null"));
    }
}
