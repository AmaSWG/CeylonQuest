using System.Text.RegularExpressions;
using CeylonQuest.E2E.Tests.Visitor.Infrastructure;
using CeylonQuest.E2E.Tests.Visitor.Pages;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Visitor.Tests;

public sealed class VisitorCatalogBrowsingTests(ITestOutputHelper output) : VisitorCatalogTestBase(output)
{
    [CatalogFact, Trait("CaseId", "VC_001")]
    public void ValidLogin_ExploreDisplaysCatalogCards() => Run((ui, explore) => {
        Assert.Equal(new[] { "all", "all", "all", "newest" }, explore.FilterValues);
        Assert.Equal("", explore.SearchTerm);
        explore.AssertAllPages(VisitorExplorePage.Newest(explore.Listings));
        ui.Browser.Step("Successful visitor login and catalog cards verified across all loaded pages.");
    });

    [CatalogTheory, Trait("CaseId", "VC_002")]
    [InlineData("Experience")]
    [InlineData("Restaurant")]
    [InlineData("Accommodation")]
    public void ServiceType_DisplaysOnlySelectedCategory(string type) => Run((_, explore) => {
        explore.Choose(type); // Require a positive fixture for every category.
        explore.Category(type.ToLowerInvariant());
        explore.AssertAllPages(VisitorExplorePage.Newest(explore.Listings.Where(item => item.Type == type)));
        explore.Category("all");
        explore.AssertPage(VisitorExplorePage.Newest(explore.Listings), 1);
    });

    [CatalogTheory, Trait("CaseId", "VC_003")]
    [InlineData("title")]
    [InlineData("description")]
    [InlineData("location")]
    [InlineData("providerBusinessName")]
    public void KeywordSearch_MatchesEachSupportedFieldIgnoringCase(string field) => Run((ui, explore) => {
        // Require a witness whose term is absent from its other search fields.
        // Otherwise a broken description/provider search could pass via its title.
        var witness = explore.Listings.SelectMany(item => Terms(item.Text(field)).Select(term => (Item: item, Term: term)))
            .FirstOrDefault(candidate => CatalogListing.SearchFields.Where(key => key != field)
                .All(key => !candidate.Item.Text(key).Contains(candidate.Term, StringComparison.OrdinalIgnoreCase)));
        Assert.True(witness.Item is not null,
            $"Test data needs a {field} keyword absent from the same listing's other searchable fields.");
        var term = witness.Term.ToUpperInvariant();
        explore.Search(term);
        var expected = VisitorExplorePage.Newest(explore.Listings.Where(item => item.Matches(term)));
        Assert.Contains(expected, item => item.Id == witness.Item!.Id);
        explore.AssertAllPages(expected);
        ui.Browser.Step("Case-insensitive search verified for field: " + field);
    });

    private static IEnumerable<string> Terms(string text)
    {
        if (!string.IsNullOrWhiteSpace(text) && text.Length <= 100) yield return text.Trim();
        foreach (Match match in Regex.Matches(text, @"[\p{L}\p{N}]{4,}")) yield return match.Value;
    }

    [CatalogFact, Trait("CaseId", "VC_004")]
    public void ClearSearch_RestoresCatalog() => Run((_, explore) => {
        var listing = explore.Listings[0];
        explore.Search(listing.Title);
        explore.AssertAllPages(VisitorExplorePage.Newest(explore.Listings.Where(item => item.Matches(listing.Title))));
        explore.ClearSearch();
        Assert.Equal("", explore.SearchTerm);
        explore.AssertAllPages(VisitorExplorePage.Newest(explore.Listings));
    });

    [CatalogFact, Trait("CaseId", "VC_005")]
    public void LocationFilter_DisplaysMatchingLocations() => Run((_, explore) => {
        var location = SupportedLocation(explore);
        explore.Location(location);
        explore.AssertAllPages(VisitorExplorePage.Newest(explore.Listings.Where(item =>
            item.Location.Contains(location, StringComparison.OrdinalIgnoreCase))));
        explore.Location("all");
        explore.AssertPage(VisitorExplorePage.Newest(explore.Listings), 1);
    });

    [CatalogTheory, Trait("CaseId", "VC_006")]
    [InlineData("budget")]
    [InlineData("mid")]
    public void PriceFilter_RespectsSelectedBand(string band) => Run((_, explore) => {
        var expected = VisitorExplorePage.Newest(explore.Listings.Where(item => CatalogListing.PriceBand(item.Price) == band));
        Assert.True(expected.Length > 0, "Test data requires at least one " + band + " listing in the first 50 results.");
        explore.Price(band);
        explore.AssertAllPages(expected);
        explore.Price("all");
        explore.AssertPage(VisitorExplorePage.Newest(explore.Listings), 1);
    });

    [CatalogFact, Trait("CaseId", "VC_007")]
    public void CombinedFilters_ApplyCategoryLocationPriceAndSearchTogether() => Run((_, explore) => {
        var location = SupportedLocation(explore, supportedPriceOnly: true);
        var fixture = explore.Listings.First(item => item.Price <= 20000 &&
            item.Location.Contains(location, StringComparison.OrdinalIgnoreCase));
        var band = CatalogListing.PriceBand(fixture.Price);
        explore.Category(fixture.Type.ToLowerInvariant());
        explore.Location(location);
        explore.Price(band);
        explore.Search(fixture.Title);
        var expected = VisitorExplorePage.Newest(explore.Listings.Where(item => item.Type == fixture.Type &&
            item.Location.Contains(location, StringComparison.OrdinalIgnoreCase) &&
            CatalogListing.PriceBand(item.Price) == band && item.Matches(fixture.Title)));
        Assert.Contains(expected, item => item.Id == fixture.Id);
        explore.AssertAllPages(expected);
        Assert.Equal(new[] { fixture.Type.ToLowerInvariant(), location, band, "newest" }, explore.FilterValues);
    });

    [CatalogTheory, Trait("CaseId", "VC_008")]
    [InlineData("price_asc")]
    [InlineData("price_desc")]
    [InlineData("newest")]
    public void SortOrder_IsCorrectAcrossResultPages(string order) => Run((_, explore) => {
        Assert.True(explore.Listings.Select(item => item.Price).Distinct().Count() >= 2, "Sorting requires different listing prices.");
        Assert.True(explore.Listings.Select(item => item.CreatedAt).Distinct().Count() >= 2, "Newest sorting requires different creation dates.");
        // Deliberately change away from newest before verifying the default sort.
        explore.Sort(order == "newest" ? "price_asc" : "newest");
        explore.Sort(order);
        var expected = order switch {
            "price_asc" => explore.Listings.OrderBy(item => item.Price).ToArray(),
            "price_desc" => explore.Listings.OrderByDescending(item => item.Price).ToArray(),
            _ => VisitorExplorePage.Newest(explore.Listings)
        };
        explore.AssertAllPages(expected);
    });

    private static string SupportedLocation(VisitorExplorePage explore, bool supportedPriceOnly = false)
    {
        var location = explore.LocationOptions.FirstOrDefault(option => explore.Listings.Any(item =>
            (!supportedPriceOnly || item.Price <= 20000) && item.Location.Contains(option, StringComparison.OrdinalIgnoreCase)));
        Assert.True(location is not null, "Test data requires a listing in one of the location dropdown's cities.");
        return location!;
    }
}
