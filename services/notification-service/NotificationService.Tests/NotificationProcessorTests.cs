using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using NotificationService.Data;
using NotificationService.Models;
using NotificationService.Services;
using Xunit;
using Moq;

namespace NotificationService.Tests;

public class NotificationProcessorTests
{
    [Theory]
    [InlineData("EventId")]
    [InlineData("BookingId")]
    [InlineData("ListingId")]
    [InlineData("VisitorId")]
    [InlineData("VerifiedAtUtc")]
    public async Task InvalidAttendanceInvitation_DoesNotResolveRecipientsOrWriteState(string field)
    {
        await using var db = Database();
        var payload = Event("review.eligibility-granted");
        payload["VerifiedAtUtc"] = "2026-10-11T10:00:00Z";
        payload[field] = field.EndsWith("Id") ? Guid.Empty.ToString() : "0001-01-01T00:00:00";
        var resolver = new Mock<IRecipientResolver>(MockBehavior.Strict);
        await Assert.ThrowsAsync<JsonException>(() => new NotificationEventProcessor(db, resolver.Object)
            .ProcessAsync("review.eligibility-granted", payload.ToJsonString(), default));
        Assert.Empty(db.Notifications); Assert.Empty(db.ProcessedEvents); Assert.Empty(db.BookingContexts);
        resolver.Verify(r => r.ProviderAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        resolver.Verify(r => r.BookingAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
            It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        resolver.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("booking.created")]
    [InlineData("booking.canceled")]
    [InlineData("review.submitted")]
    public async Task ResolverThrows_PropagatesOriginalErrorWithoutAcknowledgingOrWriting(string topic)
    {
        await using var db = Database();
        var payload = Event(topic);
        var listing = Guid.Parse(payload["ListingId"]!.GetValue<string>());
        var booking = Guid.Parse(payload["BookingId"]!.GetValue<string>());
        var visitor = Guid.Parse(payload["VisitorId"]!.GetValue<string>());
        var provider = Guid.Parse(payload["ProviderUserId"]!.GetValue<string>());
        using var cancellation = new CancellationTokenSource(); var token = cancellation.Token;
        var resolver = new Mock<IRecipientResolver>(MockBehavior.Strict);
        var failure = new HttpRequestException("Simulated recipient lookup failure");
        if (topic == "booking.canceled")
            resolver.Setup(r => r.BookingAsync(booking, listing, visitor, provider, token)).ThrowsAsync(failure);
        else resolver.Setup(r => r.ProviderAsync(listing, provider, token)).ThrowsAsync(failure);
        Assert.Same(failure, await Assert.ThrowsAsync<HttpRequestException>(() =>
            new NotificationEventProcessor(db, resolver.Object).ProcessAsync(topic, payload.ToJsonString(), token)));
        db.ChangeTracker.Clear();
        Assert.Empty(db.Notifications); Assert.Empty(db.ProcessedEvents); Assert.Empty(db.BookingContexts);
        Assert.Empty(db.FailedEvents); // Quarantine records belong to the consumer, not the processor.
        if (topic == "booking.canceled") resolver.Verify(r => r.BookingAsync(booking, listing, visitor, provider, token), Times.Once);
        else resolver.Verify(r => r.ProviderAsync(listing, provider, token), Times.Once);
        resolver.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Cancellation_UsesResolvedRecipientsAndExactTokenRatherThanUnresolvedEventValues()
    {
        await using var db = Database(); var payload = Event("booking.canceled");
        var booking = Guid.Parse(payload["BookingId"]!.GetValue<string>());
        var listing = Guid.Parse(payload["ListingId"]!.GetValue<string>());
        var visitor = Guid.Parse(payload["VisitorId"]!.GetValue<string>());
        var provider = Guid.Parse(payload["ProviderUserId"]!.GetValue<string>());
        var resolvedVisitor = Guid.NewGuid(); var resolvedProvider = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource(); var token = cancellation.Token;
        var resolver = new Mock<IRecipientResolver>(MockBehavior.Strict);
        resolver.Setup(r => r.BookingAsync(booking, listing, visitor, provider, token))
            .ReturnsAsync((resolvedVisitor, resolvedProvider));
        await new NotificationEventProcessor(db, resolver.Object).ProcessAsync("booking.canceled", payload.ToJsonString(), token);
        db.ChangeTracker.Clear();
        var notifications = await db.Notifications.ToListAsync();
        Assert.Equal(new[] { resolvedVisitor, resolvedProvider }.Order(), notifications.Select(n => n.RecipientUserId).Order());
        Assert.All(notifications, n => { Assert.Equal(booking, n.BookingId); Assert.Equal(listing, n.ListingId); Assert.False(n.IsRead); });
        Assert.Single(db.ProcessedEvents); Assert.Empty(db.BookingContexts);
        resolver.Verify(r => r.BookingAsync(booking, listing, visitor, provider, token), Times.Once);
        resolver.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("booking.created")]
    [InlineData("payment.completed")]
    [InlineData("booking.canceled")]
    [InlineData("review.submitted")]
    public async Task LegacyEventWithoutEventId_UsesBusinessKeyDespiteChangedJsonRepresentation(string topic)
    {
        await using var db = Database(); var payload = Event(topic); payload.Remove("EventId");
        var resolver = new Mock<IRecipientResolver>(MockBehavior.Strict);
        var provider = Guid.Parse(payload["ProviderUserId"]!.GetValue<string>());
        var visitor = Guid.Parse(payload["VisitorId"]!.GetValue<string>());
        var listing = Guid.Parse(payload["ListingId"]!.GetValue<string>());
        var booking = Guid.Parse(payload["BookingId"]!.GetValue<string>());
        if (topic == "booking.canceled") resolver.Setup(r => r.BookingAsync(booking, listing, visitor, provider, default)).ReturnsAsync((visitor, provider));
        else if (topic != "payment.completed") resolver.Setup(r => r.ProviderAsync(listing, provider, default)).ReturnsAsync(provider);
        var processor = new NotificationEventProcessor(db, resolver.Object);
        await processor.ProcessAsync(topic, payload.ToJsonString(), default);
        var ids = await db.Notifications.Select(n => n.Id).ToListAsync();
        payload["IgnoredMetadata"] = "added on replay";
        await processor.ProcessAsync(topic, payload.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), default);
        db.ChangeTracker.Clear();
        Assert.Equal(ids.Order(), (await db.Notifications.Select(n => n.Id).ToListAsync()).Order());
        var expectedKey = topic + ":" + (topic switch
        {
            "payment.completed" => payload["PaymentId"]!.GetValue<string>(),
            "review.submitted" => payload["ReviewId"]!.GetValue<string>(),
            "booking.canceled" => $"{booking}:{DateTime.Parse(payload["CanceledAt"]!.GetValue<string>(), null, System.Globalization.DateTimeStyles.RoundtripKind):O}",
            _ => booking.ToString()
        });
        Assert.Equal(expectedKey, (await db.ProcessedEvents.SingleAsync()).EventKey);
        if (topic == "booking.canceled") resolver.Verify(r => r.BookingAsync(booking, listing, visitor, provider, default), Times.Once);
        else if (topic != "payment.completed") resolver.Verify(r => r.ProviderAsync(listing, provider, default), Times.Once);
        resolver.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Process_CanceledToken_DoesNotCallResolverOrWriteRelationalState()
    {
        await using var fixture = await SqliteNotificationTestDatabase.CreateAsync();
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var resolver = new Mock<IRecipientResolver>(MockBehavior.Strict);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new NotificationEventProcessor(fixture.Db, resolver.Object)
            .ProcessAsync("booking.created", Event("booking.created").ToJsonString(), cancellation.Token));
        Assert.Empty(fixture.Db.Notifications); Assert.Empty(fixture.Db.ProcessedEvents); Assert.Empty(fixture.Db.BookingContexts);
        resolver.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SaveFailsAfterSqlWrites_TransactionRollsBackNotificationsContextAcknowledgementAndFailureResolution()
    {
        var interceptor = new FailAfterSqlSave();
        await using var fixture = await SqliteNotificationTestDatabase.CreateAsync(interceptor);
        var db = fixture.Db; var payload = Event("booking.created"); var json = payload.ToJsonString();
        var failureKey = NotificationEventProcessor.PayloadKey("booking.created", json);
        db.FailedEvents.Add(new() { EventKey = failureKey, Topic = "booking.created", Payload = json, AttemptCount = 2 });
        await db.SaveChangesAsync(); interceptor.Enabled = true;
        var resolver = new Mock<IRecipientResolver>(MockBehavior.Strict);
        var listing = Guid.Parse(payload["ListingId"]!.GetValue<string>());
        var provider = Guid.Parse(payload["ProviderUserId"]!.GetValue<string>());
        resolver.Setup(r => r.ProviderAsync(listing, provider, default)).ReturnsAsync(provider);
        var processor = new NotificationEventProcessor(db, resolver.Object);
        Assert.Same(interceptor.Failure, await Assert.ThrowsAsync<DbUpdateException>(() => processor.ProcessAsync("booking.created", json, default)));
        db.ChangeTracker.Clear();
        Assert.Empty(await db.Notifications.ToListAsync()); Assert.Empty(await db.ProcessedEvents.ToListAsync());
        Assert.Empty(await db.BookingContexts.ToListAsync());
        var failed = await db.FailedEvents.SingleAsync(); Assert.False(failed.Resolved); Assert.Equal(2, failed.AttemptCount);
        interceptor.Enabled = false;
        await processor.ProcessAsync("booking.created", json, default);
        db.ChangeTracker.Clear();
        Assert.Equal(2, await db.Notifications.CountAsync()); Assert.Single(db.ProcessedEvents); Assert.Single(db.BookingContexts);
        Assert.True((await db.FailedEvents.SingleAsync()).Resolved);
        resolver.Verify(r => r.ProviderAsync(listing, provider, default), Times.Exactly(2)); resolver.VerifyNoOtherCalls();
    }

    private sealed class FailAfterSqlSave : SaveChangesInterceptor
    {
        internal bool Enabled { get; set; }
        internal DbUpdateException Failure { get; } = new("Simulated failure after database writes");
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (Enabled) throw Failure;
            return new(result);
        }
    }

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
    public async Task BookingProducerReviewContract_MapsUtcTimestampAndComment()
    {
        await using var db = Database(); var payload = Event("review.submitted");
        payload["SubmittedAtUtc"] = payload["SubmittedAt"]!.DeepClone(); payload.Remove("SubmittedAt");
        await Processor(db).ProcessAsync("review.submitted", payload.ToJsonString(), default);
        var notification = Assert.Single(db.Notifications);
        Assert.Contains("Excellent", notification.Message);
        Assert.Equal(DateTimeKind.Utc, notification.CreatedAtUtc.Kind);
    }
    [Fact]
    public async Task AttendanceInvitation_NotifiesOnlyVisitor_AndDeduplicatesReplay()
    {
        await using var db = Database(); var payload = Event("review.eligibility-granted");
        payload["VerifiedAtUtc"] = "2026-10-11T10:00:00Z"; payload["ListingTitle"] = "Cooking class";
        var processor = Processor(db);
        await processor.ProcessAsync("review.eligibility-granted", payload.ToJsonString(), default);
        await processor.ProcessAsync("review.eligibility-granted", payload.ToJsonString(), default);
        var notification = Assert.Single(db.Notifications);
        Assert.Equal(Guid.Parse(payload["VisitorId"]!.GetValue<string>()), notification.RecipientUserId);
        Assert.Contains("My Bookings", notification.Message); Assert.Contains("Cooking class", notification.Message);
        Assert.Single(db.ProcessedEvents);
    }
    [Fact]
    public void FailureKeys_AreStable_AndDistinguishTopicsAndPayloads()
    {
        var key = NotificationEventProcessor.PayloadKey("booking.created","{}");
        Assert.Equal(key,NotificationEventProcessor.PayloadKey("booking.created","{}"));
        Assert.NotEqual(key,NotificationEventProcessor.PayloadKey("payment.completed","{}"));
        Assert.NotEqual(key,NotificationEventProcessor.PayloadKey("booking.created","null"));
    }

    [Theory]
    [InlineData("SubmittedAt")]
    [InlineData("SubmittedAtUtc")]
    public async Task ReviewTimestampNames_CreateOneProviderNotification(
        string timestampProperty)
    {
        await using var db = Database();
        var payload = Event("review.submitted");

        var submittedAt = new DateTime(
            2026, 10, 12, 10, 0, 0, DateTimeKind.Utc);

        // Supply only one timestamp name in each test.
        payload.Remove("SubmittedAt");
        payload[timestampProperty] = submittedAt.ToString("O");

        var providerId = Guid.Parse(
            payload["ProviderUserId"]!.GetValue<string>());
        var visitorId = Guid.Parse(
            payload["VisitorId"]!.GetValue<string>());

        var processor = Processor(db);
        var json = payload.ToJsonString();

        await processor.ProcessAsync("review.submitted", json, default);

        var notification = Assert.Single(db.Notifications);

        Assert.Equal(providerId, notification.RecipientUserId);
        Assert.Equal("New review", notification.Title);
        Assert.Equal<int?>(5, notification.Rating);
        Assert.Equal(submittedAt, notification.OccurredAtUtc);
        Assert.Contains("5/5 stars", notification.Message);
        Assert.Contains("Excellent", notification.Message);
        Assert.DoesNotContain(
            db.Notifications,
            n => n.RecipientUserId == visitorId);
        Assert.Single(db.ProcessedEvents);

        // Kafka replay must not create another notification.
        await processor.ProcessAsync("review.submitted", json, default);

        Assert.Single(db.Notifications);
        Assert.Single(db.ProcessedEvents);
    }

    [Theory]
    [InlineData("SubmittedAt", false)]
    [InlineData("SubmittedAtUtc", false)]
    [InlineData("SubmittedAt", true)]
    [InlineData("SubmittedAtUtc", true)]
    public async Task ReviewMissingOrDefaultTimestamp_IsRejected(
    string timestampProperty,
        bool includeDefaultTimestamp)
    {
        await using var db = Database();
        var payload = Event("review.submitted");

        payload.Remove("SubmittedAt");

        if (includeDefaultTimestamp)
        {
            payload[timestampProperty] = "0001-01-01T00:00:00";
        }

        await Assert.ThrowsAsync<JsonException>(() =>
            Processor(db).ProcessAsync(
                "review.submitted", payload.ToJsonString(), default));

        Assert.Empty(db.Notifications);
        Assert.Empty(db.ProcessedEvents);
    }

    [Fact]
    public async Task BookingServiceReviewPayload_CreatesProviderNotification()
    {
        await using var db = Database();

        var providerId = Guid.NewGuid();
        var visitorId = Guid.NewGuid();
        var submittedAt = new DateTime(
            2026, 10, 12, 10, 0, 0, DateTimeKind.Utc);

        var producerEvent = new BookingService.Events.ReviewSubmittedEvent(
            EventId: Guid.NewGuid(),
            EventVersion: 1,
            ReviewId: Guid.NewGuid(),
            BookingId: Guid.NewGuid(),
            BookingType: "Experience",
            ListingId: Guid.NewGuid(),
            ListingTitle: "Test experience",
            VisitorId: visitorId,
            ProviderId: Guid.NewGuid(),
            Rating: 5,
            SubmittedAtUtc: submittedAt,
            ProviderUserId: providerId,
            ReviewText: "Excellent");

        var payload = JsonSerializer.Serialize(producerEvent);
        var processor = Processor(db);

        await processor.ProcessAsync("review.submitted", payload, default);
        await processor.ProcessAsync("review.submitted", payload, default);

        var notification = Assert.Single(db.Notifications);

        Assert.Equal(providerId, notification.RecipientUserId);
        Assert.Equal(submittedAt, notification.OccurredAtUtc);
        Assert.Equal<int?>(5, notification.Rating);
        Assert.Equal<Guid?>(producerEvent.ReviewId, notification.ReviewId);
        Assert.Contains("Excellent", notification.Message);
        Assert.DoesNotContain(
            db.Notifications,
            n => n.RecipientUserId == visitorId);
        Assert.Single(db.ProcessedEvents);
    }
}
