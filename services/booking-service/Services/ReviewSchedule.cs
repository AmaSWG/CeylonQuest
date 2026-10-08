using System.Globalization;
using System.Text.RegularExpressions;

namespace BookingService.Services;

/// <summary>Capture schedules in UTC; unknown schedules must never enable a review.</summary>
public static class ReviewSchedule
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");

    public static DateTime? SlotEnd(DateOnly date, string slot, string? duration = null)
    {
        var parts = Regex.Split(slot.Trim(), @"\s*[-–—]\s*");
        if (!TryTime(parts[0], out var start)) return null;
        var startLocal = date.ToDateTime(start);
        DateTime endLocal;
        if (parts.Length == 2 && TryTime(parts[1], out var end))
        {
            endLocal = date.ToDateTime(end);
            if (end <= start) endLocal = endLocal.AddDays(1);
        }
        else if (parts.Length == 1 && !string.IsNullOrWhiteSpace(duration))
        {
            var match = Regex.Match(duration.Trim(), @"^(\d+(?:\.\d+)?)\s*(hours?|hrs?|minutes?|mins?)$", RegexOptions.IgnoreCase);
            if (!match.Success || !double.TryParse(match.Groups[1].Value, NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var amount) || amount <= 0 || amount > 1440) return null;
            endLocal = match.Groups[2].Value.StartsWith("h", StringComparison.OrdinalIgnoreCase)
                ? startLocal.AddHours(amount) : startLocal.AddMinutes(amount);
        }
        else return null;
        return TimeZoneInfo.ConvertTimeToUtc(endLocal, Zone);
    }

    public static DateTime? CheckoutEnd(DateOnly date, string? checkoutTime)
        => TryTime(checkoutTime ?? string.Empty, out var time)
            ? TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(time), Zone) : null;

    private static bool TryTime(string value, out TimeOnly time) => TimeOnly.TryParseExact(value.Trim().ToUpperInvariant(),
        new[] { "h:mm tt", "hh:mm tt", "H:mm", "HH:mm" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
}
