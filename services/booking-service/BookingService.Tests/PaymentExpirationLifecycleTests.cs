using System.Security.Claims;
using BookingService.Controllers;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Events;
using BookingService.Models;
using BookingService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Shared.Kafka;
using Stripe;
using Stripe.Checkout;

namespace BookingService.Tests;

public class PaymentExpirationLifecycleTests
{
    private static BookingDbContext Db() => new(new DbContextOptionsBuilder<BookingDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static IPayableBooking Booking(string type, DateTime created, Guid listing, Guid visitor)
    {
        var day = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        IPayableBooking booking = type switch
        {
            "Restaurant" => new RestaurantReservation { Id = Guid.NewGuid(), VisitorId = visitor, RestaurantId = listing, ReservationDate = day, TimeSlot = "09:00 AM - 10:00 AM", PartySize = 2, TotalPrice = 100 },
            "Accommodation" => new AccommodationBooking { Id = Guid.NewGuid(), VisitorId = visitor, AccommodationId = listing, CheckInDate = day, CheckOutDate = day.AddDays(2), GuestCount = 2, TotalPrice = 100 },
            _ => new Booking { Id = Guid.NewGuid(), VisitorId = visitor, ListingId = listing, BookingDate = day, TimeSlot = "09:00", ParticipantCount = 2, TotalAmount = 100 }
        };


        booking.CreatedAt = created;
        return booking;
    }

    private static ControllerContext Visitor(Guid visitor)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        { new Claim(ClaimTypes.NameIdentifier, visitor.ToString()) }, "test")) };
        context.Request.Headers.Authorization = "Bearer provider-token";
        return new ControllerContext { HttpContext = context };
    }

    [Theory]
    [InlineData("Experience")]
    [InlineData("Restaurant")]
    [InlineData("Accommodation")]
    public async Task DeadlineMatrixAndOutboxAreIdempotent(string type)
    {
        using var db = Db();
        var now = DateTime.UtcNow;
        var records = new List<IPayableBooking>();
        foreach (var age in new[] { 14, 15, 60 })
        foreach (var payment in new[] { PaymentStatus.Unpaid, PaymentStatus.Failed, PaymentStatus.Paid })
        {
            var booking = Booking(type, now.AddMinutes(-age), Guid.NewGuid(), Guid.NewGuid());
            booking.PaymentStatus = payment;
            if (payment == PaymentStatus.Paid) booking.Status = BookingStatus.Confirmed;
            db.Add(booking);
            records.Add(booking);
        }
        await db.SaveChangesAsync();
        var kafka = new Mock<IKafkaProducer>();
        var service = new PendingPaymentExpirationService(db, kafka.Object);
        Assert.Equal(4, await service.ExpireAsync(now));
        foreach (var booking in records)
        {
            var expected = booking.PaymentStatus == PaymentStatus.Paid ? BookingStatus.Confirmed
                : booking.CreatedAt <= now.AddMinutes(-15) ? BookingStatus.Cancelled : BookingStatus.PendingPayment;
            Assert.Equal(expected, booking.Status);
        }
        Assert.Equal(4, await db.BookingCancellationMessages.CountAsync());
        Assert.Equal(0, await service.ExpireAsync(now));
        await service.PublishPendingCancellationsAsync();
        await service.PublishPendingCancellationsAsync();
        kafka.Verify(k => k.PublishAsync("booking.canceled", It.IsAny<string>(),
            It.Is<BookingCanceledEvent>(e => e.ListingType == type && e.ParticipantCount == (type == "Accommodation" ? 1 : 2)),
            It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Theory]
    [InlineData("Experience")]
    [InlineData("Restaurant")]
    [InlineData("Accommodation")]
    public async Task BrokerOutageDoesNotPreventStaleOrDeletedBookingExpiration(string type)
    {
        using var db = Db();
        var now = DateTime.UtcNow;
        var booking = Booking(type, now.AddDays(-2), Guid.NewGuid(), Guid.NewGuid());
        db.Add(booking);
        db.Entry(booking).Property("IsDeleted").CurrentValue = true;
        db.BookingCancellationMessages.Add(new BookingCancellationMessage { BookingId = Guid.NewGuid(), Payload = "{}" });
        await db.SaveChangesAsync();
        var kafka = new Mock<IKafkaProducer>();
        kafka.Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BookingCanceledEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Broker unavailable"));
        var service = new PendingPaymentExpirationService(db, kafka.Object);
        Assert.Equal(1, await service.ExpireAsync(now));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PublishPendingCancellationsAsync());
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(2, await db.BookingCancellationMessages.CountAsync(m => m.PublishedAt == null));
    }

    [Theory]
    [InlineData("Experience", 14, PaymentStatus.Unpaid)]
    [InlineData("Experience", 14, PaymentStatus.Failed)]
    [InlineData("Restaurant", 14, PaymentStatus.Failed)]
    [InlineData("Accommodation", 14, PaymentStatus.Failed)]
    [InlineData("Experience", 15, PaymentStatus.Unpaid)]
    [InlineData("Restaurant", 15, PaymentStatus.Failed)]
    [InlineData("Accommodation", 15, PaymentStatus.Failed)]
    public async Task CheckoutEnforcesOriginalDeadline(string type, int age, PaymentStatus payment)
    {
        using var db = Db();
        var visitor = Guid.NewGuid();
        var created = DateTime.UtcNow.AddMinutes(-age);
        var booking = Booking(type, created, Guid.NewGuid(), visitor);
        booking.PaymentStatus = payment;
        db.Add(booking);
        await db.SaveChangesAsync();
        var checkout = new Mock<SessionService>();
        checkout.Setup(s => s.CreateAsync(It.IsAny<SessionCreateOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Session { Id = "cs_test", Url = "https://checkout.stripe.com/test" });
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Frontend:BaseUrl"] = "https://example.com" }).Build();
        var controller = new PaymentsController(db, Mock.Of<IKafkaProducer>(), configuration, checkout.Object)
        { ControllerContext = Visitor(visitor) };
        var result = await controller.CreateCheckoutSession(new CreatePaymentRequest { BookingId = booking.Id, BookingType = type });
        if (age < 15)
        {
            Assert.IsType<OkObjectResult>(result);
            var transaction = await db.PaymentTransactions.SingleAsync();
            Assert.Equal(created.AddMinutes(15), transaction.CheckoutDeadline);
            Assert.Equal(created, booking.CreatedAt);
            Assert.Equal(BookingStatus.PendingPayment, booking.Status);
            Assert.IsType<OkObjectResult>(await controller.CreateCheckoutSession(
                new CreatePaymentRequest { BookingId = booking.Id, BookingType = type }));
            Assert.Equal(2, await db.PaymentTransactions.CountAsync());
            Assert.All(db.PaymentTransactions, attempt => Assert.Equal(created.AddMinutes(15), attempt.CheckoutDeadline));
            Assert.Equal(created, booking.CreatedAt);
        }
        else
        {
            Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(BookingStatus.Cancelled, booking.Status);
            Assert.Empty(db.PaymentTransactions);
            Assert.Single(db.BookingCancellationMessages);
            checkout.VerifyNoOtherCalls();
        }
    }

    [Fact]
    public async Task ExpiredBookingsAppearInVisitorAndProviderReportsWithZeroRevenue()
    {
        using var db = Db();
        var visitor = Guid.NewGuid();
        var listing = Guid.NewGuid();
        foreach (var type in new[] { "Experience", "Restaurant", "Accommodation" })
            db.Add(Booking(type, DateTime.UtcNow.AddHours(-1), listing, visitor));
        await db.SaveChangesAsync();
        await new PendingPaymentExpirationService(db, Mock.Of<IKafkaProducer>()).ExpireAsync(DateTime.UtcNow);
        var catalog = new Mock<ICatalogService>();
        catalog.Setup(c => c.GetReportOwnedListingIdsAsync("provider-token")).ReturnsAsync(new ProviderReportListingIds
        { ActivityIds = new() { listing }, RestaurantIds = new() { listing }, AccommodationIds = new() { listing } });
        var report = await new BookingsRevenueReportService(db, catalog.Object).GenerateAsync("provider-token", new());
        Assert.Equal(3, report.TotalBookings);
        Assert.Equal(3, report.StatusCounts["Cancelled"]);
        Assert.False(report.StatusCounts.ContainsKey("PendingPayment"));
        Assert.Equal(0, report.TotalRevenue);
        Assert.Equal(3, report.BookingTypeCounts.Count);
        var controller = new UserBookingsController(db) { ControllerContext = Visitor(visitor) };
        var result = Assert.IsType<OkObjectResult>(await controller.GetMyBookingsAndReservations());
        var rows = Assert.IsAssignableFrom<IEnumerable<UserBookingResponse>>(result.Value);
        Assert.Equal(3, rows.Count());
        Assert.All(rows, row => Assert.Equal("Cancelled", row.Status));

        var listings = new List<CatalogProviderListingResponse> { new() { Id = listing } };
        catalog.Setup(c => c.GetMyActivityListingsAsync("provider-token")).ReturnsAsync(listings);
        catalog.Setup(c => c.GetMyRestaurantListingsAsync("provider-token")).ReturnsAsync(listings);
        catalog.Setup(c => c.GetMyAccommodationListingsAsync("provider-token")).ReturnsAsync(listings);
        var provider = new ProviderBookingsController(db, catalog.Object, Mock.Of<IIdentityService>())
        { ControllerContext = Visitor(visitor) };
        var providerResult = Assert.IsType<OkObjectResult>(await provider.GetMyBookingsAndReservations());
        var providerRows = Assert.IsAssignableFrom<IEnumerable<ProviderBookingResponse>>(providerResult.Value);
        Assert.Equal(3, providerRows.Count());
        Assert.All(providerRows, row => Assert.Equal("Cancelled", row.Status));
    }

    [Theory]
    [InlineData("HandleExpiredCheckout")]
    [InlineData("HandleFailedPaymentIntent")]
    [InlineData("HandleSuccessfulPayment")]
    public async Task LateWebhookCannotReviveCancelledBooking(string method)
    {
        using var db = Db();
        var booking = Booking("Experience", DateTime.UtcNow.AddHours(-1), Guid.NewGuid(), Guid.NewGuid());
        booking.Status = BookingStatus.Cancelled;
        db.Add(booking);
        var attempt = new PaymentTransaction { Id = Guid.NewGuid(), BookingId = booking.Id, VisitorId = booking.VisitorId };
        db.Add(attempt);
        await db.SaveChangesAsync();
        var metadata = new Dictionary<string, string> { ["transactionId"] = attempt.Id.ToString() };
        object evt = method == "HandleFailedPaymentIntent" ? new PaymentIntent { Metadata = metadata }
            : new Session { Metadata = metadata, AmountTotal = 0, Currency = "lkr", PaymentStatus = "paid" };
        var controller = new PaymentsController(db, Mock.Of<IKafkaProducer>(), new ConfigurationBuilder().Build());
        var handler = typeof(PaymentsController).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        await (Task)handler.Invoke(controller, new[] { evt })!;
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.NotEqual(PaymentStatus.Paid, booking.PaymentStatus);
    }

    [Fact]
    public async Task OpenCheckoutIsExpiredOnceAtOriginalDeadline()
    {
        using var db = Db();
        var now = DateTime.UtcNow;
        db.Add(new PaymentTransaction { Id = Guid.NewGuid(), CheckoutSessionId = "cs_test", CheckoutDeadline = now });
        await db.SaveChangesAsync();
        var checkout = new Mock<SessionService>();
        checkout.Setup(s => s.GetAsync("cs_test", It.IsAny<SessionGetOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Session { Id = "cs_test", Status = "open" });
        checkout.Setup(s => s.ExpireAsync("cs_test", It.IsAny<SessionExpireOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Session { Id = "cs_test", Status = "expired" });
        var service = new PendingPaymentExpirationService(db, Mock.Of<IKafkaProducer>());
        await service.CloseExpiredCheckoutsAsync(checkout.Object, now.AddTicks(-1));
        checkout.VerifyNoOtherCalls();
        await service.CloseExpiredCheckoutsAsync(checkout.Object, now);
        await service.CloseExpiredCheckoutsAsync(checkout.Object, now.AddMinutes(1));
        checkout.Verify(s => s.ExpireAsync("cs_test", It.IsAny<SessionExpireOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StripeOutageLeavesFailedCleanupQueuedAndDoesNotBlockOtherSessions()
    {
        using var db = Db();
        var now = DateTime.UtcNow;
        var unavailable = new PaymentTransaction { Id = Guid.NewGuid(), CheckoutSessionId = "cs_unavailable", CheckoutDeadline = now };
        var available = new PaymentTransaction { Id = Guid.NewGuid(), CheckoutSessionId = "cs_available", CheckoutDeadline = now };
        db.AddRange(unavailable, available);
        await db.SaveChangesAsync();
        var checkout = new Mock<SessionService>();
        checkout.SetupSequence(s => s.GetAsync("cs_unavailable", It.IsAny<SessionGetOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StripeException("Temporary outage"))
            .ReturnsAsync(new Session { Id = "cs_unavailable", Status = "open" });
        checkout.Setup(s => s.GetAsync("cs_available", It.IsAny<SessionGetOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Session { Id = "cs_available", Status = "open" });
        checkout.Setup(s => s.ExpireAsync(It.IsAny<string>(), It.IsAny<SessionExpireOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Session { Status = "expired" });
        var service = new PendingPaymentExpirationService(db, Mock.Of<IKafkaProducer>());
        await Assert.ThrowsAsync<AggregateException>(() => service.CloseExpiredCheckoutsAsync(checkout.Object, now));
        Assert.Null(unavailable.CheckoutClosedAt);
        Assert.NotNull(available.CheckoutClosedAt);
        await service.CloseExpiredCheckoutsAsync(checkout.Object, now.AddSeconds(5));
        Assert.NotNull(unavailable.CheckoutClosedAt);
        checkout.Verify(s => s.ExpireAsync("cs_available", It.IsAny<SessionExpireOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Experience", BookingStatus.Cancelled)]
    [InlineData("Restaurant", BookingStatus.Cancelled)]
    [InlineData("Accommodation", BookingStatus.Cancelled)]
    [InlineData("Experience", BookingStatus.Confirmed)]
    public async Task StripeCleanupFailureCannotReopenTerminalBooking(string type, BookingStatus terminalStatus)
    {
        using var db = Db();
        var booking = Booking(type, DateTime.UtcNow.AddMinutes(-14), Guid.NewGuid(), Guid.NewGuid());
        db.Add(booking);
        await db.SaveChangesAsync();
        var checkout = new Mock<SessionService>();
        checkout.Setup(s => s.CreateAsync(It.IsAny<SessionCreateOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                // The worker/another successful attempt finishes while Stripe is responding.
                booking.Status = terminalStatus;
                booking.PaymentStatus = terminalStatus == BookingStatus.Confirmed ? PaymentStatus.Paid : PaymentStatus.Failed;
                await db.SaveChangesAsync();
                return new Session { Id = "cs_test", Url = "https://checkout.stripe.com/test" };
            });
        checkout.Setup(s => s.ExpireAsync("cs_test", It.IsAny<SessionExpireOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StripeException("Temporary Stripe outage"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Frontend:BaseUrl"] = "https://example.com" }).Build();
        var controller = new PaymentsController(db, Mock.Of<IKafkaProducer>(), configuration, checkout.Object)
        { ControllerContext = Visitor(booking.VisitorId) };

        await controller.CreateCheckoutSession(new CreatePaymentRequest { BookingId = booking.Id, BookingType = type });

        await db.Entry(booking).ReloadAsync();
        Assert.Equal(terminalStatus, booking.Status);
        if (terminalStatus == BookingStatus.Confirmed) Assert.Equal(PaymentStatus.Paid, booking.PaymentStatus);
    }

    [Theory]
    [InlineData("Experience", true)]
    [InlineData("Restaurant", true)]
    [InlineData("Accommodation", true)]
    [InlineData("Experience", false)]
    [InlineData("Restaurant", false)]
    [InlineData("Accommodation", false)]
    public async Task ConcurrentPaymentAndExpirationCannotOverwriteWinningTransition(string type, bool paymentWins)
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var first = new BookingDbContext(options);
        var booking = Booking(type, DateTime.UtcNow.AddHours(-1), Guid.NewGuid(), Guid.NewGuid());
        first.Add(booking);
        await first.SaveChangesAsync();
        using var second = new BookingDbContext(options);
        var stale = (IPayableBooking)(await second.FindAsync(booking.GetType(), booking.Id))!;

        if (paymentWins)
        {
            booking.Status = BookingStatus.Confirmed;
            booking.PaymentStatus = PaymentStatus.Paid;
            PendingPaymentExpirationService.MarkExpired(stale, DateTime.UtcNow);
        }
        else
        {
            PendingPaymentExpirationService.MarkExpired(booking, DateTime.UtcNow);
            stale.Status = BookingStatus.Confirmed;
            stale.PaymentStatus = PaymentStatus.Paid;
        }

        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        await first.Entry(booking).ReloadAsync();
        Assert.Equal(paymentWins ? BookingStatus.Confirmed : BookingStatus.Cancelled, booking.Status);
        Assert.Equal(paymentWins ? PaymentStatus.Paid : PaymentStatus.Failed, booking.PaymentStatus);
    }
}
