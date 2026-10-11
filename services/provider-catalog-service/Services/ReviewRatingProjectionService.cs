using System.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using ProviderCatalogService.Data;
using ProviderCatalogService.Events;
using ProviderCatalogService.Models;

namespace ProviderCatalogService.Services;

public class ReviewRatingProjectionService(CatalogDbContext db)
{
    public static bool IsValid(ReviewSubmittedEvent? evt) => evt is
        { EventVersion: 1, Rating: >= 1 and <= 5 }
        && evt.EventId != Guid.Empty && evt.ReviewId != Guid.Empty && evt.ListingId != Guid.Empty
        && evt.ProviderId != Guid.Empty && evt.BookingId != Guid.Empty && evt.VisitorId != Guid.Empty
        && evt.BookingType is "Experience" or "Restaurant" or "Accommodation";

    public async Task<bool> ApplyAsync(ReviewSubmittedEvent evt, CancellationToken token = default)
    {
        if (!IsValid(evt)) throw new ArgumentException("Invalid review event.", nameof(evt));
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token) : null;
        if (await db.ReviewRatingContributions.AnyAsync(r => r.ReviewId == evt.ReviewId || r.EventId == evt.EventId, token)) return true;

        db.ReviewRatingContributions.Add(new ReviewRatingContribution
        {
            ReviewId = evt.ReviewId, EventId = evt.EventId, ListingId = evt.ListingId,
            BookingType = evt.BookingType, Rating = evt.Rating, ProcessedAtUtc = DateTime.UtcNow
        });
        try
        {
            // Insert inbox first; both it and the atomic increment commit together.
            if (db.Database.IsRelational()) await db.SaveChangesAsync(token);
            var changed = evt.BookingType switch
            {
                "Experience" => await IncrementAsync<ActivityListing>(evt.ListingId, evt.Rating, token),
                "Restaurant" => await IncrementAsync<RestaurantListing>(evt.ListingId, evt.Rating, token),
                _ => await IncrementAsync<AccommodationListing>(evt.ListingId, evt.Rating, token)
            };
            // Listings can be physically deleted while an event is in transit. Keep the
            // contribution for recovery without blocking the entire Kafka partition.
            if (changed == 0 && !db.Database.IsRelational()) await db.SaveChangesAsync(token);
            if (transaction != null) await transaction.CommitAsync(token);
            return changed > 0;
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
        {
            // Another instance won the same inbox insert. Its atomic update owns this review.
            if (transaction != null) await transaction.RollbackAsync(token);
            db.ChangeTracker.Clear();
            return true;
        }
    }

    private async Task<int> IncrementAsync<T>(Guid listingId, int rating, CancellationToken token) where T : class, IListingRating
    {
        var query = db.Set<T>().Where(l => l.Id == listingId);
        if (db.Database.IsRelational())
        {
            var changed = await query.ExecuteUpdateAsync(set => set
                .SetProperty(l => l.RatingSum, l => l.RatingSum + rating)
                .SetProperty(l => l.ReviewCount, l => l.ReviewCount + 1), token);
            if (changed > 0)
                await query.ExecuteUpdateAsync(set => set.SetProperty(l => l.AverageRating,
                    l => Math.Round(l.RatingSum / (decimal)l.ReviewCount, 2)), token);
            return changed;
        }
        var listing = await query.SingleOrDefaultAsync(token);
        if (listing == null) return 0;
        listing.RatingSum += rating; listing.ReviewCount++;
        listing.AverageRating = Math.Round(listing.RatingSum / (decimal)listing.ReviewCount, 2, MidpointRounding.AwayFromZero);
        await db.SaveChangesAsync(token);
        return 1;
    }

    /// <summary>Repair listing totals from durable, deduplicated consumed contributions.</summary>
    public async Task RebuildAsync(Guid listingId, string bookingType, CancellationToken token = default)
    {
        if (listingId == Guid.Empty || bookingType is not ("Experience" or "Restaurant" or "Accommodation"))
            throw new ArgumentException("A listing ID and supported booking type are required.");
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token) : null;
        var contributions = db.ReviewRatingContributions.Where(r => r.ListingId == listingId && r.BookingType == bookingType);
        var totals = await contributions.GroupBy(r => r.ListingId)
            .Select(g => new { Count = g.Count(), Sum = g.Sum(r => (long)r.Rating) }).SingleOrDefaultAsync(token);
        IListingRating? listing = bookingType switch
        {
            "Experience" => await db.ActivityListings.SingleOrDefaultAsync(l => l.Id == listingId, token),
            "Restaurant" => await db.RestaurantListings.SingleOrDefaultAsync(l => l.Id == listingId, token),
            _ => await db.AccommodationListings.SingleOrDefaultAsync(l => l.Id == listingId, token)
        };
        if (listing == null) throw new KeyNotFoundException("Listing not found.");
        listing.RatingSum = totals?.Sum ?? 0; listing.ReviewCount = totals?.Count ?? 0;
        listing.AverageRating = listing.ReviewCount == 0 ? 0
            : Math.Round(listing.RatingSum / (decimal)listing.ReviewCount, 2, MidpointRounding.AwayFromZero);
        await db.SaveChangesAsync(token);
        if (transaction != null) await transaction.CommitAsync(token);
    }
}
