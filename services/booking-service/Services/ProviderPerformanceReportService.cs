using System.ComponentModel.DataAnnotations;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Services;

public class ProviderPerformanceReportService
    : IProviderPerformanceReportService
{
    private readonly BookingDbContext _db;
    private readonly ICatalogService _catalog;

    public ProviderPerformanceReportService(
        BookingDbContext db,
        ICatalogService catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    public async Task<ProviderPerformanceReportResponse> GenerateAsync(
        string accessToken,
        ProviderPerformanceReportQuery query,
        CancellationToken cancellationToken = default)
    {
        Validator.ValidateObject(query, new ValidationContext(query), true);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException(
                "Admin access token is required.", nameof(accessToken));
        }

        var catalogProviders = await _catalog.GetReportProvidersAsync(
     accessToken,
     cancellationToken);

        var listings = await _catalog.GetAdminListingsAsync(accessToken);
        cancellationToken.ThrowIfCancellationRequested();

        var listingsByProvider = listings
            .GroupBy(l => l.ProviderId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var providers = new Dictionary<Guid, ProviderPerformanceReportRow>();

        foreach (var catalogProvider in catalogProviders)
        {
            if (query.ProviderId.HasValue &&
                catalogProvider.ProviderId != query.ProviderId.Value)
            {
                continue;
            }

            var categories = listingsByProvider.TryGetValue(
                catalogProvider.ProviderId,
                out var providerListings)
                ? providerListings
                    .Select(l => l.BookingType)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c)
                    .ToList()
                : new List<string>();

            // Providers without listings use their registered service category.
            if (categories.Count == 0)
            {
                var category = MapServiceTypeToCategory(
                    catalogProvider.ServiceType);

                if (category != null)
                {
                    categories.Add(category);
                }
            }

            if (query.Category != null &&
                !categories.Contains(
                    query.Category,
                    StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            providers.Add(
                catalogProvider.ProviderId,
                new ProviderPerformanceReportRow
                {
                    ProviderId = catalogProvider.ProviderId,
                    ProviderName = catalogProvider.ProviderName,
                    Categories = query.Category == null
                        ? categories
                        : categories
                            .Where(c => c.Equals(
                                query.Category,
                                StringComparison.OrdinalIgnoreCase))
                            .ToList()
                });
        }

        var selectedListings = listings
            .Where(l => providers.ContainsKey(l.ProviderId))
            .Where(l =>
                query.Category == null ||
                l.BookingType.Equals(
                    query.Category,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (providers.Count == 0)
        {
            return new ProviderPerformanceReportResponse { Filters = query };
        }

        var ownership = selectedListings.ToDictionary(
            l => (l.BookingType.ToUpperInvariant(), l.Id),
            l => l.ProviderId);

        void AddRevenue(string category, Guid listingId, decimal revenue)
        {
            if (ownership.TryGetValue(
                (category.ToUpperInvariant(), listingId),
                out var providerId))
            {
                providers[providerId].TotalRevenue += revenue;
            }
        }

        var experienceIds = selectedListings
            .Where(l => l.BookingType.Equals(
                "Experience", StringComparison.OrdinalIgnoreCase))
            .Select(l => l.Id)
            .ToArray();

        if (experienceIds.Length > 0)
        {
            var bookings = _db.Bookings.AsNoTracking()
                .Where(b => !b.IsDeleted &&
                    experienceIds.Contains(b.ListingId) &&
                    (b.Status == BookingStatus.Confirmed ||
                     b.Status == BookingStatus.Completed));

            if (query.StartDate.HasValue)
            {
                var start = query.StartDate.Value;
                bookings = bookings.Where(b => b.BookingDate >= start);
            }

            if (query.EndDate.HasValue)
            {
                var end = query.EndDate.Value;
                bookings = bookings.Where(b => b.BookingDate <= end);
            }

            var totals = await bookings
                .GroupBy(b => b.ListingId)
                .Select(g => new
                {
                    ListingId = g.Key,
                    Revenue = g.Sum(b => b.TotalAmount - b.RefundAmount)
                })
                .ToListAsync(cancellationToken);

            foreach (var total in totals)
                AddRevenue("Experience", total.ListingId, total.Revenue);
        }

        var restaurantIds = selectedListings
            .Where(l => l.BookingType.Equals(
                "Restaurant", StringComparison.OrdinalIgnoreCase))
            .Select(l => l.Id)
            .ToArray();

        if (restaurantIds.Length > 0)
        {
            var reservations = _db.RestaurantReservations.AsNoTracking()
                .Where(r => !r.IsDeleted &&
                    restaurantIds.Contains(r.RestaurantId) &&
                    (r.Status == ReservationStatus.Confirmed ||
                     r.Status == ReservationStatus.Completed));

            if (query.StartDate.HasValue)
            {
                var start = query.StartDate.Value;
                reservations = reservations.Where(r =>
                    r.ReservationDate >= start);
            }

            if (query.EndDate.HasValue)
            {
                var end = query.EndDate.Value;
                reservations = reservations.Where(r =>
                    r.ReservationDate <= end);
            }

            var totals = await reservations
                .GroupBy(r => r.RestaurantId)
                .Select(g => new
                {
                    ListingId = g.Key,
                    Revenue = g.Sum(r => r.TotalPrice - r.RefundAmount)
                })
                .ToListAsync(cancellationToken);

            foreach (var total in totals)
                AddRevenue("Restaurant", total.ListingId, total.Revenue);
        }

        var accommodationIds = selectedListings
            .Where(l => l.BookingType.Equals(
                "Accommodation", StringComparison.OrdinalIgnoreCase))
            .Select(l => l.Id)
            .ToArray();

        if (accommodationIds.Length > 0)
        {
            var bookings = _db.AccommodationBookings.AsNoTracking()
                .Where(a => !a.IsDeleted &&
                    accommodationIds.Contains(a.AccommodationId) &&
                    (a.Status == AccommodationBookingStatus.Confirmed ||
                     a.Status == AccommodationBookingStatus.Completed));

            if (query.StartDate.HasValue)
            {
                var start = query.StartDate.Value;
                bookings = bookings.Where(a => a.CheckInDate >= start);
            }

            if (query.EndDate.HasValue)
            {
                var end = query.EndDate.Value;
                bookings = bookings.Where(a => a.CheckInDate <= end);
            }

            var totals = await bookings
                .GroupBy(a => a.AccommodationId)
                .Select(g => new
                {
                    ListingId = g.Key,
                    Revenue = g.Sum(a => a.TotalPrice - a.RefundAmount)
                })
                .ToListAsync(cancellationToken);

            foreach (var total in totals)
                AddRevenue("Accommodation", total.ListingId, total.Revenue);
        }

        var providerIds = providers.Keys.ToArray();

        var reviews = _db.ListingReviews.AsNoTracking()
            .Where(r => providerIds.Contains(r.ProviderId));

        if (query.Category != null)
        {
            var category = query.Category.ToUpperInvariant();
            reviews = reviews.Where(r =>
                r.BookingType.ToUpper() == category);
        }

        if (query.StartDate.HasValue)
        {
            var startUtc = query.StartDate.Value.ToDateTime(
                TimeOnly.MinValue, DateTimeKind.Utc);

            reviews = reviews.Where(r => r.CreatedAtUtc >= startUtc);
        }

        if (query.EndDate.HasValue &&
            query.EndDate.Value < DateOnly.MaxValue)
        {
            var endExclusiveUtc = query.EndDate.Value.AddDays(1)
                .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            reviews = reviews.Where(r =>
                r.CreatedAtUtc < endExclusiveUtc);
        }

        var ratings = await reviews
            .GroupBy(r => r.ProviderId)
            .Select(g => new
            {
                ProviderId = g.Key,
                Count = g.Count(),
                Average = g.Average(r => (double)r.Rating),
                One = g.Count(r => r.Rating == 1),
                Two = g.Count(r => r.Rating == 2),
                Three = g.Count(r => r.Rating == 3),
                Four = g.Count(r => r.Rating == 4),
                Five = g.Count(r => r.Rating == 5)
            })
            .ToListAsync(cancellationToken);

        foreach (var rating in ratings)
        {
            var provider = providers[rating.ProviderId];

            provider.TotalReviewCount = rating.Count;
            provider.AverageRating = rating.Average;
            provider.StarRatingBreakdown = new StarRatingBreakdown
            {
                OneStar = rating.One,
                TwoStars = rating.Two,
                ThreeStars = rating.Three,
                FourStars = rating.Four,
                FiveStars = rating.Five
            };
        }

        IEnumerable<ProviderPerformanceReportRow> results = providers.Values;

        // Unrated providers do not satisfy an explicitly supplied minimum.
        if (query.MinimumRating.HasValue)
        {
            results = results.Where(p =>
                p.AverageRating.HasValue &&
                p.AverageRating.Value >= query.MinimumRating.Value);
        }

        var ascending = query.SortDirection.Equals(
            "Asc", StringComparison.OrdinalIgnoreCase);

        IOrderedEnumerable<ProviderPerformanceReportRow> sorted;

        if (query.SortBy.Equals(
            "AverageRating", StringComparison.OrdinalIgnoreCase))
        {
            // Unrated providers appear last in either direction.
            var ratedFirst = results.OrderBy(p => !p.AverageRating.HasValue);

            sorted = ascending
                ? ratedFirst.ThenBy(p => p.AverageRating)
                : ratedFirst.ThenByDescending(p => p.AverageRating);
        }
        else
        {
            sorted = ascending
                ? results.OrderBy(p => p.TotalRevenue)
                : results.OrderByDescending(p => p.TotalRevenue);
        }

        return new ProviderPerformanceReportResponse
        {
            Filters = query,
            Providers = sorted
                .ThenBy(p => p.ProviderName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(p => p.ProviderId)
                .ToList()
        };

    }
    private static string? MapServiceTypeToCategory(string serviceType)
    {
        return serviceType.Trim().ToLowerInvariant() switch
        {
            "hotel" or "accommodation" => "Accommodation",
            "restaurant" => "Restaurant",
            "tour" or "activity" or "experience" => "Experience",
            _ => null
        };
    }
}