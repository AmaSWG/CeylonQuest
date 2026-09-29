namespace BookingService.DTOs;

public class AdminBookingsRevenueReportResponse
{
    public AdminBookingsRevenueReportQuery Filters { get; set; } = new();
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal BookingGrowthPercentage { get; set; }
    public decimal CancellationRate { get; set; }
    public bool HasData => TotalBookings > 0;
    public string? Message => HasData ? null : "No data available for the selected criteria.";
    public string RevenueBasis { get; set; } = "Confirmed and completed booking value less refunds; excludes pending and cancelled bookings.";
    public AdminServiceSummary? MostBookedService { get; set; }
    public AdminProviderSummary? TopProviderByBookings { get; set; }
    public AdminProviderSummary? TopProviderByRevenue { get; set; }
    public List<AdminDistributionItem> BookingTypeDistribution { get; set; } = new();
    public List<AdminDistributionItem> StatusDistribution { get; set; } = new();
    public List<AdminRevenueTrendItem> RevenueOverTime { get; set; } = new();
    /// <summary>Bookings count grouped by date/period — used by the Bookings Over Time graph.</summary>
    public List<AdminBookingsTrendItem> BookingsOverTime { get; set; } = new();
    /// <summary>Revenue per booking type — used by the Revenue by Booking Type chart.</summary>
    public List<AdminRevenueByTypeItem> RevenueByBookingType { get; set; } = new();
    public List<AdminServiceSummary> TopServices { get; set; } = new();
    public List<AdminProviderSummary> TopProviders { get; set; } = new();
    public List<AdminBookingReportRecord> Records { get; set; } = new();
    public List<AdminProviderOption> Providers { get; set; } = new();
}

public class AdminServiceSummary
{
    public string ServiceName { get; set; } = string.Empty;
    public string BookingType { get; set; } = string.Empty;
    public string? ProviderName { get; set; }
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal PercentageOfBookings { get; set; }
}

public class AdminProviderSummary
{
    public Guid? ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal CancellationRate { get; set; }
}

public class AdminDistributionItem
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class AdminRevenueTrendItem
{
    public string Period { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class AdminBookingsTrendItem
{
    public string Period { get; set; } = string.Empty;
    public int BookingCount { get; set; }
}

public class AdminRevenueByTypeItem
{
    public string BookingType { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int BookingCount { get; set; }
}

public class AdminBookingReportRecord
{
    public string? ProviderName { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string BookingType { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal BookingValue { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal Revenue { get; set; }
}

public class AdminProviderOption
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
}