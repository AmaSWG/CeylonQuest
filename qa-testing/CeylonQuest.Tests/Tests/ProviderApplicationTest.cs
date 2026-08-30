using CeylonQuest.Tests.Configuration;
using CeylonQuest.Tests.Pages;
using Xunit;
using System.Net;
using System.Net.Http.Headers;

namespace CeylonQuest.Tests.Tests;

public class ProviderApplicationTest : BaseTest
{
   [Fact]
   public void Admin_ShouldBeAbleToViewPendingProviderApplications()
   {
       RegistrationPage registrationPage = new(Driver);
       LoginPage loginPage = new(Driver);
       ProviderApplicationPage applicationPage = new(Driver);

       registrationPage.Open();
       registrationPage.ClickLogin();

       loginPage.Login(
           TestConfiguration.AdminEmail,
           TestConfiguration.AdminPassword
       );

       Assert.True(
           loginPage.WaitForLoginSuccess(),
           "Admin login did not complete successfully."
       );

       Assert.Equal("Admin", loginPage.GetUserRole());

       applicationPage.OpenProviderApplications();

       Assert.True(
           applicationPage.IsPendingApplicationDisplayed(),
           "Admin should be able to see a Pending provider application."
       );
   }

    [Fact]
    public void ApprovedProvider_ShouldBeAbleToLoginAsProvider()
    {
        RegistrationPage registrationPage = new(Driver);
        LoginPage loginPage = new(Driver);
        RoleAccessPage rolePage = new(Driver);

        registrationPage.Open();
        registrationPage.ClickLogin();

        loginPage.Login(
            TestConfiguration.ProviderEmail,
            TestConfiguration.ProviderPassword
        );

        Assert.True(
            loginPage.WaitForLoginSuccess(),
            "Approved Provider login did not complete successfully."
        );

        Assert.Equal(
            "Provider",
            loginPage.GetUserRole()
        );

        Assert.True(
            rolePage.IsProviderDashboardDisplayed(),
            "Approved Provider should be shown the Provider dashboard."
        );

        Assert.False(
            rolePage.IsAdminDashboardPresent(),
            "Provider must not be shown the Admin dashboard."
        );
    }

    [Fact]
    public async Task ProviderToken_ShouldBeForbiddenFromAdminApi()
    {
        RegistrationPage registrationPage = new(Driver);
        LoginPage loginPage = new(Driver);

        registrationPage.Open();
        registrationPage.ClickLogin();

        loginPage.Login(
            TestConfiguration.ProviderEmail,
            TestConfiguration.ProviderPassword
        );

        Assert.True(
            loginPage.WaitForLoginSuccess(),
            "Provider login did not complete successfully."
        );

        Assert.Equal(
            "Provider",
            loginPage.GetUserRole()
        );

        string? token = loginPage.GetAuthToken();

        Assert.False(
            string.IsNullOrWhiteSpace(token),
            "Provider authentication token was not available."
        );

        using HttpClient client = new();
        client.BaseAddress = new Uri("http://localhost:5000");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token
            );

        HttpResponseMessage response =
            await client.GetAsync("/api/admin/users");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode
        );
    }

    [Fact]
    public void Admin_ShouldBeAbleToReviewProviderApplicationDetails()
    {
        RegistrationPage registrationPage = new(Driver);
        LoginPage loginPage = new(Driver);
        ProviderApplicationPage applicationPage = new(Driver);

        registrationPage.Open();
        registrationPage.ClickLogin();

        loginPage.Login(
            TestConfiguration.AdminEmail,
            TestConfiguration.AdminPassword
        );

        Assert.True(
            loginPage.WaitForLoginSuccess(),
            "Admin login did not complete successfully."
        );

        applicationPage.OpenProviderApplications();

        Assert.True(
            applicationPage.IsPendingApplicationDisplayed(),
            "A Pending provider application should be available."
        );

        applicationPage.ClickFirstReviewDetails();

        Assert.True(
            applicationPage.IsApplicationDetailsModalDisplayed(),
            "Provider application details should be displayed."
        );
    }

}