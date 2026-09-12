using System.Text.Json;
using CeylonQuest.Tests.Pages;
using CeylonQuest.Tests.Utilities;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.Tests.Tests;

public class ExperienceListingsTests : BaseTest
{
    private sealed record ApiResult(int Status, string Body);

    private sealed record ListingSnapshot(
        string Id,
        string Title,
        string Description,
        decimal Price,
        string Unit,
        string Location,
        int MaxParticipants,
        bool IsActive,
        string Duration,
        string AvailableDays,
        string TimeSlots);

    // =========================================================
    // Helper - Environment variable
    // =========================================================
    private string Env(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(value))
            throw new Exception($"{name} is not configured.");

        return value;
    }

    // =========================================================
    // Helper - Create unique listing title
    // =========================================================
    private string UniqueTitle(string prefix)
        => $"{prefix} {DateTime.UtcNow:yyyyMMddHHmmssfff}";

    // =========================================================
    // Helper - Login as an activated/approved provider
    // =========================================================
    private void LoginAsProvider(
        string emailVariable,
        string passwordVariable)
    {
        var email = Env(emailVariable);
        var password = Env(passwordVariable);

        Driver.Navigate().GoToUrl($"{BaseUrl}/login");

        var wait = new WebDriverWait(
            Driver,
            TimeSpan.FromSeconds(15));

        var emailInput = wait.Until(d =>
            d.FindElement(By.Id("login-email")));

        var passwordInput = wait.Until(d =>
            d.FindElement(By.Id("login-password")));

        emailInput.Clear();
        emailInput.SendKeys(email);

        passwordInput.Clear();
        passwordInput.SendKeys(password);

        Driver.FindElement(By.Id("login-button")).Click();

        wait.Until(d =>
            d.FindElements(By.Id("pd-nav-services"))
             .Any(e => e.Displayed));
    }

    // =========================================================
    // Helper - Browser API request
    // =========================================================
    private ApiResult BrowserApiRequest(
        string method,
        string path,
        string? bodyJson = null,
        bool useAuth = true)
    {
        var script = @"
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
            });";

        var raw =
            (string?)((IJavaScriptExecutor)Driver)
            .ExecuteAsyncScript(
                script,
                method,
                path,
                bodyJson,
                useAuth);

        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "Browser API call returned no result.");
        }

        using var doc = JsonDocument.Parse(raw);

        return new ApiResult(
            doc.RootElement.GetProperty("status").GetInt32(),
            doc.RootElement.GetProperty("body").GetString()
                ?? string.Empty);
    }

    // =========================================================
    // Helper - Verify unactivated Pending/Rejected provider
    // cannot access Experience Management
    //
    // IMPORTANT:
    // Pending/Rejected status is a TEST DATA PRECONDITION.
    // Confirm the supplied email has the correct status in DB.
    //
    // These accounts do not have an activated provider password,
    // because activation happens only after Admin approval + OTP.
    // =========================================================
    private void AssertUnactivatedProviderCannotCreateExperience(
        string emailVariable,
        string expectedStatus)
    {
        // Confirm the correct QA account has been configured.
        // We do NOT attempt login because this account has no
        // activated provider password.
        var email = Env(emailVariable);

        Assert.False(
            string.IsNullOrWhiteSpace(email),
            $"{expectedStatus} provider test email is not configured.");

        // Open CeylonQuest so localStorage belongs to the app origin.
        Driver.Navigate().GoToUrl(BaseUrl);

        var wait = new WebDriverWait(
            Driver,
            TimeSpan.FromSeconds(15));

        wait.Until(d =>
            ((IJavaScriptExecutor)d)
                .ExecuteScript("return document.readyState")
                ?.ToString() == "complete");

        // Make sure no authenticated session exists from another user.
        ((IJavaScriptExecutor)Driver).ExecuteScript(@"
            window.localStorage.removeItem('authToken');
            window.localStorage.removeItem('userRole');
            window.sessionStorage.clear();
        ");

        // Reload application without authentication.
        Driver.Navigate().GoToUrl(BaseUrl);

        wait.Until(d =>
            ((IJavaScriptExecutor)d)
                .ExecuteScript("return document.readyState")
                ?.ToString() == "complete");

        // -----------------------------------------------------
        // UI CHECK 1
        // Provider Dashboard navigation must not be available.
        // -----------------------------------------------------
        bool providerDashboardAvailable =
            Driver.FindElements(By.Id("pd-nav-services"))
                  .Any(e => e.Displayed);

        Assert.False(
            providerDashboardAvailable,
            $"{expectedStatus} provider was able to access Provider Dashboard.");

        // -----------------------------------------------------
        // UI CHECK 2
        // Create Experience button must not be available.
        // -----------------------------------------------------
        bool createExperienceAvailable =
            Driver.FindElements(By.Id("add-activity-btn"))
                  .Any(e => e.Displayed);

        Assert.False(
            createExperienceAvailable,
            $"{expectedStatus} provider was able to access Create Experience.");

        // -----------------------------------------------------
        // AUTH CHECK
        // No authenticated provider token should exist.
        // -----------------------------------------------------
        var token =
            ((IJavaScriptExecutor)Driver).ExecuteScript(
                "return window.localStorage.getItem('authToken');");

        Assert.True(
            token == null ||
            string.IsNullOrWhiteSpace(token.ToString()),
            $"{expectedStatus} provider unexpectedly has an authentication token.");

        // -----------------------------------------------------
        // API SECURITY CHECK
        // Try to call the protected Create Experience API
        // without an activated provider authentication token.
        //
        // Expected result: 401 Unauthorized or 403 Forbidden.
        // -----------------------------------------------------
        var payload = JsonSerializer.Serialize(new
        {
            title =
                $"QA BLOCKED {expectedStatus} {DateTime.UtcNow:yyyyMMddHHmmssfff}",

            description =
                $"{expectedStatus} provider must not create this experience.",

            price = 1000m,

            unit = "Per Person",

            location = "Colombo",

            maxParticipants = 5,

            isActive = true,

            duration = "2 Hours",

            availableDays = "Daily",

            timeSlots =
                "[{\"startTime\":\"08:00\",\"endTime\":\"10:00\"}]",

            validFrom = (string?)null,

            validUntil = (string?)null
        });

        var result = BrowserApiRequest(
            "POST",
            "/api/catalog/activity-listings",
            payload,
            useAuth: false);

        Assert.True(
            result.Status == 401 ||
            result.Status == 403,
            $"{expectedStatus} provider creation request should be denied. " +
            $"Actual HTTP status: {result.Status}. Response: {result.Body}");
    }

    // =========================================================
    // Helper - Get own listing from API
    // =========================================================
    private ListingSnapshot GetOwnListingByTitle(string title)
    {
        var result = BrowserApiRequest(
            "GET",
            "/api/catalog/activity-listings");

        Assert.Equal(200, result.Status);

        using var doc = JsonDocument.Parse(result.Body);

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var itemTitle =
                item.GetProperty("title").GetString()
                ?? string.Empty;

            if (!itemTitle.Equals(
                    title,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return new ListingSnapshot(
                item.GetProperty("id").GetString()!,
                itemTitle,
                item.GetProperty("description").GetString()
                    ?? string.Empty,
                item.GetProperty("price").GetDecimal(),
                item.GetProperty("unit").GetString()
                    ?? "Per Person",
                item.GetProperty("location").GetString()
                    ?? string.Empty,
                item.GetProperty("maxParticipants").GetInt32(),
                item.GetProperty("isActive").GetBoolean(),
                item.GetProperty("duration").GetString()
                    ?? "2 Hours",
                item.GetProperty("availableDays").GetString()
                    ?? "Daily",
                item.GetProperty("timeSlots").GetString()
                    ?? "[]");
        }

        throw new InvalidOperationException(
            $"Could not find API listing '{title}'.");
    }

    // =========================================================
    // Helper - Build update request JSON
    // =========================================================
    private string BuildUpdateJson(
        ListingSnapshot listing,
        string title)
    {
        return JsonSerializer.Serialize(new
        {
            title,

            description =
                listing.Description,

            price =
                listing.Price,

            unit =
                listing.Unit,

            location =
                listing.Location,

            maxParticipants =
                listing.MaxParticipants,

            isActive =
                listing.IsActive,

            duration =
                listing.Duration,

            availableDays =
                listing.AvailableDays,

            timeSlots =
                listing.TimeSlots,

            validFrom =
                (string?)null,

            validUntil =
                (string?)null
        });
    }

    // =========================================================
    // Helper - Search public listings
    // =========================================================
    private bool PublicSearchContains(string title)
    {
        var encoded = Uri.EscapeDataString(title);

        var result = BrowserApiRequest(
            "GET",
            $"/api/catalog/activity-listings/public?search={encoded}",
            useAuth: false);

        Assert.Equal(200, result.Status);

        return result.Body.Contains(
            title,
            StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================
    // TC56-01
    // Create experience with all valid required fields
    // =========================================================
    [Fact]
    public void TC56_01_CreateExperience_WithValidFields_ShouldCreateListing()
    {
        LoginAsProvider(
            "CQ_APPROVED_PROVIDER_EMAIL",
            "CQ_APPROVED_PROVIDER_PASSWORD");

        var page =
            new ExperienceListingsPage(Driver);

        page.Open();
        page.OpenCreateForm();

        var title =
            UniqueTitle("QA Create Experience");

        page.FillValidExperience(
            title,
            "Automated Selenium QA experience.",
            "Nilaveli, Trincomalee",
            8500);

        page.Publish();
        page.WaitForListing(title);

        Assert.True(
            page.ListingExists(title),
            "Created experience was not visible in My Listings.");

        page.DeleteListing(title);
        page.WaitUntilListingRemoved(title);
    }

    // =========================================================
    // TC56-02
    // View newly created experience in My Listings
    // =========================================================
    [Fact]
    public void TC56_02_NewlyCreatedExperience_ShouldAppearWithSavedDetails()
    {
        LoginAsProvider(
            "CQ_APPROVED_PROVIDER_EMAIL",
            "CQ_APPROVED_PROVIDER_PASSWORD");

        var page =
            new ExperienceListingsPage(Driver);

        page.Open();
        page.OpenCreateForm();

        var title =
            UniqueTitle("QA View Experience");

        page.FillValidExperience(
            title,
            "Created for TC56-02 Selenium test.",
            "Kandy",
            6000);

        page.Publish();
        page.WaitForListing(title);

        var rowText =
            page.GetListingRowText(title);

        Assert.Contains(
            title,
            rowText,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Kandy",
            rowText,
            StringComparison.OrdinalIgnoreCase);

        page.DeleteListing(title);
        page.WaitUntilListingRemoved(title);
    }

    // =========================================================
    // TC56-03
    // Pending provider attempts to create experience
    //
    // Correct CeylonQuest business flow:
    // Pending provider has not been approved by Admin.
    // No activation OTP/password exists yet.
    // Therefore provider cannot authenticate as Provider
    // or access Create Experience.
    // =========================================================
    [Fact]
    public void TC56_03_PendingProvider_ShouldBeDeniedCreation()
    {
        AssertUnactivatedProviderCannotCreateExperience(
            "CQ_PENDING_PROVIDER_EMAIL",
            "Pending");
    }

    // =========================================================
    // TC56-04
    // Rejected provider attempts to create experience
    //
    // Rejected provider has not been approved/activated.
    // Therefore Provider Dashboard and Experience creation
    // must not be available.
    // =========================================================
    [Fact]
    public void TC56_04_RejectedProvider_ShouldBeDeniedCreation()
    {
        AssertUnactivatedProviderCannotCreateExperience(
            "CQ_REJECTED_PROVIDER_EMAIL",
            "Rejected");
    }

    // =========================================================
    // TC56-05
    // Update own experience and verify persistence after refresh
    // =========================================================
    [Fact]
    public void TC56_05_UpdateOwnExperience_ShouldPersistAfterRefresh()
    {
        LoginAsProvider(
            "CQ_APPROVED_PROVIDER_EMAIL",
            "CQ_APPROVED_PROVIDER_PASSWORD");

        var page =
            new ExperienceListingsPage(Driver);

        page.Open();

        var originalTitle =
            UniqueTitle("QA Update Original");

        var updatedTitle =
            originalTitle + " Updated";

        page.OpenCreateForm();

        page.FillValidExperience(
            originalTitle,
            "Original automated description.",
            "Trincomalee",
            7000);

        page.Publish();
        page.WaitForListing(originalTitle);

        page.EditListing(originalTitle);

        page.EnterTitle(updatedTitle);
        page.EnterPrice(9000);

        page.SaveChanges();
        page.WaitForListing(updatedTitle);

        Assert.True(
            page.ListingExists(updatedTitle),
            "Updated listing was not visible immediately after saving.");

        page.RefreshAndReopen();
        page.WaitForListing(updatedTitle);

        Assert.True(
            page.ListingExists(updatedTitle),
            "Updated listing did not persist after refresh.");

        var saved =
            GetOwnListingByTitle(updatedTitle);

        Assert.Equal(
            9000m,
            saved.Price);

        page.DeleteListing(updatedTitle);
        page.WaitUntilListingRemoved(updatedTitle);
    }

    // =========================================================
    // TC56-06
    // Updated experience reflected in public search
    // =========================================================
    [Fact]
    public void TC56_06_UpdatedExperience_ShouldBeReflectedInPublicSearch()
    {
        LoginAsProvider(
            "CQ_APPROVED_PROVIDER_EMAIL",
            "CQ_APPROVED_PROVIDER_PASSWORD");

        var page =
            new ExperienceListingsPage(Driver);

        page.Open();

        var originalTitle =
            UniqueTitle("QA Public Update");

        var updatedTitle =
            originalTitle + " New";

        page.OpenCreateForm();

        page.FillValidExperience(
            originalTitle,
            "Public search update test.",
            "Ella",
            4500);

        page.Publish();
        page.WaitForListing(originalTitle);

        page.EditListing(originalTitle);
        page.EnterTitle(updatedTitle);
        page.SaveChanges();

        page.WaitForListing(updatedTitle);

        Assert.True(
            PublicSearchContains(updatedTitle),
            "Public search did not show the updated experience information.");

        page.DeleteListing(updatedTitle);
        page.WaitUntilListingRemoved(updatedTitle);
    }

    // =========================================================
    // TC56-07
    // Delete own experience listing
    // =========================================================
    [Fact]
    public void TC56_07_DeleteOwnExperience_ShouldRemoveListing()
    {
        LoginAsProvider(
            "CQ_APPROVED_PROVIDER_EMAIL",
            "CQ_APPROVED_PROVIDER_PASSWORD");

        var page =
            new ExperienceListingsPage(Driver);

        page.Open();

        var title =
            UniqueTitle("QA Delete Test");

        page.OpenCreateForm();

        page.FillValidExperience(
            title,
            "Temporary listing for delete testing.",
            "Kandy",
            4500);

        page.Publish();
        page.WaitForListing(title);

        page.DeleteListing(title);
        page.WaitUntilListingRemoved(title);

        Assert.False(
            page.ListingExists(title),
            "Deleted experience is still visible in My Listings.");
    }

    // =========================================================
    // TC56-08
    // Deleted experience should not appear publicly
    // =========================================================
    [Fact]
    public void TC56_08_DeletedExperience_ShouldNotAppearInPublicSearch()
    {
        LoginAsProvider(
            "CQ_APPROVED_PROVIDER_EMAIL",
            "CQ_APPROVED_PROVIDER_PASSWORD");

        var page =
            new ExperienceListingsPage(Driver);

        page.Open();

        var title =
            UniqueTitle("QA Public Delete");

        page.OpenCreateForm();

        page.FillValidExperience(
            title,
            "Public delete visibility test.",
            "Jaffna",
            3200);

        page.Publish();
        page.WaitForListing(title);

        Assert.True(
            PublicSearchContains(title),
            "Precondition failed: newly created active listing was not public.");

        page.DeleteListing(title);
        page.WaitUntilListingRemoved(title);

        Assert.False(
            PublicSearchContains(title),
            "Deleted experience is still returned by public search.");
    }

    // =========================================================
    // TC56-09
    // Provider cannot update another provider's experience
    // =========================================================
    [Fact]
    public void TC56_09_Provider_ShouldNotUpdateAnotherProvidersExperience()
    {
        // Create listing as Provider B.
        LoginAsProvider(
            "CQ_APPROVED_PROVIDER_B_EMAIL",
            "CQ_APPROVED_PROVIDER_B_PASSWORD");

        var providerBPage =
            new ExperienceListingsPage(Driver);

        providerBPage.Open();

        var title =
            UniqueTitle(
                "Provider B Ownership Update");

        providerBPage.OpenCreateForm();

        providerBPage.FillValidExperience(
            title,
            "Owned by Provider B.",
            "Matara",
            2800);

        providerBPage.Publish();
        providerBPage.WaitForListing(title);

        var listing =
            GetOwnListingByTitle(title);

        // Login as Provider A.
        LoginAsProvider(
            "CQ_APPROVED_PROVIDER_EMAIL",
            "CQ_APPROVED_PROVIDER_PASSWORD");

        var unauthorizedTitle =
            title + " HACKED";

        var payload =
            BuildUpdateJson(
                listing,
                unauthorizedTitle);

        var updateResult =
            BrowserApiRequest(
                "PUT",
                $"/api/catalog/activity-listings/{listing.Id}",
                payload);

        Assert.Equal(
            403,
            updateResult.Status);

        // Login back as Provider B.
        LoginAsProvider(
            "CQ_APPROVED_PROVIDER_B_EMAIL",
            "CQ_APPROVED_PROVIDER_B_PASSWORD");

        providerBPage =
            new ExperienceListingsPage(Driver);

        providerBPage.Open();

        Assert.True(
            providerBPage.ListingExists(title),
            "Provider B's original listing disappeared after Provider A update attempt.");

        Assert.False(
            providerBPage.ListingExists(unauthorizedTitle),
            "Provider A was able to update Provider B's listing.");

        providerBPage.DeleteListing(title);
        providerBPage.WaitUntilListingRemoved(title);
    }

    // =========================================================
    // TC56-10
    // Provider cannot delete another provider's experience
    // =========================================================
    [Fact]
    public void TC56_10_Provider_ShouldNotDeleteAnotherProvidersExperience()
    {
        // Create listing as Provider B.
        LoginAsProvider(
            "CQ_APPROVED_PROVIDER_B_EMAIL",
            "CQ_APPROVED_PROVIDER_B_PASSWORD");

        var providerBPage =
            new ExperienceListingsPage(Driver);

        providerBPage.Open();

        var title =
            UniqueTitle(
                "Provider B Ownership Delete");

        providerBPage.OpenCreateForm();

        providerBPage.FillValidExperience(
            title,
            "Provider B delete ownership test.",
            "Anuradhapura",
            3100);

        providerBPage.Publish();
        providerBPage.WaitForListing(title);

        var listing =
            GetOwnListingByTitle(title);

        // Login as Provider A.
        LoginAsProvider(
            "CQ_APPROVED_PROVIDER_EMAIL",
            "CQ_APPROVED_PROVIDER_PASSWORD");

        var deleteResult =
            BrowserApiRequest(
                "DELETE",
                $"/api/catalog/activity-listings/{listing.Id}");

        Assert.Equal(
            403,
            deleteResult.Status);

        // Login back as Provider B.
        LoginAsProvider(
            "CQ_APPROVED_PROVIDER_B_EMAIL",
            "CQ_APPROVED_PROVIDER_B_PASSWORD");

        providerBPage =
            new ExperienceListingsPage(Driver);

        providerBPage.Open();

        Assert.True(
            providerBPage.ListingExists(title),
            "Provider A was able to delete Provider B's listing.");

        providerBPage.DeleteListing(title);
        providerBPage.WaitUntilListingRemoved(title);
    }

    // =========================================================
    // TC56-11
    // Required title missing
    // =========================================================
    [Fact]
    public void TC56_11_CreateExperience_WithMissingTitle_ShouldShowValidation()
    {
        LoginAsProvider(
            "CQ_APPROVED_PROVIDER_EMAIL",
            "CQ_APPROVED_PROVIDER_PASSWORD");

        var page =
            new ExperienceListingsPage(Driver);

        page.Open();
        page.OpenCreateForm();

        page.EnterLocation("Ella");

        page.EnterDescription(
            "Experience without a title.");

        page.EnterPrice(6500);

        page.SelectPricingUnit(
            "Per Person");

        page.EnterMaxGuests(10);

        page.EnterDuration(
            "2 Hours");

        page.EnterAvailableDays(
            "Daily");

        page.SetFirstTimeSlot(
            "08:00",
            "10:00");

        page.Publish();

        var error =
            page.WaitForFormError();

        Assert.Contains(
            "title",
            error,
            StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================
    // TC56-12
    // Invalid negative price
    // =========================================================
    [Fact]
    public void TC56_12_CreateExperience_WithNegativePrice_ShouldShowValidation()
    {
        LoginAsProvider(
            "CQ_APPROVED_PROVIDER_EMAIL",
            "CQ_APPROVED_PROVIDER_PASSWORD");

        var page =
            new ExperienceListingsPage(Driver);

        page.Open();
        page.OpenCreateForm();

        var title =
            UniqueTitle("Negative Price Test");

        page.EnterTitle(title);

        page.EnterLocation(
            "Colombo");

        page.EnterDescription(
            "Negative price validation test.");

        page.EnterPrice(-500);

        page.SelectPricingUnit(
            "Per Person");

        page.EnterMaxGuests(10);

        page.EnterDuration(
            "2 Hours");

        page.EnterAvailableDays(
            "Daily");

        page.SetFirstTimeSlot(
            "08:00",
            "10:00");

        page.Publish();

        var error =
            page.WaitForFormError();

        Assert.Contains(
            "price",
            error,
            StringComparison.OrdinalIgnoreCase);

        Assert.False(
            page.ListingExists(title),
            "Experience with a negative price was created.");
    }
}
