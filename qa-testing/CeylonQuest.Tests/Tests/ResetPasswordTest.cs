using CeylonQuest.Tests.Pages;
using Xunit;

namespace CeylonQuest.Tests.Tests;

public class ResetPasswordTest : BaseTest
{
    [Fact]
    public void ResetPassword_WithoutToken_ShouldShowInvalidLink()
    {
        ResetPasswordPage resetPage =
            new ResetPasswordPage(Driver);

        resetPage.OpenWithoutToken();

        Assert.True(
            resetPage.IsInvalidLinkDisplayed(),
            "Invalid password-reset link message was not displayed."
        );

        Assert.Contains(
            "Invalid Link",
            resetPage.GetInvalidLinkTitle()
        );

        Assert.Contains(
            "invalid or has expired",
            resetPage.GetInvalidLinkMessage()
        );
    }

    [Fact]
    public void ResetPassword_WithMismatchedPasswords_ShouldNotAllowSubmit()
    {
        ResetPasswordPage resetPage =
            new ResetPasswordPage(Driver);

        resetPage.OpenWithToken("test-token");

        resetPage.EnterPasswords(
            "ValidPassword123#",
            "DifferentPassword123#"
        );
        // must have the exact same message to test successfully
        Assert.Contains(
            "Passwords do not match",
            resetPage.GetPasswordMatchMessage()
        );

        Assert.False(
            resetPage.IsResetButtonEnabled(),
            "Reset Password button should be disabled when passwords do not match."
        );
    }

    [Fact]
    public void ResetPassword_WithWeakPassword_ShouldNotAllowSubmit()
    {
        ResetPasswordPage resetPage = new(Driver);

        resetPage.OpenWithToken("dummy-token");

        resetPage.EnterNewPassword("abc");
        resetPage.EnterConfirmPassword("abc");

        Assert.True(
            resetPage.IsWeakPasswordRequirementDisplayed(),
            "Weak password requirements should be shown."
        );

        Assert.False(
            resetPage.IsSubmitButtonEnabled(),
            "Reset password button should remain disabled for a weak password."
        );
    }

    [Fact]
    public void ResetPassword_WithInvalidToken_ShouldShowError()
    {
        ResetPasswordPage resetPage = new(Driver);

        resetPage.OpenWithToken("invalid-token");

        resetPage.EnterNewPassword("StrongPass123!");
        resetPage.EnterConfirmPassword("StrongPass123!");

        Assert.True(
            resetPage.IsSubmitButtonEnabled(),
            "Submit button should be enabled for matching valid passwords."
        );

        resetPage.ClickSubmit();

        Assert.True(
            resetPage.IsResetErrorDisplayed(),
            "An error should be displayed for an invalid reset token."
        );
    }

    [Fact]
    public void ResetPassword_WithMatchingValidPasswords_ShouldEnableSubmit()
    {
        ResetPasswordPage resetPage = new(Driver);

        resetPage.OpenWithToken("dummy-token");

        resetPage.EnterNewPassword("StrongPass123!");
        resetPage.EnterConfirmPassword("StrongPass123!");

        Assert.True(
            resetPage.IsPasswordMatchDisplayed(),
            "The UI should indicate that the passwords match."
        );

        Assert.True(
            resetPage.IsSubmitButtonEnabled(),
            "Submit button should be enabled when passwords are valid and matching."
        );
    }

}