using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using CeylonQuest.Tests.Pages;
using CeylonQuest.Tests.Utilities;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.Tests.Tests;

public class RestaurantListingsTests : BaseTest
{
    private sealed record ApiResult(
        int Status,
        string Body);

    // =========================================================
    // ENVIRONMENT VARIABLES
    // =========================================================

    private string GetEnvironmentVariable(
        string variableName)
    {
        var value =
            Environment.GetEnvironmentVariable(
                variableName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{variableName} is not configured.");
        }

        return value;
    }

    // =========================================================
    // UNIQUE RESTAURANT NAME
    // =========================================================

    private string CreateUniqueName(
        string prefix)
    {
        return
            $"{prefix} " +
            $"{DateTime.UtcNow:yyyyMMddHHmmssfff}";
    }

    // =========================================================
    // LOGIN
    // =========================================================

    private void Login(
        string emailVariable = "QA_PROVIDER_EMAIL",
        string passwordVariable = "QA_PROVIDER_PASSWORD")
    {
        Driver.Navigate()
            .GoToUrl(
                $"{BaseUrl}/login");

        var wait =
            new WebDriverWait(
                Driver,
                TimeSpan.FromSeconds(20));

        var emailInput =
            wait.Until(d =>
                d.FindElement(
                    By.Id("login-email")));

        var passwordInput =
            wait.Until(d =>
                d.FindElement(
                    By.Id("login-password")));

        emailInput.Clear();

        emailInput.SendKeys(
            GetEnvironmentVariable(
                emailVariable));

        passwordInput.Clear();

        passwordInput.SendKeys(
            GetEnvironmentVariable(
                passwordVariable));

        Driver.FindElement(
                By.Id("login-button"))
            .Click();

        wait.Until(d =>
            d.FindElements(
                    By.Id("pd-nav-services"))
                .Any(x =>
                {
                    try
                    {
                        return x.Displayed;
                    }
                    catch
                    {
                        return false;
                    }
                }));
    }

    // =========================================================
    // CLEAR SESSION
    // =========================================================

    private void ClearSession()
    {
        Driver.Navigate()
            .GoToUrl(BaseUrl);

        ((IJavaScriptExecutor)Driver)
            .ExecuteScript(
                @"
                localStorage.clear();
                sessionStorage.clear();
                ");

        Driver.Navigate()
            .GoToUrl(
                $"{BaseUrl}/login");
    }

    // =========================================================
    // API REQUEST
    // =========================================================

    private ApiResult SendApiRequest(
        string method,
        string path,
        string? body = null,
        bool authenticated = true)
    {
        const string script = @"
            const done =
                arguments[arguments.length - 1];

            const method =
                arguments[0];

            const path =
                arguments[1];

            const body =
                arguments[2];

            const authenticated =
                arguments[3];

            const headers = {};

            if (body) {
                headers['Content-Type'] =
                    'application/json';
            }

            if (authenticated) {

                const token =
                    localStorage.getItem(
                        'authToken'
                    );

                if (token) {
                    headers['Authorization'] =
                        'Bearer ' + token;
                }
            }

            fetch(path, {
                method: method,
                headers: headers,
                body: body || undefined
            })

            .then(async response => {

                const text =
                    await response.text();

                done(
                    JSON.stringify({
                        status:
                            response.status,

                        body:
                            text
                    })
                );
            })

            .catch(error => {

                done(
                    JSON.stringify({
                        status: 0,

                        body:
                            String(error)
                    })
                );
            });
        ";

        var result =
            (string?)((IJavaScriptExecutor)Driver)
                .ExecuteAsyncScript(
                    script,
                    method,
                    path,
                    body,
                    authenticated);

        if (string.IsNullOrWhiteSpace(
                result))
        {
            throw new InvalidOperationException(
                "API request returned no result.");
        }

        using var document =
            JsonDocument.Parse(
                result);

        var status =
            document.RootElement
                .GetProperty("status")
                .GetInt32();

        var responseBody =
            document.RootElement
                .GetProperty("body")
                .GetString()
            ?? string.Empty;

        return new ApiResult(
            status,
            responseBody);
    }

    // =========================================================
    // CREATE RESTAURANT THROUGH API
    // =========================================================

    private string CreateRestaurantThroughApi(
        string restaurantName)
    {
        var requestBody =
            JsonSerializer.Serialize(
                new
                {
                    name =
                        restaurantName,

                    description =
                        "Restaurant created for Selenium QA testing.",

                    cuisineType =
                        "Sri Lankan Cuisine",

                    diningStyle =
                        "Fine Dining",

                    location =
                        "Negombo City",

                    pricePerPerson =
                        3500m,

                    priceRange =
                        "Moderate",

                    openingHours =
                        "09:00 AM - 10:00 PM",

                    setMenuDetails =
                        "Traditional Sri Lankan menu.",

                    dietaryOptions =
                        "Vegetarian options available.",

                    groupSizeCategory =
                        "Table for Two",

                    seatingCapacity =
                        20,

                    isActive =
                        true
                });

        var result =
            SendApiRequest(
                "POST",
                "/api/catalog/restaurant-listings",
                requestBody);

        Assert.True(
            result.Status == 201,
            $"Expected HTTP 201 but received " +
            $"{result.Status}. Response: {result.Body}");

        using var document =
            JsonDocument.Parse(
                result.Body);

        var id =
            document.RootElement
                .GetProperty("id")
                .GetString();

        if (string.IsNullOrWhiteSpace(
                id))
        {
            throw new InvalidOperationException(
                "Created restaurant did not return an ID.");
        }

        return id;
    }

    // =========================================================
    // PUBLIC SEARCH
    // =========================================================

    private ApiResult SearchPublicRestaurant(
        string restaurantName)
    {
        return SendApiRequest(
            "GET",
            "/api/catalog/restaurant-listings/public" +
            $"?search={Uri.EscapeDataString(restaurantName)}",
            null,
            false);
    }

    private bool PublicResultsContainRestaurant(
        ApiResult result,
        string restaurantName)
    {
        if (result.Status != 200)
            return false;

        using var document =
            JsonDocument.Parse(
                result.Body);

        if (document.RootElement.ValueKind
            != JsonValueKind.Array)
        {
            return false;
        }

        return document.RootElement
            .EnumerateArray()
            .Any(item =>
            {
                if (!item.TryGetProperty(
                        "name",
                        out var nameProperty))
                {
                    return false;
                }

                return string.Equals(
                    nameProperty.GetString(),
                    restaurantName,
                    StringComparison.OrdinalIgnoreCase);
            });
    }

    // =========================================================
    // GET PUBLIC RESTAURANT PROPERTY
    //
    // FIX FOR TC57-03 / TC57-04:
    // Do not search the complete raw JSON string.
    // Find the exact restaurant first, then read its property.
    // =========================================================

    private string? GetPublicRestaurantProperty(
        string restaurantName,
        string propertyName)
    {
        var result =
            SearchPublicRestaurant(
                restaurantName);

        Assert.Equal(
            200,
            result.Status);

        using var document =
            JsonDocument.Parse(
                result.Body);

        if (document.RootElement.ValueKind
            != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in
                 document.RootElement
                     .EnumerateArray())
        {
            if (!item.TryGetProperty(
                    "name",
                    out var nameProperty))
            {
                continue;
            }

            var name =
                nameProperty.GetString()
                ?? string.Empty;

            if (!name.Equals(
                    restaurantName,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!item.TryGetProperty(
                    propertyName,
                    out var property))
            {
                return null;
            }

            return property.ValueKind switch
            {
                JsonValueKind.String =>
                    property.GetString(),

                JsonValueKind.Number =>
                    property.ToString(),

                JsonValueKind.True =>
                    "true",

                JsonValueKind.False =>
                    "false",

                _ =>
                    property.ToString()
            };
        }

        return null;
    }

    // =========================================================
    // WAIT FOR UPDATED PUBLIC VALUE
    //
    // Gives the frontend/backend a few seconds to reflect
    // the saved update before the assertion is made.
    // =========================================================

    private string WaitForPublicRestaurantProperty(
        string restaurantName,
        string propertyName,
        Func<string, bool> expectedCondition)
    {
        string latestValue =
            string.Empty;

        for (int attempt = 0;
             attempt < 10;
             attempt++)
        {
            latestValue =
                GetPublicRestaurantProperty(
                    restaurantName,
                    propertyName)
                ?? string.Empty;

            if (expectedCondition(
                    latestValue))
            {
                return latestValue;
            }

            Thread.Sleep(
                500);
        }

        return latestValue;
    }

    // =========================================================
    // TIME NORMALIZATION
    //
    // Accepts common equivalent formats:
    //
    // 10:00 AM - 11:00 PM
    // 10:00-23:00
    // 10:00 - 23:00
    // =========================================================

    private bool OpeningHoursAreCorrect(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return false;
        }

        var normalized =
            value
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", "");

        return
            normalized.Contains(
                "10:00AM-11:00PM")
            ||
            normalized.Contains(
                "10:00-23:00")
            ||
            normalized.Contains(
                "10:00:00-23:00:00");
    }

    // =========================================================
    // CLEANUP DELETE
    // =========================================================

    private void DeleteRestaurantThroughApi(
        string restaurantId)
    {
        if (string.IsNullOrWhiteSpace(
                restaurantId))
        {
            return;
        }

        SendApiRequest(
            "DELETE",
            $"/api/catalog/restaurant-listings/{restaurantId}");
    }

    // =========================================================
    // TC57-01
    // CREATE RESTAURANT
    // =========================================================

    [Fact]
    public void TC57_01_CreateRestaurant_WithValidDetails()
    {
        Login();

        var page =
            new RestaurantListingsPage(
                Driver);

        page.Open();

        var restaurantName =
            CreateUniqueName(
                "QA Restaurant");

        page.OpenCreateForm();

        page.FillValidRestaurant(
            restaurantName);

        page.SubmitCreate();

        page.WaitForListing(
            restaurantName);

        Assert.True(
            page.ListingExists(
                restaurantName),
            "Created restaurant was not visible in My Listings.");

        page.DeleteListing(
            restaurantName);

        page.WaitUntilRemoved(
            restaurantName);
    }

    // =========================================================
    // TC57-02
    // PUBLIC SEARCH
    // =========================================================

    [Fact]
    public void TC57_02_CreatedRestaurant_IsVisibleInPublicSearch()
    {
        Login();

        var restaurantName =
            CreateUniqueName(
                "QA Public Restaurant");

        var restaurantId =
            CreateRestaurantThroughApi(
                restaurantName);

        try
        {
            var result =
                SearchPublicRestaurant(
                    restaurantName);

            Assert.Equal(
                200,
                result.Status);

            Assert.True(
                PublicResultsContainRestaurant(
                    result,
                    restaurantName),
                "Created restaurant was not visible in public search.");
        }
        finally
        {
            DeleteRestaurantThroughApi(
                restaurantId);
        }
    }

    // =========================================================
    // TC57-03
    // UPDATE OPENING HOURS
    // =========================================================

    [Fact]
    public void TC57_03_UpdateRestaurant_OpeningHours()
    {
        Login();

        var page =
            new RestaurantListingsPage(
                Driver);

        page.Open();

        var restaurantName =
            CreateUniqueName(
                "QA Hours Restaurant");

        page.OpenCreateForm();

        page.FillValidRestaurant(
            restaurantName);

        page.SubmitCreate();

        page.WaitForListing(
            restaurantName);

        page.OpenEditForm(
            restaurantName);

        // 10:00 AM -> 11:00 PM
        page.SetOpeningHours(
            "10:00",
            "23:00");

        page.SubmitUpdate();

        // Read the actual openingHours property,
        // instead of searching entire raw JSON.
        var openingHours =
            WaitForPublicRestaurantProperty(
                restaurantName,
                "openingHours",
                OpeningHoursAreCorrect);

        Assert.False(
            string.IsNullOrWhiteSpace(
                openingHours),
            "Public restaurant response did not contain openingHours.");

        Assert.True(
            OpeningHoursAreCorrect(
                openingHours),
            $"Opening hours were not updated correctly. " +
            $"Actual API value: '{openingHours}'");

        // Cleanup
        page.DeleteListing(
            restaurantName);

        page.WaitUntilRemoved(
            restaurantName);
    }

    // =========================================================
    // TC57-04
    // UPDATE PRICE RANGE
    // =========================================================

    [Fact]
    public void TC57_04_UpdateRestaurant_PriceRange()
    {
        Login();

        var page =
            new RestaurantListingsPage(
                Driver);

        page.Open();

        var restaurantName =
            CreateUniqueName(
                "QA Price Restaurant");

        page.OpenCreateForm();

        page.FillValidRestaurant(
            restaurantName);

        page.SubmitCreate();

        page.WaitForListing(
            restaurantName);

        page.OpenEditForm(
            restaurantName);

        page.SelectPriceRange(
            "Upscale");

        page.SubmitUpdate();

        var priceRange =
            WaitForPublicRestaurantProperty(
                restaurantName,
                "priceRange",
                value =>
                    value.Equals(
                        "Upscale",
                        StringComparison.OrdinalIgnoreCase));

        Assert.False(
            string.IsNullOrWhiteSpace(
                priceRange),
            "Public restaurant response did not contain priceRange.");

        Assert.Equal(
            "Upscale",
            priceRange,
            ignoreCase: true);

        // Cleanup
        page.DeleteListing(
            restaurantName);

        page.WaitUntilRemoved(
            restaurantName);
    }

    // =========================================================
    // TC57-05
    // OTHER PROVIDER CANNOT EDIT
    // =========================================================

    [Fact]
    public void TC57_05_EditRestaurant_NotOwnedByProvider_IsForbidden()
    {
        Login();

        var restaurantName =
            CreateUniqueName(
                "Provider A Restaurant");

        var restaurantId =
            CreateRestaurantThroughApi(
                restaurantName);

        try
        {
            ClearSession();

            Login(
                "QA_PROVIDER_B_EMAIL",
                "QA_PROVIDER_B_PASSWORD");

            var requestBody =
                JsonSerializer.Serialize(
                    new
                    {
                        name =
                            restaurantName,

                        description =
                            "Unauthorized restaurant edit attempt.",

                        cuisineType =
                            "Sri Lankan Cuisine",

                        diningStyle =
                            "Fine Dining",

                        location =
                            "Colombo City",

                        pricePerPerson =
                            5000m,

                        priceRange =
                            "Upscale",

                        openingHours =
                            "10:00 AM - 11:00 PM",

                        setMenuDetails =
                            "Unauthorized menu change.",

                        dietaryOptions =
                            "Vegetarian",

                        groupSizeCategory =
                            "Table for Two",

                        seatingCapacity =
                            20,

                        isActive =
                            true
                    });

            var result =
                SendApiRequest(
                    "PUT",
                    $"/api/catalog/restaurant-listings/{restaurantId}",
                    requestBody);

            Assert.Equal(
                403,
                result.Status);
        }
        finally
        {
            ClearSession();

            Login();

            DeleteRestaurantThroughApi(
                restaurantId);
        }
    }

    // =========================================================
    // TC57-06
    // OTHER PROVIDER CANNOT DELETE
    // =========================================================

    [Fact]
    public void TC57_06_DeleteRestaurant_NotOwnedByProvider_IsForbidden()
    {
        Login();

        var restaurantName =
            CreateUniqueName(
                "Provider A Delete Restaurant");

        var restaurantId =
            CreateRestaurantThroughApi(
                restaurantName);

        try
        {
            ClearSession();

            Login(
                "QA_PROVIDER_B_EMAIL",
                "QA_PROVIDER_B_PASSWORD");

            var result =
                SendApiRequest(
                    "DELETE",
                    $"/api/catalog/restaurant-listings/{restaurantId}");

            Assert.Equal(
                403,
                result.Status);

            var publicResult =
                SearchPublicRestaurant(
                    restaurantName);

            Assert.True(
                PublicResultsContainRestaurant(
                    publicResult,
                    restaurantName),
                "Restaurant disappeared after unauthorized delete attempt.");
        }
        finally
        {
            ClearSession();

            Login();

            DeleteRestaurantThroughApi(
                restaurantId);
        }
    }

    // =========================================================
    // TC57-07
    // DELETE OWN RESTAURANT
    // =========================================================

    [Fact]
    public void TC57_07_DeleteOwnRestaurant()
    {
        Login();

        var page =
            new RestaurantListingsPage(
                Driver);

        page.Open();

        var restaurantName =
            CreateUniqueName(
                "QA Delete Restaurant");

        page.OpenCreateForm();

        page.FillValidRestaurant(
            restaurantName);

        page.SubmitCreate();

        page.WaitForListing(
            restaurantName);

        page.DeleteListing(
            restaurantName);

        page.WaitUntilRemoved(
            restaurantName);

        Assert.False(
            page.ListingExists(
                restaurantName),
            "Deleted restaurant was still visible.");
    }

    // =========================================================
    // TC57-08
    // DELETED RESTAURANT REMOVED FROM PUBLIC SEARCH
    // =========================================================

    [Fact]
    public void TC57_08_DeletedRestaurant_IsRemovedFromPublicSearch()
    {
        Login();

        var restaurantName =
            CreateUniqueName(
                "QA Removed Restaurant");

        var restaurantId =
            CreateRestaurantThroughApi(
                restaurantName);

        var beforeDelete =
            SearchPublicRestaurant(
                restaurantName);

        Assert.True(
            PublicResultsContainRestaurant(
                beforeDelete,
                restaurantName));

        var deleteResult =
            SendApiRequest(
                "DELETE",
                $"/api/catalog/restaurant-listings/{restaurantId}");

        Assert.Equal(
            204,
            deleteResult.Status);

        var afterDelete =
            SearchPublicRestaurant(
                restaurantName);

        Assert.Equal(
            200,
            afterDelete.Status);

        Assert.False(
            PublicResultsContainRestaurant(
                afterDelete,
                restaurantName),
            "Deleted restaurant was still returned by public search.");
    }

    // =========================================================
    // TC57-09
    // MISSING REQUIRED FIELD
    // =========================================================

    [Fact]
    public void TC57_09_MissingRequiredField_ShouldShowValidation()
    {
        Login();

        var page =
            new RestaurantListingsPage(
                Driver);

        page.Open();

        var restaurantName =
            CreateUniqueName(
                "QA Missing Field Restaurant");

        page.OpenCreateForm();

        page.FillValidRestaurant(
            restaurantName);

        // Restaurant name is required.
        page.ClearRestaurantName();

        page.SubmitCreateWithoutWaitingForSuccess();

        Assert.True(
            page.IsCreateFormStillOpen(),
            "Create form closed even though Restaurant Name was empty.");

        Assert.False(
            page.ListingExists(
                restaurantName),
            "Restaurant was created even though a required field was missing.");
    }

    // =========================================================
    // TC57-10
    // INVALID OPENING HOURS
    //
    // IMPORTANT:
    // Do NOT change this test just to make it green.
    //
    // If the application creates the restaurant when:
    // Opening = 22:00
    // Closing = 09:00
    //
    // this test should FAIL and should be reported to DEV.
    // =========================================================

    [Fact]
    public void TC57_10_InvalidOpeningHours_ShouldShowValidation()
    {
        Login();

        var page =
            new RestaurantListingsPage(
                Driver);

        page.Open();

        var restaurantName =
            CreateUniqueName(
                "QA Invalid Hours Restaurant");

        page.OpenCreateForm();

        page.FillValidRestaurant(
            restaurantName);

        // Invalid according to the current test requirement:
        // Opening = 10:00 PM
        // Closing = 09:00 AM
        page.SetOpeningHours(
            "22:00",
            "09:00");

        page.SubmitCreateWithoutWaitingForSuccess();

        Assert.True(
            page.IsCreateFormStillOpen(),
            "Create form closed even though opening hours were invalid.");

        Assert.False(
            page.ListingExists(
                restaurantName),
            "Restaurant was created with invalid opening hours.");
    }
}