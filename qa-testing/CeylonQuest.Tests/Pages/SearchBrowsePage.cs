using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Pages;

public class SearchBrowsePage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public SearchBrowsePage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
    }

    private IWebElement ExploreNavigation =>
        _wait.Until(d =>
            d.FindElements(By.Id("nav-explore"))
                .First(e => e.Displayed));

    private IWebElement SearchInput =>
        _wait.Until(d =>
            d.FindElements(
                    By.CssSelector("input[aria-label='Search listings']"))
                .First(e => e.Displayed));

    public void Open()
    {
        ExploreNavigation.Click();

        _wait.Until(d =>
            d.FindElements(
                    By.XPath("//h1[normalize-space()='Explore Sri Lanka']"))
                .Any(e => e.Displayed));

        WaitForResultsToFinishLoading();
    }

    public void Search(string keyword)
    {
        var input = SearchInput;

        input.Click();
        input.Clear();
        input.SendKeys(keyword);
        input.SendKeys(Keys.Enter);

        WaitForSearchToSettle();
    }

    public void ClearSearch()
    {
        var clearButton = _driver
            .FindElements(By.CssSelector("button[aria-label='Clear search']"))
            .FirstOrDefault(e => e.Displayed && e.Enabled);

        if (clearButton != null)
        {
            clearButton.Click();
        }
        else
        {
            var input = SearchInput;
            input.Click();
            input.Clear();
            input.SendKeys(Keys.Enter);
        }

        WaitForSearchToSettle();
    }

    public IReadOnlyCollection<IWebElement> VisibleCards()
    {
        return _driver
            .FindElements(By.CssSelector(".vd-service-card"))
            .Where(e => e.Displayed)
            .ToList();
    }

    public int VisibleCardCount()
    {
        return VisibleCards().Count;
    }

    public bool HasListing(string title)
    {
        return VisibleCards().Any(card =>
            card.FindElements(
                    By.CssSelector(".vd-service-card__title"))
                .Any(e =>
                    e.Displayed &&
                    string.Equals(
                        e.Text.Trim(),
                        title,
                        StringComparison.OrdinalIgnoreCase)));
    }

    public IWebElement GetListingCard(string title)
    {
        return _wait.Until(_ =>
        {
            return VisibleCards()
                .FirstOrDefault(card =>
                    card.FindElements(
                            By.CssSelector(".vd-service-card__title"))
                        .Any(e =>
                            e.Displayed &&
                            string.Equals(
                                e.Text.Trim(),
                                title,
                                StringComparison.OrdinalIgnoreCase)));
        })!;
    }

    public string GetListingType(string title)
    {
        var card = GetListingCard(title);

        return card
            .FindElement(By.CssSelector(".vd-type-badge"))
            .Text
            .Trim();
    }

    public IReadOnlyList<string> VisibleTitles()
    {
        return VisibleCards()
            .Select(card =>
                card.FindElement(
                        By.CssSelector(".vd-service-card__title"))
                    .Text
                    .Trim())
            .ToList();
    }

    public string ResultsCountText()
    {
        return _wait
            .Until(d =>
                d.FindElements(By.CssSelector(".vd-results-count"))
                    .First(e => e.Displayed))
            .Text
            .Trim();
    }

    public bool NoResultsMessageVisible()
    {
        return _driver
            .FindElements(By.CssSelector(".vd-empty-search"))
            .Any(e =>
                e.Displayed &&
                e.Text.Contains(
                    "No listings matched your search",
                    StringComparison.OrdinalIgnoreCase));
    }

    public string WaitForNoResults(string keyword)
    {
        return _wait.Until(d =>
        {
            var empty = d
                .FindElements(By.CssSelector(".vd-empty-search"))
                .FirstOrDefault(e => e.Displayed);

            if (empty == null)
                return null;

            if (!empty.Text.Contains(
                    "No listings matched your search",
                    StringComparison.OrdinalIgnoreCase))
                return null;

            if (!empty.Text.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase))
                return null;

            return empty.Text;
        })!;
    }

    public string NoResultsText()
    {
        return _wait
            .Until(d =>
                d.FindElements(By.CssSelector(".vd-empty-search"))
                    .First(e => e.Displayed))
            .Text;
    }

    public bool PaginationVisible()
    {
        return _driver
            .FindElements(By.CssSelector(".vd-pagination"))
            .Any(e => e.Displayed);
    }

    public string PageIndicatorText()
    {
        return _wait
            .Until(d =>
                d.FindElements(By.CssSelector(".vd-page-indicator"))
                    .First(e => e.Displayed))
            .Text
            .Trim();
    }

    public int CurrentPage()
    {
        var text = PageIndicatorText();

        var parts = text.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 2 &&
            int.TryParse(parts[1], out var page))
        {
            return page;
        }

        throw new InvalidOperationException(
            $"Could not parse page indicator: '{text}'.");
    }

    public void GoToNextPage()
    {
        var currentPage = CurrentPage();

        var button = _wait.Until(d =>
            d.FindElements(
                    By.XPath(
                        "//button[contains(@class,'vd-page-btn') " +
                        "and contains(normalize-space(),'Next')]"))
                .FirstOrDefault(e =>
                    e.Displayed &&
                    e.Enabled));

        button!.Click();

        _wait.Until(_ =>
            CurrentPage() == currentPage + 1);

        WaitForResultsToFinishLoading();
    }

    public void GoToPreviousPage()
    {
        var currentPage = CurrentPage();

        var button = _wait.Until(d =>
            d.FindElements(
                    By.XPath(
                        "//button[contains(@class,'vd-page-btn') " +
                        "and contains(normalize-space(),'Previous')]"))
                .FirstOrDefault(e =>
                    e.Displayed &&
                    e.Enabled));

        button!.Click();

        _wait.Until(_ =>
            CurrentPage() == currentPage - 1);

        WaitForResultsToFinishLoading();
    }

    public void WaitForListing(string title)
    {
        _wait.Until(d =>
        {
            var titles = d.FindElements(
                By.CssSelector(".vd-service-card__title"));

            return titles.Any(e =>
                e.Displayed &&
                string.Equals(
                    e.Text.Trim(),
                    title,
                    StringComparison.OrdinalIgnoreCase));
        });
    }

    private void WaitForSearchToSettle()
    {
        _wait.Until(d =>
        {
            var loading = d
                .FindElements(By.CssSelector(".vd-loading-card"))
                .Any(e => e.Displayed);

            if (loading)
                return false;

            var cards = d
                .FindElements(By.CssSelector(".vd-service-card"))
                .Any(e => e.Displayed);

            var empty = d
                .FindElements(By.CssSelector(".vd-empty-search"))
                .Any(e => e.Displayed);

            return cards || empty;
        });
    }

    private void WaitForResultsToFinishLoading()
    {
        _wait.Until(d =>
            !d.FindElements(
                    By.CssSelector(".vd-loading-card"))
                .Any(e => e.Displayed));
    }
}