namespace BookingService.DTOs;

public class BookingsRevenueReportResponse
{
    public BookingsRevenueReportQuery Filters { get; set; } = new();
    public int TotalBookings { get; set; }
    // Booking value after refunds, not a payment settlement total.
    public decimal TotalRevenue { get; set; }
    public string RevenueBasis { get; set; } = "Confirmed and completed booking value less refunds; excludes pending and cancelled bookings.";
    public bool HasData => TotalBookings > 0;
    public string? Message => HasData ? null : "No data available for the selected criteria.";
    public Dictionary<string, int> StatusCounts { get; set; } = new();
    public Dictionary<string, int> BookingTypeCounts { get; set; } = new();
    public List<BookingReportRecord> Records { get; set; } = new();
}

public class BookingReportRecord
{
    public string BookingType { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal BookingValue { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal Revenue { get; set; }
}

public class ProviderReportListingIds
{
    public HashSet<Guid> ActivityIds { get; set; } = new();
    public HashSet<Guid> RestaurantIds { get; set; } = new();
    public HashSet<Guid> AccommodationIds { get; set; } = new();
}
