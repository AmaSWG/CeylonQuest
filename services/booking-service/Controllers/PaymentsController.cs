using BookingService.Data;
using BookingService.DTOs;
using BookingService.Events;
using BookingService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Kafka;
using System.Security.Claims;

namespace BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly BookingDbContext _context;
    private readonly IKafkaProducer _kafkaProducer;

    public PaymentsController(
        BookingDbContext context,
        IKafkaProducer kafkaProducer)
    {
        _context = context;
        _kafkaProducer = kafkaProducer;
    }

    // =========================================================
    // GET: /api/payments/booking/{bookingId}
    // Display payment details before payment
    // =========================================================
    [HttpGet("booking/{bookingId:guid}")]
    public async Task<IActionResult> GetPaymentDetails(Guid bookingId)
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

        var booking = await _context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b =>
                b.Id == bookingId &&
                b.VisitorId == visitorId);

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
            listingId = booking.ListingId,
            listingTitle = booking.ListingTitle,
            bookingDate = booking.BookingDate,
            timeSlot = booking.TimeSlot,
            participantCount = booking.ParticipantCount,
            unitPrice = booking.UnitPrice,
            totalAmount = booking.TotalAmount,
            bookingStatus = booking.Status.ToString(),
            paymentStatus = booking.PaymentStatus.ToString(),
            paymentReference = booking.PaymentReference
        });
    }

    // =========================================================
    // POST: /api/payments
    // Process simulated payment
    // =========================================================
    [HttpPost]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] CreatePaymentRequest request)
    {
        // 1. Get logged-in visitor ID
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

        // 2. Validate booking ID
        if (request.BookingId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "A valid booking is required."
            });
        }

        // 3. Find booking belonging to logged-in visitor
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b =>
                b.Id == request.BookingId &&
                b.VisitorId == visitorId);

        if (booking == null)
        {
            return NotFound(new
            {
                message = "Booking not found."
            });
        }

        // 4. Block payment for cancelled booking
        if (booking.Status == BookingStatus.Cancelled)
        {
            return BadRequest(new
            {
                message =
                    "Payment cannot be made for a cancelled booking."
            });
        }

        // 5. Block payment for completed booking
        if (booking.Status == BookingStatus.Completed)
        {
            return BadRequest(new
            {
                message =
                    "Payment cannot be made for a completed booking."
            });
        }

        // 6. Prevent duplicate payment
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

        // Block refunded booking
        if (booking.PaymentStatus == PaymentStatus.Refunded)
        {
            return Conflict(new
            {
                message =
                    "This booking has already been refunded."
            });
        }

        // 7. Generate simulated transaction reference
        var transactionReference =
            $"PAY-{Guid.NewGuid()
                .ToString("N")[..12]
                .ToUpperInvariant()}";

        var now = DateTime.UtcNow;

        // 8. Create payment transaction
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            VisitorId = visitorId,

            // Amount always comes from backend booking data
            Amount = booking.TotalAmount,

            TransactionReference = transactionReference,
            CreatedAt = now,
            ProcessedAt = now
        };

        // =====================================================
        // SUCCESSFUL SIMULATED PAYMENT
        // =====================================================
        if (request.SimulateSuccess)
        {
            // Update payment transaction
            transaction.Status = PaymentStatus.Paid;

            // Update booking
            booking.PaymentStatus = PaymentStatus.Paid;
            booking.Status = BookingStatus.Confirmed;
            booking.PaymentReference = transactionReference;
            booking.UpdatedAt = now;

            // Store transaction
            _context.PaymentTransactions.Add(transaction);

            // Save payment + booking changes
            await _context.SaveChangesAsync();

            // =================================================
            // EVENT 1: PAYMENT COMPLETED
            // =================================================
            var paymentCompletedEvent =
                new PaymentCompletedEvent
                {
                    PaymentId = transaction.Id,
                    BookingId = booking.Id,
                    VisitorId = visitorId,
                    Amount = transaction.Amount,
                    TransactionReference =
                        transaction.TransactionReference,
                    PaymentStatus =
                        booking.PaymentStatus.ToString(),
                    BookingStatus =
                        booking.Status.ToString(),
                    CompletedAt = now
                };

            await _kafkaProducer.PublishAsync(
                "payment.completed",
                booking.Id.ToString(),
                paymentCompletedEvent
            );

            // =================================================
            // EVENT 2: PAYMENT CONFIRMED
            // =================================================
            var paymentConfirmedEvent =
                new PaymentConfirmedEvent
                {
                    PaymentId = transaction.Id,
                    BookingId = booking.Id,
                    VisitorId = visitorId,
                    ListingId = booking.ListingId,
                    ListingTitle = booking.ListingTitle,
                    Amount = transaction.Amount,
                    TransactionReference =
                        transaction.TransactionReference,
                    PaymentStatus =
                        booking.PaymentStatus.ToString(),
                    BookingStatus =
                        booking.Status.ToString(),
                    BookingDate = booking.BookingDate,
                    TimeSlot = booking.TimeSlot,
                    ConfirmedAt = now
                };

            await _kafkaProducer.PublishAsync(
                "payment.confirmed",
                booking.Id.ToString(),
                paymentConfirmedEvent
            );

            // Return successful payment result
            return Ok(new PaymentResponse
            {
                TransactionId = transaction.Id,
                BookingId = booking.Id,
                ListingTitle = booking.ListingTitle,
                Amount = transaction.Amount,
                TransactionReference =
                    transaction.TransactionReference,
                PaymentStatus =
                    booking.PaymentStatus.ToString(),
                BookingStatus =
                    booking.Status.ToString(),
                Message =
                    "Payment completed successfully. Your booking is confirmed.",
                ProcessedAt = transaction.ProcessedAt
            });
        }

        // =====================================================
        // FAILED / BACK / CANCELLED SIMULATED PAYMENT
        // =====================================================

        transaction.Status = PaymentStatus.Failed;

        transaction.FailureReason =
            "The simulated payment was cancelled or unsuccessful.";

        // Payment failed
        booking.PaymentStatus = PaymentStatus.Failed;

        // IMPORTANT:
        // Booking remains unconfirmed
        booking.Status = BookingStatus.PendingPayment;

        booking.PaymentReference = transactionReference;
        booking.UpdatedAt = now;

        // Store failed payment attempt
        _context.PaymentTransactions.Add(transaction);

        // Save failed transaction + booking state
        await _context.SaveChangesAsync();

        // =====================================================
        // EVENT 3: PAYMENT FAILED
        // =====================================================
        var paymentFailedEvent =
            new PaymentFailedEvent
            {
                PaymentId = transaction.Id,
                BookingId = booking.Id,
                VisitorId = visitorId,
                Amount = transaction.Amount,
                TransactionReference =
                    transaction.TransactionReference,
                PaymentStatus =
                    booking.PaymentStatus.ToString(),
                FailureReason =
                    transaction.FailureReason ??
                    "Payment failed.",
                FailedAt = now
            };

        await _kafkaProducer.PublishAsync(
            "payment.failed",
            booking.Id.ToString(),
            paymentFailedEvent
        );

        // Return failed payment result
        return Ok(new PaymentResponse
        {
            TransactionId = transaction.Id,
            BookingId = booking.Id,
            ListingTitle = booking.ListingTitle,
            Amount = transaction.Amount,
            TransactionReference =
                transaction.TransactionReference,
            PaymentStatus =
                booking.PaymentStatus.ToString(),
            BookingStatus =
                booking.Status.ToString(),
            Message =
                "Payment was unsuccessful. Your booking has not been confirmed.",
            ProcessedAt = transaction.ProcessedAt
        });
    }
}