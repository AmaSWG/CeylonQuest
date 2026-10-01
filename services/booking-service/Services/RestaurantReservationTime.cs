using System.Globalization;

namespace BookingService.Services;

public static class RestaurantReservationTime
{
    public const string PastSlotMessage = "This time slot has already passed. Please select a future time slot.";
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");

    public static string? Validate(DateOnly date, string slot, DateTimeOffset utcNow)
    {
        var now = TimeZoneInfo.ConvertTime(utcNow, Zone);
        if (date < DateOnly.FromDateTime(now.DateTime))
            return "Reservation date cannot be in the past.";

        var start = slot.Split('-', 2)[0].Trim();
        if (!TimeOnly.TryParseExact(start.ToUpperInvariant(),
                new[] { "hh:mm tt", "h:mm tt", "HH:mm", "H:mm" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            return "The reservation start time could not be determined.";

        return date.ToDateTime(time) <= now.DateTime ? PastSlotMessage : null;
    }
}
