using System;
using System.Collections.Generic;

namespace ProviderCatalogService.DTOs;

public class ReportSummaryDto
{
    public int TotalListings { get; set; }
    public int ActiveListings { get; set; }
    public int TotalCapacity { get; set; }
    public int BookedCapacity { get; set; }
    public int RemainingCapacity { get; set; }
    public double OccupancyRate { get; set; }
    public int LowAvailabilityCount { get; set; }
    public int SoldOutCount { get; set; }
}

public class CategoryReportDto
{
    public string Category { get; set; } = string.Empty; // "Experience" | "Restaurant" | "Accommodation"
    public string DisplayName { get; set; } = string.Empty; // "Experiences" | "Dining" | "Stays"
    public int ListingCount { get; set; }
    public int TotalCapacity { get; set; }
    public int RemainingCapacity { get; set; }
    public int BookedCapacity { get; set; }
}

public class LocationReportDto
{
    public string Location { get; set; } = string.Empty;
    public int ListingCount { get; set; }
    public int TotalCapacity { get; set; }
    public int LowStockAlerts { get; set; }
    public List<string> PresentCategories { get; set; } = new();
    public List<string> MissingCategories { get; set; } = new();
}

public class CoverageGapDto
{
    public string Location { get; set; } = string.Empty;
    public List<string> MissingCategories { get; set; } = new();
    public List<string> PresentCategories { get; set; } = new();
    public string Description { get; set; } = string.Empty;
}

public class LowAvailabilityItemDto
{
    public Guid ListingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string TimeSlot { get; set; } = string.Empty;
    public int TotalCapacity { get; set; }
    public int RemainingCapacity { get; set; }
    public string Status { get; set; } = "Low"; // "Sold Out" | "Critical"
    public string ProviderBusinessName { get; set; } = string.Empty;
}

public class InventoryReportResponse
{
    public ReportSummaryDto Summary { get; set; } = new();
    public List<CategoryReportDto> ByCategory { get; set; } = new();
    public List<LocationReportDto> ByLocation { get; set; } = new();
    public List<CoverageGapDto> CoverageGaps { get; set; } = new();
    public List<LowAvailabilityItemDto> LowAvailabilityAlerts { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}