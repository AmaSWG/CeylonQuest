using System.ComponentModel.DataAnnotations;
using BookingService.Models;

namespace BookingService.DTOs;

public class BookingsRevenueReportQuery : IValidatableObject
{
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Status { get; set; }
    public string? BookingType { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && EndDate.HasValue && StartDate > EndDate)
            yield return new ValidationResult("startDate must not be after endDate.", new[] { nameof(StartDate), nameof(EndDate) });

        if (Status != null && !Enum.GetNames<BookingStatus>().Contains(Status, StringComparer.OrdinalIgnoreCase))
            yield return new ValidationResult("Invalid status. Use PendingPayment, Confirmed, Cancelled or Completed.", new[] { nameof(Status) });

        if (BookingType != null && !new[] { "Experience", "Restaurant", "Accommodation" }.Contains(BookingType, StringComparer.OrdinalIgnoreCase))
            yield return new ValidationResult("Invalid bookingType. Use Experience, Restaurant or Accommodation.", new[] { nameof(BookingType) });
    }
}
