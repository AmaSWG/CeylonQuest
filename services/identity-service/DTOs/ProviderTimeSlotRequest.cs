using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs;

public class ProviderTimeSlotRequest
{
    private const string NotBlankPattern = @"^(?!\s*$).+";

    [Required(ErrorMessage = "Date is required.")]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Date must be in YYYY-MM-DD format.")]
    public string Date { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start time is required.")]
    [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$", ErrorMessage = "StartTime must be in HH:mm format.")]
    public string StartTime { get; set; } = string.Empty;

    [Required(ErrorMessage = "End time is required.")]
    [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d)$", ErrorMessage = "EndTime must be in HH:mm format.")]
    public string EndTime { get; set; } = string.Empty;

    public bool IsAvailable { get; set; } = true;
}
