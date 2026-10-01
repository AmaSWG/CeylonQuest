using System;

namespace ProviderCatalogService.DTOs;

public class AccommodationListingResponse
{
    public Guid Id { get; set; }

    public Guid ProviderId { get; set; }

    public string ProviderBusinessName { get; set; } = string.Empty;

    public string RoomType { get; set; } = string.Empty;

    public string PropertyType { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public decimal PricePerNight { get; set; }

    public int MaxGuests { get; set; }

    public string BedDetails { get; set; } = string.Empty;

    public int MinStayNights { get; set; }

    public string Amenities { get; set; } = string.Empty;

    public string BathroomDetails { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? Images { get; set; }
}


