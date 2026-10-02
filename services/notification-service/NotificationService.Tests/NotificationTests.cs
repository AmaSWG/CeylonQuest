using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using NotificationService.Controllers;
using NotificationService.Data;
using NotificationService.DTOs;
using NotificationService.Events;
using NotificationService.Models;
using NotificationService.Services;
using NotificationHandler = NotificationService.Services.NotificationService;
using Xunit;

namespace NotificationService.Tests;

public class NotificationTests
{
    // Mapping tests only: MySQL integration must separately verify transactions and uniqueness.
    private static NotificationDbContext Database() => new(new DbContextOptionsBuilder<NotificationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

    [Fact]
    public async Task BookingThenCancellation_NotifiesBothIdentityUsers_AndReplayIsIgnored()
    {
        await using var db = Database();

        var visitor = Guid.NewGuid(); 
        var provider = Guid.NewGuid(); 
        var booking = Guid.NewGuid(); 
        var listing = Guid.NewGuid();

        var resolver = new RecipientResolver(db, new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { [$"Notifications:ListingProviders:{listing}"] = provider.ToString() }).Build());

        var processor = new NotificationEventProcessor(db, resolver);

        var created = JsonSerializer.Serialize(new BookingCreatedEvent
        {
            BookingId = booking, VisitorId = visitor, ListingId = listing,
            ListingType = "Experience", BookingDate = "2026-10-01", TimeSlot = "10:00",
            ParticipantCount = 2, CreatedAt = DateTime.UtcNow
        });

        await processor.ProcessAsync("booking.created", created, default);
        await processor.ProcessAsync("booking.created", created, default);
        Assert.Equal(2, await db.Notifications.CountAsync());
        Assert.Equal(new[] { visitor, provider }.Order(), db.Notifications.Select(n => n.RecipientUserId).AsEnumerable().Order());

        var canceled = JsonSerializer.Serialize(new BookingCanceledEvent
        {
            BookingId = booking, ListingId = listing, Reason = "Plans changed", CanceledAt = DateTime.UtcNow,
            RefundAmount = 100, RefundStatus = "Pending"
        });

        await processor.ProcessAsync("booking.canceled", canceled, default);
        await processor.ProcessAsync("booking.canceled", canceled, default);
        Assert.Equal(4, await db.Notifications.CountAsync());

        Assert.All(db.Notifications.Where(n => n.EventType == "booking.canceled"), n =>
        {
            Assert.Equal(100m, n.RefundAmount);
            Assert.Contains("Plans changed", n.Message);
        });
    }

    [Fact]
    public async Task PaymentNotifiesVisitor_ReviewNotifiesProvider()
    {
        await using var db = Database();
        var visitor = Guid.NewGuid(); 
        var provider = Guid.NewGuid(); 
        var booking = Guid.NewGuid();
        var processor = new NotificationEventProcessor(db, new RecipientResolver(db, new ConfigurationBuilder().Build()));
        await processor.ProcessAsync("payment.completed", JsonSerializer.Serialize(new PaymentCompletedEvent
        {
            BookingId = booking, PaymentId = Guid.NewGuid(), VisitorId = visitor,
            Amount = 1250.50m, Currency = "LKR", CompletedAt = DateTime.UtcNow, TransactionReference = "receipt-123"
        }), default);
        await processor.ProcessAsync("review.submitted", JsonSerializer.Serialize(new ReviewSubmittedEvent
        {
            ReviewId = Guid.NewGuid(), BookingId = booking, ListingId = Guid.NewGuid(),
            ProviderUserId = provider, Rating = 4, ReviewText = "Great experience", SubmittedAt = DateTime.UtcNow
        }), default);
        var payment = await db.Notifications.SingleAsync(n => n.EventType == "payment.completed");
        Assert.Equal(visitor, payment.RecipientUserId);
        Assert.Equal(1250.50m, payment.Amount);
        var review = await db.Notifications.SingleAsync(n => n.EventType == "review.submitted");
        Assert.Equal(provider, review.RecipientUserId);
        Assert.Equal(4, review.Rating);
        Assert.Contains("Great experience", review.Message);
    }

    [Fact]
    public async Task MissingRecipientCanRecover_WithoutPartialNotifications()
    {
        await using var db = Database();
        var listing = Guid.NewGuid();
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var processor = new NotificationEventProcessor(db, new RecipientResolver(db, config));
        var payload = JsonSerializer.Serialize(new BookingCreatedEvent
        {
            BookingId = Guid.NewGuid(), VisitorId = Guid.NewGuid(), ListingId = listing,
            BookingDate = "2026-10-01", CreatedAt = DateTime.UtcNow
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() => processor.ProcessAsync("booking.created", payload, default));
        Assert.Empty(db.Notifications); Assert.Empty(db.ProcessedEvents);
        config[$"Notifications:ListingProviders:{listing}"] = Guid.NewGuid().ToString();
        await processor.ProcessAsync("booking.created", payload, default);
        await processor.ProcessAsync("booking.created", payload, default);
        Assert.Equal(2, await db.Notifications.CountAsync());
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task InvalidEventsAreNotAcknowledgedByProcessor(string payload)
    {
        await using var db = Database();
        var processor = new NotificationEventProcessor(db, new RecipientResolver(db, new ConfigurationBuilder().Build()));
        await Assert.ThrowsAsync<JsonException>(() => processor.ProcessAsync("booking.created", payload, default));
        Assert.Empty(db.ProcessedEvents); Assert.Empty(db.Notifications);
    }

    [Fact]
    public async Task ListAndUnreadAreRestrictedToAuthenticatedUser()
    {
        await using var db = Database();
        var user = Guid.NewGuid();
        db.Notifications.AddRange(new Notification { RecipientUserId = user, EventKey = "a" },
            new Notification { RecipientUserId = Guid.NewGuid(), EventKey = "b" });
        await db.SaveChangesAsync();
        var controller = new NotificationController(new NotificationHandler(db))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.ToString()) }, "test"))
            } }
        };
        var result = Assert.IsType<OkObjectResult>(await controller.Get());
        var response = Assert.IsType<NotificationListResponse>(result.Value);
        Assert.Single(response.Items); Assert.Equal(1, response.UnreadCount);
        Assert.IsType<NotFoundResult>(await controller.Read(db.Notifications.Single(n => n.RecipientUserId != user).Id, default));
        Assert.IsType<BadRequestObjectResult>(await controller.Get(page: 0));
    }

    [Fact]
    public async Task MissingIdentityClaimIsRejected()
    {
        await using var db = Database();
        var controller = new NotificationController(new NotificationHandler(db))
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        Assert.IsType<UnauthorizedResult>(await controller.Get());
    }

    [Fact]
    public async Task CompletedReplayDoesNotNeedRecipientLookup()
    {
        await using var db = Database();
        var listing = Guid.NewGuid();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { [$"Notifications:ListingProviders:{listing}"] = Guid.NewGuid().ToString() }).Build();
        var processor = new NotificationEventProcessor(db, new RecipientResolver(db, config));
        var payload = JsonSerializer.Serialize(new BookingCreatedEvent
        {
            BookingId = Guid.NewGuid(), VisitorId = Guid.NewGuid(), ListingId = listing,
            BookingDate = "2026-10-01", CreatedAt = DateTime.UtcNow
        });
        await processor.ProcessAsync("booking.created", payload, default);
        config[$"Notifications:ListingProviders:{listing}"] = null;
        await processor.ProcessAsync("booking.created", payload, default);
        Assert.Equal(2, await db.Notifications.CountAsync());
    }
}
