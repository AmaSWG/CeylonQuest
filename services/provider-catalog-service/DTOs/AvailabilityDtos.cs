using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProviderCatalogService.DTOs;

public class SlotAvailabilityDto
{
    public string TimeSlot { get; set; } = string.Empty;
    public int TotalCapacity { get; set; }
    public int RemainingCapacity { get; set; }
    public bool IsFullyBooked => RemainingCapacity <= 0;
}

public class ListingDateAvailabilityResponse
{
    public Guid ListingId { get; set; }
    public string Date { get; set; } = string.Empty;
    public bool IsOperatingDay { get; set; }
    public bool IsFullyBooked { get; set; }
    public string? ValidFrom { get; set; }
    public string? ValidUntil { get; set; }
    public string? AvailableDays { get; set; }
    public string? TimeSlots { get; set; }
    public List<SlotAvailabilityDto> Slots { get; set; } = new();
}

public class SetAvailabilityRequest
{
    [Required]
    public string Date { get; set; } = string.Empty; // "YYYY-MM-DD"

    [Required]
    public string TimeSlot { get; set; } = string.Empty;

    [Range(1, 10000, ErrorMessage = "Capacity must be greater than 0.")]
    public int Capacity { get; set; }
}

public class SimulateBookingEventRequest
{
    [Required]
    public Guid ListingId { get; set; }

    [Required]
    public string Date { get; set; } = string.Empty; // "YYYY-MM-DD"

    [Required]
    public string TimeSlot { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Guest count must be at least 1.")]
    public int GuestCount { get; set; } = 1;
}

public class SimulateBookingCanceledEventRequest
{
    [Required]
    public Guid ListingId { get; set; }
    [Required]
    public string Date { get; set; } = string.Empty; // "YYYY-MM-DD"
    [Required]
    public string TimeSlot { get; set; } = string.Empty;
    [Range(1, 100, ErrorMessage = "Guest count must be at least 1.")]
    public int GuestCount { get; set; } = 1;
}
public class SimulateBookingUpdatedEventRequest
{
    [Required]
    public Guid ListingId { get; set; }
    [Required]
    public string OldDate { get; set; } = string.Empty; // "YYYY-MM-DD"
    [Required]
    public string OldTimeSlot { get; set; } = string.Empty;
    [Range(1, 100, ErrorMessage = "Old guest count must be at least 1.")]
    public int OldGuestCount { get; set; } = 1;
    [Required]
    public string NewDate { get; set; } = string.Empty; // "YYYY-MM-DD"
    [Required]
    public string NewTimeSlot { get; set; } = string.Empty;
    [Range(1, 100, ErrorMessage = "New guest count must be at least 1.")]
    public int NewGuestCount { get; set; } = 1;
}
