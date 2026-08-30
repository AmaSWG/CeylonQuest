using CeylonQuest.Tests.Configuration;
using CeylonQuest.Tests.Pages;
using Xunit;

namespace CeylonQuest.Tests.Tests;

public class RegistrationTest : BaseTest
{
    [Fact]
    public void RegisterUser_WithValidDetails_ShouldSucceed()
    {
        RegistrationPage registrationPage =
            new RegistrationPage(Driver);

        registrationPage.Open();

        string uniqueEmail =
            $"qauser{DateTime.Now.Ticks}@test.com";

        registrationPage.RegisterUser(
            firstName: "QA",
            lastName: "Tester",
            email: uniqueEmail,
            phoneNumber: "0771234567",
            nationality: "Sri Lankan",
            password: "Password123#"
        );

        Assert.True(
            registrationPage.IsRegistrationSuccessful(),
            "Registration success message was not displayed."
        );

        Assert.Contains(
            "Registration Successful",
            registrationPage.GetSuccessMessage()
        );
    }

    [Fact]
    public void RegisterUser_WithDuplicateEmail_ShouldShowError()
    {
        RegistrationPage registrationPage =
            new RegistrationPage(Driver);

        registrationPage.Open();

        registrationPage.RegisterUser(
            firstName: "QA",
            lastName: "Tester",
            email: TestConfiguration.VisitorEmail,
            phoneNumber: "0771234567",
            nationality: "Sri Lankan",
            password: "Password123#"
        );

        string errorMessage = registrationPage.GetErrorMessage();

        Assert.Contains(
            "Email already in use",
            errorMessage
        );
    }
}