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
    /// Retrieves availability for an activity, restaurant,
    /// or accommodation on a selected date.
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
    /// Sets or updates the total capacity for a specific
    /// availability time slot.
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
    /// Deducts capacity from an availability slot.
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
                    if (rest.SeatingCapacity <= 0)
                    {
                        return false;
                    }

                    defaultCapacity = rest.SeatingCapacity;
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
    /// Attempts to reserve capacity before a booking
    /// or restaurant reservation is created.
    ///
    /// Existing slots use an atomic conditional update
    /// to help prevent overbooking.
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

        /*
         * First try an atomic conditional update.
         *
         * UPDATE AvailabilitySlots
         * SET RemainingCapacity =
         *     RemainingCapacity - guestCount
         * WHERE ListingId = ...
         *   AND Date = ...
         *   AND TimeSlot = ...
         *   AND RemainingCapacity >= guestCount
         */

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

        /*
         * If the slot already exists but the update
         * changed zero rows, there was not enough capacity.
         */
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

        /*
         * No persisted slot exists yet.
         * Determine the default capacity from listing type.
         */
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
                if (restaurant.SeatingCapacity <= 0)
                {
                    return false;
                }

                defaultCapacity = restaurant.SeatingCapacity;
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

        if (defaultCapacity < guestCount)
        {
            return false;
        }

        /*
         * Create the first persisted slot.
         *
         * The database unique index on:
         * ListingId + Date + TimeSlot
         * prevents duplicate slots.
         */
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
            /*
             * Another request may have created the same
             * slot at the same time.
             */
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
    /// Builds availability for an activity listing.
    /// Existing Experience functionality is unchanged.
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
    /// Builds restaurant availability using reservation
    /// time slots configured by the restaurant provider.
    ///
    /// OpeningHours describes when the restaurant is open.
    /// TimeSlots describes when visitors may make reservations.
    /// </summary>
    private async Task<ListingDateAvailabilityResponse>
        BuildRestaurantAvailabilityAsync(
            RestaurantListing rest,
            DateOnly date)
    {
        var isOperating = true;

        /*
         * Use the provider-configured restaurant
         * reservation slots.
         *
         * Example:
         * 09:00 AM - 10:00 AM,
         * 10:00 AM - 11:00 AM,
         * 11:00 AM - 12:00 PM
         */
        var slots =
            ParseRestaurantTimeSlots(
                rest.TimeSlots);

        /*
         * If no reservation slots have been configured,
         * do not expose an invalid booking option.
         */
        if (slots.Count == 0)
        {
            return new ListingDateAvailabilityResponse
            {
                ListingId = rest.Id,
                Date = date.ToString("yyyy-MM-dd"),
                IsOperatingDay = false,
                IsFullyBooked = true,
                ValidFrom = null,
                ValidUntil = null,
                AvailableDays = "Daily",
                TimeSlots = rest.TimeSlots,
                Slots = new List<SlotAvailabilityDto>()
            };
        }

        if (rest.SeatingCapacity <= 0)
        {
            return new ListingDateAvailabilityResponse
            {
                ListingId = rest.Id,
                Date = date.ToString("yyyy-MM-dd"),
                IsOperatingDay = false,
                IsFullyBooked = true,
                ValidFrom = null,
                ValidUntil = null,
                AvailableDays = "Daily",
                TimeSlots = rest.TimeSlots,
                Slots = new List<SlotAvailabilityDto>()
            };
        }

        var defaultCap = rest.SeatingCapacity;

        /*
         * Get capacity records already created
         * for this restaurant/date.
         */
        var existingOverrides =
            await _db.AvailabilitySlots
                .AsNoTracking()
                .Where(s =>
                    s.ListingId == rest.Id &&
                    s.Date == date)
                .ToListAsync();

        var slotDtos =
            new List<SlotAvailabilityDto>();

        /*
         * Each configured reservation slot receives
         * its own availability/capacity.
         */
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

            // Provider-configured reservation slots
            TimeSlots = rest.TimeSlots,

            Slots = slotDtos
        };
    }

    /// <summary>
    /// Builds accommodation availability.
    /// Existing accommodation functionality is unchanged.
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
    /// Determines whether a date falls within the
    /// configured activity operating schedule.
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
    /// Parses restaurant reservation time slots.
    ///
    /// Unlike experiences, restaurants do not receive
    /// default reservation slots when none are configured.
    /// </summary>
    private static List<string> ParseRestaurantTimeSlots(
        string? timeSlots)
    {
        if (string.IsNullOrWhiteSpace(timeSlots) ||
            timeSlots == "[]")
        {
            return new List<string>();
        }

        return timeSlots
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Where(slot =>
                !string.IsNullOrWhiteSpace(slot))
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Parses Experience time slots.
    /// Existing Experience behaviour is unchanged.
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
    /// Restores capacity to a slot.
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
            /*
             * No persisted slot means availability
             * is already represented by default capacity.
             */
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
    /// Updates capacity when an existing booking changes.
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