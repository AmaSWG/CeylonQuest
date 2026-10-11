namespace BookingService.DTOs;

public class ProviderPerformanceReportResponse
{
    public ProviderPerformanceReportQuery Filters { get; set; } = new();

    public List<ProviderPerformanceReportRow> Providers { get; set; } = new();
}

public class ProviderPerformanceReportRow
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new();

    public decimal TotalRevenue { get; set; }
    public int TotalReviewCount { get; set; }
    public double? AverageRating { get; set; }

    public StarRatingBreakdown StarRatingBreakdown { get; set; } = new();

    public bool NoReviews => TotalReviewCount == 0;
    public bool LowRating => AverageRating.HasValue && AverageRating.Value < 3.0;

    public string RatingState =>
        NoReviews ? "No Reviews" :
        LowRating ? "Low Rating" :
        "Rated";
}

public class StarRatingBreakdown
{
    public int OneStar { get; set; }
    public int TwoStars { get; set; }
    public int ThreeStars { get; set; }
    public int FourStars { get; set; }
    public int FiveStars { get; set; }
}