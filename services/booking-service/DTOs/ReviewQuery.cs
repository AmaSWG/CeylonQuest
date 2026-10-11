using System.ComponentModel.DataAnnotations;

namespace BookingService.DTOs;

public class ReviewQuery
{
    [RegularExpression("^(Experience|Restaurant|Accommodation)$")]
    public string? BookingType { get; set; }
    [Range(1, 5)]
    public int? Rating { get; set; }
    [Range(1, 1000000)]
    public int Page { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}
