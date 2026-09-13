using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;
using ProviderCatalogService.Models;
using ProviderCatalogService.Services;
using Xunit;

namespace ProviderCatalogService.Tests;

public class InventoryReportTests
{
    private CatalogDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CatalogDbContext(options);
    }

    // ── Test 1: Grouping by Category & Location with Slot-Level Capacity ─────────
    [Fact]
    public async Task Scenario1_GenerateReport_GroupsByCategoryAndLocation()
    {
        var db = CreateInMemoryDbContext();
        var service = new InventoryReportService(db);

        var providerId = Guid.NewGuid();
        db.Providers.Add(new Provider { Id = providerId, BusinessName = "Lanka Trails", Email = "trails@example.com" });

        // Add 2 Experiences in Kandy (10 spots + 15 spots = 25 spots/day)
        db.ActivityListings.Add(new ActivityListing
        {
            Id = Guid.NewGuid(), ProviderId = providerId, Title = "Kandy Trek", Location = "Kandy", MaxParticipants = 10, IsActive = true
        });
        db.ActivityListings.Add(new ActivityListing
        {
            Id = Guid.NewGuid(), ProviderId = providerId, Title = "Temple Tour", Location = "Kandy", MaxParticipants = 15, IsActive = true
        });

        // Add 1 Restaurant in Galle (30 seats/day)
        db.RestaurantListings.Add(new RestaurantListing
        {
            Id = Guid.NewGuid(), ProviderId = providerId, Name = "Galle Seafood", Location = "Galle", SeatingCapacity = 30, IsActive = true
        });

        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // 1-day window (today to today)
        var report = await service.GenerateReportAsync(null, today, today);

        Assert.Equal(3, report.Summary.TotalListings);
        Assert.Equal(2, report.ByCategory.First(c => c.Category == "Experience").ListingCount);
        Assert.Equal(1, report.ByCategory.First(c => c.Category == "Restaurant").ListingCount);

        var kandyLoc = report.ByLocation.First(l => l.Location == "Kandy");
        Assert.Equal(2, kandyLoc.ListingCount);
        Assert.Equal(25, kandyLoc.TotalCapacity); // 10 + 15 on that day
    }

    // ── Test 2: Low Availability & Sold Out Highlights (Single Canonical Rule) ───
    [Fact]
    public async Task Scenario2_LowAvailabilityHighlight_FlagsSoldOutAndLowSlots()
    {
        var db = CreateInMemoryDbContext();
        var service = new InventoryReportService(db);

        var providerId = Guid.NewGuid();
        db.Providers.Add(new Provider { Id = providerId, BusinessName = "Mirissa Waves", Email = "surf@example.com" });

        var actId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        db.ActivityListings.Add(new ActivityListing
        {
            Id = actId, ProviderId = providerId, Title = "Surf Lesson", Location = "Mirissa", MaxParticipants = 10, TimeSlots = "08:00 - 10:00, 14:00 - 16:00", IsActive = true
        });

        // Add 1 sold-out slot (0 remaining)
        db.AvailabilitySlots.Add(new AvailabilitySlot
        {
            ListingId = actId, Date = date, TimeSlot = "08:00 - 10:00", TotalCapacity = 10, RemainingCapacity = 0
        });

        // Add 1 low-stock slot (2 remaining <= 3)
        db.AvailabilitySlots.Add(new AvailabilitySlot
        {
            ListingId = actId, Date = date, TimeSlot = "14:00 - 16:00", TotalCapacity = 10, RemainingCapacity = 2
        });

        await db.SaveChangesAsync();

        var report = await service.GenerateReportAsync(null, date, date);

        Assert.Equal(1, report.Summary.SoldOutCount);
        Assert.Equal(1, report.Summary.LowAvailabilityCount);
        Assert.Equal(2, report.LowAvailabilityAlerts.Count);
        Assert.Contains(report.LowAvailabilityAlerts, a => a.Status == "Sold Out");
        Assert.Contains(report.LowAvailabilityAlerts, a => a.Status == "Low");
    }

    // ── Test 3: Multi-Day Window Capacity & Occupancy Math ───────────────────────
    [Fact]
    public async Task MultiDayWindow_AggregatesCapacityAndOccupancyConsistently()
    {
        var db = CreateInMemoryDbContext();
        var service = new InventoryReportService(db);

        var providerId = Guid.NewGuid();
        db.Providers.Add(new Provider { Id = providerId, BusinessName = "Sigiriya Treks", Email = "sigiriya@example.com" });

        var actId = Guid.NewGuid();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = startDate.AddDays(2); // 3 days total (Day 0, Day 1, Day 2)

        // 10 spots per day
        db.ActivityListings.Add(new ActivityListing
        {
            Id = actId, ProviderId = providerId, Title = "Rock Fortress Climb", Location = "Sigiriya", MaxParticipants = 10, TimeSlots = "07:00 - 10:00", IsActive = true
        });

        // Day 1 has 4 booked spots (6 remaining)
        db.AvailabilitySlots.Add(new AvailabilitySlot
        {
            ListingId = actId, Date = startDate, TimeSlot = "07:00 - 10:00", TotalCapacity = 10, RemainingCapacity = 6
        });

        // Day 2 has 2 booked spots (8 remaining)
        db.AvailabilitySlots.Add(new AvailabilitySlot
        {
            ListingId = actId, Date = startDate.AddDays(1), TimeSlot = "07:00 - 10:00", TotalCapacity = 10, RemainingCapacity = 8
        });

        // Day 3 has 0 booked spots (10 remaining by default)

        await db.SaveChangesAsync();

        var report = await service.GenerateReportAsync(null, startDate, endDate);

        // 3 days * 10 spots/day = 30 total window capacity
        Assert.Equal(30, report.Summary.TotalCapacity);
        // 4 booked (Day 1) + 2 booked (Day 2) + 0 booked (Day 3) = 6 booked spots
        Assert.Equal(6, report.Summary.BookedCapacity);
        // 6 remaining (Day 1) + 8 remaining (Day 2) + 10 remaining (Day 3) = 24 remaining spots
        Assert.Equal(24, report.Summary.RemainingCapacity);
        // Occupancy = (6 / 30) * 100 = 20.0%
        Assert.Equal(20.0, report.Summary.OccupancyRate);
        Assert.Equal(report.Summary.TotalCapacity, report.Summary.BookedCapacity + report.Summary.RemainingCapacity);
    }

    // ── Test 4: Scenario 3 — Filter by Location ─────────────────────────────────
    [Fact]
    public async Task Scenario3_ApplyFilters_ByLocation_ReturnsOnlyMatchingLocation()
    {
        var db = CreateInMemoryDbContext();
        var service = new InventoryReportService(db);

        var provId = Guid.NewGuid();
        db.Providers.Add(new Provider { Id = provId, BusinessName = "Island Host", Email = "host@example.com" });

        db.ActivityListings.Add(new ActivityListing { Id = Guid.NewGuid(), ProviderId = provId, Title = "Ella Hike", Location = "Ella", MaxParticipants = 10, IsActive = true });
        db.ActivityListings.Add(new ActivityListing { Id = Guid.NewGuid(), ProviderId = provId, Title = "Galle Walk", Location = "Galle", MaxParticipants = 12, IsActive = true });
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await service.GenerateReportAsync(null, today, today, null, "Ella");

        Assert.Equal(1, report.Summary.TotalListings);
        Assert.Single(report.ByLocation);
        Assert.Equal("Ella", report.ByLocation[0].Location);
    }

    // ── Test 5: Scenario 3 — Filter by Category ─────────────────────────────────
    [Fact]
    public async Task Scenario3_ApplyFilters_ByCategory_ExcludesOtherCategories()
    {
        var db = CreateInMemoryDbContext();
        var service = new InventoryReportService(db);

        var provId = Guid.NewGuid();
        db.Providers.Add(new Provider { Id = provId, BusinessName = "Multi Service", Email = "multi@example.com" });

        db.ActivityListings.Add(new ActivityListing { Id = Guid.NewGuid(), ProviderId = provId, Title = "Whale Watching", Location = "Mirissa", MaxParticipants = 20, IsActive = true });
        db.RestaurantListings.Add(new RestaurantListing { Id = Guid.NewGuid(), ProviderId = provId, Name = "Ocean Grill", Location = "Mirissa", SeatingCapacity = 40, IsActive = true });
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await service.GenerateReportAsync(null, today, today, "Experience", null);

        Assert.Equal(1, report.Summary.TotalListings);
        Assert.Single(report.ByCategory);
        Assert.Equal("Experience", report.ByCategory[0].Category);
        Assert.Equal(20, report.ByCategory[0].TotalCapacity);
    }

    // ── Test 6: Provider Scoping (Data Isolation) ───────────────────────────────
    [Fact]
    public async Task ProviderScoping_OnlyReturnsListingsBelongingToSpecificProvider()
    {
        var db = CreateInMemoryDbContext();
        var service = new InventoryReportService(db);

        var provA = Guid.NewGuid();
        var provB = Guid.NewGuid();
        db.Providers.Add(new Provider { Id = provA, BusinessName = "Provider A", Email = "a@example.com" });
        db.Providers.Add(new Provider { Id = provB, BusinessName = "Provider B", Email = "b@example.com" });

        db.ActivityListings.Add(new ActivityListing { Id = Guid.NewGuid(), ProviderId = provA, Title = "Tour A", Location = "Kandy", MaxParticipants = 10, IsActive = true });
        db.ActivityListings.Add(new ActivityListing { Id = Guid.NewGuid(), ProviderId = provB, Title = "Tour B", Location = "Kandy", MaxParticipants = 15, IsActive = true });
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await service.GenerateReportAsync(provA, today, today);

        Assert.Equal(1, report.Summary.TotalListings);
        Assert.Equal(10, report.Summary.TotalCapacity);
    }

    // ── Test 7: Regional Coverage Gaps Identification ───────────────────────────
    [Fact]
    public async Task ServerSide_CoverageGaps_AccuratelyIdentifiesMissingCategories()
    {
        var db = CreateInMemoryDbContext();
        var service = new InventoryReportService(db);

        var providerId = Guid.NewGuid();
        db.Providers.Add(new Provider { Id = providerId, BusinessName = "Ella Stays", Email = "ella@example.com" });

        // Ella only has Accommodations
        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = Guid.NewGuid(), ProviderId = providerId, RoomType = "Mountain Villa", Location = "Ella", MaxGuests = 4, IsActive = true
        });
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await service.GenerateReportAsync(null, today, today);

        var ellaLoc = report.ByLocation.FirstOrDefault(l => l.Location == "Ella");
        Assert.NotNull(ellaLoc);
        Assert.Contains("Accommodation", ellaLoc.PresentCategories);
        Assert.Contains("Experience", ellaLoc.MissingCategories);
        Assert.Contains("Restaurant", ellaLoc.MissingCategories);

        var ellaGap = report.CoverageGaps.FirstOrDefault(g => g.Location == "Ella");
        Assert.NotNull(ellaGap);
        Assert.Contains("Accommodation", ellaGap.PresentCategories);
        Assert.Contains("Experience", ellaGap.MissingCategories);
        Assert.Contains("Restaurant", ellaGap.MissingCategories);
    }

    [Fact]
    public async Task NonOperatingDays_AreExcludedFromSlotsAndNotMarkedAsSoldOut()
    {
        var db = CreateInMemoryDbContext();
        var service = new InventoryReportService(db);

        var providerId = Guid.NewGuid();
        db.Providers.Add(new Provider { Id = providerId, BusinessName = "Weekend Tours", Email = "weekend@example.com" });

        // Listing operates ONLY on Sundays
        db.ActivityListings.Add(new ActivityListing
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            Title = "Sunday Cycling",
            Location = "Colombo",
            MaxParticipants = 10,
            AvailableDays = "Sunday", // Only 1 day a week
            IsActive = true
        });

        await db.SaveChangesAsync();

        // 7-day window from Monday to Sunday (contains 6 closed days and 1 operating Sunday)
        var monday = new DateOnly(2026, 9, 14); // Monday
        var sunday = new DateOnly(2026, 9, 20); // Sunday

        var report = await service.GenerateReportAsync(null, monday, sunday);

        // 1 operating day * 10 spots = 10 total window capacity (NOT 70)
        Assert.Equal(10, report.Summary.TotalCapacity);
        Assert.Equal(0, report.Summary.BookedCapacity);
        Assert.Equal(10, report.Summary.RemainingCapacity);

        // Closed days must NOT be marked as sold out or low stock
        Assert.Equal(0, report.Summary.SoldOutCount);
        Assert.Equal(0, report.Summary.LowAvailabilityCount);
        Assert.Empty(report.LowAvailabilityAlerts);
    }
}