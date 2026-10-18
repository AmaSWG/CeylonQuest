using CeylonQuest.E2E.Tests.Visitor.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.E2E.Tests.Visitor.Pages;

internal sealed class VisitorExplorePage(VisitorCatalogUi ui)
{
    private const int PageSize = 9;
    private static readonly By Cards = By.CssSelector(".vd-explore .vd-service-card");
    private static readonly By SearchInput = By.CssSelector(".vd-explore .vd-search-bar__input");
    private static readonly By FilterSelects = By.CssSelector(".vd-explore .vd-filters select");
    public CatalogListing[] Listings { get; private set; } = [];
    public string SearchTerm => ui.Browser.Visible(SearchInput).GetDomProperty("value") ?? "";
    public int ActivePage => ui.Browser.Driver.FindElements(By.CssSelector(".vd-pagination__page--active"))
        .Where(element => element.Displayed).Select(element => int.Parse(element.Text)).DefaultIfEmpty(1).Single();
    public string[] LocationOptions => Select(1).Options.Select(option => option.GetDomAttribute("value") ?? "")
        .Where(value => value != "all").ToArray();

    public void Open()
    {
        ui.Browser.Click(By.Id("nav-explore"));
        ui.Browser.Visible(By.CssSelector(".vd-explore h1"));
        var response = ui.WaitResponse("/api/catalog/search");
        var body = response.Body;
        var items = body.ValueKind == System.Text.Json.JsonValueKind.Array ? body : body.GetProperty("items");
        Listings = items.EnumerateArray().Select(item => new CatalogListing(item.Clone())).ToArray();
        Assert.NotEmpty(Listings);
        Assert.InRange(Listings.Length, 1, 50);
        Assert.Equal(Listings.Length, Listings.Select(item => item.Id).Distinct().Count());
        Assert.All(Listings, item => {
            Assert.True(Guid.TryParse(item.Id, out _), "Catalog listing requires a valid ID.");
            Assert.Contains(item.Type, new[] { "Experience", "Restaurant", "Accommodation" });
            Assert.False(string.IsNullOrWhiteSpace(item.Title));
            Assert.True(item.Price > 0, "Use valid, priced catalog listings.");
        });
        // Cards have no listing ID attributes. Refuse ambiguous visible identities.
        Assert.Equal(Listings.Length, Listings.Select(item => (item.Type, item.Title, item.Provider)).Distinct().Count());
        ui.Browser.Step($"Explore loaded {Listings.Length} listings from the real catalog response.");
        AssertPage(Newest(Listings), 1);
    }

    public static CatalogListing[] Newest(IEnumerable<CatalogListing> items) => items.OrderByDescending(item => item.CreatedAt).ToArray();
    private SelectElement Select(int index) => new(ui.Browser.Visible(By.XPath(
        $"//div[contains(concat(' ',normalize-space(@class),' '),' vd-filters ')]/select[{index + 1}]")));
    public void Category(string value) => Select(0).SelectByValue(value);
    public void Location(string value) => Select(1).SelectByValue(value);
    public void Price(string value) => Select(2).SelectByValue(value);
    public void Sort(string value) => Select(3).SelectByValue(value);
    public string[] FilterValues => ui.Browser.Driver.FindElements(FilterSelects)
        .Select(element => new SelectElement(element).SelectedOption.GetDomAttribute("value") ?? "").ToArray();
    public void Search(string term) => ui.Browser.Fill(SearchInput, term);
    public void ClearSearch() => ui.Browser.Click(By.CssSelector(".vd-explore .vd-search-bar__clear"));

    public CatalogListing Choose(string type)
    {
        var title = VisitorCatalogSettings.Title(type);
        var candidates = Listings.Where(item => item.Type == type && (string.IsNullOrWhiteSpace(title) || item.Title == title)).ToArray();
        Assert.True(candidates.Length > 0, $"Test data missing: {type} '{title ?? "any active listing"}' in the first 50 catalog results.");
        if (!string.IsNullOrWhiteSpace(title)) Assert.Single(candidates);
        return candidates[0];
    }

    public void AssertPage(IReadOnlyList<CatalogListing> expected, int page)
    {
        Assert.NotEmpty(expected); // Positive scenarios must not pass with an empty catalog.
        var slice = expected.Skip((page - 1) * PageSize).Take(PageSize).ToArray();
        Assert.NotEmpty(slice);
        ui.Browser.Until(driver => {
            var cards = driver.FindElements(Cards).Where(element => element.Displayed).ToArray();
            return ActivePage == page && cards.Length == slice.Length && cards.Select((card, index) =>
                card.FindElement(By.CssSelector(".vd-service-card__title")).Text == slice[index].Title &&
                card.FindElement(By.CssSelector(".vd-service-card__provider-name")).Text == "By " + slice[index].Provider &&
                card.FindElement(By.CssSelector(".vd-type-badge")).Text.Equals(slice[index].Type, StringComparison.OrdinalIgnoreCase))
                .All(matches => matches);
        }, $"Catalog page {page} did not contain the expected ordered listings ({expected.Count} filtered results).");
        VisitorCatalogUi.EqualText($"Showing {slice.Length} of {expected.Count} listings",
            ui.Browser.Visible(By.CssSelector(".vd-results-count")).Text);
        var actual = ui.Browser.Driver.FindElements(Cards).Where(card => card.Displayed).ToArray();
        for (var index = 0; index < slice.Length; index++) AssertCard(actual[index], slice[index]);
        Assert.DoesNotContain(ui.Browser.Driver.FindElements(By.CssSelector(".vd-explore .vd-form-error")), element => element.Displayed);
    }

    private void AssertCard(IWebElement card, CatalogListing item)
    {
        VisitorCatalogUi.EqualText(item.Title, card.FindElement(By.CssSelector(".vd-service-card__title")).Text);
        VisitorCatalogUi.EqualText(item.Text("description"), card.FindElement(By.CssSelector(".vd-service-card__desc")).Text);
        VisitorCatalogUi.EqualText(ui.Money(item.Price), card.FindElement(By.CssSelector(".vd-service-card__amount")).Text);
        VisitorCatalogUi.EqualText("/ " + item.Unit, card.FindElement(By.CssSelector(".vd-service-card__unit")).Text);
        if (!string.IsNullOrWhiteSpace(item.Location))
            Assert.Contains(item.Location, card.FindElement(By.CssSelector(".vd-service-card__tags")).Text);
        Assert.True(card.FindElement(By.CssSelector(".vd-btn--secondary")).Enabled);
        Assert.True(card.FindElement(By.CssSelector(".vd-btn--primary")).Enabled);
    }

    public void AssertAllPages(IReadOnlyList<CatalogListing> expected)
    {
        GoToPage(1);
        var pages = (expected.Count + PageSize - 1) / PageSize;
        for (var page = 1; page <= pages; page++)
        {
            GoToPage(page);
            AssertPage(expected, page);
        }
    }

    private IWebElement PaginationButton(string text) => ui.Browser.Driver.FindElements(By.CssSelector(".vd-pagination__btn"))
        .Single(button => button.Displayed && button.Text == text);
    public void GoToPage(int target)
    {
        while (ActivePage != target)
        {
            var current = ActivePage;
            var button = PaginationButton(current < target ? "Next" : "Previous");
            Assert.True(button.Enabled, $"Cannot navigate from catalog page {current} to {target}.");
            button.Click();
            ui.Browser.Until(_ => ActivePage != current, "Pagination did not change the current page.");
        }
    }

    public IWebElement Card(CatalogListing listing) => ui.Browser.Driver.FindElements(Cards).Single(card =>
        card.Displayed && card.FindElement(By.CssSelector(".vd-service-card__title")).Text == listing.Title &&
        card.FindElement(By.CssSelector(".vd-service-card__provider-name")).Text == "By " + listing.Provider &&
        card.FindElement(By.CssSelector(".vd-type-badge")).Text.Equals(listing.Type, StringComparison.OrdinalIgnoreCase));

    public void Show(CatalogListing listing)
    {
        Category(listing.Type.ToLowerInvariant());
        Search(listing.Title);
        var expected = Newest(Listings.Where(item => item.Type == listing.Type && item.Matches(listing.Title)));
        var index = Array.FindIndex(expected, item => item.Id == listing.Id);
        Assert.True(index >= 0);
        GoToPage(index / PageSize + 1);
        AssertPage(expected, index / PageSize + 1);
    }

    public VisitorListingDetailsPage OpenDetails(CatalogListing listing, bool clickCard = false)
    {
        Show(listing);
        if (clickCard) Card(listing).FindElement(By.CssSelector(".vd-service-card__title")).Click();
        else Card(listing).FindElement(By.CssSelector(".vd-btn--secondary")).Click();
        ui.Browser.Visible(By.CssSelector(".vd-detail-modal"));
        return new VisitorListingDetailsPage(ui, listing);
    }

    public VisitorAvailabilityPage OpenAvailabilityFromCard(CatalogListing listing)
    {
        Show(listing);
        Card(listing).FindElement(By.CssSelector(".vd-btn--primary")).Click();
        ui.Browser.Visible(By.CssSelector(".vd-booking-modal"));
        return new VisitorAvailabilityPage(ui, listing);
    }
}
