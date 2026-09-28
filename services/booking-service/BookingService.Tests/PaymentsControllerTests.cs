using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BookingService.Controllers;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Events;
using BookingService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using Shared.Kafka;
using Stripe;
using Stripe.Checkout;

namespace BookingService.Tests;

public class PaymentsControllerTests
{
    private const string Secret = "whsec_unit_test_only";

    [Theory]
    [InlineData("Experience")]
    [InlineData("Accommodation")]
    [InlineData("Restaurant")]
    public async Task Checkout_UsesStoredAmount_AndSuccessIsIdempotent(string type)
    {
        using var fixture = new Fixture(type);
        await fixture.Create();
        var transaction = Assert.Single(fixture.Db.PaymentTransactions);
        Assert.Equal(1234.56m, transaction.Amount);
        Assert.Equal(type, transaction.BookingType);
        Assert.Equal(123456, fixture.Options!.LineItems.Single().PriceData.UnitAmount);
        Assert.Equal(type, fixture.Options.Metadata["bookingType"]);
        Assert.Equal(type, fixture.Options.PaymentIntentData.Metadata["bookingType"]);
        Assert.Equal(BookingStatus.PendingPayment, fixture.Booking.Status);

        await fixture.Success(transaction);
        await fixture.Success(transaction);
        Assert.Equal(PaymentStatus.Paid, fixture.Booking.PaymentStatus);
        Assert.Equal(BookingStatus.Confirmed, fixture.Booking.Status);
        Assert.Equal(PaymentStatus.Paid, transaction.Status);
        fixture.Kafka.Verify(k => k.PublishAsync("payment.completed", fixture.Booking.Id.ToString(),
            It.Is<PaymentCompletedEvent>(e => e.BookingType == type && e.Amount == 1234.56m),
            It.IsAny<CancellationToken>()), Times.Once);
        fixture.Kafka.Verify(k => k.PublishAsync("payment.confirmed", fixture.Booking.Id.ToString(),
            It.Is<PaymentConfirmedEvent>(e => e.BookingType == type && e.BookingStatus == "Confirmed"),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.IsType<ConflictObjectResult>(await fixture.Controller.CreateCheckoutSession(fixture.Request));
        Assert.Single(fixture.Db.PaymentTransactions);
    }

    [Theory]
    [InlineData("Experience")]
    [InlineData("Accommodation")]
    [InlineData("Restaurant")]
    public async Task Failure_CanLaterSucceed_AndLateFailureCannotUndoPayment(string type)
    {
        using var fixture = new Fixture(type);
        await fixture.Create();
        var transaction = Assert.Single(fixture.Db.PaymentTransactions);
        await fixture.Failure(transaction);
        await fixture.Failure(transaction);
        Assert.Equal(PaymentStatus.Failed, fixture.Booking.PaymentStatus);
        Assert.Equal(BookingStatus.PendingPayment, fixture.Booking.Status);
        Assert.Equal("Card declined", transaction.FailureReason);
        await fixture.Success(transaction);
        await fixture.Failure(transaction);
        Assert.Equal(PaymentStatus.Paid, fixture.Booking.PaymentStatus);
        Assert.Null(transaction.FailureReason);
        fixture.VerifyOneFailure(type);
    }

    [Theory]
    [InlineData("Experience")]
    [InlineData("Accommodation")]
    [InlineData("Restaurant")]
    public async Task Cancel_IsIdempotent_AndNewAttemptCanSucceed(string type)
    {
        using var fixture = new Fixture(type);
        await fixture.Create();
        var first = Assert.Single(fixture.Db.PaymentTransactions);
        Assert.IsType<OkObjectResult>(await fixture.Controller.CancelCheckout(first.Id));
        Assert.IsType<OkObjectResult>(await fixture.Controller.CancelCheckout(first.Id));
        Assert.Equal(PaymentStatus.Failed, first.Status);
        Assert.Equal(PaymentStatus.Failed, fixture.Booking.PaymentStatus);
        Assert.Equal(BookingStatus.PendingPayment, fixture.Booking.Status);
        fixture.VerifyOneFailure(type);
        await fixture.Create();
        var second = fixture.Db.PaymentTransactions.Single(t => t.Id != first.Id);
        await fixture.Success(second);
        Assert.Equal(PaymentStatus.Paid, fixture.Booking.PaymentStatus);
        Assert.Equal(PaymentStatus.Failed, first.Status);
        Assert.IsType<ConflictObjectResult>(await fixture.Controller.CancelCheckout(second.Id));
    }

    [Theory]
    [InlineData("Experience")]
    [InlineData("Accommodation")]
    [InlineData("Restaurant")]
    public async Task Ownership_AndWebhookSignature_AreEnforced(string type)
    {
        using var fixture = new Fixture(type);
        await fixture.Create();
        var transaction = Assert.Single(fixture.Db.PaymentTransactions);
        fixture.Controller.HttpContext.User = Fixture.User(Guid.NewGuid());
        Assert.IsType<NotFoundObjectResult>(await fixture.Controller.GetPaymentDetails(fixture.Booking.Id, type));
        Assert.IsType<NotFoundObjectResult>(await fixture.Controller.CreateCheckoutSession(fixture.Request));
        Assert.IsType<NotFoundObjectResult>(await fixture.Controller.CancelCheckout(transaction.Id));
        fixture.Controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        fixture.Controller.Request.Headers["Stripe-Signature"] = "invalid";
        Assert.IsType<BadRequestObjectResult>(await fixture.Controller.StripeWebhook());
        Assert.Equal(PaymentStatus.Unpaid, transaction.Status);
    }

    [Theory]
    [InlineData("Experience")]
    [InlineData("Accommodation")]
    [InlineData("Restaurant")]
    public async Task CheckoutApiFailure_SavesReasonAndPublishesFailure(string type)
    {
        using var fixture = new Fixture(type);
        fixture.Checkout.Setup(s => s.CreateAsync(It.IsAny<SessionCreateOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StripeException("Sandbox checkout unavailable"));
        Assert.IsType<BadRequestObjectResult>(await fixture.Controller.CreateCheckoutSession(fixture.Request));
        var transaction = Assert.Single(fixture.Db.PaymentTransactions);
        Assert.Equal(PaymentStatus.Failed, transaction.Status);
        Assert.Equal("Sandbox checkout unavailable", transaction.FailureReason);
        Assert.Equal(PaymentStatus.Failed, fixture.Booking.PaymentStatus);
        Assert.Equal(BookingStatus.PendingPayment, fixture.Booking.Status);
        fixture.VerifyOneFailure(type);
    }

    [Theory]
    [InlineData("Experience")]
    [InlineData("Accommodation")]
    [InlineData("Restaurant")]
    public async Task ExpiredCheckout_FailsOnce_AndAllowsRetry(string type)
    {
        using var fixture = new Fixture(type);
        await fixture.Create();
        var transaction = Assert.Single(fixture.Db.PaymentTransactions);
        var session = new Session
        {
            Id = "cs_test_unit", Object = "checkout.session",
            Metadata = new() { ["transactionId"] = transaction.Id.ToString() }
        };
        await fixture.Webhook("checkout.session.expired", session);
        await fixture.Webhook("checkout.session.expired", session);
        Assert.Equal(PaymentStatus.Failed, transaction.Status);
        Assert.Equal(BookingStatus.PendingPayment, fixture.Booking.Status);
        fixture.VerifyOneFailure(type);
        await fixture.Create();
        Assert.Equal(2, fixture.Db.PaymentTransactions.Count());
    }

    [Fact]
    public async Task ExistingExperienceRequest_StillDefaultsToExperience()
    {
        using var fixture = new Fixture("Experience");
        Assert.IsType<OkObjectResult>(await fixture.Controller.CreateCheckoutSession(
            new CreatePaymentRequest { BookingId = fixture.Booking.Id }));
        Assert.Equal("Experience", Assert.Single(fixture.Db.PaymentTransactions).BookingType);
    }

    private sealed class Fixture : IDisposable
    {
        public BookingDbContext Db { get; } = new(new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public Mock<IKafkaProducer> Kafka { get; } = new();
        public PaymentsController Controller { get; }
        public IPayableBooking Booking { get; }
        public SessionCreateOptions? Options { get; private set; }
        public Mock<SessionService> Checkout { get; } = new();
        public CreatePaymentRequest Request => new() { BookingId = Booking.Id, BookingType = Booking.BookingType };

        public Fixture(string type)
        {
            var id = Guid.NewGuid();
            var visitorId = Guid.NewGuid();
            Booking = type switch
            {
                "Experience" => new Booking { Id = id, VisitorId = visitorId, TotalAmount = 1234.56m },
                "Accommodation" => new AccommodationBooking { Id = id, VisitorId = visitorId, TotalPrice = 1234.56m },
                _ => new RestaurantReservation { Id = id, VisitorId = visitorId, TotalPrice = 1234.56m }
            };
            Db.Add(Booking);
            Db.SaveChanges();
            var checkout = Checkout;
            checkout.Setup(s => s.CreateAsync(It.IsAny<SessionCreateOptions>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                .Callback<SessionCreateOptions, RequestOptions, CancellationToken>((o, _, _) => Options = o)
                .ReturnsAsync(new Session { Id = "cs_test_unit", Url = "https://checkout.stripe.com/test" });
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:WebhookSecret"] = Secret
            }).Build();
            Controller = new PaymentsController(Db, Kafka.Object, config, checkout.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = User(visitorId) } }
            };
        }

        public static ClaimsPrincipal User(Guid id) => new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString())
        }, "test"));

        public async Task Create() => Assert.IsType<OkObjectResult>(await Controller.CreateCheckoutSession(Request));

        public Task Success(PaymentTransaction transaction) => Webhook("checkout.session.completed", new Session
        {
            Id = "cs_test_unit", Object = "checkout.session", PaymentStatus = "paid", Currency = "lkr",
            AmountTotal = 123456, Metadata = new() { ["transactionId"] = transaction.Id.ToString() }
        });

        public Task Failure(PaymentTransaction transaction) => Webhook("payment_intent.payment_failed", new PaymentIntent
        {
            Id = "pi_test_unit", Object = "payment_intent", LastPaymentError = new StripeError { Message = "Card declined" },
            Metadata = new() { ["transactionId"] = transaction.Id.ToString() }
        });

        public async Task Webhook(string type, object data)
        {
            var json = JsonConvert.SerializeObject(new
            {
                id = "evt_test_unit", @object = "event", api_version = StripeConfiguration.ApiVersion,
                type, livemode = false, data = new { @object = data }
            });
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret),
                Encoding.UTF8.GetBytes($"{timestamp}.{json}"))).ToLowerInvariant();
            Controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            Controller.Request.Headers["Stripe-Signature"] = $"t={timestamp},v1={signature}";
            Assert.IsType<OkResult>(await Controller.StripeWebhook());
        }

        public void VerifyOneFailure(string type) => Kafka.Verify(k => k.PublishAsync("payment.failed", Booking.Id.ToString(),
            It.Is<PaymentFailedEvent>(e => e.BookingType == type && e.PaymentStatus == "Failed"),
            It.IsAny<CancellationToken>()), Times.Once);

        public void Dispose() => Db.Dispose();
    }
}
