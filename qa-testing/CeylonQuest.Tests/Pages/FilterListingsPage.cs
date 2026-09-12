using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CeylonQuest.Tests.Pages;

public class FilterListingsPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public FilterListingsPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
    }

    public void Open()
    {
        var exploreButton = _wait.Until(d =>
            d.FindElements(By.Id("nav-explore"))
             .FirstOrDefault(e => e.Displayed && e.Enabled));

        if (exploreButton == null)
            throw new NoSuchElementException("Explore navigation button was not found.");

        exploreButton.Click();

        _wait.Until(d =>
            d.FindElements(By.CssSelector(".vd-filter-panel"))
             .Any(e => e.Displayed));

        WaitForResults();
    }

    public void SelectLocation(string location)
    {
        var button = _wait.Until(d =>
            d.FindElements(By.CssSelector(".vd-loc-pill"))
             .FirstOrDefault(e =>
                 e.Displayed &&
                 e.Enabled &&
                 e.Text.Trim().Equals(
                     location,
                     StringComparison.OrdinalIgnoreCase)));

        if (button == null)
            throw new NoSuchElementException(
                $"Location '{location}' was not found.");

        button.Click();

        Thread.Sleep(800);

        WaitForResults();
    }

    public bool IsLocationActive(string location)
    {
        var button = _driver
            .FindElements(By.CssSelector(".vd-loc-pill"))
            .FirstOrDefault(e =>
                e.Displayed &&
                e.Text.Trim().Equals(
                    location,
                    StringComparison.OrdinalIgnoreCase));

        if (button == null)
            return false;

        var classes = button.GetAttribute("class") ?? "";

        return classes.Contains(
            "active",
            StringComparison.OrdinalIgnoreCase);
    }

    public void SelectCategory(string category)
    {
        var button = _wait.Until(d =>
            d.FindElements(By.CssSelector(".vd-type-pill"))
             .FirstOrDefault(e =>
                 e.Displayed &&
                 e.Enabled &&
                 e.Text.Contains(
                     category,
                     StringComparison.OrdinalIgnoreCase)));

        if (button == null)
            throw new NoSuchElementException(
                $"Category '{category}' was not found.");

        button.Click();

        Thread.Sleep(800);

        WaitForResults();
    }

    public bool IsCategoryActive(string category)
    {
        var button = _driver
            .FindElements(By.CssSelector(".vd-type-pill"))
            .FirstOrDefault(e =>
                e.Displayed &&
                e.Text.Contains(
                    category,
                    StringComparison.OrdinalIgnoreCase));

        if (button == null)
            return false;

        var classes = button.GetAttribute("class") ?? "";

        return classes.Contains(
            "active",
            StringComparison.OrdinalIgnoreCase);
    }

    public void SelectPriceRange5000To15000()
    {
        var button = _wait.Until(d =>
            d.FindElements(By.CssSelector(".vd-preset-btn"))
             .FirstOrDefault(e =>
                 e.Displayed &&
                 e.Enabled &&
                 e.Text.Contains("5k") &&
                 e.Text.Contains("15k")));

        if (button == null)
            throw new NoSuchElementException(
                "5k - 15k price preset was not found.");

        button.Click();

        Thread.Sleep(1000);

        WaitForResults();

        Thread.Sleep(300);
    }

    public void SetPriceRange(
        decimal minimum,
        decimal maximum)
    {
        var inputs = _wait.Until(d =>
            d.FindElements(By.CssSelector(".vd-price-input"))
             .Where(e => e.Displayed)
             .ToList());

        if (inputs.Count < 2)
            throw new NoSuchElementException(
                "Minimum and maximum price inputs were not found.");

        var minimumInput = inputs[0];
        var maximumInput = inputs[1];

        minimumInput.Clear();

        minimumInput.SendKeys(
            minimum.ToString(
                CultureInfo.InvariantCulture));

        maximumInput.Clear();

        maximumInput.SendKeys(
            maximum.ToString(
                CultureInfo.InvariantCulture));

        maximumInput.SendKeys(Keys.Tab);

        Thread.Sleep(1200);

        WaitForResults();
    }

    public void ClearAllFilters()
    {
        var button = _wait.Until(d =>
            d.FindElements(By.CssSelector(".vd-clear-all-btn"))
             .FirstOrDefault(e =>
                 e.Displayed &&
                 e.Enabled));

        if (button == null)
            throw new NoSuchElementException(
                "Clear All Filters button was not found.");

        button.Click();

        Thread.Sleep(1000);

        WaitForResults();
    }

    public bool FiltersAreCleared()
    {
        var allLocationsActive =
            IsLocationActive("All Locations");

        var allListingsActive =
            IsCategoryActive("All Listings");

        return allLocationsActive &&
               allListingsActive;
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

    public int GetVisibleListingCount()
    {
        return VisibleCards().Count;
    }

    public bool AllVisibleCardsContainLocation(
        string location)
    {
        var cards = VisibleCards();

        if (cards.Count == 0)
            return false;

        return cards.All(card =>
            card.Text.Contains(
                location,
                StringComparison.OrdinalIgnoreCase));
    }

    public bool AllVisibleCardsContainCategory(
        string category)
    {
        var cards = VisibleCards();

        if (cards.Count == 0)
            return false;

        return cards.All(card =>
            card.Text.Contains(
                category,
                StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<decimal> GetVisiblePrices()
    {
        var prices = new List<decimal>();

        foreach (var card in VisibleCards())
        {
            var priceElement = card
                .FindElements(
                    By.CssSelector(
                        ".vd-service-card__price"))
                .FirstOrDefault();

            if (priceElement == null)
                continue;

            var match = Regex.Match(
                priceElement.Text,
                @"[\d,]+(?:\.\d+)?");

            if (!match.Success)
                continue;

            var cleanedPrice =
                match.Value.Replace(",", "");

            if (decimal.TryParse(
                    cleanedPrice,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var price))
            {
                prices.Add(price);
            }
        }

        return prices;
    }

    public bool AllVisiblePricesWithinRange(
        decimal minimum,
        decimal maximum)
    {
        var prices = GetVisiblePrices();

        if (prices.Count == 0)
            return false;

        return prices.All(price =>
            price >= minimum &&
            price <= maximum);
    }

    public bool NoResultsVisible()
    {
        return _driver
            .FindElements(
                By.CssSelector(".vd-empty-search"))
            .Any(e => e.Displayed);
    }

    private void WaitForResults()
    {
        _wait.Until(d =>
        {
            var loading =
                d.FindElements(
                    By.CssSelector(".vd-loading-card"))
                 .Any(e => e.Displayed);

            if (loading)
                return false;

            var cards =
                d.FindElements(
                    By.CssSelector(".vd-service-card"))
                 .Any(e => e.Displayed);

            var empty =
                d.FindElements(
                    By.CssSelector(".vd-empty-search"))
                 .Any(e => e.Displayed);

            return cards || empty;
        });
    }
}