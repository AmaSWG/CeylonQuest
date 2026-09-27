using System.ComponentModel.DataAnnotations;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Services;

public class AdminBookingsRevenueReportService : IAdminBookingsRevenueReportService
{
    private readonly BookingDbContext _db;
    private readonly ICatalogService _catalog;

    private sealed class Row
    {
        public string Type { get; init; } = string.Empty;
        public Guid ListingId { get; init; }
        public string Service { get; init; } = string.Empty;
        public DateOnly Date { get; init; }
        public string Status { get; init; } = string.Empty;
        public decimal Value { get; init; }
        public decimal Refund { get; init; }
        public decimal Revenue { get; init; }
        public bool Cancelled { get; init; }
    }

    public AdminBookingsRevenueReportService(BookingDbContext db, ICatalogService catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    public async Task<AdminBookingsRevenueReportResponse> GenerateAsync(string accessToken, AdminBookingsRevenueReportQuery query)
    {
        Validator.ValidateObject(query, new ValidationContext(query), true);
        if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("Admin access token is required.", nameof(accessToken));

        var listings = await _catalog.GetAdminListingsAsync(accessToken);
        var metadata = listings.ToDictionary(l => (l.BookingType, l.Id));
        var type = query.BookingType;
        var status = query.Status;
        bool Includes(string value) => type == null || value.Equals(type, StringComparison.OrdinalIgnoreCase);
        bool StatusMatches(string value) => status == null || value.Equals(status, StringComparison.OrdinalIgnoreCase);
        var rows = new List<Row>();

        if (Includes("Experience"))
        {
            var records = _db.Bookings.AsNoTracking().AsEnumerable()
                .Where(b => InRange(b.BookingDate, query) && StatusMatches(b.Status.ToString()));
            rows.AddRange(records.Select(b => new Row { Type = "Experience", ListingId = b.ListingId, Service = b.ListingTitle, Date = b.BookingDate, Status = b.Status.ToString(), Value = b.TotalAmount, Refund = b.RefundAmount, Revenue = Billable(b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed, b.TotalAmount, b.RefundAmount), Cancelled = b.Status == BookingStatus.Cancelled }));
        }
        if (Includes("Restaurant"))
        {
            var records = _db.RestaurantReservations.AsNoTracking().AsEnumerable()
                .Where(r => InRange(r.ReservationDate, query) && StatusMatches(r.Status.ToString()));
            rows.AddRange(records.Select(r => new Row { Type = "Restaurant", ListingId = r.RestaurantId, Service = r.RestaurantName, Date = r.ReservationDate, Status = r.Status.ToString(), Value = r.TotalPrice, Refund = r.RefundAmount, Revenue = Billable(r.Status == ReservationStatus.Confirmed, r.TotalPrice, r.RefundAmount), Cancelled = r.Status == ReservationStatus.Cancelled }));
        }
        if (Includes("Accommodation"))
        {
            var records = _db.AccommodationBookings.AsNoTracking().AsEnumerable()
                .Where(a => InRange(a.CheckInDate, query) && StatusMatches(a.Status.ToString()));
            rows.AddRange(records.Select(a => new Row { Type = "Accommodation", ListingId = a.AccommodationId, Service = a.AccommodationName, Date = a.CheckInDate, Status = a.Status.ToString(), Value = a.TotalPrice, Refund = a.RefundAmount, Revenue = Billable(a.Status == AccommodationBookingStatus.Confirmed || a.Status == AccommodationBookingStatus.Completed, a.TotalPrice, a.RefundAmount), Cancelled = a.Status == AccommodationBookingStatus.Cancelled }));
        }

        var reportRows = rows.Select(r => new { Row = r, Listing = metadata.GetValueOrDefault((r.Type, r.ListingId)) }).Where(x => !query.ProviderId.HasValue || x.Listing?.ProviderId == query.ProviderId.Value).ToList();
        var filtered = reportRows.Select(x => x.Row).ToList();
        var response = new AdminBookingsRevenueReportResponse { Filters = query, TotalBookings = filtered.Count, TotalRevenue = filtered.Sum(r => r.Revenue), CancellationRate = Percentage(filtered.Count(r => r.Cancelled), filtered.Count), Providers = listings.GroupBy(l => new { l.ProviderId, l.ProviderName }).Select(g => new AdminProviderOption { ProviderId = g.Key.ProviderId, ProviderName = g.Key.ProviderName }).OrderBy(p => p.ProviderName).ToList() };
        response.BookingGrowthPercentage = await CalculateGrowthAsync(query, filtered.Count);
        response.BookingTypeDistribution = Distribution(filtered.Select(r => r.Type), new[] { "Experience", "Restaurant", "Accommodation" });
        response.StatusDistribution = Distribution(filtered.Select(r => r.Status));
        response.Records = filtered.OrderByDescending(r => r.Date).Select(r => new AdminBookingReportRecord { ProviderName = metadata.GetValueOrDefault((r.Type, r.ListingId))?.ProviderName, ServiceName = r.Service, BookingType = r.Type, Date = r.Date, Status = r.Status, BookingValue = r.Value, RefundAmount = r.Refund, Revenue = r.Revenue }).ToList();
        response.TopServices = filtered.GroupBy(r => new { r.Type, r.Service, Provider = metadata.GetValueOrDefault((r.Type, r.ListingId))?.ProviderName }).Select(g => new AdminServiceSummary { ServiceName = g.Key.Service, BookingType = g.Key.Type, ProviderName = g.Key.Provider, TotalBookings = g.Count(), TotalRevenue = g.Sum(r => r.Revenue), PercentageOfBookings = Percentage(g.Count(), filtered.Count) }).OrderByDescending(s => s.TotalBookings).Take(5).ToList();
        response.MostBookedService = response.TopServices.FirstOrDefault();
        response.TopProviders = filtered.GroupBy(r => metadata.GetValueOrDefault((r.Type, r.ListingId))).Where(g => g.Key != null).Select(g => new AdminProviderSummary { ProviderId = g.Key!.ProviderId, ProviderName = g.Key.ProviderName, TotalBookings = g.Count(), TotalRevenue = g.Sum(r => r.Revenue), CancellationRate = Percentage(g.Count(r => r.Cancelled), g.Count()) }).OrderByDescending(p => p.TotalBookings).Take(5).ToList();
        response.TopProviderByBookings = response.TopProviders.FirstOrDefault();
        response.TopProviderByRevenue = response.TopProviders.OrderByDescending(p => p.TotalRevenue).FirstOrDefault();
        response.RevenueOverTime = RevenueTrend(filtered, query);
        // Bookings Over Time — booking count grouped by the same date/period as revenue trend
        response.BookingsOverTime = BookingsTrend(filtered, query);
        // Revenue by Booking Type — revenue and count per category (Experience/Restaurant/Accommodation)
        response.RevenueByBookingType = RevenueByType(filtered);
        return response;
    }

    private async Task<decimal> CalculateGrowthAsync(AdminBookingsRevenueReportQuery query, int currentCount)
    {
        if (!query.StartDate.HasValue || !query.EndDate.HasValue) return 0m;
        var length = query.EndDate.Value.DayNumber - query.StartDate.Value.DayNumber + 1;
        var previous = new AdminBookingsRevenueReportQuery { StartDate = query.StartDate.Value.AddDays(-length), EndDate = query.StartDate.Value.AddDays(-1), Status = query.Status, BookingType = query.BookingType, ProviderId = query.ProviderId };
        var previousReport = await GenerateAsyncForGrowth(previous);
        return previousReport == 0 ? currentCount == 0 ? 0m : 100m : Math.Round((decimal)(currentCount - previousReport) / previousReport * 100m, 2);
    }

    private async Task<int> GenerateAsyncForGrowth(AdminBookingsRevenueReportQuery query)
    {
        bool Includes(string value) => query.BookingType == null || query.BookingType.Equals(value, StringComparison.OrdinalIgnoreCase);
        var bookingStatus = query.Status == null ? (BookingStatus?)null : Enum.Parse<BookingStatus>(query.Status, true);
        var restaurantStatus = query.Status != null && Enum.TryParse<ReservationStatus>(query.Status, true, out var parsedRestaurantStatus) ? parsedRestaurantStatus : (ReservationStatus?)null;
        var accommodationStatus = query.Status != null && Enum.TryParse<AccommodationBookingStatus>(query.Status, true, out var parsedAccommodationStatus) ? parsedAccommodationStatus : (AccommodationBookingStatus?)null;
        var count = 0;
        if (Includes("Experience")) count += await _db.Bookings.CountAsync(b => b.BookingDate >= query.StartDate && b.BookingDate <= query.EndDate && (!bookingStatus.HasValue || b.Status == bookingStatus.Value));
        if (Includes("Restaurant")) count += await _db.RestaurantReservations.CountAsync(r => r.ReservationDate >= query.StartDate && r.ReservationDate <= query.EndDate && (!restaurantStatus.HasValue || r.Status == restaurantStatus.Value));
        if (Includes("Accommodation")) count += await _db.AccommodationBookings.CountAsync(a => a.CheckInDate >= query.StartDate && a.CheckInDate <= query.EndDate && (!accommodationStatus.HasValue || a.Status == accommodationStatus.Value));
        return count;
    }

    private static bool InRange(DateOnly date, AdminBookingsRevenueReportQuery q) => (!q.StartDate.HasValue || date >= q.StartDate) && (!q.EndDate.HasValue || date <= q.EndDate);
    private static decimal Billable(bool eligible, decimal value, decimal refund) => eligible ? value - refund : 0m;
    private static decimal Percentage(int count, int total) => total == 0 ? 0m : Math.Round((decimal)count / total * 100m, 2);
    private static List<AdminDistributionItem> Distribution(IEnumerable<string> values, IEnumerable<string>? expected = null)
    {
        var counts = values.GroupBy(v => v).ToDictionary(g => g.Key, g => g.Count());
        var names = expected ?? counts.Keys.OrderBy(k => k);
        var total = counts.Values.Sum();
        return names.Select(name => new AdminDistributionItem { Name = name, Count = counts.GetValueOrDefault(name), Percentage = Percentage(counts.GetValueOrDefault(name), total) }).Where(x => x.Count > 0 || expected != null).ToList();
    }

    /// <summary>
    /// Groups filtered rows by date (daily for spans &lt;90 days, monthly otherwise).
    /// Used by the Revenue Over Time bar chart.
    /// </summary>
    private static List<AdminRevenueTrendItem> RevenueTrend(List<Row> rows, AdminBookingsRevenueReportQuery query)
    {
        if (rows.Count == 0) return new();
        var monthly = query.StartDate.HasValue && query.EndDate.HasValue && query.EndDate.Value.DayNumber - query.StartDate.Value.DayNumber >= 90;
        return rows.GroupBy(r => monthly ? new DateOnly(r.Date.Year, r.Date.Month, 1) : r.Date).OrderBy(g => g.Key).Select(g => new AdminRevenueTrendItem { Period = g.Key.ToString(monthly ? "yyyy-MM" : "yyyy-MM-dd"), Revenue = g.Sum(r => r.Revenue) }).ToList();
    }

    /// <summary>
    /// Groups filtered rows by the same date/period as the revenue trend but counts bookings.
    /// Used by the Bookings Over Time bar chart.
    /// </summary>
    private static List<AdminBookingsTrendItem> BookingsTrend(List<Row> rows, AdminBookingsRevenueReportQuery query)
    {
        if (rows.Count == 0) return new();
        var monthly = query.StartDate.HasValue && query.EndDate.HasValue && query.EndDate.Value.DayNumber - query.StartDate.Value.DayNumber >= 90;
        return rows.GroupBy(r => monthly ? new DateOnly(r.Date.Year, r.Date.Month, 1) : r.Date).OrderBy(g => g.Key).Select(g => new AdminBookingsTrendItem { Period = g.Key.ToString(monthly ? "yyyy-MM" : "yyyy-MM-dd"), BookingCount = g.Count() }).ToList();
    }

    /// <summary>
    /// Aggregates revenue and booking count per booking type (Experience / Restaurant / Accommodation).
    /// Only types with at least one booking are included.
    /// </summary>
    private static List<AdminRevenueByTypeItem> RevenueByType(List<Row> rows)
    {
        if (rows.Count == 0) return new();
        var order = new[] { "Experience", "Restaurant", "Accommodation" };
        return rows.GroupBy(r => r.Type)
            .Select(g => new AdminRevenueByTypeItem { BookingType = g.Key, Revenue = g.Sum(r => r.Revenue), BookingCount = g.Count() })
            .OrderBy(x => Array.IndexOf(order, x.BookingType))
            .ToList();
    }
}