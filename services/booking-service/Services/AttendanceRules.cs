using BookingService.Models;

namespace BookingService.Services;

public static class AttendanceRules
{
    public static string? InvalidCompletion(IPayableBooking booking, DateTime now)
    {
        if (booking.Status is not (BookingStatus.Confirmed or BookingStatus.Completed))
            return "Only confirmed or completed bookings can have attendance verified.";
        var end = ReviewService.ScheduledEnd(booking);
        if (end == null) return "The scheduled end time is unavailable. Please contact support.";
        if (end >= now) return "Attendance can be verified after the scheduled service has ended.";
        return null;
    }

    public static void Verify(IPayableBooking booking, AttendanceStatus status, Guid verifier, DateTime now)
    {
        if (status is not (AttendanceStatus.Attended or AttendanceStatus.NoShow))
            throw new ReviewException(400, "Choose Attended or NoShow.");
        var reason = InvalidCompletion(booking, now);
        if (reason != null) throw new ReviewException(409, reason);
        if (booking.AttendanceStatus != AttendanceStatus.Pending)
            throw new ReviewException(409, "Attendance is already verified. Corrections require an administrative dispute resolution.");
        booking.Status = BookingStatus.Completed;
        booking.AttendanceStatus = status;
        booking.AttendanceVerifiedByUserId = verifier;
        booking.AttendanceVerifiedAtUtc = now;
        booking.UpdatedAt = now;
    }
}
