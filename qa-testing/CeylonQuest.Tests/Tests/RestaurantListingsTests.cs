using System;
using System.Linq;
using System.Text.Json;
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

    // ENV VARIABLES

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

    
    // UNIQUE NAME
   

    private string CreateUniqueName(
        string prefix)
    {
        return
            $"{prefix} " +
            $"{DateTime.UtcNow:yyyyMMddHHmmssfff}";
    }

    
    // LOGIN


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
                .Any(x => x.Displayed));
    }

    // CLEAR SESSION
    

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

    // API REQUEST
    

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

        if (string.IsNullOrWhiteSpace(result))
        {
            throw new InvalidOperationException(
                "API request returned no result.");
        }

        using var document =
            JsonDocument.Parse(result);

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

  
    // CREATE RESTAURANT THROUGH API
  

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

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException(
                "Created restaurant did not return an ID.");
        }

        return id;
    }

   
    // PUBLIC SEARCH
  

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
        {
            return false;
        }

        using var document =
            JsonDocument.Parse(
                result.Body);

        return document.RootElement
            .EnumerateArray()
            .Any(item =>
                string.Equals(
                    item
                        .GetProperty("name")
                        .GetString(),

                    restaurantName,

                    StringComparison.OrdinalIgnoreCase));
    }

    // CLEANUP DELETE
   

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

    
    // TC57-01
 

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
                restaurantName));
    }

    
    // TC57-02
    

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
                    restaurantName));
        }
        finally
        {
            DeleteRestaurantThroughApi(
                restaurantId);
        }
    }

    // TC57-03
   

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

        page.SetOpeningHours(
            "10:00",
            "23:00");

        page.SubmitUpdate();

        var publicResult =
            SearchPublicRestaurant(
                restaurantName);

        Assert.Equal(
            200,
            publicResult.Status);

        Assert.Contains(
            "10:00 AM - 11:00 PM",
            publicResult.Body);
    }

    
    // TC57-04
    

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

        var publicResult =
            SearchPublicRestaurant(
                restaurantName);

        Assert.Equal(
            200,
            publicResult.Status);

        Assert.Contains(
            "Upscale",
            publicResult.Body);
    }


    // TC57-05
  

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

        ClearSession();

        Login();

        DeleteRestaurantThroughApi(
            restaurantId);
    }

   
    // TC57-06
    

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
                restaurantName));

        ClearSession();

        Login();

        DeleteRestaurantThroughApi(
            restaurantId);
    }

    // TC57-07
    

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
                restaurantName));
    }

   
    // TC57-08
    

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
                restaurantName));
    }

    
    // TC57-09
    // MISSING REQUIRED FIELD
   

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

        // Name is required.
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

    
    // TC57-10
    // INVALID OPENING HOURS


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

        // Invalid time combination:
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