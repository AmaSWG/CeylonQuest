namespace ProviderCatalogService.Models;

public interface IListingRating
{
    Guid Id { get; set; }
    long RatingSum { get; set; }
    int ReviewCount { get; set; }
    decimal AverageRating { get; set; }
}
