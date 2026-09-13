using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;
using ProviderCatalogService.Services;
using Xunit;

namespace ProviderCatalogService.Tests;

public class AvailabilityTests
{
    private CatalogDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CatalogDbContext(options);
    }

    [Fact]
    public async Task Scenario1_SetAvailability_CreatesRecordAndShowsAsBookable()
    {
        var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);
        var listingId = Guid.NewGuid();

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            Title = "Kite Surfing Lesson",
            MaxParticipants = 8,
            TimeSlots = "09:00 AM - 11:00 AM",
            AvailableDays = "Daily",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var date = new DateOnly(2026, 9, 20);

        // Provider sets capacity to 12
        var success = await service.SetSlotCapacityAsync(listingId, date, "09:00 AM - 11:00 AM", 12);
        Assert.True(success);

        // Visitor queries availability
        var response = await service.GetAvailabilityForDateAsync(listingId, date);
        Assert.NotNull(response);
        Assert.True(response.IsOperatingDay);
        Assert.False(response.IsFullyBooked);
        Assert.Equal(12, response.Slots[0].TotalCapacity);
        Assert.Equal(12, response.Slots[0].RemainingCapacity);
    }

    [Fact]
    public async Task Scenario2_InvalidCapacity_ThrowsArgumentException()
    {
        var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);
        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 20);

        // Zero capacity rejected
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SetSlotCapacityAsync(listingId, date, "09:00 AM - 11:00 AM", 0));

        // Negative capacity rejected
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SetSlotCapacityAsync(listingId, date, "09:00 AM - 11:00 AM", -5));
    }

    [Fact]
    public async Task Scenario3_FullyBookedSlot_ShowsAsFullyBooked()
    {
        var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);
        var listingId = Guid.NewGuid();

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            Title = "Cooking Class",
            MaxParticipants = 4,
            TimeSlots = "10:00 AM - 12:00 PM",
            AvailableDays = "Daily",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var date = new DateOnly(2026, 9, 22);

        // Book all 4 participants
        await service.DeductCapacityAsync(listingId, date, "10:00 AM - 12:00 PM", 4);

        var response = await service.GetAvailabilityForDateAsync(listingId, date);
        Assert.NotNull(response);
        Assert.True(response.IsFullyBooked);
        Assert.Equal(0, response.Slots[0].RemainingCapacity);
        Assert.True(response.Slots[0].IsFullyBooked);
    }
}