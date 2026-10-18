using CeylonQuest.E2E.Tests.Visitor.Infrastructure;
using CeylonQuest.E2E.Tests.Visitor.Pages;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Visitor.Tests;

public sealed class VisitorCatalogDetailsTests(ITestOutputHelper output) : VisitorCatalogTestBase(output)
{
    [CatalogTheory, Trait("CaseId", "VC_011")]
    [InlineData("Experience")]
    [InlineData("Restaurant")]
    [InlineData("Accommodation")]
    public void ViewDetails_DisplaysCategorySpecificCatalogData(string type) => Run((_, explore) => {
        var listing = explore.Choose(type);
        var details = explore.OpenDetails(listing);
        details.AssertDetails();
        var values = explore.FilterValues;
        var search = explore.SearchTerm;
        var page = explore.ActivePage;
        details.Close();
        Assert.Equal(values, explore.FilterValues);
        Assert.Equal(search, explore.SearchTerm);
        explore.AssertPage(VisitorExplorePage.Newest(explore.Listings.Where(item =>
            item.Type == listing.Type && item.Matches(listing.Title))), page);
    });

    [CatalogFact, Trait("CaseId", "VC_012")]
    public void ClickingListingCard_OpensTheSameDetails() => Run((_, explore) => {
        var listing = explore.Listings[0];
        var details = explore.OpenDetails(listing, clickCard: true);
        details.AssertDetails();
        details.Close();
    });

    [CatalogTheory, Trait("CaseId", "VC_014")]
    [InlineData(true)]
    [InlineData(false)]
    public void RatingSummary_DisplaysReviewedAndUnreviewedStates(bool reviewed) => Run((ui, explore) => {
        var listing = explore.Listings.FirstOrDefault(item => (item.Int("reviewCount") > 0) == reviewed);
        Assert.True(listing is not null, reviewed ? "Test data requires a listing with existing reviews."
            : "Test data requires a listing without reviews.");
        explore.Show(listing!);
        var card = explore.Card(listing!);
        if (reviewed)
        {
            var count = listing!.Int("reviewCount");
            Assert.InRange(listing.Number("averageRating"), 1, 5);
            var score = ui.Browser.Script.ExecuteScript("return Number(arguments[0]).toFixed(1)",
                (double)listing.Number("averageRating")) as string;
            VisitorCatalogUi.EqualText(score!, card.FindElement(By.CssSelector(".vd-rating-score")).Text);
            VisitorCatalogUi.EqualText($"{count} {(count == 1 ? "review" : "reviews")}", card.FindElement(By.CssSelector(".vd-rating-count")).Text);
            Assert.Equal($"Rating {score} out of 5 stars from {count} reviews",
                card.FindElement(By.CssSelector(".vd-rating-pill")).GetDomAttribute("aria-label"));
            Assert.Empty(card.FindElements(By.CssSelector(".vd-rating-empty")));
        }
        else
        {
            Assert.Equal(0, listing!.Int("reviewCount"));
            Assert.Equal("No reviews yet", card.FindElement(By.CssSelector(".vd-rating-empty")).GetDomAttribute("aria-label"));
            Assert.Contains("No reviews yet", card.FindElement(By.CssSelector(".vd-rating-empty")).Text);
            Assert.Empty(card.FindElements(By.CssSelector(".vd-rating-pill")));
        }
    });
}
