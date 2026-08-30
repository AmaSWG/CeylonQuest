using CeylonQuest.Tests.Configuration;
using CeylonQuest.Tests.Pages;
using Xunit;

namespace CeylonQuest.Tests.Tests;

public class LoginTest : BaseTest
{
    // checking success login
    [Fact]
    public void Login_WithRegisteredVisitor_ShouldSucceed()
    {
        RegistrationPage registrationPage = new(Driver);
        LoginPage loginPage = new(Driver);

        registrationPage.Open();
        registrationPage.ClickLogin();

        loginPage.Login(
            TestConfiguration.VisitorEmail,
            TestConfiguration.VisitorPassword
        );

        Thread.Sleep(1500); // temporary debugging only

        Console.WriteLine($"Current URL: {Driver.Url}");
        Console.WriteLine($"authToken: {loginPage.GetLocalStorageValue("authToken")}");
        Console.WriteLine($"userRole: {loginPage.GetLocalStorageValue("userRole")}");

        if (loginPage.IsLoginErrorDisplayed())
        {
            Console.WriteLine($"Login error: {loginPage.GetLoginError()}");
        }

        Assert.True(
            loginPage.HasAuthToken(),
            "Login failed because no authentication token was stored."
        );
    }

    // Login with wrong password
    [Fact]
    public void Login_WithWrongPassword_ShouldShowError()
    {
        RegistrationPage registrationPage =
            new RegistrationPage(Driver);

        LoginPage loginPage =
            new LoginPage(Driver);

        registrationPage.Open();
        registrationPage.ClickLogin();

        // Correct registered email, but wrong password
        loginPage.Login(
            TestConfiguration.VisitorEmail,
            "WrongPassword123#"
        );

        Assert.True(
            loginPage.IsLoginErrorDisplayed(),
            "Expected login error was not displayed."
        );

        Assert.Contains(
            "Invalid credentials",
            loginPage.GetLoginError()
        );

        Assert.False(
            loginPage.HasAuthToken(),
            "Auth token should not be created when the password is incorrect."
        );
    }

    //Login with wrong email
    [Fact]
    public void Login_WithWrongEmail_ShouldShowError()
    {
        RegistrationPage registrationPage =
            new RegistrationPage(Driver);

        LoginPage loginPage =
            new LoginPage(Driver);

        registrationPage.Open();
        registrationPage.ClickLogin();

        // Email that does not exist, but use the correct test password
        loginPage.Login(
            $"nonexistent{DateTime.Now.Ticks}@test.com",
            TestConfiguration.VisitorPassword
        );

        Assert.True(
            loginPage.IsLoginErrorDisplayed(),
            "Expected login error was not displayed."
        );

        Assert.Contains(
            "Invalid credentials",
            loginPage.GetLoginError()
        );

        Assert.False(
            loginPage.HasAuthToken(),
            "Auth token should not be created when the email is not registered."
        );
    }

    //Login with empty email
    [Fact]
    public void Login_WithEmptyEmail_ShouldShowError()
    {
        RegistrationPage registrationPage =
            new RegistrationPage(Driver);

        LoginPage loginPage =
            new LoginPage(Driver);

        registrationPage.Open();
        registrationPage.ClickLogin();

        //Leave email empty
        loginPage.EnterEmail("");

        //password has a value
        loginPage.EnterPassword(
            TestConfiguration.VisitorPassword
        );

        loginPage.ClickLogin();

        Assert.True(
            loginPage.IsLoginErrorDisplayed(),
             "Expected validation error was not displayed."
        );

        Assert.Contains(
            "Please enter your email address.",
            loginPage.GetLoginError()
        );

        Assert.False(
            loginPage.HasAuthToken(),
             "Authentication token should not be created."
        );
    }

    [Fact]
    public void Login_WithEmptyPassword_ShouldShowError()
    {
        RegistrationPage registrationPage =
            new RegistrationPage(Driver);

        LoginPage loginPage =
            new LoginPage(Driver);

        registrationPage.Open();
        registrationPage.ClickLogin();

        // Correct registered email
        loginPage.EnterEmail(
            TestConfiguration.VisitorEmail
        );

        // Leave password empty
        loginPage.EnterPassword("");

        loginPage.ClickLogin();

        Assert.True(
            loginPage.IsLoginErrorDisplayed(),
            "Expected validation error was not displayed."
        );

        Assert.Contains(
            "Please enter your password.",
            loginPage.GetLoginError()
        );

        Assert.False(
            loginPage.HasAuthToken(),
            "Authentication token should not be created."
        );
    }

}