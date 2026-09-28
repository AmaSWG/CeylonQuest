using BookingService.Data;
using BookingService.DTOs;
using BookingService.Events;
using BookingService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Kafka;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly BookingDbContext _context;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IConfiguration _configuration;
    private readonly SessionService _checkoutService;

    public PaymentsController(
        BookingDbContext context,
        IKafkaProducer kafkaProducer,
        IConfiguration configuration,
        SessionService? checkoutService = null)
    {
        _context = context;
        _kafkaProducer = kafkaProducer;
        _configuration = configuration;
        _checkoutService = checkoutService ?? new SessionService();
    }


    // =========================================================
    // GET: /api/Payments/booking/{bookingId}
    // =========================================================

    [HttpGet("booking/{bookingId:guid}")]
    public async Task<IActionResult> GetPaymentDetails(Guid bookingId, [FromQuery] string bookingType = "Experience")
    {
        var visitorIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(visitorIdValue) ||
            !Guid.TryParse(visitorIdValue, out var visitorId))
        {
            return Unauthorized(new
            {
                message = "Invalid visitor authentication."
            });
        }

        if (!IsSupportedType(bookingType)) return BadRequest(new { message = "Invalid booking type." });
        var booking = await FindBooking(bookingId, bookingType, visitorId);

        if (booking == null)
        {
            return NotFound(new
            {
                message = "Booking not found."
            });
        }

        return Ok(new
        {
            bookingId = booking.Id,
            bookingType = booking.BookingType,
            listingId = booking.ListingId,
            listingTitle = booking.ListingTitle,
            bookingDate = booking.BookingDate,
            timeSlot = booking.TimeSlot,
            participantCount = booking.ParticipantCount,
            unitPrice = booking.UnitPrice,
            totalAmount = booking.TotalAmount,
            currency = "LKR",
            bookingStatus = booking.Status.ToString(),
            paymentStatus = booking.PaymentStatus.ToString(),
            paymentReference = booking.PaymentReference
        });
    }


    // =========================================================
    // POST: /api/Payments/create-checkout-session
    // =========================================================

    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession(
        [FromBody] CreatePaymentRequest request)
    {
        // Get logged-in visitor
        var visitorIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(visitorIdValue) ||
            !Guid.TryParse(visitorIdValue, out var visitorId))
        {
            return Unauthorized(new
            {
                message = "Invalid visitor authentication."
            });
        }


        // Validate booking ID
        if (request.BookingId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "A valid booking is required."
            });
        }


        // Find visitor's booking
        if (!IsSupportedType(request.BookingType)) return BadRequest(new { message = "Invalid booking type." });
        var booking = await FindBooking(request.BookingId, request.BookingType, visitorId);

        if (booking == null)
        {
            return NotFound(new
            {
                message = "Booking not found."
            });
        }


        // Cancelled booking cannot be paid
        if (booking.Status == BookingStatus.Cancelled)
        {
            return BadRequest(new
            {
                message =
                    "Payment cannot be made for a cancelled booking."
            });
        }


        // Completed booking cannot be paid
        if (booking.Status == BookingStatus.Completed)
        {
            return BadRequest(new
            {
                message =
                    "Payment cannot be made for a completed booking."
            });
        }


        // Prevent duplicate successful payment
        if (booking.PaymentStatus == PaymentStatus.Paid)
        {
            return Conflict(new
            {
                message =
                    "Payment has already been completed for this booking.",

                bookingId = booking.Id,
                paymentReference = booking.PaymentReference
            });
        }


        // Refunded booking cannot be paid again
        if (booking.PaymentStatus == PaymentStatus.Refunded)
        {
            return Conflict(new
            {
                message =
                    "This booking has already been refunded."
            });
        }


        // Validate backend-calculated amount
        if (booking.TotalAmount <= 0)
        {
            return BadRequest(new
            {
                message =
                    "Booking amount must be greater than zero."
            });
        }


        // =====================================================
        // Create local payment transaction
        // =====================================================

        var transactionId = Guid.NewGuid();

        var transactionReference =
            $"PAY-{Guid.NewGuid()
                .ToString("N")[..12]
                .ToUpperInvariant()}";

        var now = DateTime.UtcNow;

        var transaction = new PaymentTransaction
        {
            Id = transactionId,
            BookingId = booking.Id,
            BookingType = booking.BookingType,
            VisitorId = visitorId,

            // Never accept payment amount from frontend
            Amount = booking.TotalAmount,

            TransactionReference = transactionReference,
            Status = PaymentStatus.Unpaid,
            CreatedAt = now,
            ProcessedAt = null
        };

        _context.PaymentTransactions.Add(transaction);

        booking.PaymentStatus =
            PaymentStatus.Unpaid;

        booking.Status =
            BookingStatus.PendingPayment;

        booking.PaymentReference =
            transactionReference;

        booking.UpdatedAt =
            now;

        await _context.SaveChangesAsync();


        // =====================================================
        // Convert LKR amount to Stripe minor units
        //
        // LKR 6000.00 -> 600000
        // =====================================================

        var stripeAmount =
            checked((long)Math.Round(
                booking.TotalAmount * 100m,
                0,
                MidpointRounding.AwayFromZero));


        // =====================================================
        // Frontend URLs
        // =====================================================

        const string frontendBaseUrl =
            "http://localhost:5173";

        var successUrl =
            $"{frontendBaseUrl}/payment/success" +
            "?session_id={CHECKOUT_SESSION_ID}";


        // If customer returns/cancels, frontend receives
        // the exact local transaction ID.
        var cancelUrl =
            $"{frontendBaseUrl}/payment/cancel" +
            $"?bookingId={booking.Id}" +
            $"&transactionId={transaction.Id}";


        // =====================================================
        // Create Stripe Checkout Session
        // =====================================================

        try
        {
            var options = new SessionCreateOptions
            {
                Mode = "payment",

                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,

                ClientReferenceId =
                    booking.Id.ToString(),

                Metadata =
                    new Dictionary<string, string>
                    {
                        ["bookingId"] =
                            booking.Id.ToString(),

                        ["bookingType"] = booking.BookingType,

                        ["visitorId"] =
                            visitorId.ToString(),

                        ["transactionId"] =
                            transaction.Id.ToString(),

                        ["transactionReference"] =
                            transaction.TransactionReference
                    },

                PaymentIntentData =
                    new SessionPaymentIntentDataOptions
                    {
                        Metadata =
                            new Dictionary<string, string>
                            {
                                ["bookingId"] =
                                    booking.Id.ToString(),

                                ["bookingType"] = booking.BookingType,

                                ["visitorId"] =
                                    visitorId.ToString(),

                                ["transactionId"] =
                                    transaction.Id.ToString(),

                                ["transactionReference"] =
                                    transaction.TransactionReference
                            }
                    },

                LineItems =
                    new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Quantity = 1,

                            PriceData =
                                new SessionLineItemPriceDataOptions
                                {
                                    Currency = "lkr",

                                    UnitAmount =
                                        stripeAmount,

                                    ProductData =
                                        new SessionLineItemPriceDataProductDataOptions
                                        {
                                            Name =
                                                booking.ListingTitle,

                                            Description =
                                                $"CeylonQuest booking - " +
                                                $"{booking.BookingDate} " +
                                                $"{booking.TimeSlot}"
                                        }
                                }
                        }
                    }
            };

            var session =
                await _checkoutService.CreateAsync(options);

            return Ok(new
            {
                message =
                    "Stripe Checkout Session created successfully.",

                bookingId =
                    booking.Id,

                transactionId =
                    transaction.Id,

                transactionReference =
                    transaction.TransactionReference,

                amount =
                    booking.TotalAmount,

                currency =
                    "LKR",

                checkoutSessionId =
                    session.Id,

                checkoutUrl =
                    session.Url
            });
        }
        catch (StripeException ex)
        {
            // Stripe failed before checkout could start
            transaction.Status =
                PaymentStatus.Failed;

            transaction.FailureReason =
                ex.StripeError?.Message
                ?? ex.Message;

            transaction.ProcessedAt =
                DateTime.UtcNow;

            booking.PaymentStatus =
                PaymentStatus.Failed;

            booking.Status =
                BookingStatus.PendingPayment;

            booking.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await PublishPaymentFailedEvent(
                transaction,
                booking,
                transaction.FailureReason
                    ?? "Stripe Checkout Session could not be created."
            );

            return BadRequest(new
            {
                message =
                    "Unable to create Stripe Checkout Session.",

                error =
                    ex.StripeError?.Message
                    ?? ex.Message
            });
        }
    }


    // =========================================================
    // POST: /api/Payments/cancel-checkout/{transactionId}
    //
    // Called when user returns from Stripe without paying.
    // =========================================================

    [HttpPost("cancel-checkout/{transactionId:guid}")]
    public async Task<IActionResult> CancelCheckout(
        Guid transactionId)
    {
        // Get logged-in visitor
        var visitorIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(visitorIdValue) ||
            !Guid.TryParse(visitorIdValue, out var visitorId))
        {
            return Unauthorized(new
            {
                message =
                    "Invalid visitor authentication."
            });
        }


        // Find exact payment attempt belonging to visitor
        var transaction =
            await _context.PaymentTransactions
                .FirstOrDefaultAsync(p =>
                    p.Id == transactionId &&
                    p.VisitorId == visitorId);

        if (transaction == null)
        {
            return NotFound(new
            {
                message =
                    "Payment transaction not found."
            });
        }


        // Find associated booking
        var booking = await FindBooking(transaction.BookingId, transaction.BookingType, visitorId);

        if (booking == null)
        {
            return NotFound(new
            {
                message =
                    "Booking not found."
            });
        }


        // Never overwrite a successful payment
        if (transaction.Status == PaymentStatus.Paid ||
            booking.PaymentStatus == PaymentStatus.Paid)
        {
            return Conflict(new
            {
                message =
                    "Payment has already been completed."
            });
        }


        // Idempotency:
        // cancellation may be called more than once.
        if (transaction.Status == PaymentStatus.Failed)
        {
            return Ok(new
            {
                message =
                    "Payment attempt is already marked as failed.",

                bookingId =
                    booking.Id,

                transactionId =
                    transaction.Id,

                paymentStatus =
                    booking.PaymentStatus.ToString(),

                bookingStatus =
                    booking.Status.ToString()
            });
        }


        var now =
            DateTime.UtcNow;


        // Mark payment attempt as failed
        transaction.Status =
            PaymentStatus.Failed;

        transaction.FailureReason =
            "Payment cancelled by user.";

        transaction.ProcessedAt =
            now;


        // Booking stays unconfirmed
        booking.PaymentStatus =
            PaymentStatus.Failed;

        booking.Status =
            BookingStatus.PendingPayment;

        booking.UpdatedAt =
            now;

        await _context.SaveChangesAsync();


        // =====================================================
        // EVENT 3: PAYMENT FAILED
        // =====================================================

        await PublishPaymentFailedEvent(
            transaction,
            booking,
            transaction.FailureReason
        );


        return Ok(new
        {
            message =
                "Payment was cancelled. Booking remains pending payment.",

            bookingId =
                booking.Id,

            transactionId =
                transaction.Id,

            paymentStatus =
                booking.PaymentStatus.ToString(),

            bookingStatus =
                booking.Status.ToString()
        });
    }


    // =========================================================
    // POST: /api/Payments/webhook
    // Stripe webhook endpoint
    // =========================================================

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        var webhookSecret =
            _configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "Stripe webhook secret is not configured."
                });
        }


        string json;

        using (var reader =
               new StreamReader(HttpContext.Request.Body))
        {
            json =
                await reader.ReadToEndAsync();
        }


        Event stripeEvent;

        try
        {
            stripeEvent =
                EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    webhookSecret
                );
        }
        catch (StripeException)
        {
            return BadRequest(new
            {
                message =
                    "Invalid Stripe webhook signature."
            });
        }

        if (stripeEvent.Livemode)
        {
            return BadRequest(new { message = "Only Stripe Sandbox events are supported." });
        }

        // =====================================================
        // Stripe event:
        // checkout.session.completed
        // =====================================================

        if (stripeEvent.Type ==
            "checkout.session.completed")
        {
            var session =
                stripeEvent.Data.Object
                as Session;

            if (session == null)
            {
                return Ok();
            }


            // Confirm only when Stripe says it was paid
            if (!string.Equals(
                    session.PaymentStatus,
                    "paid",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Ok();
            }


            await HandleSuccessfulPayment(
                session);

            return Ok();
        }


        // =====================================================
        // Stripe event:
        // checkout.session.expired
        // =====================================================

        if (stripeEvent.Type ==
            "checkout.session.expired")
        {
            var session =
                stripeEvent.Data.Object
                as Session;

            if (session == null)
            {
                return Ok();
            }

            await HandleExpiredCheckout(
                session);

            return Ok();
        }


        // =====================================================
        // Stripe event:
        // payment_intent.payment_failed
        // =====================================================

        if (stripeEvent.Type ==
            "payment_intent.payment_failed")
        {
            var paymentIntent =
                stripeEvent.Data.Object
                as PaymentIntent;

            if (paymentIntent == null)
            {
                return Ok();
            }

            await HandleFailedPaymentIntent(
                paymentIntent);

            return Ok();
        }


        // Ignore other Stripe events
        return Ok();
    }


    // =========================================================
    // Handle successful Stripe Checkout
    // =========================================================

    private async Task HandleSuccessfulPayment(
        Session session)
    {
        if (!TryGetGuid(
                session.Metadata,
                "transactionId",
                out var transactionId))
        {
            return;
        }


        var transaction =
            await _context.PaymentTransactions
                .FirstOrDefaultAsync(p =>
                    p.Id == transactionId);

        if (transaction == null)
        {
            return;
        }


        var booking = await FindBooking(transaction.BookingId, transaction.BookingType, transaction.VisitorId);

        if (booking == null)
        {
            return;
        }


        // Webhooks can be delivered multiple times.
        if (transaction.Status == PaymentStatus.Paid ||
            booking.PaymentStatus == PaymentStatus.Paid)
        {
            return;
        }


        // Ensure transaction belongs to booking owner
        if (booking.VisitorId !=
            transaction.VisitorId)
        {
            return;
        }


        // Validate amount returned by Stripe
        var expectedAmount =
            checked((long)Math.Round(
                transaction.Amount * 100m,
                0,
                MidpointRounding.AwayFromZero));


        if (session.AmountTotal != expectedAmount ||
            !string.Equals(session.Currency, "lkr", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }


        var now =
            DateTime.UtcNow;


        // Update transaction
        transaction.Status =
            PaymentStatus.Paid;

        transaction.FailureReason =
            null;

        transaction.ProcessedAt =
            now;


        // Update booking
        booking.PaymentStatus =
            PaymentStatus.Paid;

        booking.Status =
            BookingStatus.Confirmed;

        booking.PaymentReference =
            session.Id;

        booking.UpdatedAt =
            now;


        await _context.SaveChangesAsync();


        // =====================================================
        // EVENT 1: PAYMENT COMPLETED
        // =====================================================

        var paymentCompletedEvent =
            new PaymentCompletedEvent
            {
                BookingType = booking.BookingType,
                PaymentId =
                    transaction.Id,

                BookingId =
                    booking.Id,

                VisitorId =
                    booking.VisitorId,

                Amount =
                    transaction.Amount,

                TransactionReference =
                    transaction.TransactionReference,

                PaymentStatus =
                    booking.PaymentStatus.ToString(),

                BookingStatus =
                    booking.Status.ToString(),

                CompletedAt =
                    now
            };


        await _kafkaProducer.PublishAsync(
            "payment.completed",
            booking.Id.ToString(),
            paymentCompletedEvent
        );


        // =====================================================
        // EVENT 2: PAYMENT CONFIRMED
        // =====================================================

        var paymentConfirmedEvent =
            new PaymentConfirmedEvent
            {
                BookingType = booking.BookingType,
                PaymentId =
                    transaction.Id,

                BookingId =
                    booking.Id,

                VisitorId =
                    booking.VisitorId,

                ListingId =
                    booking.ListingId,

                ListingTitle =
                    booking.ListingTitle,

                Amount =
                    transaction.Amount,

                TransactionReference =
                    transaction.TransactionReference,

                PaymentStatus =
                    booking.PaymentStatus.ToString(),

                BookingStatus =
                    booking.Status.ToString(),

                BookingDate =
                    booking.BookingDate,

                TimeSlot =
                    booking.TimeSlot,

                ConfirmedAt =
                    now
            };


        await _kafkaProducer.PublishAsync(
            "payment.confirmed",
            booking.Id.ToString(),
            paymentConfirmedEvent
        );
    }


    // =========================================================
    // Handle expired Stripe Checkout Session
    // =========================================================

    private async Task HandleExpiredCheckout(
        Session session)
    {
        if (!TryGetGuid(
                session.Metadata,
                "transactionId",
                out var transactionId))
        {
            return;
        }


        var transaction =
            await _context.PaymentTransactions
                .FirstOrDefaultAsync(p =>
                    p.Id == transactionId);

        if (transaction == null)
        {
            return;
        }


        // Never overwrite successful payment
        if (transaction.Status ==
            PaymentStatus.Paid)
        {
            return;
        }


        // Already failed/cancelled
        if (transaction.Status ==
            PaymentStatus.Failed)
        {
            return;
        }


        var booking = await FindBooking(transaction.BookingId, transaction.BookingType, transaction.VisitorId);

        if (booking == null)
        {
            return;
        }


        // Never overwrite paid booking
        if (booking.PaymentStatus ==
            PaymentStatus.Paid)
        {
            return;
        }


        var now =
            DateTime.UtcNow;


        transaction.Status =
            PaymentStatus.Failed;

        transaction.FailureReason =
            "Stripe Checkout Session expired before payment was completed.";

        transaction.ProcessedAt =
            now;


        booking.PaymentStatus =
            PaymentStatus.Failed;

        booking.Status =
            BookingStatus.PendingPayment;

        booking.UpdatedAt =
            now;


        await _context.SaveChangesAsync();


        // EVENT 3: PAYMENT FAILED
        await PublishPaymentFailedEvent(
            transaction,
            booking,
            transaction.FailureReason
        );
    }


    // =========================================================
    // Handle Stripe payment failure
    // =========================================================

    private async Task HandleFailedPaymentIntent(
        PaymentIntent paymentIntent)
    {
        if (!TryGetGuid(
                paymentIntent.Metadata,
                "transactionId",
                out var transactionId))
        {
            return;
        }


        var transaction =
            await _context.PaymentTransactions
                .FirstOrDefaultAsync(p =>
                    p.Id == transactionId);

        if (transaction == null)
        {
            return;
        }


        // Never overwrite successful payment
        if (transaction.Status ==
            PaymentStatus.Paid)
        {
            return;
        }


        // Failure may be delivered more than once
        if (transaction.Status ==
            PaymentStatus.Failed)
        {
            return;
        }


        var booking = await FindBooking(transaction.BookingId, transaction.BookingType, transaction.VisitorId);

        if (booking == null)
        {
            return;
        }


        // Never overwrite paid booking
        if (booking.PaymentStatus ==
            PaymentStatus.Paid)
        {
            return;
        }


        var now =
            DateTime.UtcNow;


        var failureReason =
            paymentIntent.LastPaymentError?.Message
            ?? "Stripe payment was unsuccessful.";


        transaction.Status =
            PaymentStatus.Failed;

        transaction.FailureReason =
            failureReason;

        transaction.ProcessedAt =
            now;


        booking.PaymentStatus =
            PaymentStatus.Failed;

        booking.Status =
            BookingStatus.PendingPayment;

        booking.UpdatedAt =
            now;


        await _context.SaveChangesAsync();


        // =====================================================
        // EVENT 3: PAYMENT FAILED
        // =====================================================

        await PublishPaymentFailedEvent(
            transaction,
            booking,
            failureReason
        );
    }


    // =========================================================
    // Publish payment.failed Kafka event
    // =========================================================

    private async Task PublishPaymentFailedEvent(
        PaymentTransaction transaction,
        IPayableBooking booking,
        string failureReason)
    {
        var paymentFailedEvent =
            new PaymentFailedEvent
            {
                BookingType = booking.BookingType,
                PaymentId =
                    transaction.Id,

                BookingId =
                    booking.Id,

                VisitorId =
                    booking.VisitorId,

                Amount =
                    transaction.Amount,

                TransactionReference =
                    transaction.TransactionReference,

                PaymentStatus =
                    booking.PaymentStatus.ToString(),

                FailureReason =
                    failureReason,

                FailedAt =
                    transaction.ProcessedAt
                    ?? DateTime.UtcNow
            };


        await _kafkaProducer.PublishAsync(
            "payment.failed",
            booking.Id.ToString(),
            paymentFailedEvent
        );
    }


    // =========================================================
    // Metadata GUID helper
    // =========================================================

    private static bool IsSupportedType(string? bookingType) =>
        bookingType is "Experience" or "Accommodation" or "Restaurant";

    private async Task<IPayableBooking?> FindBooking(Guid id, string bookingType, Guid visitorId)
    {
        return bookingType switch
        {
            "Experience" => await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.VisitorId == visitorId),
            "Accommodation" => await _context.AccommodationBookings.FirstOrDefaultAsync(b => b.Id == id && b.VisitorId == visitorId),
            "Restaurant" => await _context.RestaurantReservations.FirstOrDefaultAsync(b => b.Id == id && b.VisitorId == visitorId),
            _ => null
        };
    }

    private static bool TryGetGuid(
        IDictionary<string, string>? metadata,
        string key,
        out Guid value)
    {
        value =
            Guid.Empty;

        if (metadata == null)
        {
            return false;
        }


        if (!metadata.TryGetValue(
                key,
                out var text))
        {
            return false;
        }


        return Guid.TryParse(
            text,
            out value);
    }
}
