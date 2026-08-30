using CeylonQuest.Tests.Configuration;
using CeylonQuest.Tests.Pages;
using Xunit;

namespace CeylonQuest.Tests.Tests;

public class LogoutTest : BaseTest
{
    [Fact]
    public void Visitor_Logout_ShouldClearSessionAndExitDashboard()
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

        Assert.Equal(
            "Visitor",
            loginPage.GetUserRole()
        );

        Assert.True(
            rolePage.IsVisitorDashboardDisplayed(),
            "Visitor dashboard should be displayed before logout."
        );

        // Logout
        rolePage.ClickVisitorLogout();

        // Authentication information should be removed
        Assert.False(
            loginPage.HasAuthToken(),
            "Authentication token should be removed after logout."
        );

        Assert.False(
            loginPage.HasUserRole(),
            "User role should be removed after logout."
        );

        // Visitor dashboard should no longer be accessible/displayed
        Assert.False(
            rolePage.IsVisitorDashboardPresent(),
            "Visitor dashboard should not remain displayed after logout."
        );
    }

    [Fact]
    public void Admin_Logout_ShouldClearSessionAndExitDashboard()
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
            "Admin dashboard should be displayed before logout."
        );

        rolePage.ClickAdminLogout();

        Assert.False(
            loginPage.HasAuthToken(),
            "Authentication token should be removed after Admin logout."
        );

        Assert.False(
            loginPage.HasUserRole(),
            "User role should be removed after Admin logout."
        );

        Assert.False(
            rolePage.IsAdminDashboardPresent(),
            "Admin dashboard should not remain displayed after logout."
        );
    }
}