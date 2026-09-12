using CeylonQuest.Tests.Pages;
using CeylonQuest.Tests.Utilities;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.Tests.Tests;

public class FilterListingsTests : BaseTest
{
    private string Env(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{name} is not configured.");
        }

        return value;
    }

    private void ClearSession()
    {
        Driver.Navigate().GoToUrl(BaseUrl);

        ((IJavaScriptExecutor)Driver).ExecuteScript(@"
            window.localStorage.clear();
            window.sessionStorage.clear();
        ");
    }

    private void LoginAsVisitor()
    {
        ClearSession();

        Driver.Navigate().GoToUrl($"{BaseUrl}/login");

        var wait = new WebDriverWait(
            Driver,
            TimeSpan.FromSeconds(30));

        var email = wait.Until(d =>
            d.FindElement(By.Id("login-email")));

        email.Clear();
        email.SendKeys(Env("QA_VISITOR_EMAIL"));

        var password = wait.Until(d =>
            d.FindElement(By.Id("login-password")));

        password.Clear();
        password.SendKeys(Env("QA_VISITOR_PASSWORD"));

        Driver.FindElement(
            By.Id("login-button"))
            .Click();

        wait.Until(d =>
            d.FindElements(By.Id("nav-explore"))
             .Any(e => e.Displayed && e.Enabled));
    }

    private FilterListingsPage OpenFilters()
    {
        LoginAsVisitor();

        var page = new FilterListingsPage(Driver);

        page.Open();

        return page;
    }

    // ---------------------------------------------------
    // TC59-01 - Filter by Location
    // ---------------------------------------------------

    [Fact]
    public void TC59_01_FilterByLocation()
    {
        var page = OpenFilters();

        page.SelectLocation("Colombo");

        Assert.True(
            page.IsLocationActive("Colombo"),
            "Colombo location filter is not active.");

        Assert.True(
            page.VisibleCardCount() > 0,
            "No Colombo listings were returned.");

        Assert.True(
            page.AllVisibleCardsContainLocation("Colombo"),
            "One or more listings do not match Colombo.");
    }

    // ---------------------------------------------------
    // TC59-02 - Filter by Category
    // ---------------------------------------------------

    [Fact]
    public void TC59_02_FilterByCategory()
    {
        var page = OpenFilters();

        page.SelectCategory("Experiences");

        Assert.True(
            page.IsCategoryActive("Experiences"),
            "Experiences category is not active.");

        Assert.True(
            page.VisibleCardCount() > 0,
            "No Experience listings were returned.");

        Assert.True(
            page.AllVisibleCardsContainCategory("Experience"),
            "One or more listings are not Experience listings.");
    }

    // ---------------------------------------------------
    // TC59-03 - Filter by Price Range
    // ---------------------------------------------------

    [Fact]
    public void TC59_03_FilterByPriceRange()
    {
        var page = OpenFilters();

        page.SelectPriceRange5000To15000();

        var prices = page.GetVisiblePrices();

        Assert.NotEmpty(prices);

        Assert.True(
            page.AllVisiblePricesWithinRange(
                5000,
                15000),
            $"Price filter failed. Displayed prices: {string.Join(", ", prices)}");
    }

    // ---------------------------------------------------
    // TC59-04 - Combined Filters
    // ---------------------------------------------------

    [Fact]
    public void TC59_04_CombinedFilters()
    {
        var page = OpenFilters();

        page.SelectLocation("Colombo");

        Assert.True(
            page.IsLocationActive("Colombo"),
            "Colombo location filter is not active.");

        page.SelectCategory("Experiences");

        Assert.True(
            page.IsCategoryActive("Experiences"),
            "Experiences category filter is not active.");

        Assert.True(
            page.VisibleCardCount() > 0,
            "No listings matched the combined filters.");

        Assert.True(
            page.AllVisibleCardsContainLocation("Colombo"),
            "One or more listings do not match Colombo.");

        Assert.True(
            page.AllVisibleCardsContainCategory("Experience"),
            "One or more listings do not match Experience category.");
    }

    // ---------------------------------------------------
    // TC59-05 - Combined Filters With No Matching Result
    // ---------------------------------------------------

    [Fact]
    public void TC59_05_CombinedFiltersWithNoMatchingResult()
    {
        var page = OpenFilters();

        page.SelectLocation("Colombo");

        page.SelectCategory("Experiences");

        page.SetPriceRange(
            999999,
            1000000);

        Assert.True(
            page.NoResultsVisible(),
            "No-results state was not displayed.");
    }

    // ---------------------------------------------------
    // TC59-06 - Clear Active Filters
    // ---------------------------------------------------

    [Fact]
    public void TC59_06_ClearActiveFilters()
    {
        var page = OpenFilters();

        page.SelectLocation("Colombo");

        Assert.True(
            page.IsLocationActive("Colombo"),
            "Location filter was not activated.");

        page.SelectCategory("Experiences");

        Assert.True(
            page.IsCategoryActive("Experiences"),
            "Category filter was not activated.");

        Thread.Sleep(1000);

        page.ClearAllFilters();

        Assert.True(
            page.IsLocationActive("All Locations"),
            "Location filter was not cleared.");

        Assert.True(
            page.IsCategoryActive("All Listings"),
            "Category filter was not cleared.");
    }

    // ---------------------------------------------------
    // TC59-07 - Full List Returns After Clearing Filters
    // ---------------------------------------------------

    [Fact]
    public void TC59_07_FullListReturnsAfterClearingFilters()
    {
        var page = OpenFilters();

        var originalCount =
            page.GetVisibleListingCount();

        Assert.True(
            originalCount > 0,
            "Initial unfiltered listing count was zero.");

        page.SelectLocation("Colombo");

        page.SelectCategory("Experiences");

        page.ClearAllFilters();

        var afterClearCount =
            page.GetVisibleListingCount();

        Assert.Equal(
            originalCount,
            afterClearCount);
    }

    // ---------------------------------------------------
    // TC59-08 - Change One Filter While Others Remain Active
    // ---------------------------------------------------

    [Fact]
    public void TC59_08_ChangeOneFilterWhileOthersRemainActive()
    {
        var page = OpenFilters();

        page.SelectLocation("Colombo");

        Assert.True(
            page.IsLocationActive("Colombo"),
            "Colombo location was not active.");

        page.SelectCategory("Experiences");

        Assert.True(
            page.IsCategoryActive("Experiences"),
            "Experiences category was not active.");

        page.SelectLocation("All Locations");

        Assert.True(
            page.IsLocationActive("All Locations"),
            "Location filter did not change to All Locations.");

        Assert.True(
            page.IsCategoryActive("Experiences"),
            "Experience category was removed when location changed.");

        Assert.True(
            page.VisibleCardCount() > 0,
            "No Experience listings were displayed.");

        Assert.True(
            page.AllVisibleCardsContainCategory("Experience"),
            "One or more listings do not match the still-active Experience category.");
    }
}