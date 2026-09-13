using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;

namespace ProviderCatalogService.Services;

public class InventoryReportService
{
    private readonly CatalogDbContext _db;
    private static readonly string[] PlatformCategories = { "Experience", "Restaurant", "Accommodation" };

    public InventoryReportService(CatalogDbContext db)
    {
        _db = db;
    }

    public async Task<InventoryReportResponse> GenerateReportAsync(
        Guid? providerId,
        DateOnly startDate,
        DateOnly endDate,
        string? categoryFilter = null,
        string? locationFilter = null,
        string? statusFilter = null)
    {
        var catFilter = categoryFilter?.Trim();
        var locFilter = locationFilter?.Trim();

        // 1. Fetch Listings
        var actQuery = _db.ActivityListings.Include(a => a.Provider).AsNoTracking().Where(a => a.IsActive);
        var restQuery = _db.RestaurantListings.Include(r => r.Provider).AsNoTracking().Where(r => r.IsActive);
        var stayQuery = _db.AccommodationListings.Include(ac => ac.Provider).AsNoTracking().Where(ac => ac.IsActive);

        if (providerId.HasValue)
        {
            actQuery = actQuery.Where(a => a.ProviderId == providerId.Value);
            restQuery = restQuery.Where(r => r.ProviderId == providerId.Value);
            stayQuery = stayQuery.Where(ac => ac.ProviderId == providerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(locFilter))
        {
            actQuery = actQuery.Where(a => EF.Functions.Like(a.Location, $"%{locFilter}%"));
            restQuery = restQuery.Where(r => EF.Functions.Like(r.Location, $"%{locFilter}%"));
            stayQuery = stayQuery.Where(ac => EF.Functions.Like(ac.Location, $"%{locFilter}%"));
        }

        List<ActivityListing> activities = new();
        List<RestaurantListing> restaurants = new();
        List<AccommodationListing> stays = new();

        if (string.IsNullOrWhiteSpace(catFilter) || catFilter.Equals("Experience", StringComparison.OrdinalIgnoreCase))
            activities = await actQuery.ToListAsync();

        if (string.IsNullOrWhiteSpace(catFilter) || catFilter.Equals("Restaurant", StringComparison.OrdinalIgnoreCase))
            restaurants = await restQuery.ToListAsync();

        if (string.IsNullOrWhiteSpace(catFilter) || catFilter.Equals("Accommodation", StringComparison.OrdinalIgnoreCase))
            stays = await stayQuery.ToListAsync();

        // 2. Fetch all DB slot overrides within the window
        var allListingIds = activities.Select(a => a.Id)
            .Concat(restaurants.Select(r => r.Id))
            .Concat(stays.Select(s => s.Id))
            .ToHashSet();

        var dbSlots = await _db.AvailabilitySlots
            .AsNoTracking()
            .Where(s => allListingIds.Contains(s.ListingId) && s.Date >= startDate && s.Date <= endDate)
            .ToListAsync();

        // 3. Build Unified Virtual Slot Matrix across all days in window
        var resolvedSlots = new List<ResolvedSlot>();
        var totalDays = endDate.DayNumber - startDate.DayNumber + 1;

        // --- Activities (Experiences) ---
        foreach (var act in activities)
        {
            var defaultCap = act.MaxParticipants > 0 ? act.MaxParticipants : 10;
            var timeSlots = ParseTimeSlots(act.TimeSlots);

            for (var d = startDate; d <= endDate; d = d.AddDays(1))
            {
                var isOperating = IsDateInOperatingSchedule(d, act.ValidFrom, act.ValidUntil, act.AvailableDays);

                // Skip days when the activity is NOT scheduled to operate (e.g. closed on Tuesdays)
                if (!isOperating) continue;

                foreach (var ts in timeSlots)
                {
                    var overrideSlot = dbSlots.FirstOrDefault(s => s.ListingId == act.Id && s.Date == d && s.TimeSlot.Equals(ts, StringComparison.OrdinalIgnoreCase));
                    var total = overrideSlot?.TotalCapacity ?? defaultCap;
                    var remaining = overrideSlot != null ? overrideSlot.RemainingCapacity : defaultCap;
                    var booked = Math.Max(0, total - remaining);

                    resolvedSlots.Add(new ResolvedSlot
                    {
                        ListingId = act.Id,
                        Title = act.Title,
                        Category = "Experience",
                        Location = act.Location,
                        ProviderBusinessName = act.Provider?.BusinessName ?? "",
                        Date = d,
                        TimeSlot = ts,
                        TotalCapacity = total,
                        RemainingCapacity = remaining,
                        BookedCapacity = booked
                    });
                }
            }
        }
        // --- Restaurants (Dining) ---
        foreach (var rest in restaurants)
        {
            var defaultCap = rest.SeatingCapacity > 0 ? rest.SeatingCapacity : 20;
            var timeSlot = string.IsNullOrWhiteSpace(rest.OpeningHours) ? "11:30 AM - 10:00 PM" : rest.OpeningHours;

            for (var d = startDate; d <= endDate; d = d.AddDays(1))
            {
                var overrideSlot = dbSlots.FirstOrDefault(s => s.ListingId == rest.Id && s.Date == d && s.TimeSlot.Equals(timeSlot, StringComparison.OrdinalIgnoreCase));
                var total = overrideSlot?.TotalCapacity ?? defaultCap;
                var remaining = overrideSlot != null ? overrideSlot.RemainingCapacity : defaultCap;
                var booked = Math.Max(0, total - remaining);

                resolvedSlots.Add(new ResolvedSlot
                {
                    ListingId = rest.Id,
                    Title = rest.Name,
                    Category = "Restaurant",
                    Location = rest.Location,
                    ProviderBusinessName = rest.Provider?.BusinessName ?? "",
                    Date = d,
                    TimeSlot = timeSlot,
                    TotalCapacity = total,
                    RemainingCapacity = remaining,
                    BookedCapacity = booked
                });
            }
        }

        // --- Stays (Accommodations) ---
        foreach (var stay in stays)
        {
            var defaultCap = stay.MaxGuests > 0 ? stay.MaxGuests : 2;
            var timeSlot = "Check-in 14:00 / Nightly";

            for (var d = startDate; d <= endDate; d = d.AddDays(1))
            {
                var overrideSlot = dbSlots.FirstOrDefault(s => s.ListingId == stay.Id && s.Date == d && s.TimeSlot.Equals(timeSlot, StringComparison.OrdinalIgnoreCase));
                var total = overrideSlot?.TotalCapacity ?? defaultCap;
                var remaining = overrideSlot != null ? overrideSlot.RemainingCapacity : defaultCap;
                var booked = Math.Max(0, total - remaining);

                resolvedSlots.Add(new ResolvedSlot
                {
                    ListingId = stay.Id,
                    Title = stay.RoomType,
                    Category = "Accommodation",
                    Location = stay.Location,
                    ProviderBusinessName = stay.Provider?.BusinessName ?? "",
                    Date = d,
                    TimeSlot = timeSlot,
                    TotalCapacity = total,
                    RemainingCapacity = remaining,
                    BookedCapacity = booked
                });
            }
        }

        // 4. Low Availability & Sold Out Alerts (Sorted by Date and TimeSlot)
        var lowAlerts = new List<LowAvailabilityItemDto>();
        foreach (var slot in resolvedSlots)
        {
            var isSoldOut = slot.TotalCapacity > 0 && slot.RemainingCapacity <= 0;
            var isLow = !isSoldOut && slot.TotalCapacity > 0 && (slot.RemainingCapacity <= 3 || slot.RemainingCapacity <= slot.TotalCapacity * 0.25);

            if (isSoldOut || isLow)
            {
                lowAlerts.Add(new LowAvailabilityItemDto
                {
                    ListingId = slot.ListingId,
                    Title = slot.Title,
                    Category = slot.Category,
                    Location = slot.Location,
                    Date = slot.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    TimeSlot = slot.TimeSlot,
                    TotalCapacity = slot.TotalCapacity,
                    RemainingCapacity = slot.RemainingCapacity,
                    Status = isSoldOut ? "Sold Out" : "Low",
                    ProviderBusinessName = slot.ProviderBusinessName
                });
            }
        }

        lowAlerts = lowAlerts
            .OrderBy(a => a.Date, StringComparer.Ordinal)
            .ThenBy(a => a.TimeSlot, StringComparer.Ordinal)
            .ToList();

        // 5. Category Aggregations (Coherent Window Sums)
        var byCategory = new List<CategoryReportDto>();
        var cats = new[] { ("Experience", "Experiences", activities.Count), ("Restaurant", "Dining", restaurants.Count), ("Accommodation", "Stays", stays.Count) };

        foreach (var (catKey, displayName, count) in cats)
        {
            if (string.IsNullOrWhiteSpace(catFilter) || catFilter.Equals(catKey, StringComparison.OrdinalIgnoreCase))
            {
                var catSlots = resolvedSlots.Where(s => s.Category == catKey).ToList();
                byCategory.Add(new CategoryReportDto
                {
                    Category = catKey,
                    DisplayName = displayName,
                    ListingCount = count,
                    TotalCapacity = catSlots.Sum(s => s.TotalCapacity),
                    BookedCapacity = catSlots.Sum(s => s.BookedCapacity),
                    RemainingCapacity = catSlots.Sum(s => s.RemainingCapacity)
                });
            }
        }

        // 6. Location Aggregations & Coverage Gaps
        var allLocations = activities.Select(a => a.Location)
            .Concat(restaurants.Select(r => r.Location))
            .Concat(stays.Select(s => s.Location))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(l => l)
            .ToList();

        var byLocation = new List<LocationReportDto>();
        var coverageGaps = new List<CoverageGapDto>();

        foreach (var loc in allLocations)
        {
            var locSlots = resolvedSlots.Where(s => s.Location.Equals(loc, StringComparison.OrdinalIgnoreCase)).ToList();
            var locActs = activities.Count(a => a.Location.Equals(loc, StringComparison.OrdinalIgnoreCase));
            var locRests = restaurants.Count(r => r.Location.Equals(loc, StringComparison.OrdinalIgnoreCase));
            var locStays = stays.Count(s => s.Location.Equals(loc, StringComparison.OrdinalIgnoreCase));

            var present = new List<string>();
            if (locActs > 0) present.Add("Experience");
            if (locRests > 0) present.Add("Restaurant");
            if (locStays > 0) present.Add("Accommodation");

            var missing = PlatformCategories.Except(present).ToList();
            var locAlerts = lowAlerts.Count(a => a.Location.Equals(loc, StringComparison.OrdinalIgnoreCase));

            byLocation.Add(new LocationReportDto
            {
                Location = loc,
                ListingCount = locActs + locRests + locStays,
                TotalCapacity = locSlots.Sum(s => s.TotalCapacity),
                LowStockAlerts = locAlerts,
                PresentCategories = present,
                MissingCategories = missing
            });

            if (missing.Count > 0)
            {
                coverageGaps.Add(new CoverageGapDto
                {
                    Location = loc,
                    PresentCategories = present,
                    MissingCategories = missing,
                    Description = $"{loc} has {string.Join(", ", present.Select(ToDisplayCategory))} but lacks {string.Join(", ", missing.Select(ToDisplayCategory))} listings."
                });
            }
        }

        // 7. Overall Summary (Exact Coherent Math)
        var totalCapAll = resolvedSlots.Sum(s => s.TotalCapacity);
        var bookedCapAll = resolvedSlots.Sum(s => s.BookedCapacity);
        var remCapAll = resolvedSlots.Sum(s => s.RemainingCapacity);
        var occupancy = totalCapAll > 0 ? Math.Round((double)bookedCapAll / totalCapAll * 100, 2) : 0.0;

        var summary = new ReportSummaryDto
        {
            TotalListings = activities.Count + restaurants.Count + stays.Count,
            ActiveListings = activities.Count + restaurants.Count + stays.Count,
            TotalCapacity = totalCapAll,
            BookedCapacity = bookedCapAll,
            RemainingCapacity = remCapAll,
            OccupancyRate = occupancy,
            LowAvailabilityCount = lowAlerts.Count(a => a.Status == "Low"),
            SoldOutCount = lowAlerts.Count(a => a.Status == "Sold Out")
        };

        return new InventoryReportResponse
        {
            Summary = summary,
            ByCategory = byCategory,
            ByLocation = byLocation,
            CoverageGaps = coverageGaps,
            LowAvailabilityAlerts = lowAlerts,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private static List<string> ParseTimeSlots(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string> { "09:00 - 12:00" };
        var list = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                      .Where(s => !string.IsNullOrWhiteSpace(s))
                      .ToList();
        return list.Count > 0 ? list : new List<string> { "09:00 - 12:00" };
    }

    private static bool IsDateInOperatingSchedule(DateOnly date, DateTime? validFrom, DateTime? validUntil, string? availableDays)
    {
        if (validFrom.HasValue && date < DateOnly.FromDateTime(validFrom.Value)) return false;
        if (validUntil.HasValue && date > DateOnly.FromDateTime(validUntil.Value)) return false;
        if (string.IsNullOrWhiteSpace(availableDays) || availableDays.Equals("Daily", StringComparison.OrdinalIgnoreCase)) return true;

        var dayName = date.DayOfWeek.ToString();
        var days = availableDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return days.Any(d => d.Equals(dayName, StringComparison.OrdinalIgnoreCase) || d.StartsWith(dayName[..3], StringComparison.OrdinalIgnoreCase));
    }

    private static string ToDisplayCategory(string canonical) => canonical switch
    {
        "Experience" => "Experiences",
        "Restaurant" => "Dining",
        "Accommodation" => "Stays",
        _ => canonical
    };

    private class ResolvedSlot
    {
        public Guid ListingId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ProviderBusinessName { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public string TimeSlot { get; set; } = string.Empty;
        public int TotalCapacity { get; set; }
        public int RemainingCapacity { get; set; }
        public int BookedCapacity { get; set; }
    }
}