using System.Text.Json;
using CeylonQuest.Tests.Pages;
using CeylonQuest.Tests.Utilities;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.Tests.Tests;

public class SearchBrowseTests : BaseTest
{
    private sealed record ApiResult(int Status, string Body);

    private string Env(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{name} is not configured.");

        return value;
    }

    private static string UniqueToken(string prefix)
    {
        var value = $"{prefix}{DateTime.UtcNow:HHmmssfff}{Guid.NewGuid():N}";
        return value[..Math.Min(22, prefix.Length + 17)];
    }

    private void ClearSession()
    {
        Driver.Navigate().GoToUrl(BaseUrl);

        ((IJavaScriptExecutor)Driver).ExecuteScript(@"
            window.localStorage.clear();
            window.sessionStorage.clear();
        ");
    }

    private void Login(
        string emailVariable,
        string passwordVariable,
        string dashboardElementId)
    {
        ClearSession();

        Driver.Navigate().GoToUrl($"{BaseUrl}/login");

        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(30));

        var email = wait.Until(d => d.FindElement(By.Id("login-email")));
        var password = wait.Until(d => d.FindElement(By.Id("login-password")));

        email.Clear();
        email.SendKeys(Env(emailVariable));

        password.Clear();
        password.SendKeys(Env(passwordVariable));

        Driver.FindElement(By.Id("login-button")).Click();

        wait.Until(d =>
            d.FindElements(By.Id(dashboardElementId))
             .Any(e => e.Displayed));
    }

    private void LoginAsProvider()
    {
        Login(
            "QA_PROVIDER_EMAIL",
            "QA_PROVIDER_PASSWORD",
            "pd-nav-services");
    }

    private void LoginAsVisitor()
    {
        Login(
            "QA_VISITOR_EMAIL",
            "QA_VISITOR_PASSWORD",
            "nav-explore");
    }

    private ApiResult BrowserApiRequest(
        string method,
        string path,
        string? bodyJson = null,
        bool useAuth = true)
    {
        const string script = @"
            const done = arguments[arguments.length - 1];

            const method = arguments[0];
            const path = arguments[1];
            const body = arguments[2];
            const useAuth = arguments[3];

            const headers = {};

            if (body) {
                headers['Content-Type'] = 'application/json';
            }

            if (useAuth) {
                const token = window.localStorage.getItem('authToken');

                if (token) {
                    headers['Authorization'] = 'Bearer ' + token;
                }
            }

            fetch(path, {
                method: method,
                headers: headers,
                body: body || undefined
            })
            .then(async response => {
                const text = await response.text();

                done(JSON.stringify({
                    status: response.status,
                    body: text
                }));
            })
            .catch(error => {
                done(JSON.stringify({
                    status: 0,
                    body: String(error)
                }));
            });
        ";

        var raw = (string?)((IJavaScriptExecutor)Driver)
            .ExecuteAsyncScript(
                script,
                method,
                path,
                bodyJson,
                useAuth);

        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException(
                "Browser API request returned no result.");

        using var doc = JsonDocument.Parse(raw);

        return new ApiResult(
            doc.RootElement.GetProperty("status").GetInt32(),
            doc.RootElement.GetProperty("body").GetString() ?? string.Empty);
    }

    private string CreateExperience(
        string title,
        string description)
    {
        var body = JsonSerializer.Serialize(new
        {
            title,
            description,
            price = 4500m,
            unit = "Per Person",
            location = "Negombo",
            maxParticipants = 10,
            isActive = true,
            duration = "2 Hours",
            availableDays = "Daily",
            timeSlots = "08:00 AM - 10:00 AM"
        });

        var result = BrowserApiRequest(
            "POST",
            "/api/catalog/activity-listings",
            body);

        Assert.Equal(201, result.Status);

        using var doc = JsonDocument.Parse(result.Body);

        return doc.RootElement
                  .GetProperty("id")
                  .GetString()
               ?? throw new InvalidOperationException(
                   "Experience create response did not contain an ID.");
    }

    private string CreateRestaurant(
        string name,
        string description)
    {
        var body = JsonSerializer.Serialize(new
        {
            name,
            description,
            cuisineType = "Sri Lankan",
            diningStyle = "Casual Dining",
            location = "Negombo",
            pricePerPerson = 3200m,
            priceRange = "Moderate",
            openingHours = "09:00 AM - 10:00 PM",
            setMenuDetails = "Traditional local menu",
            dietaryOptions = "Vegetarian options available",
            groupSizeCategory = "Table for Two",
            seatingCapacity = 20,
            isActive = true
        });

        var result = BrowserApiRequest(
            "POST",
            "/api/catalog/restaurant-listings",
            body);

        Assert.Equal(201, result.Status);

        using var doc = JsonDocument.Parse(result.Body);

        return doc.RootElement
                  .GetProperty("id")
                  .GetString()
               ?? throw new InvalidOperationException(
                   "Restaurant create response did not contain an ID.");
    }

    private void DeleteExperience(string id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            BrowserApiRequest(
                "DELETE",
                $"/api/catalog/activity-listings/{id}");
        }
    }

    private void DeleteRestaurant(string id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            BrowserApiRequest(
                "DELETE",
                $"/api/catalog/restaurant-listings/{id}");
        }
    }

    private void CleanupAsProvider(
        IEnumerable<string> experienceIds,
        IEnumerable<string> restaurantIds)
    {
        try
        {
            LoginAsProvider();

            foreach (var id in experienceIds)
                DeleteExperience(id);

            foreach (var id in restaurantIds)
                DeleteRestaurant(id);
        }
        catch
        {
            // Cleanup failure should not hide the original test failure.
        }
    }

    private SearchBrowsePage OpenVisitorExplore()
    {
        LoginAsVisitor();

        var page = new SearchBrowsePage(Driver);
        page.Open();

        return page;
    }

    private void VerifyBackendSearchContains(
        string keyword,
        string expectedTitle)
    {
        var url =
            "http://localhost:5141/api/catalog/search" +
            $"?q={Uri.EscapeDataString(keyword)}" +
            "&page=1&pageSize=8";

        Driver.Navigate().GoToUrl(url);

        var wait = new WebDriverWait(
            Driver,
            TimeSpan.FromSeconds(20));

        wait.Until(d =>
            d.PageSource.Contains(
                expectedTitle,
                StringComparison.OrdinalIgnoreCase));

        Assert.Contains(
            expectedTitle,
            Driver.PageSource,
            StringComparison.OrdinalIgnoreCase);
    }

    private void AssertListingType(
        SearchBrowsePage page,
        string title,
        string expectedType)
    {
        var actualType = page.GetListingType(title);

        Assert.True(
            string.Equals(
                expectedType,
                actualType,
                StringComparison.OrdinalIgnoreCase),
            $"Expected listing type '{expectedType}', but actual type was '{actualType}'.");
    }

    private List<string> CreatePaginationData(
        int count,
        string commonToken)
    {
        var ids = new List<string>();

        for (var i = 1; i <= count; i++)
        {
            ids.Add(
                CreateExperience(
                    $"QA Browse {commonToken} {i:00}",
                    $"Pagination QA experience {i:00} for {commonToken}."));
        }

        return ids;
    }

    // =========================================================
    // TC58-01 - Search keyword matching an experience
    // =========================================================

    [Fact]
    public void TC58_01_SearchKeywordMatchingExperience()
    {
        var experienceIds = new List<string>();

        var token = UniqueToken("dive");
        var title = $"QA Diving Adventure {token}";

        LoginAsProvider();

        experienceIds.Add(
            CreateExperience(
                title,
                $"Guided reef diving adventure for Selenium search testing {token}."));

        try
        {
            VerifyBackendSearchContains(token, title);

            var page = OpenVisitorExplore();

            page.Search(token);
            page.WaitForListing(title);

            Assert.True(
                page.HasListing(title),
                $"Expected experience '{title}' was not displayed.");

            AssertListingType(
                page,
                title,
                "Experience");
        }
        finally
        {
            CleanupAsProvider(
                experienceIds,
                Array.Empty<string>());
        }
    }

    // =========================================================
    // TC58-02 - Search keyword matching a restaurant
    // =========================================================

    [Fact]
    public void TC58_02_SearchKeywordMatchingRestaurant()
    {
        var restaurantIds = new List<string>();

        var token = UniqueToken("food");
        var name = $"QA Restaurant {token}";

        LoginAsProvider();

        restaurantIds.Add(
            CreateRestaurant(
                name,
                $"Fresh local dishes prepared for Selenium search testing {token}."));

        try
        {
            VerifyBackendSearchContains(token, name);

            var page = OpenVisitorExplore();

            page.Search(token);
            page.WaitForListing(name);

            Assert.True(
                page.HasListing(name),
                $"Expected restaurant '{name}' was not displayed.");

            AssertListingType(
                page,
                name,
                "Restaurant");
        }
        finally
        {
            CleanupAsProvider(
                Array.Empty<string>(),
                restaurantIds);
        }
    }

    // =========================================================
    // TC58-03 - Keyword matches experiences and restaurants
    // =========================================================

    [Fact]
    public void TC58_03_KeywordMatchesExperiencesAndRestaurants()
    {
        var experienceIds = new List<string>();
        var restaurantIds = new List<string>();

        var token = UniqueToken("common");

        var experienceTitle = $"QA Experience {token}";
        var restaurantName = $"QA Dining {token}";

        LoginAsProvider();

        experienceIds.Add(
            CreateExperience(
                experienceTitle,
                $"Shared discovery keyword {token}."));

        restaurantIds.Add(
            CreateRestaurant(
                restaurantName,
                $"Shared discovery keyword {token} for dining."));

        try
        {
            VerifyBackendSearchContains(
                token,
                experienceTitle);

            VerifyBackendSearchContains(
                token,
                restaurantName);

            var page = OpenVisitorExplore();

            page.Search(token);

            page.WaitForListing(experienceTitle);
            page.WaitForListing(restaurantName);

            Assert.True(page.HasListing(experienceTitle));
            Assert.True(page.HasListing(restaurantName));

            AssertListingType(
                page,
                experienceTitle,
                "Experience");

            AssertListingType(
                page,
                restaurantName,
                "Restaurant");

            Assert.True(
                page.VisibleCardCount() >= 2,
                "Expected at least two matching listings.");
        }
        finally
        {
            CleanupAsProvider(
                experienceIds,
                restaurantIds);
        }
    }

    // =========================================================
    // TC58-04 - Search with no matching listings
    // =========================================================

    [Fact]
    public void TC58_04_SearchWithNoMatchingListings()
    {
        var page = OpenVisitorExplore();

        var impossibleKeyword =
            $"noresult{Guid.NewGuid():N}";

        page.Search(impossibleKeyword);

        var message =
            page.WaitForNoResults(impossibleKeyword);

        Assert.True(
            page.NoResultsMessageVisible(),
            "Friendly no-results message was not displayed.");

        Assert.Contains(
            "No listings matched your search",
            message,
            StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================
    // TC58-05 - Browse with no keyword
    // =========================================================

    [Fact]
    public void TC58_05_BrowseWithNoKeyword()
    {
        var experienceIds = new List<string>();

        var token = UniqueToken("browse");
        var title = $"QA Browse Listing {token}";

        LoginAsProvider();

        experienceIds.Add(
            CreateExperience(
                title,
                "Active listing used to verify browse mode."));

        try
        {
            var page = OpenVisitorExplore();

            page.ClearSearch();

            Assert.True(
                page.VisibleCardCount() > 0,
                "Expected at least one listing in browse mode.");

            Assert.False(
                page.NoResultsMessageVisible());

            Assert.Contains(
                "Showing",
                page.ResultsCountText(),
                StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            CleanupAsProvider(
                experienceIds,
                Array.Empty<string>());
        }
    }

    // =========================================================
    // TC58-06 - Move to next pagination page
    // =========================================================

    [Fact]
    public void TC58_06_MoveToNextPaginationPage()
    {
        var experienceIds = new List<string>();
        var token = UniqueToken("page");

        LoginAsProvider();

        experienceIds.AddRange(
            CreatePaginationData(
                9,
                token));

        try
        {
            var page = OpenVisitorExplore();

            page.ClearSearch();

            Assert.True(
                page.PaginationVisible(),
                "Pagination was not visible.");

            Assert.Equal(
                1,
                page.CurrentPage());

            var firstPageTitles =
                page.VisibleTitles();

            page.GoToNextPage();

            Assert.Equal(
                2,
                page.CurrentPage());

            var secondPageTitles =
                page.VisibleTitles();

            Assert.NotEmpty(secondPageTitles);

            Assert.NotEqual(
                string.Join("|", firstPageTitles),
                string.Join("|", secondPageTitles));
        }
        finally
        {
            CleanupAsProvider(
                experienceIds,
                Array.Empty<string>());
        }
    }

    // =========================================================
    // TC58-07 - Return to previous pagination page
    // =========================================================

    [Fact]
    public void TC58_07_ReturnToPreviousPaginationPage()
    {
        var experienceIds = new List<string>();
        var token = UniqueToken("prev");

        LoginAsProvider();

        experienceIds.AddRange(
            CreatePaginationData(
                9,
                token));

        try
        {
            var page = OpenVisitorExplore();

            page.ClearSearch();

            Assert.True(
                page.PaginationVisible(),
                "Pagination was not visible.");

            Assert.Equal(
                1,
                page.CurrentPage());

            page.GoToNextPage();

            Assert.Equal(
                2,
                page.CurrentPage());

            page.GoToPreviousPage();

            Assert.Equal(
                1,
                page.CurrentPage());
        }
        finally
        {
            CleanupAsProvider(
                experienceIds,
                Array.Empty<string>());
        }
    }

    // =========================================================
    // TC58-08 - Search after browsing another page
    // =========================================================

    [Fact]
    public void TC58_08_SearchAfterBrowsingAnotherPage()
    {
        var experienceIds = new List<string>();

        var paginationToken =
            UniqueToken("edge");

        var searchToken =
            UniqueToken("target");

        var targetTitle =
            $"QA Search Target {searchToken}";

        LoginAsProvider();

        experienceIds.AddRange(
            CreatePaginationData(
                9,
                paginationToken));

        experienceIds.Add(
            CreateExperience(
                targetTitle,
                $"Unique listing used after browsing page two {searchToken}."));

        try
        {
            VerifyBackendSearchContains(
                searchToken,
                targetTitle);

            var page = OpenVisitorExplore();

            page.ClearSearch();

            Assert.True(
                page.PaginationVisible(),
                "Pagination was not visible.");

            page.GoToNextPage();

            Assert.Equal(
                2,
                page.CurrentPage());

            page.Search(searchToken);
            page.WaitForListing(targetTitle);

            Assert.True(
                page.HasListing(targetTitle),
                $"Expected listing '{targetTitle}' was not displayed.");

            if (page.PaginationVisible())
            {
                Assert.Equal(
                    1,
                    page.CurrentPage());
            }
        }
        finally
        {
            CleanupAsProvider(
                experienceIds,
                Array.Empty<string>());
        }
    }

    // =========================================================
    // TC58-09 - Search keyword in listing description
    // =========================================================

    [Fact]
    public void TC58_09_SearchKeywordInListingDescription()
    {
        var experienceIds = new List<string>();

        var descriptionToken =
            UniqueToken("desc");

        var title =
            $"QA Scenic Adventure {Guid.NewGuid():N}";

        LoginAsProvider();

        experienceIds.Add(
            CreateExperience(
                title,
                $"This description contains the unique searchable term {descriptionToken}."));

        try
        {
            VerifyBackendSearchContains(
                descriptionToken,
                title);

            var page = OpenVisitorExplore();

            page.Search(descriptionToken);
            page.WaitForListing(title);

            Assert.True(
                page.HasListing(title),
                $"Expected listing '{title}' was not displayed.");

            Assert.False(
                title.Contains(
                    descriptionToken,
                    StringComparison.OrdinalIgnoreCase),
                "The description keyword should not be part of the title.");
        }
        finally
        {
            CleanupAsProvider(
                experienceIds,
                Array.Empty<string>());
        }
    }
}

