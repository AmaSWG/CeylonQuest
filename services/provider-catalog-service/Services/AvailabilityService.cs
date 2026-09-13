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

    public async Task<ListingDateAvailabilityResponse?> GetAvailabilityForDateAsync(Guid listingId, DateOnly date)
    {
        // 1. Try Activity Listing
        var activity = await _db.ActivityListings
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == listingId && a.IsActive);

        if (activity != null)
        {
            return await BuildActivityAvailabilityAsync(activity, date);
        }

        // 2. Try Restaurant Listing
        var restaurant = await _db.RestaurantListings
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == listingId && r.IsActive);

        if (restaurant != null)
        {
            return await BuildRestaurantAvailabilityAsync(restaurant, date);
        }

        // 3. Try Accommodation Listing (Stays)
        var accommodation = await _db.AccommodationListings
            .AsNoTracking()
            .FirstOrDefaultAsync(ac => ac.Id == listingId && ac.IsActive);

        if (accommodation != null)
        {
            return await BuildAccommodationAvailabilityAsync(accommodation, date);
        }

        return null;
    }

    public async Task<bool> SetSlotCapacityAsync(Guid listingId, DateOnly date, string timeSlot, int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than 0.");

        var existing = await _db.AvailabilitySlots
            .FirstOrDefaultAsync(s => s.ListingId == listingId && s.Date == date && s.TimeSlot == timeSlot);

        if (existing != null)
        {
            var bookedCount = existing.TotalCapacity - existing.RemainingCapacity;
            existing.TotalCapacity = capacity;
            existing.RemainingCapacity = Math.Max(0, capacity - bookedCount);
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.AvailabilitySlots.Add(new AvailabilitySlot
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

    public async Task<bool> DeductCapacityAsync(Guid listingId, DateOnly date, string timeSlot, int guestCount)
    {
        var existing = await _db.AvailabilitySlots
            .FirstOrDefaultAsync(s => s.ListingId == listingId && s.Date == date && s.TimeSlot == timeSlot);

        if (existing == null)
        {
            int defaultCapacity = 10;
            var act = await _db.ActivityListings.FirstOrDefaultAsync(a => a.Id == listingId);
            if (act != null)
            {
                defaultCapacity = act.MaxParticipants > 0 ? act.MaxParticipants : 10;
            }
            else
            {
                var rest = await _db.RestaurantListings.FirstOrDefaultAsync(r => r.Id == listingId);
                if (rest != null)
                {
                    defaultCapacity = rest.SeatingCapacity > 0 ? rest.SeatingCapacity : 20;
                }
                else
                {
                    defaultCapacity = 1; // 1 room / unit for accommodations
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

        existing.RemainingCapacity = Math.Max(0, existing.RemainingCapacity - guestCount);
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return true;
    }

    private async Task<ListingDateAvailabilityResponse> BuildActivityAvailabilityAsync(ActivityListing activity, DateOnly date)
    {
        var isOperating = IsDateInOperatingSchedule(date, activity.ValidFrom, activity.ValidUntil, activity.AvailableDays);
        var slots = ParseTimeSlots(activity.TimeSlots);
        var defaultCap = activity.MaxParticipants > 0 ? activity.MaxParticipants : 10;

        var existingOverrides = await _db.AvailabilitySlots
            .AsNoTracking()
            .Where(s => s.ListingId == activity.Id && s.Date == date)
            .ToListAsync();

        var slotDtos = new List<SlotAvailabilityDto>();

        foreach (var slot in slots)
        {
            var matched = existingOverrides.FirstOrDefault(o => o.TimeSlot.Equals(slot, StringComparison.OrdinalIgnoreCase));
            var total = matched?.TotalCapacity ?? defaultCap;
            var remaining = matched?.RemainingCapacity ?? defaultCap;

            slotDtos.Add(new SlotAvailabilityDto
            {
                TimeSlot = slot,
                TotalCapacity = total,
                RemainingCapacity = isOperating ? remaining : 0
            });
        }

        return new ListingDateAvailabilityResponse
        {
            ListingId = activity.Id,
                Date = date.ToString("yyyy-MM-dd"),
                IsOperatingDay = isOperating,
                IsFullyBooked = !isOperating || slotDtos.All(s => s.IsFullyBooked),
                ValidFrom = activity.ValidFrom?.ToString("yyyy-MM-dd"),
                ValidUntil = activity.ValidUntil?.ToString("yyyy-MM-dd"),
                AvailableDays = string.IsNullOrWhiteSpace(activity.AvailableDays) ? "Daily" : activity.AvailableDays,
                TimeSlots = activity.TimeSlots,
                Slots = slotDtos
        };
    }

    private async Task<ListingDateAvailabilityResponse> BuildRestaurantAvailabilityAsync(RestaurantListing rest, DateOnly date)
    {
        var isOperating = true;
        var slots = new List<string> { string.IsNullOrWhiteSpace(rest.OpeningHours) ? "11:30 AM - 10:00 PM" : rest.OpeningHours };
        var defaultCap = rest.SeatingCapacity > 0 ? rest.SeatingCapacity : 20;

        var existingOverrides = await _db.AvailabilitySlots
            .AsNoTracking()
            .Where(s => s.ListingId == rest.Id && s.Date == date)
            .ToListAsync();

        var slotDtos = new List<SlotAvailabilityDto>();
        foreach (var slot in slots)
        {
            var matched = existingOverrides.FirstOrDefault(o => o.TimeSlot.Equals(slot, StringComparison.OrdinalIgnoreCase));
            var total = matched?.TotalCapacity ?? defaultCap;
            var remaining = matched?.RemainingCapacity ?? defaultCap;

            slotDtos.Add(new SlotAvailabilityDto
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
                IsFullyBooked = slotDtos.All(s => s.IsFullyBooked),
                ValidFrom = null,
                ValidUntil = null,
                AvailableDays = "Daily",
                TimeSlots = rest.OpeningHours,
                Slots = slotDtos
        };
    }

    private async Task<ListingDateAvailabilityResponse> BuildAccommodationAvailabilityAsync(AccommodationListing ac, DateOnly date)
    {
        var slotName = $"Stay (Min {ac.MinStayNights} Night{(ac.MinStayNights > 1 ? "s" : "")})";
        const int defaultCap = 1; // 1 room/villa unit

        var existingOverride = await _db.AvailabilitySlots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ListingId == ac.Id && s.Date == date && s.TimeSlot == slotName);

        var total = existingOverride?.TotalCapacity ?? defaultCap;
        var remaining = existingOverride?.RemainingCapacity ?? defaultCap;

        var slotDtos = new List<SlotAvailabilityDto>
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
            IsFullyBooked = slotDtos.All(s => s.IsFullyBooked),
            ValidFrom = null,
            ValidUntil = null,
            AvailableDays = "Daily",
            TimeSlots = $"Min {ac.MinStayNights} Night Stay",
            Slots = slotDtos
        };
    }

    private static bool IsDateInOperatingSchedule(DateOnly date, DateTime? validFrom, DateTime? validUntil, string? availableDays)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        if (validFrom.HasValue && dateTime.Date < validFrom.Value.Date) return false;
        if (validUntil.HasValue && dateTime.Date > validUntil.Value.Date) return false;

        if (string.IsNullOrWhiteSpace(availableDays) || availableDays.Equals("Daily", StringComparison.OrdinalIgnoreCase))
            return true;

        var dayOfWeek = date.DayOfWeek.ToString().ToLower();
        var raw = availableDays.ToLower();

        if (raw.Contains("weekday") && (date.DayOfWeek >= DayOfWeek.Monday && date.DayOfWeek <= DayOfWeek.Friday)) return true;
        if (raw.Contains("weekend") && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)) return true;

        return raw.Contains(dayOfWeek) || raw.Contains(dayOfWeek.Substring(0, 3));
    }

    private static List<string> ParseTimeSlots(string? timeSlots)
    {
        if (string.IsNullOrWhiteSpace(timeSlots) || timeSlots == "[]")
            return new List<string> { "09:00 AM - 11:00 AM", "01:00 PM - 03:00 PM" };

        return timeSlots.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}