using System.ComponentModel.DataAnnotations;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Services;

public class BookingsRevenueReportService : IBookingsRevenueReportService
{
    private readonly BookingDbContext _db;
    private readonly ICatalogService _catalog;

    public BookingsRevenueReportService(BookingDbContext db, ICatalogService catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    public async Task<BookingsRevenueReportResponse> GenerateAsync(string accessToken, BookingsRevenueReportQuery query)
    {
        Validator.ValidateObject(query, new ValidationContext(query), true);
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Provider access token is required.", nameof(accessToken));

        // Resolve all ownership before any booking queries. A lookup failure must abort the report.
        var owned = await _catalog.GetReportOwnedListingIdsAsync(accessToken);
        var records = new List<BookingReportRecord>();
        var status = query.Status == null ? (BookingStatus?)null : Enum.Parse<BookingStatus>(query.Status, true);
        bool Includes(string type) => query.BookingType == null || string.Equals(query.BookingType, type, StringComparison.OrdinalIgnoreCase);

        if (Includes("Experience") && owned.ActivityIds.Count > 0)
        {
            var bookings = _db.Bookings.AsNoTracking().Where(b => owned.ActivityIds.Contains(b.ListingId));
            if (query.StartDate.HasValue) bookings = bookings.Where(b => b.BookingDate >= query.StartDate.Value);
            if (query.EndDate.HasValue) bookings = bookings.Where(b => b.BookingDate <= query.EndDate.Value);
            if (status.HasValue) bookings = bookings.Where(b => b.Status == status.Value);
            records.AddRange(await bookings.Select(b => new BookingReportRecord
            {
                BookingType = "Experience", ServiceName = b.ListingTitle, Date = b.BookingDate,
                Status = b.Status.ToString(), BookingValue = b.TotalAmount, RefundAmount = b.RefundAmount,
                Revenue = b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed
                    ? b.TotalAmount - b.RefundAmount : 0m
            }).ToListAsync());
        }

        if (Includes("Restaurant") && owned.RestaurantIds.Count > 0 &&
            status != BookingStatus.Completed)
        {
            var reservations = _db.RestaurantReservations.AsNoTracking().Where(r => owned.RestaurantIds.Contains(r.RestaurantId));
            if (query.StartDate.HasValue) reservations = reservations.Where(r => r.ReservationDate >= query.StartDate.Value);
            if (query.EndDate.HasValue) reservations = reservations.Where(r => r.ReservationDate <= query.EndDate.Value);
            if (status.HasValue)
            {
                var reservationStatus = Enum.Parse<ReservationStatus>(status.Value.ToString());
                reservations = reservations.Where(r => r.Status == reservationStatus);
            }
            records.AddRange(await reservations.Select(r => new BookingReportRecord
            {
                BookingType = "Restaurant", ServiceName = r.RestaurantName, Date = r.ReservationDate,
                Status = r.Status.ToString(), BookingValue = r.TotalPrice, RefundAmount = r.RefundAmount,
                Revenue = r.Status == ReservationStatus.Confirmed ? r.TotalPrice - r.RefundAmount : 0m
            }).ToListAsync());
        }

        if (Includes("Accommodation") && owned.AccommodationIds.Count > 0)
        {
            var stays = _db.AccommodationBookings.AsNoTracking().Where(a => owned.AccommodationIds.Contains(a.AccommodationId));
            if (query.StartDate.HasValue) stays = stays.Where(a => a.CheckInDate >= query.StartDate.Value);
            if (query.EndDate.HasValue) stays = stays.Where(a => a.CheckInDate <= query.EndDate.Value);
            if (status.HasValue)
            {
                var stayStatus = Enum.Parse<AccommodationBookingStatus>(status.Value.ToString());
                stays = stays.Where(a => a.Status == stayStatus);
            }
            records.AddRange(await stays.Select(a => new BookingReportRecord
            {
                BookingType = "Accommodation", ServiceName = a.AccommodationName, Date = a.CheckInDate,
                Status = a.Status.ToString(), BookingValue = a.TotalPrice, RefundAmount = a.RefundAmount,
                Revenue = a.Status == AccommodationBookingStatus.Confirmed || a.Status == AccommodationBookingStatus.Completed
                    ? a.TotalPrice - a.RefundAmount : 0m
            }).ToListAsync());
        }

        return new BookingsRevenueReportResponse
        {
            Filters = query,
            TotalBookings = records.Count,
            TotalRevenue = records.Sum(r => r.Revenue),
            StatusCounts = records.GroupBy(r => r.Status).ToDictionary(g => g.Key, g => g.Count()),
            BookingTypeCounts = records.GroupBy(r => r.BookingType).ToDictionary(g => g.Key, g => g.Count()),
            Records = records.OrderByDescending(r => r.Date).ThenBy(r => r.BookingType).ThenBy(r => r.ServiceName).ToList()
        };
    }
}
