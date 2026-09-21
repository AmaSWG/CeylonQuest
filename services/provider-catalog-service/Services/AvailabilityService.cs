using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;

namespace ProviderCatalogService.Services;

public class AvailabilityService
{
    private readonly CatalogDbContext _db;

    public AvailabilityService(CatalogDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Retrieves the availability for a listing on a specific date,
    /// resolving the listing as an activity, restaurant, or accommodation.
    /// </summary>
    public async Task<ListingDateAvailabilityResponse?> GetAvailabilityForDateAsync(
        Guid listingId,
        DateOnly date)
    {
        // Try Activity Listing
        var activity = await _db.ActivityListings
            .AsNoTracking()
            .FirstOrDefaultAsync(a =>
                a.Id == listingId &&
                a.IsActive);

        if (activity != null)
        {
            return await BuildActivityAvailabilityAsync(
                activity,
                date);
        }

        // Try Restaurant Listing
        var restaurant = await _db.RestaurantListings
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.Id == listingId &&
                r.IsActive);

        if (restaurant != null)
        {
            return await BuildRestaurantAvailabilityAsync(
                restaurant,
                date);
        }

        // Try Accommodation Listing
        var accommodation = await _db.AccommodationListings
            .AsNoTracking()
            .FirstOrDefaultAsync(ac =>
                ac.Id == listingId &&
                ac.IsActive);

        if (accommodation != null)
        {
            return await BuildAccommodationAvailabilityAsync(
                accommodation,
                date);
        }

        return null;
    }

    /// <summary>
    /// Sets or updates the total capacity for a specific availability
    /// time slot, preventing reductions below the already booked count.
    /// </summary>
    public async Task<bool> SetSlotCapacityAsync(
        Guid listingId,
        DateOnly date,
        string timeSlot,
        int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentException(
                "Capacity must be greater than 0.");
        }

        var existing = await _db.AvailabilitySlots
            .FirstOrDefaultAsync(s =>
                s.ListingId == listingId &&
                s.Date == date &&
                s.TimeSlot == timeSlot);

        if (existing != null)
        {
            var bookedCount =
                existing.TotalCapacity -
                existing.RemainingCapacity;

            if (capacity < bookedCount)
            {
                throw new InvalidOperationException(
                    $"Cannot reduce capacity to {capacity} because " +
                    $"{bookedCount} spots are already booked.");
            }

            existing.TotalCapacity = capacity;
            existing.RemainingCapacity =
                capacity - bookedCount;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.AvailabilitySlots.Add(
                new AvailabilitySlot
                {
                    ListingId = listingId,
                    Date = date,
                    TimeSlot = timeSlot,
                    TotalCapacity = capacity,
                    RemainingCapacity = capacity,
                    UpdatedAt = DateTime.UtcNow
                });
        }

        await _db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Deducts the requested guest count from a slot's remaining capacity,
    /// creating the slot with a sensible default capacity if it does not exist.
    ///
    /// This method is retained for existing functionality.
    /// New booking creation should use ReserveCapacityAsync.
    /// </summary>
    public async Task<bool> DeductCapacityAsync(
        Guid listingId,
        DateOnly date,
        string timeSlot,
        int guestCount)
    {
        if (guestCount <= 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(timeSlot))
        {
            return false;
        }

        var existing = await _db.AvailabilitySlots
            .FirstOrDefaultAsync(s =>
                s.ListingId == listingId &&
                s.Date == date &&
                s.TimeSlot == timeSlot);

        if (existing == null)
        {
            int defaultCapacity = 10;

            var act = await _db.ActivityListings
                .FirstOrDefaultAsync(a =>
                    a.Id == listingId);

            if (act != null)
            {
                defaultCapacity =
                    act.MaxParticipants > 0
                        ? act.MaxParticipants
                        : 10;
            }
            else
            {
                var rest = await _db.RestaurantListings
                    .FirstOrDefaultAsync(r =>
                        r.Id == listingId);

                if (rest != null)
                {
                    defaultCapacity =
                        rest.SeatingCapacity > 0
                            ? rest.SeatingCapacity
                            : 20;
                }
                else
                {
                    defaultCapacity = 1;
                }
            }

            existing = new AvailabilitySlot
            {
                ListingId = listingId,
                Date = date,
                TimeSlot = timeSlot,
                TotalCapacity = defaultCapacity,
                RemainingCapacity = defaultCapacity,
                UpdatedAt = DateTime.UtcNow
            };

            _db.AvailabilitySlots.Add(existing);
        }

        existing.RemainingCapacity =
            Math.Max(
                0,
                existing.RemainingCapacity - guestCount);

        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Attempts to reserve capacity before a booking is created.
    ///
    /// For an existing availability row, capacity is reduced using
    /// one atomic database UPDATE with a RemainingCapacity condition.
    /// This prevents simultaneous requests from overbooking a slot.
    ///
    /// If no availability row exists yet, the default capacity is
    /// determined from the listing and a row is created.
    /// </summary>
    public async Task<bool> ReserveCapacityAsync(
        Guid listingId,
        DateOnly date,
        string timeSlot,
        int guestCount)
    {
        if (listingId == Guid.Empty)
        {
            return false;
        }

        if (guestCount <= 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(timeSlot))
        {
            return false;
        }

        timeSlot = timeSlot.Trim();

        // First attempt an atomic conditional update.
        //
        // SQL conceptually becomes:
        //
        // UPDATE AvailabilitySlots
        // SET RemainingCapacity = RemainingCapacity - guestCount
        // WHERE ListingId = ...
        //   AND Date = ...
        //   AND TimeSlot = ...
        //   AND RemainingCapacity >= guestCount;
        //
        // Only one concurrent request can consume the final
        // remaining capacity successfully.
        var affectedRows = await _db.AvailabilitySlots
            .Where(s =>
                s.ListingId == listingId &&
                s.Date == date &&
                s.TimeSlot == timeSlot &&
                s.RemainingCapacity >= guestCount)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    s => s.RemainingCapacity,
                    s => s.RemainingCapacity - guestCount)
                .SetProperty(
                    s => s.UpdatedAt,
                    DateTime.UtcNow));

        if (affectedRows == 1)
        {
            return true;
        }

        // If a row exists but the conditional update changed
        // zero rows, there is not enough remaining capacity.
        var slotExists = await _db.AvailabilitySlots
            .AsNoTracking()
            .AnyAsync(s =>
                s.ListingId == listingId &&
                s.Date == date &&
                s.TimeSlot == timeSlot);

        if (slotExists)
        {
            return false;
        }

        // No explicit AvailabilitySlot exists.
        // Determine the default capacity from the listing.
        int defaultCapacity;
        string listingType;

        var activity = await _db.ActivityListings
            .AsNoTracking()
            .FirstOrDefaultAsync(a =>
                a.Id == listingId &&
                a.IsActive);

        if (activity != null)
        {
            defaultCapacity =
                activity.MaxParticipants > 0
                    ? activity.MaxParticipants
                    : 10;

            listingType = "Experience";
        }
        else
        {
            var restaurant = await _db.RestaurantListings
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.Id == listingId &&
                    r.IsActive);

            if (restaurant != null)
            {
                defaultCapacity =
                    restaurant.SeatingCapacity > 0
                        ? restaurant.SeatingCapacity
                        : 20;

                listingType = "Restaurant";
            }
            else
            {
                var accommodation =
                    await _db.AccommodationListings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(ac =>
                            ac.Id == listingId &&
                            ac.IsActive);

                if (accommodation == null)
                {
                    return false;
                }

                defaultCapacity = 1;
                listingType = "Accommodation";
            }
        }

        // The default capacity itself is not sufficient.
        if (defaultCapacity < guestCount)
        {
            return false;
        }

        // Create the first persisted slot.
        //
        // CatalogDbContext already has a unique index on:
        // ListingId + Date + TimeSlot
        //
        // Therefore two concurrent requests cannot create
        // duplicate availability rows for the same slot.
        var newSlot = new AvailabilitySlot
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            ListingType = listingType,
            Date = date,
            TimeSlot = timeSlot,
            TotalCapacity = defaultCapacity,
            RemainingCapacity =
                defaultCapacity - guestCount,
            UpdatedAt = DateTime.UtcNow
        };

        _db.AvailabilitySlots.Add(newSlot);

        try
        {
            await _db.SaveChangesAsync();

            return true;
        }
        catch (DbUpdateException)
        {
            // A concurrent request may have inserted the same
            // ListingId + Date + TimeSlot after our existence check.
            //
            // Detach our failed entity and retry using the atomic
            // conditional update against the row that now exists.
            _db.Entry(newSlot).State =
                EntityState.Detached;

            affectedRows = await _db.AvailabilitySlots
                .Where(s =>
                    s.ListingId == listingId &&
                    s.Date == date &&
                    s.TimeSlot == timeSlot &&
                    s.RemainingCapacity >= guestCount)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        s => s.RemainingCapacity,
                        s => s.RemainingCapacity - guestCount)
                    .SetProperty(
                        s => s.UpdatedAt,
                        DateTime.UtcNow));

            return affectedRows == 1;
        }
    }

    /// <summary>
    /// Builds the availability response for an activity listing,
    /// applying the operating schedule and merging any per-slot overrides.
    /// </summary>
    private async Task<ListingDateAvailabilityResponse>
        BuildActivityAvailabilityAsync(
            ActivityListing activity,
            DateOnly date)
    {
        var isOperating =
            IsDateInOperatingSchedule(
                date,
                activity.ValidFrom,
                activity.ValidUntil,
                activity.AvailableDays);

        var slots =
            ParseTimeSlots(activity.TimeSlots);

        var defaultCap =
            activity.MaxParticipants > 0
                ? activity.MaxParticipants
                : 10;

        var existingOverrides =
            await _db.AvailabilitySlots
                .AsNoTracking()
                .Where(s =>
                    s.ListingId == activity.Id &&
                    s.Date == date)
                .ToListAsync();

        var slotDtos =
            new List<SlotAvailabilityDto>();

        foreach (var slot in slots)
        {
            var matched =
                existingOverrides.FirstOrDefault(
                    o => o.TimeSlot.Equals(
                        slot,
                        StringComparison.OrdinalIgnoreCase));

            var total =
                matched?.TotalCapacity ??
                defaultCap;

            var remaining =
                matched?.RemainingCapacity ??
                defaultCap;

            slotDtos.Add(
                new SlotAvailabilityDto
                {
                    TimeSlot = slot,
                    TotalCapacity = total,
                    RemainingCapacity =
                        isOperating
                            ? remaining
                            : 0
                });
        }

        return new ListingDateAvailabilityResponse
        {
            ListingId = activity.Id,
            Date = date.ToString("yyyy-MM-dd"),
            IsOperatingDay = isOperating,

            IsFullyBooked =
                !isOperating ||
                slotDtos.All(s =>
                    s.IsFullyBooked),

            ValidFrom =
                activity.ValidFrom?
                    .ToString("yyyy-MM-dd"),

            ValidUntil =
                activity.ValidUntil?
                    .ToString("yyyy-MM-dd"),

            AvailableDays =
                string.IsNullOrWhiteSpace(
                    activity.AvailableDays)
                    ? "Daily"
                    : activity.AvailableDays,

            TimeSlots = activity.TimeSlots,
            Slots = slotDtos
        };
    }

    /// <summary>
    /// Builds the availability response for a restaurant listing
    /// using its opening hours as the slot and its seating capacity
    /// as the default.
    /// </summary>
    private async Task<ListingDateAvailabilityResponse>
        BuildRestaurantAvailabilityAsync(
            RestaurantListing rest,
            DateOnly date)
    {
        var isOperating = true;

        var slots = new List<string>
        {
            string.IsNullOrWhiteSpace(
                rest.OpeningHours)
                ? "11:30 AM - 10:00 PM"
                : rest.OpeningHours
        };

        var defaultCap =
            rest.SeatingCapacity > 0
                ? rest.SeatingCapacity
                : 20;

        var existingOverrides =
            await _db.AvailabilitySlots
                .AsNoTracking()
                .Where(s =>
                    s.ListingId == rest.Id &&
                    s.Date == date)
                .ToListAsync();

        var slotDtos =
            new List<SlotAvailabilityDto>();

        foreach (var slot in slots)
        {
            var matched =
                existingOverrides.FirstOrDefault(
                    o => o.TimeSlot.Equals(
                        slot,
                        StringComparison.OrdinalIgnoreCase));

            var total =
                matched?.TotalCapacity ??
                defaultCap;

            var remaining =
                matched?.RemainingCapacity ??
                defaultCap;

            slotDtos.Add(
                new SlotAvailabilityDto
                {
                    TimeSlot = slot,
                    TotalCapacity = total,
                    RemainingCapacity = remaining
                });
        }

        return new ListingDateAvailabilityResponse
        {
            ListingId = rest.Id,
            Date = date.ToString("yyyy-MM-dd"),
            IsOperatingDay = isOperating,

            IsFullyBooked =
                slotDtos.All(s =>
                    s.IsFullyBooked),

            ValidFrom = null,
            ValidUntil = null,
            AvailableDays = "Daily",
            TimeSlots = rest.OpeningHours,
            Slots = slotDtos
        };
    }

    /// <summary>
    /// Builds the availability response for an accommodation listing,
    /// treating each night as a single-unit booking slot.
    /// </summary>
    private async Task<ListingDateAvailabilityResponse>
        BuildAccommodationAvailabilityAsync(
            AccommodationListing ac,
            DateOnly date)
    {
        var slotName =
            $"Stay (Min {ac.MinStayNights} " +
            $"Night{(ac.MinStayNights > 1 ? "s" : "")})";

        const int defaultCap = 1;

        var existingOverride =
            await _db.AvailabilitySlots
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.ListingId == ac.Id &&
                    s.Date == date &&
                    s.TimeSlot == slotName);

        var total =
            existingOverride?.TotalCapacity ??
            defaultCap;

        var remaining =
            existingOverride?.RemainingCapacity ??
            defaultCap;

        var slotDtos =
            new List<SlotAvailabilityDto>
            {
                new SlotAvailabilityDto
                {
                    TimeSlot = slotName,
                    TotalCapacity = total,
                    RemainingCapacity = remaining
                }
            };

        return new ListingDateAvailabilityResponse
        {
            ListingId = ac.Id,
            Date = date.ToString("yyyy-MM-dd"),
            IsOperatingDay = true,

            IsFullyBooked =
                slotDtos.All(s =>
                    s.IsFullyBooked),

            ValidFrom = null,
            ValidUntil = null,
            AvailableDays = "Daily",

            TimeSlots =
                $"Min {ac.MinStayNights} Night Stay",

            Slots = slotDtos
        };
    }

    /// <summary>
    /// Determines whether a given date falls within the listing's
    /// validity window and matches its configured available days.
    /// </summary>
    private static bool IsDateInOperatingSchedule(
        DateOnly date,
        DateTime? validFrom,
        DateTime? validUntil,
        string? availableDays)
    {
        var dateTime =
            date.ToDateTime(
                TimeOnly.MinValue);

        if (validFrom.HasValue &&
            dateTime.Date <
            validFrom.Value.Date)
        {
            return false;
        }

        if (validUntil.HasValue &&
            dateTime.Date >
            validUntil.Value.Date)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(
                availableDays) ||
            availableDays.Equals(
                "Daily",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var dayOfWeek =
            date.DayOfWeek
                .ToString()
                .ToLower();

        var raw =
            availableDays.ToLower();

        if (raw.Contains("weekday") &&
            date.DayOfWeek >= DayOfWeek.Monday &&
            date.DayOfWeek <= DayOfWeek.Friday)
        {
            return true;
        }

        if (raw.Contains("weekend") &&
            (date.DayOfWeek == DayOfWeek.Saturday ||
             date.DayOfWeek == DayOfWeek.Sunday))
        {
            return true;
        }

        return raw.Contains(dayOfWeek) ||
               raw.Contains(
                   dayOfWeek.Substring(0, 3));
    }

    /// <summary>
    /// Parses a comma-separated time slot string into individual
    /// slot entries, falling back to sensible default slots when empty.
    /// </summary>
    private static List<string> ParseTimeSlots(
        string? timeSlots)
    {
        if (string.IsNullOrWhiteSpace(
                timeSlots) ||
            timeSlots == "[]")
        {
            return new List<string>
            {
                "09:00 AM - 11:00 AM",
                "01:00 PM - 03:00 PM"
            };
        }

        return timeSlots.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .ToList();
    }

    /// <summary>
    /// Restores the requested guest count to a slot's remaining
    /// capacity, capped at the slot's total capacity.
    /// </summary>
    public async Task<bool> RestoreCapacityAsync(
        Guid listingId,
        DateOnly date,
        string timeSlot,
        int guestCount)
    {
        if (guestCount <= 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(timeSlot))
        {
            return false;
        }

        var existing =
            await _db.AvailabilitySlots
                .FirstOrDefaultAsync(s =>
                    s.ListingId == listingId &&
                    s.Date == date &&
                    s.TimeSlot == timeSlot);

        if (existing == null)
        {
            // No explicit row means availability is already
            // represented by the listing's default capacity.
            return true;
        }

        existing.RemainingCapacity =
            Math.Min(
                existing.TotalCapacity,
                existing.RemainingCapacity +
                guestCount);

        existing.UpdatedAt =
            DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Adjusts availability when a booking is updated by restoring
    /// the old slot and deducting from the new slot.
    /// </summary>
    public async Task<bool> UpdateCapacityAsync(
        Guid listingId,
        DateOnly oldDate,
        string oldTimeSlot,
        int oldGuestCount,
        DateOnly newDate,
        string newTimeSlot,
        int newGuestCount)
    {
        await RestoreCapacityAsync(
            listingId,
            oldDate,
            oldTimeSlot,
            oldGuestCount);

        await DeductCapacityAsync(
            listingId,
            newDate,
            newTimeSlot,
            newGuestCount);

        return true;
    }
}