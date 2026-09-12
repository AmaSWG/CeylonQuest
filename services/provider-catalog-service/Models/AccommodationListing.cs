using System;

namespace ProviderCatalogService.Models;

public class AccommodationListing
{
    public Guid Id { get; set; }

    public Guid ProviderId { get; set; }

    public string RoomType { get; set; } = string.Empty; // e.g. Deluxe Ocean View Suite, Presidential Villa

    public string PropertyType { get; set; } = "Boutique Hotel"; // e.g. Resort, Villa, Eco Lodge, Guest House, Homestay

    public string Location { get; set; } = string.Empty; // e.g. Galle Fort, Southern Province

    public decimal PricePerNight { get; set; }

    public int MaxGuests { get; set; } = 2;

    public string BedDetails { get; set; } = "1 King Bed"; // e.g. 1 King Bed + 1 Single Bed

    public int MinStayNights { get; set; } = 1;

    public string Amenities { get; set; } = "Free WiFi, AC, Breakfast Included";

    public string BathroomDetails { get; set; } = "En-suite Private Bathroom with Hot Water";

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Provider? Provider { get; set; }
}