using CeylonQuest.Tests.Pages;
using Xunit;

namespace CeylonQuest.Tests.Tests;

public class RegisterAndLoginTest : BaseTest
{
    [Fact]
    public void RegisterNewUser_ThenLoginWithSameUser_ShouldSucceed()
    {
        // Arrange
        RegistrationPage registrationPage =
            new RegistrationPage(Driver);

        string email =
            $"qauser{DateTime.Now.Ticks}@test.com";

        string password = "Password123#";

        // Step 1 - Open Registration page
        registrationPage.Open();

        // Step 2 - Register a new visitor
        registrationPage.RegisterUser(
            firstName: "QA",
            lastName: "Tester",
            email: email,
            phoneNumber: "0771234567",
            nationality: "Sri Lankan",
            password: password
        );

        // Step 3 - Verify registration succeeded
        Assert.True(
            registrationPage.IsRegistrationSuccessful(),
            "Registration failed."
        );

        Assert.Contains(
            "Registration Successful",
            registrationPage.GetSuccessMessage()
        );

        // Step 4 - Go to Login page
        registrationPage.ClickLogin();

        // Step 5 - Login using the SAME registered account
        LoginPage loginPage =
            new LoginPage(Driver);

        loginPage.Login(email, password);

        // Step 6 - Verify token was created
        Assert.True(
            loginPage.HasAuthToken(),
            "Login failed because no authentication token was stored."
        );

        // Step 7 - Verify visitor role
        Assert.Equal(
            "Visitor",
            loginPage.GetUserRole()
        );
    }
}