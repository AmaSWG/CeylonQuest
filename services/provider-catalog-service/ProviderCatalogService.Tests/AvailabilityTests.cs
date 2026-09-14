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

    /// <summary>
    /// Verifies that a provider setting slot capacity creates the record
    /// and the slot appears as bookable to visitors
    /// </summary>
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

    /// <summary>
    /// Verifies that zero or negative capacity values are rejected
    /// </summary>
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

    /// <summary>
    /// Verifies that a slot with no remaining capacity is reported as fully booked
    /// </summary>
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

    /// <summary>
    /// Verifies that canceling a booking restores the deducted capacity
    /// </summary>
    [Fact]
    public async Task Scenario4_BookingCanceled_RestoresCapacity()
    {
        var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);
        var listingId = Guid.NewGuid();

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            Title = "Scuba Diving",
            MaxParticipants = 6,
            TimeSlots = "09:00 AM - 11:00 AM",
            AvailableDays = "Daily",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var date = new DateOnly(2026, 9, 25);

        // Deduct 4 spots
        await service.DeductCapacityAsync(listingId, date, "09:00 AM - 11:00 AM", 4);
        var afterBooking = await service.GetAvailabilityForDateAsync(listingId, date);
        Assert.Equal(2, afterBooking!.Slots[0].RemainingCapacity);

        // Cancel 2 spots -> should restore back to 4
        await service.RestoreCapacityAsync(listingId, date, "09:00 AM - 11:00 AM", 2);
        var afterCancel = await service.GetAvailabilityForDateAsync(listingId, date);
        Assert.Equal(4, afterCancel!.Slots[0].RemainingCapacity);
    }

    /// <summary>
    /// Verifies that restoring capacity never exceeds the slot's total capacity
    /// </summary>
    [Fact]
    public async Task Scenario5_BookingCanceled_DoesNotExceedTotalCapacity()
    {
        var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);
        var listingId = Guid.NewGuid();

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            Title = "Surf Lesson",
            MaxParticipants = 5,
            TimeSlots = "08:00 AM - 10:00 AM",
            AvailableDays = "Daily",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var date = new DateOnly(2026, 9, 26);

        // Attempting to restore more capacity than total capacity caps at TotalCapacity
        await service.RestoreCapacityAsync(listingId, date, "08:00 AM - 10:00 AM", 10);
        var response = await service.GetAvailabilityForDateAsync(listingId, date);
        Assert.Equal(5, response!.Slots[0].RemainingCapacity);
    }

    /// <summary>
    /// Verifies that updating a booking restores capacity on the old slot
    /// and deducts it from the new slot
    /// </summary>
    [Fact]
    public async Task Scenario6_BookingUpdated_RebalancesCapacityBetweenSlots()
    {
        var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);
        var listingId = Guid.NewGuid();

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            Title = "Lagoon Safari",
            MaxParticipants = 10,
            TimeSlots = "09:00 AM - 11:00 AM, 02:00 PM - 04:00 PM",
            AvailableDays = "Daily",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var oldDate = new DateOnly(2026, 9, 27);
        var newDate = new DateOnly(2026, 9, 28);

        // Initial booking on oldDate for 4 spots
        await service.DeductCapacityAsync(listingId, oldDate, "09:00 AM - 11:00 AM", 4);

        // Update booking: Move from oldDate (4 guests) to newDate (6 guests)
        await service.UpdateCapacityAsync(
            listingId,
            oldDate, "09:00 AM - 11:00 AM", 4,
            newDate, "02:00 PM - 04:00 PM", 6);

        // Old date slot should be restored to 10
        var oldDateResp = await service.GetAvailabilityForDateAsync(listingId, oldDate);
        var oldSlot = oldDateResp!.Slots.Find(s => s.TimeSlot == "09:00 AM - 11:00 AM");
        Assert.Equal(10, oldSlot!.RemainingCapacity);

        // New date slot should have 4 remaining (10 - 6)
        var newDateResp = await service.GetAvailabilityForDateAsync(listingId, newDate);
        var newSlot = newDateResp!.Slots.Find(s => s.TimeSlot == "02:00 PM - 04:00 PM");
        Assert.Equal(4, newSlot!.RemainingCapacity);
    }
}