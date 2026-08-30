using CeylonQuest.Tests.Configuration;
using CeylonQuest.Tests.Pages;
using Xunit;
using System.Net;
using System.Net.Http.Headers;

namespace CeylonQuest.Tests.Tests;

public class RoleAccessTest : BaseTest
{
    [Fact]
    public void Visitor_Login_ShouldDisplayVisitorDashboard()
    {
        RegistrationPage registrationPage = new(Driver);
        LoginPage loginPage = new(Driver);
        RoleAccessPage rolePage = new(Driver);

        // Open application and navigate to Login
        registrationPage.Open();
        registrationPage.ClickLogin();

        // Login using the registered Visitor account
        loginPage.Login(
            TestConfiguration.VisitorEmail,
            TestConfiguration.VisitorPassword
        );

        // Wait until authentication token and role are stored
        Assert.True(
            loginPage.WaitForLoginSuccess(),
            "Visitor login did not complete successfully."
        );

        // Verify that the system identified the correct role
        Assert.Equal(
            "Visitor",
            loginPage.GetUserRole()
        );

        // Verify Visitor-specific dashboard is displayed
        Assert.True(
            rolePage.IsVisitorDashboardDisplayed(),
            "Visitor dashboard should be displayed after Visitor login."
        );

        // Verify Admin dashboard is not displayed
        Assert.False(
            rolePage.IsAdminDashboardDisplayed(),
            "Visitor must not be shown the Admin dashboard."
        );
    }

    [Fact]
    public void Visitor_ShouldNotHaveAccessToAdminFunctionality()
    {
        RegistrationPage registrationPage = new(Driver);
        LoginPage loginPage = new(Driver);
        RoleAccessPage rolePage = new(Driver);

        registrationPage.Open();
        registrationPage.ClickLogin();

        loginPage.Login(
            TestConfiguration.VisitorEmail,
            TestConfiguration.VisitorPassword
        );

         Assert.True(
                    loginPage.WaitForLoginSuccess(),
                    "Visitor login did not complete successfully."
                );

        Assert.Equal("Visitor", loginPage.GetUserRole());

        Assert.True(
            rolePage.IsVisitorDashboardDisplayed(),
            "Visitor dashboard should be displayed."
        );

        Assert.False(
            rolePage.IsAdminDashboardDisplayed(),
            "Visitor must not be shown the Admin dashboard."
        );

        Assert.False(
            rolePage.IsAdminUserManagementAvailable(),
            "Visitor must not have access to Admin User Management."
        );

        Assert.False(
            rolePage.IsAdminProviderManagementAvailable(),
            "Visitor must not have access to Admin Provider Management."
        );

        Assert.False(
            rolePage.IsAdminReportsAvailable(),
            "Visitor must not have access to Admin Reports."
        );
    }

    [Fact]
    public void Admin_Login_ShouldDisplayAdminDashboard()
    {
        RegistrationPage registrationPage = new(Driver);
        LoginPage loginPage = new(Driver);
        RoleAccessPage rolePage = new(Driver);

        registrationPage.Open();
        registrationPage.ClickLogin();

        loginPage.Login(
            TestConfiguration.AdminEmail,
            TestConfiguration.AdminPassword
        );

        Assert.True(
            loginPage.WaitForLoginSuccess(),
            "Visitor login did not complete successfully."
        );

        Assert.Equal("Admin", loginPage.GetUserRole());

        Assert.True(
            rolePage.IsAdminDashboardDisplayed(),
            "Admin dashboard should be displayed after Admin login."
        );

        Assert.True(
            rolePage.IsAdminUserManagementAvailable(),
            "Admin should have User Management access."
        );

        Assert.True(
            rolePage.IsAdminProviderManagementAvailable(),
            "Admin should have Provider Management access."
        );

        Assert.True(
            rolePage.IsAdminReportsAvailable(),
            "Admin should have Reports access."
        );

        Assert.False(
            rolePage.IsVisitorDashboardDisplayed(),
            "Admin should not be shown the Visitor dashboard."
        );
    }

    [Fact]
    public void Admin_ShouldNotHaveVisitorDashboardDisplayed()
    {
        RegistrationPage registrationPage = new(Driver);
        LoginPage loginPage = new(Driver);
        RoleAccessPage rolePage = new(Driver);

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

        Assert.Equal(
            "Admin",
            loginPage.GetUserRole()
        );

        Assert.True(
            rolePage.IsAdminDashboardDisplayed(),
            "Admin dashboard should be displayed."
        );

        Assert.False(
            rolePage.IsVisitorDashboardDisplayed(),
            "Admin must not be shown the Visitor dashboard."
        );
    }

    [Fact]
    public async Task VisitorToken_ShouldBeForbiddenFromAdminApi()
    {
        RegistrationPage registrationPage = new(Driver);
        LoginPage loginPage = new(Driver);

        registrationPage.Open();
        registrationPage.ClickLogin();

        loginPage.Login(
            TestConfiguration.VisitorEmail,
            TestConfiguration.VisitorPassword
        );

        Assert.True(
            loginPage.WaitForLoginSuccess(),
            "Visitor login did not complete successfully."
        );

        Assert.Equal(
            "Visitor",
            loginPage.GetUserRole()
        );

        string? token = loginPage.GetAuthToken();

        Assert.False(
            string.IsNullOrWhiteSpace(token),
            "Visitor authentication token was not available."
        );

        using HttpClient client = new HttpClient();

        client.BaseAddress = new Uri("http://localhost:5000");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response =
            await client.GetAsync("/api/admin/users");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode
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

        using HttpClient client = new HttpClient();

        client.BaseAddress = new Uri("http://localhost:5000");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response =
            await client.GetAsync("/api/admin/users");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode
        );
    }
}