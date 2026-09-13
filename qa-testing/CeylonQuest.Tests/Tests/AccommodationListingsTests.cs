using System;
using System.Linq;
using System.Text.Json;
using CeylonQuest.Tests.Pages;
using CeylonQuest.Tests.Utilities;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.Tests.Tests;

public class AccommodationListingsTests : BaseTest
{
    private sealed record ApiResult(
        int Status,
        string Body);

    // =========================================================
    // HELPERS
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

    private string CreateUniqueRoomType(
        string prefix)
    {
        return
            $"{prefix} {DateTime.UtcNow:yyyyMMddHHmmssfff}";
    }

    // =========================================================
    // LOGIN
    // =========================================================

    private void Login(
        string emailVariable =
            "QA_HOTEL_PROVIDER_EMAIL",
        string passwordVariable =
            "QA_HOTEL_PROVIDER_PASSWORD")
    {
        Driver.Navigate()
            .GoToUrl(
                $"{BaseUrl}/login");

        var wait =
            new WebDriverWait(
                Driver,
                TimeSpan.FromSeconds(30));

        var emailInput =
            wait.Until(driver =>
                driver.FindElements(
                        By.Id("login-email"))
                    .FirstOrDefault(x =>
                        x.Displayed &&
                        x.Enabled));

        var passwordInput =
            wait.Until(driver =>
                driver.FindElements(
                        By.Id("login-password"))
                    .FirstOrDefault(x =>
                        x.Displayed &&
                        x.Enabled));

        Assert.NotNull(emailInput);
        Assert.NotNull(passwordInput);

        emailInput!.Clear();
        emailInput.SendKeys(
            GetEnvironmentVariable(
                emailVariable));

        passwordInput!.Clear();
        passwordInput.SendKeys(
            GetEnvironmentVariable(
                passwordVariable));

        var loginButton =
            wait.Until(driver =>
                driver.FindElements(
                        By.Id("login-button"))
                    .FirstOrDefault(x =>
                        x.Displayed &&
                        x.Enabled));

        Assert.NotNull(loginButton);

        loginButton!.Click();

        wait.Until(driver =>
        {
            try
            {
                if (!driver.Url.Contains(
                        "/login",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var token =
                    ((IJavaScriptExecutor)driver)
                    .ExecuteScript(
                        "return localStorage.getItem('authToken');")
                    ?.ToString();

                return
                    !string.IsNullOrWhiteSpace(
                        token);
            }
            catch
            {
                return false;
            }
        });
    }

    private void ClearSession()
    {
        Driver.Navigate()
            .GoToUrl(BaseUrl);

        ((IJavaScriptExecutor)Driver)
            .ExecuteScript(
                "localStorage.clear();" +
                "sessionStorage.clear();");

        Driver.Navigate()
            .GoToUrl(
                $"{BaseUrl}/login");
    }

    // =========================================================
    // API
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

            const method = arguments[0];
            const path = arguments[1];
            const body = arguments[2];
            const authenticated = arguments[3];

            const headers = {};

            if (body) {
                headers['Content-Type'] =
                    'application/json';
            }

            if (authenticated) {
                const token =
                    localStorage.getItem('authToken');

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

        return new ApiResult(
            document.RootElement
                .GetProperty("status")
                .GetInt32(),

            document.RootElement
                .GetProperty("body")
                .GetString()
            ?? string.Empty);
    }

    private string CreateAccommodationThroughApi(
        string roomType)
    {
        var requestBody =
            JsonSerializer.Serialize(
                new
                {
                    roomType,
                    propertyType =
                        "Luxury Resort",
                    location =
                        "Negombo Beach",
                    pricePerNight =
                        12500m,
                    maxGuests =
                        2,
                    bedDetails =
                        "1 King Bed",
                    minStayNights =
                        1,
                    amenities =
                        "WiFi, Breakfast, Air Conditioning",
                    bathroomDetails =
                        "Private bathroom with hot water",
                    description =
                        "Accommodation created for Selenium QA testing.",
                    isActive =
                        true
                });

        var result =
            SendApiRequest(
                "POST",
                "/api/catalog/accommodation-listings",
                requestBody);

        Assert.True(
            result.Status == 201 ||
            result.Status == 200,
            $"Expected HTTP 201/200 but received " +
            $"{result.Status}. Response: {result.Body}");

        using var document =
            JsonDocument.Parse(
                result.Body);

        var id =
            document.RootElement
                .GetProperty("id")
                .ToString();

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException(
                "Created accommodation did not return an ID.");
        }

        return id;
    }

    private ApiResult SearchPublicAccommodation(
        string roomType)
    {
        return SendApiRequest(
            "GET",

            "/api/catalog/accommodation-listings/public" +
            $"?search={Uri.EscapeDataString(roomType)}",

            null,
            false);
    }

    private bool PublicResultsContainAccommodation(
        ApiResult result,
        string roomType)
    {
        if (result.Status != 200)
        {
            return false;
        }

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
                        "roomType",
                        out var property))
                {
                    return false;
                }

                return string.Equals(
                    property.GetString(),
                    roomType,
                    StringComparison.OrdinalIgnoreCase);
            });
    }

    private void DeleteAccommodationThroughApi(
        string accommodationId)
    {
        if (string.IsNullOrWhiteSpace(
                accommodationId))
        {
            return;
        }

        SendApiRequest(
            "DELETE",
            $"/api/catalog/accommodation-listings/{accommodationId}");
    }

    // =========================================================
    // TC-HOT-01
    // =========================================================

    [Fact]
    public void TC_HOT_01_CreateAccommodation_WithValidDetails()
    {
        Login();

        var page =
            new AccommodationListingsPage(
                Driver);

        page.Open();

        var roomType =
            CreateUniqueRoomType(
                "QA Deluxe Room");

        page.OpenCreateForm();
        page.FillValidAccommodation(roomType);
        page.SubmitCreate();
        page.WaitForListing(roomType);

        Assert.True(
            page.ListingExists(roomType));
    }

    // =========================================================
    // TC-HOT-02
    // =========================================================

    [Fact]
    public void TC_HOT_02_CreatedAccommodation_IsVisibleToVisitors()
    {
        Login();

        var roomType =
            CreateUniqueRoomType(
                "QA Public Hotel Room");

        var accommodationId =
            CreateAccommodationThroughApi(
                roomType);

        try
        {
            var result =
                SearchPublicAccommodation(
                    roomType);

            Assert.Equal(
                200,
                result.Status);

            Assert.True(
                PublicResultsContainAccommodation(
                    result,
                    roomType));
        }
        finally
        {
            DeleteAccommodationThroughApi(
                accommodationId);
        }
    }

    // =========================================================
    // TC-HOT-03
    // =========================================================

    [Fact]
    public void TC_HOT_03_UpdateAccommodation_Details()
    {
        Login();

        var page =
            new AccommodationListingsPage(
                Driver);

        page.Open();

        var roomType =
            CreateUniqueRoomType(
                "QA Update Hotel Room");

        page.OpenCreateForm();
        page.FillValidAccommodation(roomType);
        page.SubmitCreate();
        page.WaitForListing(roomType);

        page.OpenEditForm(roomType);

        page.SetLocation(
            "Galle Fort");

        page.SetDescription(
            "Updated accommodation description for Selenium QA verification.");

        page.SetAmenities(
            "WiFi, Breakfast, Pool, Airport Transfer");

        page.SubmitUpdate();

        var result =
            SearchPublicAccommodation(
                roomType);

        Assert.Equal(
            200,
            result.Status);

        Assert.Contains(
            "Galle Fort",
            result.Body);

        Assert.Contains(
            "Pool",
            result.Body);
    }

    // =========================================================
    // TC-HOT-04
    // =========================================================

    [Fact]
    public void TC_HOT_04_UpdateAccommodation_Price()
    {
        Login();

        var page =
            new AccommodationListingsPage(
                Driver);

        page.Open();

        var roomType =
            CreateUniqueRoomType(
                "QA Price Hotel Room");

        page.OpenCreateForm();
        page.FillValidAccommodation(roomType);
        page.SubmitCreate();
        page.WaitForListing(roomType);

        page.OpenEditForm(roomType);
        page.SetPrice("18500");
        page.SubmitUpdate();

        var result =
            SearchPublicAccommodation(
                roomType);

        Assert.Equal(
            200,
            result.Status);

        Assert.Contains(
            "18500",
            result.Body);
    }

    // =========================================================
    // TC-HOT-05
    // =========================================================

    [Fact]
    public void TC_HOT_05_EditAccommodation_NotOwnedByProvider_IsForbidden()
    {
        Login();

        var roomType =
            CreateUniqueRoomType(
                "Provider A Hotel Room");

        var accommodationId =
            CreateAccommodationThroughApi(
                roomType);

        ClearSession();

        Login(
            "QA_HOTEL_PROVIDER_B_EMAIL",
            "QA_HOTEL_PROVIDER_B_PASSWORD");

        var requestBody =
            JsonSerializer.Serialize(
                new
                {
                    roomType =
                        "Unauthorized Updated Room",
                    propertyType =
                        "Luxury Resort",
                    location =
                        "Colombo",
                    pricePerNight =
                        500m,
                    maxGuests =
                        1,
                    bedDetails =
                        "1 Single Bed",
                    minStayNights =
                        1,
                    amenities =
                        "None",
                    bathroomDetails =
                        "Shared bathroom",
                    description =
                        "Unauthorized accommodation update attempt.",
                    isActive =
                        true
                });

        var result =
            SendApiRequest(
                "PUT",

                $"/api/catalog/accommodation-listings/" +
                accommodationId,

                requestBody);

        Assert.Equal(
            403,
            result.Status);

        ClearSession();

        Login();

        DeleteAccommodationThroughApi(
            accommodationId);
    }

    // =========================================================
    // TC-HOT-06
    // =========================================================

    [Fact]
    public void TC_HOT_06_DeleteAccommodation_NotOwnedByProvider_IsForbidden()
    {
        Login();

        var roomType =
            CreateUniqueRoomType(
                "Provider A Protected Hotel Room");

        var accommodationId =
            CreateAccommodationThroughApi(
                roomType);

        ClearSession();

        Login(
            "QA_HOTEL_PROVIDER_B_EMAIL",
            "QA_HOTEL_PROVIDER_B_PASSWORD");

        var result =
            SendApiRequest(
                "DELETE",

                $"/api/catalog/accommodation-listings/" +
                accommodationId);

        Assert.Equal(
            403,
            result.Status);

        var publicResult =
            SearchPublicAccommodation(
                roomType);

        Assert.True(
            PublicResultsContainAccommodation(
                publicResult,
                roomType));

        ClearSession();

        Login();

        DeleteAccommodationThroughApi(
            accommodationId);
    }

    // =========================================================
    // TC-HOT-07
    // =========================================================

    [Fact]
    public void TC_HOT_07_DeleteOwnAccommodation()
    {
        Login();

        var page =
            new AccommodationListingsPage(
                Driver);

        page.Open();

        var roomType =
            CreateUniqueRoomType(
                "QA Delete Hotel Room");

        page.OpenCreateForm();
        page.FillValidAccommodation(roomType);
        page.SubmitCreate();
        page.WaitForListing(roomType);

        page.DeleteListing(roomType);
        page.WaitUntilRemoved(roomType);

        Assert.False(
            page.ListingExists(roomType));
    }

    // =========================================================
    // TC-HOT-08
    // =========================================================

    [Fact]
    public void TC_HOT_08_DeletedAccommodation_RemovedFromPublicSearch()
    {
        Login();

        var roomType =
            CreateUniqueRoomType(
                "QA Removed Hotel Room");

        var accommodationId =
            CreateAccommodationThroughApi(
                roomType);

        var beforeDelete =
            SearchPublicAccommodation(
                roomType);

        Assert.True(
            PublicResultsContainAccommodation(
                beforeDelete,
                roomType));

        var deleteResult =
            SendApiRequest(
                "DELETE",

                $"/api/catalog/accommodation-listings/" +
                accommodationId);

        Assert.True(
            deleteResult.Status == 204 ||
            deleteResult.Status == 200);

        var afterDelete =
            SearchPublicAccommodation(
                roomType);

        Assert.Equal(
            200,
            afterDelete.Status);

        Assert.False(
            PublicResultsContainAccommodation(
                afterDelete,
                roomType));
    }

    // =========================================================
    // TC-HOT-09
    // Missing Room Type
    // =========================================================

    [Fact]
    public void TC_HOT_09_MissingRequiredField_ShouldShowValidation()
    {
        Login();

        var page =
            new AccommodationListingsPage(
                Driver);

        page.Open();

        var roomType =
            CreateUniqueRoomType(
                "QA Missing Room Type");

        page.OpenCreateForm();

        page.FillValidAccommodation(
            roomType);

        // Remove Room Type
        page.ClearRoomType();

        // Try to submit
        page.SubmitCreateWithoutWaitingForSuccess();

        // Form must stay open
        Assert.True(
            page.IsCreateFormStillOpen(),
            "Create form should remain open when Room Type is missing.");

        // Wait for the real UI validation message
        Assert.True(
            page.WaitForFormError(),
            "Expected validation error for missing Room Type.");

        var errorMessage =
            page.GetFormError();

        Assert.Contains(
            "Room type is required",
            errorMessage,
            StringComparison.OrdinalIgnoreCase);

        // Original listing must not be created
        Assert.False(
            page.ListingExists(roomType),
            "Accommodation should not be created when Room Type is missing.");
    }

    // =========================================================
    // TC-HOT-10
    // Invalid Price
    // =========================================================

    [Fact]
    public void TC_HOT_10_InvalidPrice_ShouldShowValidation()
    {
        Login();

        var page =
            new AccommodationListingsPage(
                Driver);

        page.Open();

        var roomType =
            CreateUniqueRoomType(
                "QA Invalid Price Hotel Room");

        page.OpenCreateForm();

        page.FillValidAccommodation(
            roomType);

        page.SetPrice("0");

        page.SubmitCreateWithoutWaitingForSuccess();

        Assert.True(
            page.IsCreateFormStillOpen(),
            "Create form should remain open when price is invalid.");

        Assert.True(
            page.WaitForFormError(),
            "Expected validation error for invalid price.");

        var errorMessage =
            page.GetFormError();

        Assert.False(
            string.IsNullOrWhiteSpace(
                errorMessage),
            "Expected a visible price validation message.");

        Assert.False(
            page.ListingExists(roomType),
            "Accommodation should not be created with invalid price.");
    }
}