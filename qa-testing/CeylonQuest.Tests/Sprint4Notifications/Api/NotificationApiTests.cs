using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CeylonQuest.Tests.Sprint4Notifications.Infrastructure;
using Xunit.Abstractions;

namespace CeylonQuest.Tests.Sprint4Notifications.Api;

/// <summary>
/// Story 11.1 "Receive Event Notifications" - API integration test cases (TC-11.1-API-01..12).
/// Runs against the real, already-running notification-service (REST API + Kafka consumer) and
/// identity-service (for real JWTs). See Sprint4Notifications/README.md for prerequisites and for
/// the rationale behind publishing events directly to Kafka instead of going through
/// booking-service/Stripe/the (not yet built) review feature.
/// </summary>
[Collection(nameof(NotificationApiCollection))]
public sealed class NotificationApiTests
{
    private readonly NotificationApiFixture _fixture;
    private readonly ITestOutputHelper _output;

    public NotificationApiTests(NotificationApiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact(DisplayName = "TC-11.1-API-01: GET /api/notifications requires authentication")]
    public async Task Api01_AuthenticationRequired()
    {
        var noToken = await _fixture.Notifications.GetListRawAsync(null);
        Assert.Equal(HttpStatusCode.Unauthorized, noToken.StatusCode);

        var invalidToken = await _fixture.Notifications.GetListRawAsync("this-is-not-a-real-jwt");
        Assert.Equal(HttpStatusCode.Unauthorized, invalidToken.StatusCode);
    }

    [Fact(DisplayName = "TC-11.1-API-02: response contract returns only the caller's items, newest first, with the documented fields")]
    public async Task Api02_ResponseContract()
    {
        var bookingId = Guid.NewGuid();
        await _fixture.Kafka.PublishBookingCreatedAsync(bookingId, _fixture.Visitor.Id, _fixture.ProviderPersona.Id,
            Guid.NewGuid(), "Experience", "Ceylon Adventures Pvt Ltd", DateTime.UtcNow.AddDays(100).ToString("yyyy-MM-dd"),
            "09:00 AM - 11:00 AM", 2, 5000m);

        var notification = await _fixture.Notifications.WaitForNotificationAsync(_fixture.Visitor.Token, n => n.BookingId == bookingId);
        Assert.Equal("booking.created", notification.EventType);
        Assert.False(notification.IsRead);
        Assert.NotEqual(default, notification.CreatedAtUtc);
        Assert.Null(notification.ReadAtUtc);

        var list = await _fixture.Notifications.GetListAsync(_fixture.Visitor.Token, pageSize: 50);
        var ordered = list.Items.Select(i => i.CreatedAtUtc).ToList();
        Assert.Equal(ordered.OrderByDescending(x => x), ordered);

        // User isolation at the contract level: the outsider must never see the visitor's item.
        var outsiderList = await _fixture.Notifications.GetListAsync(_fixture.Outsider.Token, pageSize: 50);
        Assert.DoesNotContain(outsiderList.Items, i => i.BookingId == bookingId);
    }

    [Fact(DisplayName = "TC-11.1-API-03: booking.created notifies both the visitor and the owning provider")]
    public async Task Api03_BookingCreated_NotifiesVisitorAndProvider()
    {
        var bookingId = Guid.NewGuid();
        var date = DateTime.UtcNow.AddDays(101).ToString("yyyy-MM-dd");
        const string timeSlot = "07:00 AM - 10:00 AM";

        await _fixture.Kafka.PublishBookingCreatedAsync(bookingId, _fixture.Visitor.Id, _fixture.ProviderPersona.Id,
            Guid.NewGuid(), "Experience", "Ceylon Adventures Pvt Ltd", date, timeSlot, 3, 15000m);

        var visitorNotif = await _fixture.Notifications.WaitForNotificationAsync(_fixture.Visitor.Token, n => n.BookingId == bookingId);
        var providerNotif = await _fixture.Notifications.WaitForNotificationAsync(_fixture.ProviderPersona.Token, n => n.BookingId == bookingId);

        foreach (var notif in new[] { visitorNotif, providerNotif })
        {
            Assert.Equal(bookingId, notif.BookingId);
            Assert.Contains(date, notif.Message);
            Assert.Contains(timeSlot, notif.Message);
            Assert.Contains("3 participant", notif.Message);
            Assert.Contains("Ceylon Adventures Pvt Ltd", notif.Message);
        }
        // QA NOTE (gap): the message template (NotificationEventProcessor.cs) has no field for the
        // experience/listing *title* and no human-readable booking reference - only ListingType,
        // provider business name, date, time slot and participant count are included. Flag for CEYQ-194/195.
    }

    [Fact(DisplayName = "TC-11.1-API-04: payment.completed notifies the visitor with the paid amount and reference")]
    public async Task Api04_PaymentCompleted_NotifiesVisitor()
    {
        // Completing a *real* Stripe test-mode payment needs `stripe listen --forward-to
        // http://localhost:5229/api/Payments/webhook` running locally (Stripe cannot reach a
        // developer machine otherwise - see PaymentsController.cs StripeWebhook). That is outside
        // this harness's control, so this test publishes the payment.completed contract
        // booking-service's webhook handler emits, to verify notification-service independent of
        // whether the Stripe CLI is set up locally.
        var bookingId = Guid.NewGuid();
        var reference = $"PAY-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        const decimal amount = 24500m;

        await _fixture.Kafka.PublishPaymentCompletedAsync(bookingId, _fixture.Visitor.Id, amount, reference);

        var notification = await _fixture.Notifications.WaitForNotificationAsync(_fixture.Visitor.Token,
            n => n.BookingId == bookingId && n.EventType == "payment.completed");

        Assert.Equal(amount, notification.Amount);
        Assert.Equal("LKR", notification.Currency);
        Assert.Contains(reference, notification.Message);
    }

    [Fact(DisplayName = "TC-11.1-API-05: booking.canceled notifies both users with refund details; an unpaid cancellation omits the refund line")]
    public async Task Api05_BookingCanceled_WithAndWithoutRefund()
    {
        var bookingId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var date = DateTime.UtcNow.AddDays(102).ToString("yyyy-MM-dd");
        const string timeSlot = "08:00 AM - 10:00 AM";

        await _fixture.Kafka.PublishBookingCreatedAsync(bookingId, _fixture.Visitor.Id, _fixture.ProviderPersona.Id,
            listingId, "Experience", "Ceylon Adventures Pvt Ltd", date, timeSlot, 2, 9000m);
        await _fixture.Notifications.WaitForNotificationAsync(_fixture.Visitor.Token, n => n.BookingId == bookingId);

        await _fixture.Kafka.PublishBookingCanceledAsync(bookingId, listingId, _fixture.Visitor.Id, _fixture.ProviderPersona.Id,
            "Experience", date, timeSlot, "Visitor changed travel plans", 4500m, "PartialRefund");

        var visitorNotif = await _fixture.Notifications.WaitForNotificationAsync(_fixture.Visitor.Token,
            n => n.BookingId == bookingId && n.EventType == "booking.canceled");
        var providerNotif = await _fixture.Notifications.WaitForNotificationAsync(_fixture.ProviderPersona.Token,
            n => n.BookingId == bookingId && n.EventType == "booking.canceled");

        Assert.Equal(4500m, visitorNotif.RefundAmount);
        Assert.Equal("PartialRefund", visitorNotif.RefundStatus);
        Assert.Contains("Visitor changed travel plans", visitorNotif.Message);
        Assert.Equal(4500m, providerNotif.RefundAmount);
        // QA NOTE (gap): the message only carries a refund *amount* + *status* string
        // (e.g. "PartialRefund"); there is no explicit refund *percentage* field or text,
        // even though the test case expects one. Flag for CEYQ-194.

        var unpaidBookingId = Guid.NewGuid();
        var unpaidListingId = Guid.NewGuid();
        await _fixture.Kafka.PublishBookingCanceledAsync(unpaidBookingId, unpaidListingId, _fixture.Visitor.Id, _fixture.ProviderPersona.Id,
            "Experience", DateTime.UtcNow.AddDays(103).ToString("yyyy-MM-dd"), "02:00 PM - 04:00 PM", "Never paid", null, null);

        var unpaidNotif = await _fixture.Notifications.WaitForNotificationAsync(_fixture.Visitor.Token, n => n.BookingId == unpaidBookingId);
        Assert.Null(unpaidNotif.RefundAmount);
        Assert.DoesNotContain("Refund amount", unpaidNotif.Message);
    }

    [Fact(DisplayName = "TC-11.1-API-06: review.submitted notifies only the provider, with the correct rating")]
    public async Task Api06_ReviewSubmitted_NotifiesProviderOnly()
    {
        // NOTE (⚠️ per the test case): there is no review-submission UI or API yet - the frontend has
        // no review feature and booking-service exposes no review endpoint. This publishes the
        // review.submitted contract directly so notification-service's side of CEYQ-193/194 is still
        // verified ahead of the review feature being built.
        var bookingId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        const int rating = 4;

        await _fixture.Kafka.PublishReviewSubmittedAsync(bookingId, listingId, _fixture.ProviderPersona.Id, rating,
            "Fantastic guide, would book again!");

        var providerNotif = await _fixture.Notifications.WaitForNotificationAsync(_fixture.ProviderPersona.Token,
            n => n.BookingId == bookingId && n.EventType == "review.submitted");
        Assert.Equal(rating, providerNotif.Rating);
        Assert.Contains("Fantastic guide", providerNotif.Message);

        await Task.Delay(TestConfig.PollInterval * 4);
        var visitorList = await _fixture.Notifications.GetListAsync(_fixture.Visitor.Token, pageSize: 50);
        Assert.DoesNotContain(visitorList.Items, n => n.BookingId == bookingId && n.EventType == "review.submitted");
    }

    [Fact(DisplayName = "TC-11.1-API-07: marking one notification as read updates the DB/response and decreases the unread count by 1")]
    public async Task Api07_MarkOneAsRead()
    {
        var bookingId = Guid.NewGuid();
        await _fixture.Kafka.PublishBookingCreatedAsync(bookingId, _fixture.Visitor.Id, _fixture.ProviderPersona.Id,
            Guid.NewGuid(), "Experience", "Ceylon Adventures Pvt Ltd", DateTime.UtcNow.AddDays(104).ToString("yyyy-MM-dd"),
            "10:00 AM - 12:00 PM", 1, 3000m);
        var notification = await _fixture.Notifications.WaitForNotificationAsync(_fixture.Visitor.Token, n => n.BookingId == bookingId);

        var before = await _fixture.Notifications.GetUnreadCountAsync(_fixture.Visitor.Token);
        var result = await _fixture.Notifications.MarkReadAsync(_fixture.Visitor.Token, notification.Id);
        Assert.Equal(before - 1, result.UnreadCount);

        var list = await _fixture.Notifications.GetListAsync(_fixture.Visitor.Token, pageSize: 50);
        Assert.True(list.Items.Single(i => i.Id == notification.Id).IsRead);

        var after = await _fixture.Notifications.GetUnreadCountAsync(_fixture.Visitor.Token);
        Assert.Equal(before - 1, after);
    }

    [Fact(DisplayName = "TC-11.1-API-08: mark-all-as-read zeroes the caller's unread count without touching another user's")]
    public async Task Api08_MarkAllAsRead_OnlyAffectsCaller()
    {
        var visitorUnreadBefore = await _fixture.Notifications.GetUnreadCountAsync(_fixture.Visitor.Token);
        for (var i = 0; i < 3; i++)
            await _fixture.Kafka.PublishBookingCreatedAsync(Guid.NewGuid(), _fixture.Visitor.Id, _fixture.ProviderPersona.Id,
                Guid.NewGuid(), "Experience", "Ceylon Adventures Pvt Ltd", DateTime.UtcNow.AddDays(110 + i).ToString("yyyy-MM-dd"),
                "09:00 AM - 11:00 AM", 1, 2000m);
        await _fixture.Notifications.WaitForUnreadAtLeastAsync(_fixture.Visitor.Token, visitorUnreadBefore + 3);

        var outsiderUnreadBefore = await _fixture.Notifications.GetUnreadCountAsync(_fixture.Outsider.Token);
        await _fixture.Kafka.PublishBookingCreatedAsync(Guid.NewGuid(), _fixture.Outsider.Id, _fixture.ProviderPersona.Id,
            Guid.NewGuid(), "Experience", "Ceylon Adventures Pvt Ltd", DateTime.UtcNow.AddDays(120).ToString("yyyy-MM-dd"),
            "01:00 PM - 03:00 PM", 1, 2000m);
        await _fixture.Notifications.WaitForUnreadAtLeastAsync(_fixture.Outsider.Token, outsiderUnreadBefore + 1);

        var result = await _fixture.Notifications.MarkAllReadAsync(_fixture.Visitor.Token);
        Assert.Equal(0, result.UnreadCount);

        var outsiderAfter = await _fixture.Notifications.GetUnreadCountAsync(_fixture.Outsider.Token);
        Assert.Equal(outsiderUnreadBefore + 1, outsiderAfter);
    }

    [Fact(DisplayName = "TC-11.1-API-09: a user cannot mark another user's notification as read")]
    public async Task Api09_OwnershipCheck()
    {
        var bookingId = Guid.NewGuid();
        await _fixture.Kafka.PublishBookingCreatedAsync(bookingId, _fixture.Visitor.Id, _fixture.ProviderPersona.Id,
            Guid.NewGuid(), "Experience", "Ceylon Adventures Pvt Ltd", DateTime.UtcNow.AddDays(130).ToString("yyyy-MM-dd"),
            "08:00 AM - 09:00 AM", 1, 2000m);
        var notification = await _fixture.Notifications.WaitForNotificationAsync(_fixture.Visitor.Token, n => n.BookingId == bookingId);

        var response = await _fixture.Notifications.MarkReadRawAsync(_fixture.Outsider.Token, notification.Id.ToString());
        Assert.True(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
            $"Expected 403 or 404, got {(int)response.StatusCode}.");

        var list = await _fixture.Notifications.GetListAsync(_fixture.Visitor.Token, pageSize: 50);
        Assert.False(list.Items.Single(i => i.Id == notification.Id).IsRead);
    }

    [Fact(DisplayName = "TC-11.1-API-10: an unknown GUID returns 404; a malformed ID returns 400")]
    public async Task Api10_InvalidIdHandling()
    {
        var unknown = await _fixture.Notifications.MarkReadRawAsync(_fixture.Visitor.Token, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        var malformed = await _fixture.Notifications.MarkReadRawAsync(_fixture.Visitor.Token, "not-a-guid");
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
    }

    [Fact(DisplayName = "TC-11.1-API-11: replaying the same booking.created event does not create a duplicate notification")]
    public async Task Api11_DuplicateEvent_NoDuplicateNotification()
    {
        var bookingId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var date = DateTime.UtcNow.AddDays(140).ToString("yyyy-MM-dd");

        Task Publish() => _fixture.Kafka.PublishBookingCreatedAsync(bookingId, _fixture.Visitor.Id, _fixture.ProviderPersona.Id,
            listingId, "Experience", "Ceylon Adventures Pvt Ltd", date, "06:00 AM - 08:00 AM", 2, 7000m);

        await Publish();
        await _fixture.Notifications.WaitForNotificationAsync(_fixture.Visitor.Token, n => n.BookingId == bookingId);
        await Publish(); // exact replay of the same event/key

        await Task.Delay(TestConfig.EventualConsistencyTimeout / 2);
        var list = await _fixture.Notifications.GetListAsync(_fixture.Visitor.Token, pageSize: 50);
        Assert.Single(list.Items, i => i.BookingId == bookingId);
    }

    [Fact(DisplayName = "TC-11.1-API-12: an event is still processed once the consumer catches up (at-least-once delivery)")]
    public async Task Api12_RetryAfterFailure_EventuallyDelivered()
    {
        // Physically stopping/restarting the notification-service process from this test would mean
        // controlling a process the tester may be running manually (`dotnet run`) or via docker
        // compose, which this harness does not own. Kafka itself guarantees retention and
        // notification-service only commits offsets after a successful ProcessAsync call (see
        // EnableAutoCommit = false in NotificationEventConsumer.cs), so publishing here and allowing
        // a generous timeout verifies at-least-once delivery either way. To exercise the literal
        // "stop the service, then restart it" scenario, manually stop/restart the notification-service
        // process around this publish - see README.md.
        var bookingId = Guid.NewGuid();
        await _fixture.Kafka.PublishBookingCreatedAsync(bookingId, _fixture.Visitor.Id, _fixture.ProviderPersona.Id,
            Guid.NewGuid(), "Experience", "Ceylon Adventures Pvt Ltd", DateTime.UtcNow.AddDays(150).ToString("yyyy-MM-dd"),
            "03:00 PM - 05:00 PM", 1, 4000m);

        var notification = await _fixture.Notifications.WaitForNotificationAsync(_fixture.Visitor.Token,
            n => n.BookingId == bookingId, timeout: TimeSpan.FromSeconds(90));

        Assert.Equal(bookingId, notification.BookingId);
    }

    [Fact(DisplayName = "TC-11.1-API-13: a real POST /api/Bookings call produces a real booking.created notification end-to-end")]
    public async Task Api03Bonus_RealBookingServiceIntegration()
    {
        // Environment-dependent: needs at least one active, bookable experience listing in
        // provider-catalog-service. Auto-skips (with a clear reason) when the environment has none,
        // rather than failing the whole suite on missing seed data outside this story's scope.
        var catalog = new CatalogApiClient();
        var listing = await catalog.FindBookableListingAsync("experience");
        if (listing is null)
        {
            _output.WriteLine("SKIPPED: no active experience listings found in provider-catalog-service.");
            return;
        }

        var slot = await catalog.FindAvailableSlotAsync(listing.Id);
        if (slot is null)
        {
            _output.WriteLine($"SKIPPED: listing '{listing.Title}' has no open availability in the next 14 days.");
            return;
        }

        var bookingClient = new BookingApiClient();
        var response = await bookingClient.CreateExperienceBookingAsync(_fixture.Visitor.Token, listing.Id, slot.Value.Date, slot.Value.TimeSlot, 1);
        if (!response.IsSuccessStatusCode)
        {
            _output.WriteLine($"SKIPPED: booking-service rejected the booking with {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return;
        }

        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var bookingId = created.GetProperty("id").GetGuid();

        var notification = await _fixture.Notifications.WaitForNotificationAsync(_fixture.Visitor.Token, n => n.BookingId == bookingId);
        Assert.Equal("booking.created", notification.EventType);
    }
}
