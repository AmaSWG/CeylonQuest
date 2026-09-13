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

    // ── Test 1: Scenario 1 — Grouping by Category & Location ───────────────────
    [Fact]
    public async Task Scenario1_GenerateReport_GroupsByCategoryAndLocation()
    {
        var db = CreateInMemoryDbContext();
        var service = new InventoryReportService(db);

        var providerId = Guid.NewGuid();
        db.Providers.Add(new Provider { Id = providerId, BusinessName = "Lanka Trails", Email = "trails@example.com" });

        // Add 2 Experiences in Kandy
        db.ActivityListings.Add(new ActivityListing
        {
            Id = Guid.NewGuid(), ProviderId = providerId, Title = "Kandy Trek", Location = "Kandy", MaxParticipants = 10, IsActive = true
        });
        db.ActivityListings.Add(new ActivityListing
        {
            Id = Guid.NewGuid(), ProviderId = providerId, Title = "Temple Tour", Location = "Kandy", MaxParticipants = 15, IsActive = true
        });

        // Add 1 Restaurant in Galle
        db.RestaurantListings.Add(new RestaurantListing
        {
            Id = Guid.NewGuid(), ProviderId = providerId, Name = "Galle Seafood", Location = "Galle", SeatingCapacity = 30, IsActive = true
        });

        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await service.GenerateReportAsync(null, today, today.AddDays(7));

        Assert.Equal(3, report.Summary.TotalListings);
        Assert.Equal(2, report.ByCategory.First(c => c.Category == "Experience").ListingCount);
        Assert.Equal(1, report.ByCategory.First(c => c.Category == "Restaurant").ListingCount);

        var kandyLoc = report.ByLocation.First(l => l.Location == "Kandy");
        Assert.Equal(2, kandyLoc.ListingCount);
        Assert.Equal(25, kandyLoc.TotalCapacity);
    }

    // ── Test 2: Scenario 2 — Low Availability & Sold Out Highlights ────────────
    [Fact]
    public async Task Scenario2_LowAvailabilityHighlight_FlagsSoldOutAndCriticalSlots()
    {
        var db = CreateInMemoryDbContext();
        var service = new InventoryReportService(db);

        var providerId = Guid.NewGuid();
        db.Providers.Add(new Provider { Id = providerId, BusinessName = "Mirissa Waves", Email = "surf@example.com" });

        var actId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        db.ActivityListings.Add(new ActivityListing
        {
            Id = actId, ProviderId = providerId, Title = "Surf Lesson", Location = "Mirissa", MaxParticipants = 10, IsActive = true
        });

        // Add 1 sold-out slot (0 remaining)
        db.AvailabilitySlots.Add(new AvailabilitySlot
        {
            ListingId = actId, Date = date, TimeSlot = "08:00 AM - 10:00 AM", TotalCapacity = 10, RemainingCapacity = 0
        });

        // Add 1 critical slot (2 remaining <= 3)
        db.AvailabilitySlots.Add(new AvailabilitySlot
        {
            ListingId = actId, Date = date, TimeSlot = "02:00 PM - 04:00 PM", TotalCapacity = 10, RemainingCapacity = 2
        });

        await db.SaveChangesAsync();

        var report = await service.GenerateReportAsync(null, date, date);

        Assert.Equal(1, report.Summary.SoldOutCount);
        Assert.Equal(1, report.Summary.LowAvailabilityCount);
        Assert.Equal(2, report.LowAvailabilityAlerts.Count);
        Assert.Contains(report.LowAvailabilityAlerts, a => a.Status == "Sold Out");
        Assert.Contains(report.LowAvailabilityAlerts, a => a.Status == "Critical");
    }

    // ── Test 3: Scenario 3 — Filter by Location ────────────────────────────────
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
        var report = await service.GenerateReportAsync(null, today, today.AddDays(7), null, "Ella");

        Assert.Equal(1, report.Summary.TotalListings);
        Assert.Single(report.ByLocation);
        Assert.Equal("Ella", report.ByLocation[0].Location);
    }

    // ── Test 4: Scenario 3 — Filter by Category ────────────────────────────────
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
        var report = await service.GenerateReportAsync(null, today, today.AddDays(7), "Experience", null);

        Assert.Equal(1, report.Summary.TotalListings);
        Assert.Single(report.ByCategory);
        Assert.Equal("Experience", report.ByCategory[0].Category);
        Assert.Equal(20, report.ByCategory[0].TotalCapacity);
    }

    // ── Test 5: Provider Scoping (Data Isolation) ──────────────────────────────
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
        var report = await service.GenerateReportAsync(provA, today, today.AddDays(7));

        Assert.Equal(1, report.Summary.TotalListings);
        Assert.Equal(10, report.Summary.TotalCapacity);
    }

    // ── Test 6: Occupancy Rate Math & Calculation ──────────────────────────────
    [Fact]
    public async Task OccupancyRate_CalculatesAccuratelyBasedOnBookedSlots()
    {
        var db = CreateInMemoryDbContext();
        var service = new InventoryReportService(db);

        var provId = Guid.NewGuid();
        db.Providers.Add(new Provider { Id = provId, BusinessName = "Safari Co", Email = "safari@example.com" });

        var actId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        db.ActivityListings.Add(new ActivityListing { Id = actId, ProviderId = provId, Title = "Yala Safari", Location = "Yala", MaxParticipants = 10, IsActive = true });

        // 10 capacity, 6 remaining => 4 booked (40% occupancy)
        db.AvailabilitySlots.Add(new AvailabilitySlot
        {
            ListingId = actId, Date = date, TimeSlot = "06:00 AM - 09:00 AM", TotalCapacity = 10, RemainingCapacity = 6
        });
        await db.SaveChangesAsync();

        var report = await service.GenerateReportAsync(null, date, date);

        Assert.Equal(10, report.Summary.TotalCapacity);
        Assert.Equal(4, report.Summary.BookedCapacity);
        Assert.Equal(6, report.Summary.RemainingCapacity);
        Assert.Equal(40.0, report.Summary.OccupancyRate);
    }

    // ── Test 7: Regional Coverage Gaps Identification ──────────────────────────
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
        var report = await service.GenerateReportAsync(null, today, today.AddDays(7));

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
}