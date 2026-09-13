using System;
using System.Collections.Generic;
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

        // 1. Base Queries
        var actQuery = _db.ActivityListings.Include(a => a.Provider).AsNoTracking().Where(a => a.IsActive);
        var restQuery = _db.RestaurantListings.Include(r => r.Provider).AsNoTracking().Where(r => r.IsActive);
        var stayQuery = _db.AccommodationListings.Include(ac => ac.Provider).AsNoTracking().Where(ac => ac.IsActive);

        if (providerId.HasValue)
        {
            actQuery = actQuery.Where(a => a.ProviderId == providerId.Value);
            restQuery = restQuery.Where(r => r.ProviderId == providerId.Value);
            stayQuery = stayQuery.Where(ac => ac.ProviderId == providerId.Value);
        }

        // Apply Location Filter
        if (!string.IsNullOrWhiteSpace(locFilter))
        {
            actQuery = actQuery.Where(a => EF.Functions.Like(a.Location, $"%{locFilter}%"));
            restQuery = restQuery.Where(r => EF.Functions.Like(r.Location, $"%{locFilter}%"));
            stayQuery = stayQuery.Where(ac => EF.Functions.Like(ac.Location, $"%{locFilter}%"));
        }

        // Apply Category Filter
        List<ActivityListing> activities = new();
        List<RestaurantListing> restaurants = new();
        List<AccommodationListing> stays = new();

        if (string.IsNullOrWhiteSpace(catFilter) || catFilter.Equals("Experience", StringComparison.OrdinalIgnoreCase))
        {
            activities = await actQuery.ToListAsync();
        }

        if (string.IsNullOrWhiteSpace(catFilter) || catFilter.Equals("Restaurant", StringComparison.OrdinalIgnoreCase))
        {
            restaurants = await restQuery.ToListAsync();
        }

        if (string.IsNullOrWhiteSpace(catFilter) || catFilter.Equals("Accommodation", StringComparison.OrdinalIgnoreCase))
        {
            stays = await stayQuery.ToListAsync();
        }

        // 2. Fetch Availability Slots in Date Range
        var allListingIds = activities.Select(a => a.Id)
            .Concat(restaurants.Select(r => r.Id))
            .Concat(stays.Select(s => s.Id))
            .ToHashSet();

        var slots = await _db.AvailabilitySlots
            .AsNoTracking()
            .Where(s => allListingIds.Contains(s.ListingId) && s.Date >= startDate && s.Date <= endDate)
            .ToListAsync();

        // 3. Compute Category Aggregations
        var byCategory = new List<CategoryReportDto>();

        if (string.IsNullOrWhiteSpace(catFilter) || catFilter.Equals("Experience", StringComparison.OrdinalIgnoreCase))
        {
            byCategory.Add(new CategoryReportDto
            {
                Category = "Experience",
                DisplayName = "Experiences",
                ListingCount = activities.Count,
                TotalCapacity = activities.Sum(a => a.MaxParticipants > 0 ? a.MaxParticipants : 10),
                RemainingCapacity = activities.Sum(a => GetRemainingForListing(a.Id, a.MaxParticipants > 0 ? a.MaxParticipants : 10, slots)),
                BookedCapacity = activities.Sum(a => GetBookedForListing(a.Id, a.MaxParticipants > 0 ? a.MaxParticipants : 10, slots))
            });
        }

        if (string.IsNullOrWhiteSpace(catFilter) || catFilter.Equals("Restaurant", StringComparison.OrdinalIgnoreCase))
        {
            byCategory.Add(new CategoryReportDto
            {
                Category = "Restaurant",
                DisplayName = "Dining",
                ListingCount = restaurants.Count,
                TotalCapacity = restaurants.Sum(r => r.SeatingCapacity > 0 ? r.SeatingCapacity : 20),
                RemainingCapacity = restaurants.Sum(r => GetRemainingForListing(r.Id, r.SeatingCapacity > 0 ? r.SeatingCapacity : 20, slots)),
                BookedCapacity = restaurants.Sum(r => GetBookedForListing(r.Id, r.SeatingCapacity > 0 ? r.SeatingCapacity : 20, slots))
            });
        }

        if (string.IsNullOrWhiteSpace(catFilter) || catFilter.Equals("Accommodation", StringComparison.OrdinalIgnoreCase))
        {
            byCategory.Add(new CategoryReportDto
            {
                Category = "Accommodation",
                DisplayName = "Stays",
                ListingCount = stays.Count,
                TotalCapacity = stays.Sum(s => s.MaxGuests > 0 ? s.MaxGuests : 2),
                RemainingCapacity = stays.Sum(s => GetRemainingForListing(s.Id, s.MaxGuests > 0 ? s.MaxGuests : 2, slots)),
                BookedCapacity = stays.Sum(s => GetBookedForListing(s.Id, s.MaxGuests > 0 ? s.MaxGuests : 2, slots))
            });
        }

        // 4. Compute Location Aggregations & Combined Coverage Gaps
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
            var locActs = activities.Where(a => a.Location.Equals(loc, StringComparison.OrdinalIgnoreCase)).ToList();
            var locRests = restaurants.Where(r => r.Location.Equals(loc, StringComparison.OrdinalIgnoreCase)).ToList();
            var locStays = stays.Where(s => s.Location.Equals(loc, StringComparison.OrdinalIgnoreCase)).ToList();

            var totalCap = locActs.Sum(a => a.MaxParticipants > 0 ? a.MaxParticipants : 10) +
                           locRests.Sum(r => r.SeatingCapacity > 0 ? r.SeatingCapacity : 20) +
                           locStays.Sum(s => s.MaxGuests > 0 ? s.MaxGuests : 2);

            var present = new List<string>();
            if (locActs.Count > 0) present.Add("Experience");
            if (locRests.Count > 0) present.Add("Restaurant");
            if (locStays.Count > 0) present.Add("Accommodation");

            var missing = PlatformCategories.Except(present).ToList();

            byLocation.Add(new LocationReportDto
            {
                Location = loc,
                ListingCount = locActs.Count + locRests.Count + locStays.Count,
                TotalCapacity = totalCap,
                LowStockAlerts = slots.Count(s => s.RemainingCapacity <= 3 && (locActs.Any(a => a.Id == s.ListingId) || locRests.Any(r => r.Id == s.ListingId) || locStays.Any(st => st.Id == s.ListingId))),
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

        // 5. Low Availability / Sold Out Alerts
        var lowAlerts = new List<LowAvailabilityItemDto>();
        foreach (var slot in slots)
        {
            var isSoldOut = slot.RemainingCapacity <= 0;
            var isLow = slot.RemainingCapacity <= 3 || (slot.TotalCapacity > 0 && slot.RemainingCapacity <= slot.TotalCapacity * 0.25);

            if (isSoldOut || isLow)
            {
                string title = "Listing";
                string cat = "Experience";
                string loc = "";
                string provName = "";

                var act = activities.FirstOrDefault(a => a.Id == slot.ListingId);
                if (act != null)
                {
                    title = act.Title; cat = "Experience"; loc = act.Location; provName = act.Provider?.BusinessName ?? "";
                }
                else
                {
                    var rest = restaurants.FirstOrDefault(r => r.Id == slot.ListingId);
                    if (rest != null)
                    {
                        title = rest.Name; cat = "Restaurant"; loc = rest.Location; provName = rest.Provider?.BusinessName ?? "";
                    }
                    else
                    {
                        var stay = stays.FirstOrDefault(s => s.Id == slot.ListingId);
                        if (stay != null)
                        {
                            title = stay.RoomType; cat = "Accommodation"; loc = stay.Location; provName = stay.Provider?.BusinessName ?? "";
                        }
                    }
                }

                lowAlerts.Add(new LowAvailabilityItemDto
                {
                    ListingId = slot.ListingId,
                    Title = title,
                    Category = cat,
                    Location = loc,
                    Date = slot.Date.ToString("yyyy-MM-dd"),
                    TimeSlot = slot.TimeSlot,
                    TotalCapacity = slot.TotalCapacity,
                    RemainingCapacity = slot.RemainingCapacity,
                    Status = isSoldOut ? "Sold Out" : "Critical",
                    ProviderBusinessName = provName
                });
            }
        }

        // 6. Overall Summary
        var totalCapAll = byCategory.Sum(c => c.TotalCapacity);
        var bookedCapAll = byCategory.Sum(c => c.BookedCapacity);
        var remCapAll = Math.Max(0, totalCapAll - bookedCapAll);
        var occupancy = totalCapAll > 0 ? Math.Round((double)bookedCapAll / totalCapAll * 100, 2) : 0.0;

        var summary = new ReportSummaryDto
        {
            TotalListings = activities.Count + restaurants.Count + stays.Count,
            ActiveListings = activities.Count(a => a.IsActive) + restaurants.Count(r => r.IsActive) + stays.Count(s => s.IsActive),
            TotalCapacity = totalCapAll,
            BookedCapacity = bookedCapAll,
            RemainingCapacity = remCapAll,
            OccupancyRate = occupancy,
            LowAvailabilityCount = lowAlerts.Count(a => a.Status == "Critical"),
            SoldOutCount = lowAlerts.Count(a => a.Status == "Sold Out")
        };

        return new InventoryReportResponse
        {
            Summary = summary,
            ByCategory = byCategory,
            ByLocation = byLocation,
            CoverageGaps = coverageGaps,
            LowAvailabilityAlerts = lowAlerts
        };
    }

    private static int GetRemainingForListing(Guid listingId, int defaultCap, List<AvailabilitySlot> slots)
    {
        var matched = slots.Where(s => s.ListingId == listingId).ToList();
        return matched.Count > 0 ? matched.Sum(s => s.RemainingCapacity) : defaultCap;
    }

    private static int GetBookedForListing(Guid listingId, int defaultCap, List<AvailabilitySlot> slots)
    {
        var matched = slots.Where(s => s.ListingId == listingId).ToList();
        return matched.Count > 0 ? matched.Sum(s => Math.Max(0, s.TotalCapacity - s.RemainingCapacity)) : 0;
    }

    private static string ToDisplayCategory(string canonical) => canonical switch
    {
        "Experience" => "Experiences",
        "Restaurant" => "Dining",
        "Accommodation" => "Stays",
        _ => canonical
    };
}