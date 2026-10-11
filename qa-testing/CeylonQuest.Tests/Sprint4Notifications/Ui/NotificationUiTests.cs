using System.Net.Http.Json;
using System.Text.Json;
using CeylonQuest.Tests.Sprint4Notifications.Infrastructure;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace CeylonQuest.Tests.Sprint4Notifications.Ui;

/// <summary>
/// Story 11.1 "Receive Event Notifications" - Selenium UI test cases (TC-11.1-UI-01..11).
/// Drives the real frontend dev server against the real notification-service. Notification data is
/// seeded by publishing directly to Kafka (see KafkaEventPublisher) rather than through
/// booking-service/Stripe/the (not yet built) review feature - see README.md for the rationale and
/// for the "provider persona" technique (a second real visitor account stands in for the provider
/// recipient, since real provider onboarding needs OTP email activation + admin approval).
/// </summary>
[Collection(nameof(NotificationUiCollection))]
public sealed class NotificationUiTests
{
    private readonly NotificationUiFixture _fixture;
    private readonly ITestOutputHelper _output;

    public NotificationUiTests(NotificationUiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact(DisplayName = "TC-11.1-UI-01: bell and unread badge are shown for both visitor and provider logins")]
    public async Task Ui01_BellAndBadgeDisplay()
    {
        var visitor = await _fixture.NewUserAsync("ui01-visitor");
        var providerPersona = await _fixture.NewUserAsync("ui01-provider");

        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        Assert.True(_fixture.Shell.IsBellVisible());
        Assert.Equal(0, _fixture.Shell.ReadBellBadgeCount());

        await _fixture.Kafka.PublishBookingCreatedAsync(Guid.NewGuid(), visitor.Id, providerPersona.Id,
            Guid.NewGuid(), "Experience", "Ceylon Adventures Pvt Ltd", DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-dd"),
            "09:00 AM - 11:00 AM", 2, 5000m);
        await _fixture.NotificationsApi.WaitForNotificationAsync(visitor.Token, _ => true);

        _fixture.Shell.Refresh();
        Eventually.Assert(() => _fixture.Shell.ReadBellBadgeCount() > 0,
            message: "Unread badge should appear once a notification exists.");

        _fixture.Shell.LoginAs(providerPersona.Email, providerPersona.Password);
        Assert.True(_fixture.Shell.IsBellVisible());
        Eventually.Assert(() => _fixture.Shell.ReadBellBadgeCount() > 0);
    }

    [Fact(DisplayName = "TC-11.1-UI-02: S1 experience booking produces a visitor notification with the booking context")]
    public async Task Ui02_ExperienceBooking_VisitorNotification()
    {
        var visitor = await _fixture.NewUserAsync("ui02-visitor");
        var providerPersona = await _fixture.NewUserAsync("ui02-provider");
        var bookingId = Guid.NewGuid();
        var date = DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd");
        const string timeSlot = "09:00 AM - 11:00 AM";
        const int participants = 2;

        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        var badgeBefore = _fixture.Shell.ReadBellBadgeCount();

        await _fixture.Kafka.PublishBookingCreatedAsync(bookingId, visitor.Id, providerPersona.Id,
            Guid.NewGuid(), "Experience", "Ceylon Adventures Pvt Ltd", date, timeSlot, participants, 12000m);
        await _fixture.NotificationsApi.WaitForNotificationAsync(visitor.Token, n => n.BookingId == bookingId);

        _fixture.Shell.OpenNotifications();
        var panel = new NotificationPanelPage(_fixture.Driver);

        IWebElement? item = null;
        Eventually.Assert(() => (item = panel.FindItemContaining(date)) != null,
            message: "Expected the booking-created notification to render in the panel within 15s.");

        var text = item!.Text;
        Assert.Contains("Booking created", text);
        Assert.Contains(date, text);
        Assert.Contains(timeSlot, text);
        Assert.Contains(participants.ToString(), text);
        Assert.Contains("Ceylon Adventures Pvt Ltd", text);
        // QA NOTE (gap): the panel/message does not render the experience's own title, nor any
        // visible "booking reference" text - only the provider's business name, date, time slot and
        // participant count. Flag for CEYQ-194/195 against this scenario's expected result.

        Eventually.Assert(() => _fixture.Shell.ReadBellBadgeCount() > badgeBefore, message: "Badge should increment after the new notification.");
    }

    [Fact(DisplayName = "TC-11.1-UI-03: S1 the owning provider sees a notification for the same booking")]
    public async Task Ui03_ExperienceBooking_ProviderNotification()
    {
        var visitor = await _fixture.NewUserAsync("ui03-visitor");
        var providerPersona = await _fixture.NewUserAsync("ui03-provider");
        var bookingId = Guid.NewGuid();
        var date = DateTime.UtcNow.AddDays(4).ToString("yyyy-MM-dd");
        const string timeSlot = "01:00 PM - 03:00 PM";

        await _fixture.Kafka.PublishBookingCreatedAsync(bookingId, visitor.Id, providerPersona.Id,
            Guid.NewGuid(), "Experience", "Ceylon Adventures Pvt Ltd", date, timeSlot, 4, 20000m);
        await _fixture.NotificationsApi.WaitForNotificationAsync(providerPersona.Token, n => n.BookingId == bookingId);

        _fixture.Shell.LoginAs(providerPersona.Email, providerPersona.Password);
        _fixture.Shell.OpenNotifications();
        var panel = new NotificationPanelPage(_fixture.Driver);

        IWebElement? item = null;
        Eventually.Assert(() => (item = panel.FindItemContaining(date)) != null);
        Assert.Contains(timeSlot, item!.Text);
        Assert.Contains("4", item.Text);
    }

    [Fact(DisplayName = "TC-11.1-UI-04: S1 restaurant reservation notifies both visitor and provider panels")]
    public async Task Ui04_RestaurantReservation_BothPanels()
    {
        var visitor = await _fixture.NewUserAsync("ui04-visitor");
        var providerPersona = await _fixture.NewUserAsync("ui04-provider");
        var bookingId = Guid.NewGuid();
        var date = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd");
        const string timeSlot = "07:00 PM - 08:00 PM";
        const int partySize = 5;

        await _fixture.Kafka.PublishBookingCreatedAsync(bookingId, visitor.Id, providerPersona.Id,
            Guid.NewGuid(), "Restaurant", "Spice Garden Restaurant", date, timeSlot, partySize, 15000m);
        await _fixture.NotificationsApi.WaitForNotificationAsync(visitor.Token, n => n.BookingId == bookingId);
        await _fixture.NotificationsApi.WaitForNotificationAsync(providerPersona.Token, n => n.BookingId == bookingId);

        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenNotifications();
        var visitorPanel = new NotificationPanelPage(_fixture.Driver);
        IWebElement? visitorItem = null;
        Eventually.Assert(() => (visitorItem = visitorPanel.FindItemContaining("Spice Garden Restaurant")) != null);
        Assert.Contains(date, visitorItem!.Text);
        Assert.Contains(timeSlot, visitorItem.Text);
        Assert.Contains(partySize.ToString(), visitorItem.Text);

        _fixture.Shell.LoginAs(providerPersona.Email, providerPersona.Password);
        _fixture.Shell.OpenNotifications();
        var providerPanel = new NotificationPanelPage(_fixture.Driver);
        IWebElement? providerItem = null;
        Eventually.Assert(() => (providerItem = providerPanel.FindItemContaining("Spice Garden Restaurant")) != null);
        Assert.Contains(timeSlot, providerItem!.Text);
    }

    [Fact(DisplayName = "TC-11.1-UI-05: S2 payment completed notification shows the paid amount matching the booking total")]
    public async Task Ui05_PaymentCompleted_VisitorSeesConfirmation()
    {
        // See API-04's note: a real Stripe test-mode payment needs `stripe listen` forwarding
        // webhooks to the local booking-service, which this harness does not manage, so the
        // deterministic payment.completed contract is published directly instead.
        var visitor = await _fixture.NewUserAsync("ui05-visitor");
        var bookingId = Guid.NewGuid();
        const decimal amount = 18500m;
        var reference = $"PAY-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        await _fixture.Kafka.PublishPaymentCompletedAsync(bookingId, visitor.Id, amount, reference);
        await _fixture.NotificationsApi.WaitForNotificationAsync(visitor.Token, n => n.BookingId == bookingId && n.EventType == "payment.completed");

        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenNotifications();
        var panel = new NotificationPanelPage(_fixture.Driver);

        IWebElement? item = null;
        Eventually.Assert(() => (item = panel.FindItemContaining("Payment completed")) != null);
        Assert.Contains(reference, item!.Text);
        Assert.Contains("18500.00", item.Text);
    }

    [Fact(DisplayName = "TC-11.1-UI-06: S3 a cancellation notifies both users; the visitor's shows the refund amount")]
    public async Task Ui06_BookingCanceled_BothPanelsShowRefund()
    {
        var visitor = await _fixture.NewUserAsync("ui06-visitor");
        var providerPersona = await _fixture.NewUserAsync("ui06-provider");
        var bookingId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        var date = DateTime.UtcNow.AddDays(6).ToString("yyyy-MM-dd");
        const string timeSlot = "08:00 AM - 10:00 AM";
        const string reason = "Visitor changed travel plans";

        await _fixture.Kafka.PublishBookingCreatedAsync(bookingId, visitor.Id, providerPersona.Id,
            listingId, "Experience", "Ceylon Adventures Pvt Ltd", date, timeSlot, 2, 9000m);
        await _fixture.NotificationsApi.WaitForNotificationAsync(visitor.Token, n => n.BookingId == bookingId);

        await _fixture.Kafka.PublishBookingCanceledAsync(bookingId, listingId, visitor.Id, providerPersona.Id,
            "Experience", date, timeSlot, reason, 4500m, "PartialRefund");
        await _fixture.NotificationsApi.WaitForNotificationAsync(visitor.Token, n => n.BookingId == bookingId && n.EventType == "booking.canceled");
        await _fixture.NotificationsApi.WaitForNotificationAsync(providerPersona.Token, n => n.BookingId == bookingId && n.EventType == "booking.canceled");

        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenNotifications();
        var visitorPanel = new NotificationPanelPage(_fixture.Driver);
        IWebElement? visitorItem = null;
        Eventually.Assert(() => (visitorItem = visitorPanel.FindItemContaining("canceled")) != null);
        Assert.Contains(reason, visitorItem!.Text);
        Assert.Contains("4500.00", visitorItem.Text);
        // QA NOTE (gap): only a refund amount is shown, not a refund percentage - see the matching
        // note in NotificationApiTests.Api05.

        _fixture.Shell.LoginAs(providerPersona.Email, providerPersona.Password);
        _fixture.Shell.OpenNotifications();
        var providerPanel = new NotificationPanelPage(_fixture.Driver);
        IWebElement? providerItem = null;
        Eventually.Assert(() => (providerItem = providerPanel.FindItemContaining("canceled")) != null);
        Assert.Contains(reason, providerItem!.Text);
    }

    [Fact(DisplayName = "TC-11.1-UI-11: cancelling a real booking from My Bookings triggers a real cancellation notification")]
    public async Task Ui06Bonus_RealCancelFlowFromMyBookings()
    {
        var catalog = new CatalogApiClient();
        var listing = await catalog.FindBookableListingAsync("experience");
        if (listing is null) { _output.WriteLine("SKIPPED: no active experience listing found to book+cancel for real."); return; }
        var slot = await catalog.FindAvailableSlotAsync(listing.Id);
        if (slot is null) { _output.WriteLine($"SKIPPED: '{listing.Title}' has no open availability in the next 14 days."); return; }

        var visitor = await _fixture.NewUserAsync("ui06-bonus-visitor");
        var bookingClient = new BookingApiClient();
        var createResponse = await bookingClient.CreateExperienceBookingAsync(visitor.Token, listing.Id, slot.Value.Date, slot.Value.TimeSlot, 1);
        if (!createResponse.IsSuccessStatusCode)
        {
            _output.WriteLine($"SKIPPED: could not create a real booking to cancel ({(int)createResponse.StatusCode}).");
            return;
        }
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var bookingId = created.GetProperty("id").GetGuid();
        await _fixture.NotificationsApi.WaitForNotificationAsync(visitor.Token, n => n.BookingId == bookingId);

        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenBookings();
        var driver = _fixture.Driver;
        var wait = DriverFactory.Wait(driver);

        wait.Until(d => d.FindElements(By.CssSelector(".vb-cancel-btn")).Count > 0);
        driver.FindElements(By.CssSelector(".vb-cancel-btn"))[0].Click();
        wait.Until(d => d.FindElements(By.CssSelector(".vb-confirm-cancel-btn")).Count > 0);
        driver.FindElement(By.CssSelector(".vb-confirm-cancel-btn")).Click(); // "Yes, Continue to Cancel"
        wait.Until(d => d.FindElements(By.Id("cancellationReason")).Count > 0);
        driver.FindElement(By.Id("cancellationReason")).SendKeys("QA automated test cancellation");
        driver.FindElement(By.CssSelector(".vb-confirm-cancel-btn")).Click(); // "Confirm ... Cancellation"
        wait.Until(d => d.FindElements(By.CssSelector(".vb-success-modal")).Count > 0);

        _fixture.Shell.OpenNotifications();
        var panel = new NotificationPanelPage(driver);
        IWebElement? item = null;
        Eventually.Assert(() => (item = panel.FindItemContaining("canceled")) != null,
            message: "Expected a cancellation notification to appear after the real cancel flow.");
        Assert.Contains("QA automated test cancellation", item!.Text);
    }

    [Fact(DisplayName = "TC-11.1-UI-07: marking one notification as read updates styling/badge and persists after refresh")]
    public async Task Ui09_MarkOneAsRead()
    {
        var visitor = await _fixture.NewUserAsync("ui09-visitor");
        var providerPersona = await _fixture.NewUserAsync("ui09-provider");
        for (var i = 0; i < 3; i++)
            await _fixture.Kafka.PublishBookingCreatedAsync(Guid.NewGuid(), visitor.Id, providerPersona.Id,
                Guid.NewGuid(), "Experience", "Ceylon Adventures Pvt Ltd", DateTime.UtcNow.AddDays(10 + i).ToString("yyyy-MM-dd"),
                "10:00 AM - 11:00 AM", 1, 3000m);
        await _fixture.NotificationsApi.WaitForUnreadAtLeastAsync(visitor.Token, 3);

        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenNotifications();
        var panel = new NotificationPanelPage(_fixture.Driver);
        Eventually.Assert(() => panel.Items.Count(panel.IsUnread) >= 3);

        var badgeBefore = _fixture.Shell.ReadBellBadgeCount();
        var unreadCountBefore = panel.Items.Count(panel.IsUnread);
        panel.MarkAsRead(panel.Items.First(panel.IsUnread));

        Eventually.Assert(() => panel.Items.Count(panel.IsUnread) == unreadCountBefore - 1,
            message: "Exactly one item should flip to read styling.");
        Assert.Equal(badgeBefore - 1, _fixture.Shell.ReadBellBadgeCount());

        _fixture.Shell.Refresh();
        _fixture.Shell.OpenNotifications();
        var afterRefresh = new NotificationPanelPage(_fixture.Driver);
        Eventually.Assert(() => afterRefresh.Items.Count(afterRefresh.IsUnread) == unreadCountBefore - 1,
            message: "Read state must persist across a page refresh.");
    }

    [Fact(DisplayName = "TC-11.1-UI-08: mark-all-as-read hides the badge and persists after refresh and re-login")]
    public async Task Ui10_MarkAllAsRead()
    {
        var visitor = await _fixture.NewUserAsync("ui10-visitor");
        var providerPersona = await _fixture.NewUserAsync("ui10-provider");
        for (var i = 0; i < 2; i++)
            await _fixture.Kafka.PublishBookingCreatedAsync(Guid.NewGuid(), visitor.Id, providerPersona.Id,
                Guid.NewGuid(), "Experience", "Ceylon Adventures Pvt Ltd", DateTime.UtcNow.AddDays(20 + i).ToString("yyyy-MM-dd"),
                "11:00 AM - 12:00 PM", 1, 3000m);
        await _fixture.NotificationsApi.WaitForUnreadAtLeastAsync(visitor.Token, 2);

        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenNotifications();
        var panel = new NotificationPanelPage(_fixture.Driver);
        Eventually.Assert(() => panel.Items.Count(panel.IsUnread) >= 2);

        // Wait for the "Mark all as read" button to be enabled (not disabled / busy) before clicking.
        var markAllBtn = _fixture.Driver.FindElement(By.CssSelector(".cq-notifications__heading button"));
        Eventually.Assert(() => markAllBtn.Enabled, message: "Mark-all-as-read button should be enabled before clicking.");
        markAllBtn.Click();

        // Wait for the server round-trip to complete before asserting.
        Eventually.Assert(() => panel.Items.Count(panel.IsUnread) == 0, message: "All items should show as read.");
        Assert.Equal(0, _fixture.Shell.ReadBellBadgeCount());

        _fixture.Shell.Refresh();
        _fixture.Shell.OpenNotifications();
        var afterRefresh = new NotificationPanelPage(_fixture.Driver);
        Assert.Equal(0, afterRefresh.Items.Count(afterRefresh.IsUnread));

        // Re-login (a fresh session) to confirm the read-state persists server-side, not just in this tab.
        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenNotifications();
        var afterRelogin = new NotificationPanelPage(_fixture.Driver);
        Assert.Equal(0, afterRelogin.Items.Count(afterRelogin.IsUnread));
        Assert.Equal(0, _fixture.Shell.ReadBellBadgeCount());
    }

    [Fact(DisplayName = "TC-11.1-UI-09: a brand-new user with no notifications sees the friendly empty state")]
    public async Task Ui11_EmptyState()
    {
        var freshUser = await _fixture.NewUserAsync("ui11-empty-state");

        _fixture.Shell.LoginAs(freshUser.Email, freshUser.Password);
        Assert.Equal(0, _fixture.Shell.ReadBellBadgeCount());

        _fixture.Shell.OpenNotifications();
        var panel = new NotificationPanelPage(_fixture.Driver);
        Eventually.Assert(() => panel.IsEmptyStateShown, message: "Expected the friendly empty-state message.");
        Assert.Empty(panel.Items);
    }

    [Fact(DisplayName = "TC-11.1-UI-10: user isolation - visitors and providers only ever see their own notifications, newest first")]
    public async Task Ui12_UserIsolation()
    {
        var visitor = await _fixture.NewUserAsync("ui12-visitor-a");
        var providerPersona = await _fixture.NewUserAsync("ui12-provider-a");
        var otherVisitor = await _fixture.NewUserAsync("ui12-visitor-b");
        var otherProvider = await _fixture.NewUserAsync("ui12-provider-b");

        var bookingId = Guid.NewGuid();
        var date = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");
        await _fixture.Kafka.PublishBookingCreatedAsync(bookingId, visitor.Id, providerPersona.Id,
            Guid.NewGuid(), "Experience", "Ceylon Adventures Pvt Ltd", date, "06:00 PM - 08:00 PM", 2, 9000m);
        await _fixture.NotificationsApi.WaitForNotificationAsync(visitor.Token, n => n.BookingId == bookingId);

        _fixture.Shell.LoginAs(otherVisitor.Email, otherVisitor.Password);
        _fixture.Shell.OpenNotifications();
        var otherVisitorPanel = new NotificationPanelPage(_fixture.Driver);
        await Task.Delay(TestConfig.PollInterval * 4);
        Assert.Null(otherVisitorPanel.FindItemContaining(date));

        _fixture.Shell.LoginAs(otherProvider.Email, otherProvider.Password);
        _fixture.Shell.OpenNotifications();
        var otherProviderPanel = new NotificationPanelPage(_fixture.Driver);
        await Task.Delay(TestConfig.PollInterval * 4);
        Assert.Null(otherProviderPanel.FindItemContaining(date));

        _fixture.Shell.LoginAs(visitor.Email, visitor.Password);
        _fixture.Shell.OpenNotifications();
        var ownPanel = new NotificationPanelPage(_fixture.Driver);
        IWebElement? item = null;
        Eventually.Assert(() => (item = ownPanel.FindItemContaining(date)) != null);

        // Newest-first ordering: the item we just created should be the first one rendered.
        Assert.Equal(item!.Text, ownPanel.Items.First().Text);
    }
}
