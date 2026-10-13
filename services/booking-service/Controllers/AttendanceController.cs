using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using BookingService.Data;
using BookingService.Events;
using BookingService.Models;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace BookingService.Controllers;

[ApiController]
public class AttendanceController(BookingDbContext db, ICatalogService catalog, TimeProvider clock) : ControllerBase
{
    public record AttendanceRequest([Required, RegularExpression("^(Attended|NoShow)$")] string Status);
    public record DisputeRequest([Required, StringLength(2000, MinimumLength = 1)] string Reason);
    public record ResolutionRequest([Required, RegularExpression("^(Attended|NoShow)$")] string Status,
        [Required, StringLength(2000, MinimumLength = 1)] string Resolution);
    public record ResponseRequest([Required, StringLength(2000, MinimumLength = 1)] string Comment);

    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
    private string AccessToken => Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

    private async Task<IPayableBooking?> Booking(string type, Guid id, CancellationToken ct) => type switch
    {
        "Experience" => await db.Bookings.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, ct),
        "Restaurant" => await db.RestaurantReservations.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, ct),
        "Accommodation" => await db.AccommodationBookings.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, ct),
        _ => throw new ReviewException(400, "Unsupported booking type.")
    };

    private async Task<bool> Owns(string type, Guid listing)
    {
        var owned = await catalog.GetReportOwnedListingIdsAsync(AccessToken);
        return type switch
        {
            "Experience" => owned.ActivityIds.Contains(listing),
            "Restaurant" => owned.RestaurantIds.Contains(listing),
            "Accommodation" => owned.AccommodationIds.Contains(listing),
            _ => false
        };
    }

    private void Invite(IPayableBooking booking, DateTime now)
    {
        if (ReviewService.IneligibilityReason(booking, now, false) != null) return;
        var id = Guid.NewGuid();
        db.ReviewOutboxMessages.Add(new() { Id = id, Topic = "review.eligibility-granted",
            Payload = JsonSerializer.Serialize(new ReviewEligibilityGrantedEvent(id, booking.Id, booking.BookingType,
                booking.ListingId, booking.VisitorId, booking.ListingTitle, now)) });
    }

    private async Task<IActionResult> Save(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); return Ok(new { message = "Saved successfully." }); }
        catch (DbUpdateConcurrencyException) { return Problem(statusCode: 409, detail: "This record changed. Refresh and retry."); }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
        { return Problem(statusCode: 409, detail: "This request has already been recorded."); }
    }

    /// <summary>Verify attendance for an owned booking after its scheduled end.</summary>
    [HttpPatch("api/provider-bookings/{bookingType}/{bookingId:guid}/attendance")]
    [Authorize(Roles = "Provider")]
    public async Task<IActionResult> Verify(string bookingType, Guid bookingId, AttendanceRequest request, CancellationToken ct)
    {
        if (UserId == Guid.Empty) return Unauthorized();
        try
        {
            var booking = await Booking(bookingType, bookingId, ct);
            if (booking == null) return NotFound();
            if (!await Owns(bookingType, booking.ListingId)) return Forbid();
            var now = clock.GetUtcNow().UtcDateTime;
            AttendanceRules.Verify(booking, Enum.Parse<AttendanceStatus>(request.Status), UserId, now);
            if (!await db.ListingReviews.AnyAsync(r => r.BookingId == bookingId && r.BookingType == bookingType, ct)) Invite(booking, now);
            return await Save(ct);
        }
        catch (ReviewException ex) { return Problem(statusCode: ex.StatusCode, detail: ex.Message); }
        catch (HttpRequestException) { return Problem(statusCode: 503, detail: "Listing ownership could not be verified. Please retry."); }
    }

    /// <summary>Report disputed attendance for the authenticated visitor's booking.</summary>
    [HttpPost("api/user-bookings/{bookingType}/{bookingId:guid}/attendance-dispute")]
    [Authorize(Roles = "Visitor")]
    public async Task<IActionResult> Dispute(string bookingType, Guid bookingId, DisputeRequest request, CancellationToken ct)
    {
        if (UserId == Guid.Empty) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest();
        try
        {
            var booking = await Booking(bookingType, bookingId, ct);
            if (booking == null) return NotFound();
            if (booking.VisitorId != UserId) return Forbid();
            var reason = AttendanceRules.InvalidCompletion(booking, clock.GetUtcNow().UtcDateTime);
            if (reason != null) return Problem(statusCode: 409, detail: reason);
            if (await db.AttendanceDisputes.AnyAsync(d => d.BookingType == bookingType && d.BookingId == bookingId, ct))
                return Problem(statusCode: 409, detail: "An attendance dispute already exists for this booking.");
            db.AttendanceDisputes.Add(new() { BookingId = bookingId, BookingType = bookingType, VisitorId = UserId,
                OriginalAttendanceStatus = booking.AttendanceStatus, OriginalVerifiedByUserId = booking.AttendanceVerifiedByUserId,
                OriginalVerifiedAtUtc = booking.AttendanceVerifiedAtUtc, Reason = request.Reason.Trim(), CreatedAtUtc = clock.GetUtcNow().UtcDateTime });
            return await Save(ct);
        }
        catch (ReviewException ex) { return Problem(statusCode: ex.StatusCode, detail: ex.Message); }
    }

    [HttpGet("api/admin-bookings/attendance-disputes")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Disputes(CancellationToken ct, [FromServices] IIdentityService identity)
    {
        var disputes = await db.AttendanceDisputes.AsNoTracking().OrderByDescending(d => d.CreatedAtUtc).ToListAsync(ct);
        var names = new Dictionary<Guid, string>();
        var results = new List<object>();
        foreach (var dispute in disputes)
        {
            ct.ThrowIfCancellationRequested();
            if (!names.ContainsKey(dispute.VisitorId))
            {
                try
                {
                    var visitor = await identity.GetBookingCustomerAsync(dispute.VisitorId, AccessToken);
                    names[dispute.VisitorId] = string.IsNullOrWhiteSpace(visitor?.FullName) ? "Visitor unavailable" : visitor.FullName;
                }
                catch (HttpRequestException) { names[dispute.VisitorId] = "Visitor unavailable"; }
            }
            IPayableBooking? booking = dispute.BookingType switch
            {
                "Experience" => await db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == dispute.BookingId, ct),
                "Restaurant" => await db.RestaurantReservations.AsNoTracking().FirstOrDefaultAsync(b => b.Id == dispute.BookingId, ct),
                "Accommodation" => await db.AccommodationBookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == dispute.BookingId, ct),
                _ => null
            };
            results.Add(new {
                dispute.Id, dispute.BookingId, dispute.BookingType, dispute.VisitorId,
                ServiceName = booking?.ListingTitle ?? "Service unavailable",
                VisitorName = names[dispute.VisitorId],
                dispute.OriginalAttendanceStatus, dispute.OriginalVerifiedByUserId,
                dispute.OriginalVerifiedAtUtc, dispute.Reason, dispute.CreatedAtUtc,
                dispute.Resolution, dispute.ResolvedAtUtc, dispute.ResolvedByUserId,
                dispute.ResolvedAttendanceStatus
            });
        }
        return Ok(results);
    }

    [HttpPatch("api/admin-bookings/attendance-disputes/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Resolve(Guid id, ResolutionRequest request, CancellationToken ct)
    {
        if (UserId == Guid.Empty) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Resolution)) return BadRequest();
        var dispute = await db.AttendanceDisputes.FindAsync(new object[] { id }, ct);
        if (dispute == null) return NotFound();
        if (dispute.ResolvedAtUtc != null) return Problem(statusCode: 409, detail: "This dispute is already resolved.");
        var booking = await Booking(dispute.BookingType, dispute.BookingId, ct);
        if (booking == null) return NotFound();
        var now = clock.GetUtcNow().UtcDateTime;
        var reason = AttendanceRules.InvalidCompletion(booking, now);
        if (reason != null) return Problem(statusCode: 409, detail: reason);
        var status = Enum.Parse<AttendanceStatus>(request.Status);
        if (status == AttendanceStatus.NoShow && await db.ListingReviews.AnyAsync(r => r.BookingId == booking.Id && r.BookingType == dispute.BookingType, ct))
            return Problem(statusCode: 409, detail: "An existing reviewed attendance record cannot be invalidated through this endpoint.");
        var changed = booking.AttendanceStatus != status;
        booking.AttendanceStatus = status; booking.Status = BookingStatus.Completed;
        booking.AttendanceVerifiedByUserId = UserId; booking.AttendanceVerifiedAtUtc = now; booking.UpdatedAt = now;
        dispute.Resolution = request.Resolution.Trim(); dispute.ResolvedByUserId = UserId;
        dispute.ResolvedAtUtc = now; dispute.ResolvedAttendanceStatus = status;
        if (changed && !await db.ListingReviews.AnyAsync(r => r.BookingId == booking.Id && r.BookingType == dispute.BookingType, ct)) Invite(booking, now);
        return await Save(ct);
    }

    /// <summary>Write a provider response without modifying the visitor's review.</summary>
    [HttpPut("api/bookings/reviews/{reviewId:guid}/response")]
    [Authorize(Roles = "Provider")]
    public async Task<IActionResult> Respond(Guid reviewId, ResponseRequest request, CancellationToken ct)
    {
        if (UserId == Guid.Empty) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Comment)) return BadRequest();
        var review = await db.ListingReviews.FindAsync(new object[] { reviewId }, ct);
        if (review == null) return NotFound();
        try
        {
            if (!await Owns(review.BookingType, review.ListingId)) return Forbid();
            review.ProviderResponse = request.Comment.Trim(); review.ProviderRespondedByUserId = UserId;
            review.ProviderRespondedAtUtc = clock.GetUtcNow().UtcDateTime;
            return await Save(ct);
        }
        catch (HttpRequestException) { return Problem(statusCode: 503, detail: "Listing ownership could not be verified. Please retry."); }
    }
}
