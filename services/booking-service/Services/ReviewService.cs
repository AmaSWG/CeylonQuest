using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Events;
using BookingService.Models;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace BookingService.Services;

public class ReviewService(
    BookingDbContext db,
    ICatalogService catalog,
    TimeProvider clock,
    IHttpContextAccessor? httpContext = null,
    ReviewerProfileClient? profiles = null) : IReviewService

{
    public async Task<ReviewResponse> CreateAsync(Guid visitorId, CreateReviewRequest request, CancellationToken token = default)
    {
        var errors = new List<ValidationResult>();
        if (visitorId == Guid.Empty || !Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
            throw new ReviewException(400, "A booking, valid booking type, 1–5 rating and written review (up to 2000 characters) are required.");

        // Keep eligibility stable until commit. Unique constraints handle racing duplicates.
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token) : null;
        var booking = request.BookingType switch
        {
            "Experience" => (IPayableBooking?)await db.Bookings.FirstOrDefaultAsync(b => b.Id == request.BookingId && !b.IsDeleted, token),
            "Restaurant" => await db.RestaurantReservations.FirstOrDefaultAsync(b => b.Id == request.BookingId && !b.IsDeleted, token),
            "Accommodation" => await db.AccommodationBookings.FirstOrDefaultAsync(b => b.Id == request.BookingId && !b.IsDeleted, token),
            _ => null
        };
        if (booking == null) throw new ReviewException(404, "Booking not found.");
        if (booking.VisitorId != visitorId) throw new ReviewException(403, "This booking belongs to another visitor.");
        var now = clock.GetUtcNow().UtcDateTime;
        var alreadyReviewed = await db.ListingReviews.AnyAsync(
            r => r.BookingType == request.BookingType && r.BookingId == booking.Id, token);
        var reason = IneligibilityReason(booking, now, alreadyReviewed);
        if (reason != null) throw new ReviewException(409, reason);
        Guid? providerId = booking switch
        {
            Booking b => b.ProviderId,
            RestaurantReservation r => r.ProviderId,
            AccommodationBooking a => a.ProviderId,
            _ => null
        };

        // Legacy bookings may not yet have a provider snapshot. Never trust a client-supplied provider.
        if (providerId == null || providerId == Guid.Empty)
        {
            try
            {
                providerId = request.BookingType switch
                {
                    "Experience" => (await catalog.GetListingAsync(booking.ListingId))?.ProviderId,
                    "Restaurant" => (await catalog.GetRestaurantAsync(booking.ListingId))?.ProviderId,
                    "Accommodation" => (await catalog.GetAccommodationAsync(booking.ListingId))?.ProviderId,
                    _ => null
                };
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
            {
                token.ThrowIfCancellationRequested();
                throw new ReviewException(503, "The provider could not be resolved. Please try again later.");
            }
        }
        if (providerId == null || providerId == Guid.Empty)
            throw new ReviewException(503, "The provider could not be resolved. Please try again later.");

        var reviewerDisplayName = "Visitor";

        if (profiles != null)
        {
            var authorization = httpContext?.HttpContext?
                .Request.Headers.Authorization.ToString() ?? string.Empty;

            reviewerDisplayName = await profiles.GetDisplayNameAsync(
                authorization, token);
        }

        var review = new ListingReview
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            BookingType = request.BookingType,
            VisitorId = visitorId,
            ListingId = booking.ListingId,
            ProviderId = providerId.Value,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            ReviewerDisplayName = reviewerDisplayName,
            CreatedAtUtc = now

        };
        var eventId = Guid.NewGuid();
        var evt = new ReviewSubmittedEvent(eventId, 1, review.Id, booking.Id, request.BookingType,
            booking.ListingId, booking.ListingTitle, visitorId, providerId.Value, review.Rating, now);
        db.ListingReviews.Add(review);
        db.ReviewOutboxMessages.Add(new ReviewOutboxMessage { Id = eventId, Payload = JsonSerializer.Serialize(evt) });
        try
        {
            await db.SaveChangesAsync(token);
            if (transaction != null) await transaction.CommitAsync(token);
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
        {
            throw new ReviewException(409, "This booking has already been reviewed.");
        }
        catch (Exception ex) when (ex is MySqlException { Number: 1213 or 1205 }
            || ex is DbUpdateException { InnerException: MySqlException { Number: 1213 or 1205 } })
        {
            throw new ReviewException(409, "The booking is being updated. Please retry your request.");
        }
        return ToResponse(review);
    }

    public async Task<PagedReviewsResponse> GetAsync(Guid listingId, ReviewQuery query, CancellationToken token = default)
    {
        Validator.ValidateObject(query, new ValidationContext(query), true);
        var reviews = ForListing(listingId, query.BookingType);
        if (query.Rating.HasValue) reviews = reviews.Where(r => r.Rating == query.Rating.Value);
        var total = await reviews.CountAsync(token);
        var rows = await reviews.OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(token);
        var summary = await GetSummaryAsync(listingId, query.BookingType, token);
        return new(rows.Select(ToResponse).ToList(), query.Page, query.PageSize, total,
            (int)Math.Ceiling(total / (double)query.PageSize), summary.AverageRating, summary.ReviewCount);
    }

    public async Task<ReviewSummaryResponse> GetSummaryAsync(Guid listingId, string? bookingType, CancellationToken token = default)
    {
        var aggregate = await ForListing(listingId, bookingType).GroupBy(r => r.ListingId)
            .Select(g => new { Count = g.Count(), Sum = g.Sum(r => (long)r.Rating) }).SingleOrDefaultAsync(token);
        var average = aggregate == null ? 0m : Math.Round(aggregate.Sum / (decimal)aggregate.Count, 2, MidpointRounding.AwayFromZero);
        return new(listingId, bookingType, (double)average, aggregate?.Count ?? 0);
    }

    private IQueryable<ListingReview> ForListing(Guid listingId, string? type)
        => db.ListingReviews.AsNoTracking().Where(r => r.ListingId == listingId && (type == null || r.BookingType == type));
    private static ReviewResponse ToResponse(ListingReview r)
        => new(r.Id, r.ListingId, r.BookingType, r.Rating, r.Comment,
            DateTime.SpecifyKind(r.CreatedAtUtc, DateTimeKind.Utc), r.ReviewerDisplayName);

    public async Task<PlatformReviewResponse> CreatePlatformAsync(Guid visitorId, CreatePlatformReviewRequest request, CancellationToken token = default)
    {
        var errors = new List<ValidationResult>();
        if (visitorId == Guid.Empty || !Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
            throw new ReviewException(400, "A 1–5 rating and written review (up to 2000 characters) are required.");
        var review = new PlatformReview { Id = Guid.NewGuid(), VisitorId = visitorId,
            Rating = request.Rating, Comment = request.Comment.Trim(), CreatedAtUtc = clock.GetUtcNow().UtcDateTime };
        db.PlatformReviews.Add(review);
        await db.SaveChangesAsync(token);
        return new(review.Id, review.Rating, review.Comment, review.CreatedAtUtc);
    }

    public async Task<PagedPlatformReviewsResponse> GetPlatformAsync(ReviewQuery query, CancellationToken token = default)
    {
        Validator.ValidateObject(query, new ValidationContext(query), true);
        if (query.BookingType != null) throw new ReviewException(400, "Platform reviews do not have a booking type.");
        var all = db.PlatformReviews.AsNoTracking();
        var aggregate = await all.GroupBy(r => 1).Select(g => new { Count = g.Count(), Average = g.Average(r => (double)r.Rating) }).SingleOrDefaultAsync(token);
        var filtered = query.Rating.HasValue ? all.Where(r => r.Rating == query.Rating.Value) : all;
        var total = await filtered.CountAsync(token);
        var rows = await filtered.OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(token);
        return new(rows.Select(r => new PlatformReviewResponse(r.Id, r.Rating, r.Comment,
                DateTime.SpecifyKind(r.CreatedAtUtc, DateTimeKind.Utc))).ToList(), query.Page, query.PageSize, total,
            (int)Math.Ceiling(total / (double)query.PageSize), Math.Round(aggregate?.Average ?? 0, 2), aggregate?.Count ?? 0);
    }

    public async Task<ReviewEligibilityResponse> GetEligibilityAsync(
    Guid visitorId,
    Guid listingId,
    string bookingType,
    CancellationToken token = default)
    {
        if (visitorId == Guid.Empty || listingId == Guid.Empty
            || bookingType is not ("Experience" or "Restaurant" or "Accommodation"))
        {
            throw new ReviewException(
                400, "A listing ID and supported booking type are required.");
        }

        IEnumerable<IPayableBooking> bookings = bookingType switch
        {
            "Experience" => (await db.Bookings.AsNoTracking()
                .Where(b => b.VisitorId == visitorId
                    && b.ListingId == listingId
                    && !b.IsDeleted)
                .ToListAsync(token)).Cast<IPayableBooking>(),

            "Restaurant" => (await db.RestaurantReservations.AsNoTracking()
                .Where(b => b.VisitorId == visitorId
                    && b.RestaurantId == listingId
                    && !b.IsDeleted)
                .ToListAsync(token)).Cast<IPayableBooking>(),

            _ => (await db.AccommodationBookings.AsNoTracking()
                .Where(b => b.VisitorId == visitorId
                    && b.AccommodationId == listingId
                    && !b.IsDeleted)
                .ToListAsync(token)).Cast<IPayableBooking>()
        };

        var rows = bookings.OrderByDescending(b => b.BookingDate).ToList();

        var reviewedIds = (await db.ListingReviews.AsNoTracking()
            .Where(r => r.VisitorId == visitorId
                && r.ListingId == listingId
                && r.BookingType == bookingType)
            .Select(r => r.BookingId)
            .ToListAsync(token)).ToHashSet();

        var now = clock.GetUtcNow().UtcDateTime;

        var eligible = rows
            .Where(b => IneligibilityReason(
                b, now, reviewedIds.Contains(b.Id)) == null)
            .Select(b => new EligibleReviewBooking(
                b.Id,
                b.BookingDate,
                DateTime.SpecifyKind(
                    ScheduledEnd(b)!.Value, DateTimeKind.Utc)))
            .ToList();

     string? message = null;

        if (eligible.Count == 0)
        {
            message = rows.Count == 0
                ? "You need a completed booking for this listing to review it."
                : IneligibilityReason(
                    rows[0], now, reviewedIds.Contains(rows[0].Id));
        }

        return new ReviewEligibilityResponse(eligible, message);
    }
private static DateTime? ScheduledEnd(IPayableBooking booking)
    => booking switch
    {
        Booking b => b.ScheduledEndAtUtc
            ?? ReviewSchedule.SlotEnd(b.BookingDate, b.TimeSlot),

        RestaurantReservation r => r.ScheduledEndAtUtc
            ?? ReviewSchedule.SlotEnd(r.ReservationDate, r.TimeSlot),

        AccommodationBooking a => a.ScheduledEndAtUtc,

        _ => null
    };

private static string? IneligibilityReason(
    IPayableBooking booking,
    DateTime now,
    bool alreadyReviewed)
{
    if (alreadyReviewed)
        return "You have already reviewed this booking.";

    if (booking.Status != BookingStatus.Completed)
        return "Only completed bookings can be reviewed.";

    var end = ScheduledEnd(booking);

    if (end == null)
        return "The scheduled end time is unavailable. Please contact support.";

    if (end >= now)
        return "You can review this booking after the service has ended.";

    return null;
}
}
