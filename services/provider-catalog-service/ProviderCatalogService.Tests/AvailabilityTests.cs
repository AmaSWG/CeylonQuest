using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;
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

<<<<<<< Updated upstream
    /// <summary>
    /// Verifies that a provider setting slot capacity creates the record
    /// and the slot appears as bookable to visitors
    /// </summary>
=======
    // ============================================================
    // 1. SET AVAILABILITY
    // ============================================================

>>>>>>> Stashed changes
    [Fact]
    public async Task Scenario1_SetAvailability_CreatesRecordAndShowsAsBookable()
    {
        using var db = CreateInMemoryDbContext();
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

        var success = await service.SetSlotCapacityAsync(
            listingId,
            date,
            "09:00 AM - 11:00 AM",
            12);

        Assert.True(success);

        var response =
            await service.GetAvailabilityForDateAsync(listingId, date);

        Assert.NotNull(response);
        Assert.True(response.IsOperatingDay);
        Assert.False(response.IsFullyBooked);
        Assert.Single(response.Slots);
        Assert.Equal(12, response.Slots[0].TotalCapacity);
        Assert.Equal(12, response.Slots[0].RemainingCapacity);
    }

    /// <summary>
    /// Verifies that zero or negative capacity values are rejected
    /// </summary>
    [Fact]
    public async Task Scenario2_InvalidCapacity_ThrowsArgumentException()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 20);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SetSlotCapacityAsync(
                listingId,
                date,
                "09:00 AM - 11:00 AM",
                0));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SetSlotCapacityAsync(
                listingId,
                date,
                "09:00 AM - 11:00 AM",
                -5));
    }

    /// <summary>
    /// Verifies that a slot with no remaining capacity is reported as fully booked
    /// </summary>
    [Fact]
    public async Task SetSlotCapacity_ExistingSlot_UpdatesCapacityCorrectly()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 20);

        db.AvailabilitySlots.Add(new AvailabilitySlot
        {
            ListingId = listingId,
            Date = date,
            TimeSlot = "10:00 AM",
            TotalCapacity = 10,
            RemainingCapacity = 6,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var result = await service.SetSlotCapacityAsync(
            listingId,
            date,
            "10:00 AM",
            20);

        Assert.True(result);

        var slot = await db.AvailabilitySlots.SingleAsync();

        Assert.Equal(20, slot.TotalCapacity);

        // 10 total - 6 remaining = 4 already booked
        // New capacity = 20, so remaining should be 16
        Assert.Equal(16, slot.RemainingCapacity);
    }

    [Fact]
    public async Task SetSlotCapacity_BelowAlreadyBookedCount_SetsRemainingToZero()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 20);

        db.AvailabilitySlots.Add(new AvailabilitySlot
        {
            ListingId = listingId,
            Date = date,
            TimeSlot = "10:00 AM",
            TotalCapacity = 10,
            RemainingCapacity = 2,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var result = await service.SetSlotCapacityAsync(
            listingId,
            date,
            "10:00 AM",
            5);

        Assert.True(result);

        var slot = await db.AvailabilitySlots.SingleAsync();

        Assert.Equal(5, slot.TotalCapacity);
        Assert.Equal(0, slot.RemainingCapacity);
    }

    // ============================================================
    // 2. ACTIVITY AVAILABILITY
    // ============================================================

    [Fact]
    public async Task Scenario3_FullyBookedSlot_ShowsAsFullyBooked()
    {
        using var db = CreateInMemoryDbContext();
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

        await service.DeductCapacityAsync(
            listingId,
            date,
            "10:00 AM - 12:00 PM",
            4);

        var response =
            await service.GetAvailabilityForDateAsync(listingId, date);

        Assert.NotNull(response);
        Assert.True(response.IsFullyBooked);
        Assert.Equal(0, response.Slots[0].RemainingCapacity);
        Assert.True(response.Slots[0].IsFullyBooked);
    }

    /// <summary>
    /// Verifies that canceling a booking restores the deducted capacity
    /// </summary>
    [Fact]
<<<<<<< Updated upstream
    public async Task Scenario4_BookingCanceled_RestoresCapacity()
    {
        var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);
=======
    public async Task Activity_DefaultTimeSlots_AreReturned_WhenTimeSlotsMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

>>>>>>> Stashed changes
        var listingId = Guid.NewGuid();

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
<<<<<<< Updated upstream
            Title = "Scuba Diving",
            MaxParticipants = 6,
=======
            Title = "Default Time Activity",
            MaxParticipants = 5,
            TimeSlots = "[]",
            AvailableDays = "Daily",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var response = await service.GetAvailabilityForDateAsync(
            listingId,
            new DateOnly(2026, 9, 20));

        Assert.NotNull(response);
        Assert.Equal(2, response.Slots.Count);

        Assert.Contains(
            response.Slots,
            s => s.TimeSlot == "09:00 AM - 11:00 AM");

        Assert.Contains(
            response.Slots,
            s => s.TimeSlot == "01:00 PM - 03:00 PM");
    }

    [Fact]
    public async Task Activity_DefaultCapacity_IsTen_WhenMaxParticipantsIsZero()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            Title = "Activity",
            MaxParticipants = 0,
>>>>>>> Stashed changes
            TimeSlots = "09:00 AM - 11:00 AM",
            AvailableDays = "Daily",
            IsActive = true
        });
<<<<<<< Updated upstream
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
=======

        await db.SaveChangesAsync();

        var response = await service.GetAvailabilityForDateAsync(
            listingId,
            new DateOnly(2026, 9, 20));

        Assert.NotNull(response);
        Assert.Equal(10, response.Slots[0].TotalCapacity);
        Assert.Equal(10, response.Slots[0].RemainingCapacity);
    }

    [Fact]
    public async Task Activity_BeforeValidFrom_IsNotOperating()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            Title = "Future Activity",
            MaxParticipants = 10,
            TimeSlots = "09:00 AM - 11:00 AM",
            AvailableDays = "Daily",
            ValidFrom = new DateTime(2026, 10, 1),
            IsActive = true
        });

        await db.SaveChangesAsync();

        var response = await service.GetAvailabilityForDateAsync(
            listingId,
            new DateOnly(2026, 9, 20));

        Assert.NotNull(response);
        Assert.False(response.IsOperatingDay);
        Assert.True(response.IsFullyBooked);
        Assert.Equal(0, response.Slots[0].RemainingCapacity);
    }

    [Fact]
    public async Task Activity_AfterValidUntil_IsNotOperating()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            Title = "Expired Activity",
            MaxParticipants = 10,
            TimeSlots = "09:00 AM - 11:00 AM",
            AvailableDays = "Daily",
            ValidUntil = new DateTime(2026, 9, 1),
            IsActive = true
        });

        await db.SaveChangesAsync();

        var response = await service.GetAvailabilityForDateAsync(
            listingId,
            new DateOnly(2026, 9, 20));

        Assert.NotNull(response);
        Assert.False(response.IsOperatingDay);
        Assert.True(response.IsFullyBooked);
    }

    [Fact]
    public async Task Activity_WeekdaySchedule_OperatesOnWeekday()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();

        // 2026-09-21 is Monday
        var date = new DateOnly(2026, 9, 21);

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            Title = "Weekday Activity",
            MaxParticipants = 10,
            TimeSlots = "09:00 AM - 11:00 AM",
            AvailableDays = "Weekdays",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var response =
            await service.GetAvailabilityForDateAsync(listingId, date);

        Assert.NotNull(response);
        Assert.True(response.IsOperatingDay);
    }

    [Fact]
    public async Task Activity_WeekendSchedule_OperatesOnWeekend()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();

        // 2026-09-20 is Sunday
        var date = new DateOnly(2026, 9, 20);

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            Title = "Weekend Activity",
            MaxParticipants = 10,
            TimeSlots = "09:00 AM - 11:00 AM",
            AvailableDays = "Weekends",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var response =
            await service.GetAvailabilityForDateAsync(listingId, date);

        Assert.NotNull(response);
        Assert.True(response.IsOperatingDay);
    }

    [Fact]
    public async Task Activity_SpecificDaySchedule_OperatesOnMatchingDay()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 20);

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            Title = "Sunday Activity",
            MaxParticipants = 10,
            TimeSlots = "09:00 AM - 11:00 AM",
            AvailableDays = "Sunday",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var response =
            await service.GetAvailabilityForDateAsync(listingId, date);

        Assert.NotNull(response);
        Assert.True(response.IsOperatingDay);
    }

    [Fact]
    public async Task Activity_NonMatchingDay_IsNotOperating()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();

        // Sunday
        var date = new DateOnly(2026, 9, 20);

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            Title = "Monday Only Activity",
            MaxParticipants = 10,
            TimeSlots = "09:00 AM - 11:00 AM",
            AvailableDays = "Monday",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var response =
            await service.GetAvailabilityForDateAsync(listingId, date);

        Assert.NotNull(response);
        Assert.False(response.IsOperatingDay);
        Assert.True(response.IsFullyBooked);
    }

    // ============================================================
    // 3. RESTAURANT AVAILABILITY
    // ============================================================

    [Fact]
    public async Task RestaurantAvailability_ReturnsConfiguredCapacity()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();

        db.RestaurantListings.Add(new RestaurantListing
        {
            Id = listingId,
            Name = "Ocean Restaurant",
            OpeningHours = "06:00 PM - 10:00 PM",
            SeatingCapacity = 30,
            IsActive = true
        });

        await db.SaveChangesAsync();

        var response = await service.GetAvailabilityForDateAsync(
            listingId,
            new DateOnly(2026, 9, 20));

        Assert.NotNull(response);
        Assert.True(response.IsOperatingDay);
        Assert.False(response.IsFullyBooked);

        Assert.Single(response.Slots);
        Assert.Equal(30, response.Slots[0].TotalCapacity);
        Assert.Equal(30, response.Slots[0].RemainingCapacity);
        Assert.Equal("06:00 PM - 10:00 PM", response.Slots[0].TimeSlot);
    }

    [Fact]
    public async Task Restaurant_DefaultCapacityAndOpeningHours_AreUsed()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();

        db.RestaurantListings.Add(new RestaurantListing
        {
            Id = listingId,
            Name = "Small Cafe",
            OpeningHours = "",
            SeatingCapacity = 0,
            IsActive = true
        });

        await db.SaveChangesAsync();

        var response = await service.GetAvailabilityForDateAsync(
            listingId,
            new DateOnly(2026, 9, 20));

        Assert.NotNull(response);

        Assert.Equal(
            "11:30 AM - 10:00 PM",
            response.Slots[0].TimeSlot);

        Assert.Equal(20, response.Slots[0].TotalCapacity);
        Assert.Equal(20, response.Slots[0].RemainingCapacity);
    }

    [Fact]
    public async Task Restaurant_ExistingAvailabilityOverride_IsUsed()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 20);

        db.RestaurantListings.Add(new RestaurantListing
        {
            Id = listingId,
            Name = "Dinner Restaurant",
            OpeningHours = "06:00 PM - 10:00 PM",
            SeatingCapacity = 30,
            IsActive = true
        });

        db.AvailabilitySlots.Add(new AvailabilitySlot
        {
            ListingId = listingId,
            Date = date,
            TimeSlot = "06:00 PM - 10:00 PM",
            TotalCapacity = 25,
            RemainingCapacity = 8,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var response =
            await service.GetAvailabilityForDateAsync(listingId, date);

        Assert.NotNull(response);
        Assert.Equal(25, response.Slots[0].TotalCapacity);
        Assert.Equal(8, response.Slots[0].RemainingCapacity);
    }

    // ============================================================
    // 4. ACCOMMODATION AVAILABILITY
    // ============================================================

    [Fact]
    public async Task AccommodationAvailability_ReturnsStaySlot()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();

        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = listingId,
            RoomType = "Luxury Villa",
            MinStayNights = 2,
            IsActive = true
        });

        await db.SaveChangesAsync();

        var response = await service.GetAvailabilityForDateAsync(
            listingId,
            new DateOnly(2026, 9, 20));

        Assert.NotNull(response);
        Assert.True(response.IsOperatingDay);
        Assert.False(response.IsFullyBooked);

        Assert.Equal(
            "Stay (Min 2 Nights)",
            response.Slots[0].TimeSlot);

        Assert.Equal(1, response.Slots[0].TotalCapacity);
        Assert.Equal(1, response.Slots[0].RemainingCapacity);
    }

    [Fact]
    public async Task Accommodation_OneNight_UsesSingularNightText()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();

        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = listingId,
            RoomType = "Single Night Room",
            MinStayNights = 1,
            IsActive = true
        });

        await db.SaveChangesAsync();

        var response = await service.GetAvailabilityForDateAsync(
            listingId,
            new DateOnly(2026, 9, 20));

        Assert.NotNull(response);

        Assert.Equal(
            "Stay (Min 1 Night)",
            response.Slots[0].TimeSlot);
    }

    [Fact]
    public async Task Accommodation_ExistingOverride_IsUsed()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 20);

        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = listingId,
            RoomType = "Villa",
            MinStayNights = 1,
            IsActive = true
        });

        db.AvailabilitySlots.Add(new AvailabilitySlot
        {
            ListingId = listingId,
            Date = date,
            TimeSlot = "Stay (Min 1 Night)",
            TotalCapacity = 5,
            RemainingCapacity = 3,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var response =
            await service.GetAvailabilityForDateAsync(listingId, date);

        Assert.NotNull(response);
        Assert.Equal(5, response.Slots[0].TotalCapacity);
        Assert.Equal(3, response.Slots[0].RemainingCapacity);
    }

    // ============================================================
    // 5. DEDUCT CAPACITY
    // ============================================================

    [Fact]
    public async Task DeductCapacity_Activity_UsesMaxParticipants()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 20);

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            Title = "Safari",
            MaxParticipants = 8,
            IsActive = true
        });

        await db.SaveChangesAsync();

        var result = await service.DeductCapacityAsync(
            listingId,
            date,
            "09:00 AM",
            3);

        Assert.True(result);

        var slot = await db.AvailabilitySlots.SingleAsync();

        Assert.Equal(8, slot.TotalCapacity);
        Assert.Equal(5, slot.RemainingCapacity);
    }

    [Fact]
    public async Task DeductCapacity_ActivityWithZeroMaxParticipants_DefaultsToTen()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 20);

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            Title = "Activity",
            MaxParticipants = 0,
            IsActive = true
        });

        await db.SaveChangesAsync();

        await service.DeductCapacityAsync(
            listingId,
            date,
            "Morning",
            2);

        var slot = await db.AvailabilitySlots.SingleAsync();

        Assert.Equal(10, slot.TotalCapacity);
        Assert.Equal(8, slot.RemainingCapacity);
    }

    [Fact]
    public async Task DeductCapacity_Restaurant_UsesSeatingCapacity()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 20);

        db.RestaurantListings.Add(new RestaurantListing
        {
            Id = listingId,
            Name = "Restaurant",
            SeatingCapacity = 20,
            IsActive = true
        });

        await db.SaveChangesAsync();

        await service.DeductCapacityAsync(
            listingId,
            date,
            "Dinner",
            5);

        var slot = await db.AvailabilitySlots.SingleAsync();

        Assert.Equal(20, slot.TotalCapacity);
        Assert.Equal(15, slot.RemainingCapacity);
    }

    [Fact]
    public async Task DeductCapacity_RestaurantWithZeroCapacity_DefaultsToTwenty()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 20);

        db.RestaurantListings.Add(new RestaurantListing
        {
            Id = listingId,
            Name = "Restaurant",
            SeatingCapacity = 0,
            IsActive = true
        });

        await db.SaveChangesAsync();

        await service.DeductCapacityAsync(
            listingId,
            date,
            "Dinner",
            4);

        var slot = await db.AvailabilitySlots.SingleAsync();

        Assert.Equal(20, slot.TotalCapacity);
        Assert.Equal(16, slot.RemainingCapacity);
    }

    [Fact]
    public async Task DeductCapacity_Accommodation_DefaultsToOne()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 20);

        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = listingId,
            RoomType = "Villa",
            IsActive = true
        });

        await db.SaveChangesAsync();

        await service.DeductCapacityAsync(
            listingId,
            date,
            "Stay",
            1);

        var slot = await db.AvailabilitySlots.SingleAsync();

        Assert.Equal(1, slot.TotalCapacity);
        Assert.Equal(0, slot.RemainingCapacity);
    }

    [Fact]
    public async Task DeductCapacity_UnknownListing_DefaultsToOne()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 20);

        await service.DeductCapacityAsync(
            listingId,
            date,
            "Unknown",
            1);

        var slot = await db.AvailabilitySlots.SingleAsync();

        Assert.Equal(1, slot.TotalCapacity);
        Assert.Equal(0, slot.RemainingCapacity);
    }

    [Fact]
    public async Task DeductCapacity_ExistingSlot_DoesNotGoBelowZero()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 20);

        db.AvailabilitySlots.Add(new AvailabilitySlot
        {
            ListingId = listingId,
            Date = date,
            TimeSlot = "Morning",
            TotalCapacity = 5,
            RemainingCapacity = 2,
            UpdatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var result = await service.DeductCapacityAsync(
            listingId,
            date,
            "Morning",
            10);

        Assert.True(result);

        var slot = await db.AvailabilitySlots.SingleAsync();

        Assert.Equal(0, slot.RemainingCapacity);
    }

    // ============================================================
    // 6. UNKNOWN / INACTIVE LISTINGS
    // ============================================================

    [Fact]
    public async Task GetAvailability_UnknownListing_ReturnsNull()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var response = await service.GetAvailabilityForDateAsync(
            Guid.NewGuid(),
            new DateOnly(2026, 9, 20));

        Assert.Null(response);
    }

    [Fact]
    public async Task GetAvailability_InactiveActivity_ReturnsNull()
    {
        using var db = CreateInMemoryDbContext();
        var service = new AvailabilityService(db);

        var listingId = Guid.NewGuid();

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            Title = "Inactive Tour",
            MaxParticipants = 5,
            IsActive = false
        });

        await db.SaveChangesAsync();

        var response = await service.GetAvailabilityForDateAsync(
            listingId,
            new DateOnly(2026, 9, 20));

        Assert.Null(response);
>>>>>>> Stashed changes
    }
}