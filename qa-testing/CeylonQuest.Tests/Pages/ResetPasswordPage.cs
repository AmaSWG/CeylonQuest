using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using CeylonQuest.Tests.Configuration;

namespace CeylonQuest.Tests.Pages;

public class ResetPasswordPage
{
    private readonly IWebDriver driver;
    private readonly WebDriverWait wait;

    public ResetPasswordPage(IWebDriver driver)
    {
        this.driver = driver;
        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    private IWebElement NewPassword =>
        driver.FindElement(By.Id("newPassword"));

    private IWebElement ConfirmPassword =>
        driver.FindElement(By.Id("confirmPassword"));

    private IWebElement ResetButton =>
        driver.FindElement(
            By.CssSelector(
                ".reset-password-button--primary[type='submit']"
            )
        );

    public void OpenWithoutToken()
    {
        string url = new Uri(
            new Uri(TestConfiguration.BaseUrl),
            "reset-password"
        ).ToString();

        driver.Navigate().GoToUrl(url);
    }

    public void OpenWithToken(string token)
    {
        string url = new Uri(
            new Uri(TestConfiguration.BaseUrl),
            $"reset-password?token={Uri.EscapeDataString(token)}"
        ).ToString();

        driver.Navigate().GoToUrl(url);
    }

    public void EnterNewPassword(string password)
    {
        NewPassword.Clear();
        NewPassword.SendKeys(password);
    }

    public void EnterConfirmPassword(string password)
    {
        ConfirmPassword.Clear();
        ConfirmPassword.SendKeys(password);
    }

    public void EnterPasswords(
        string newPassword,
        string confirmPassword)
    {
        EnterNewPassword(newPassword);
        EnterConfirmPassword(confirmPassword);
    }

    public bool IsResetButtonEnabled()
    {
        return ResetButton.Enabled;
    }

    public void ClickResetPassword()
    {
        ResetButton.Click();
    }

    public bool IsInvalidLinkDisplayed()
    {
        try
        {
            return wait.Until(d =>
                d.FindElement(
                    By.CssSelector(
                        ".reset-password-error-container"
                    )
                ).Displayed
            );
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public string GetInvalidLinkTitle()
    {
        return wait.Until(d =>
            d.FindElement(
                By.CssSelector(
                    ".reset-password-error-container h3"
                )
            )
        ).Text;
    }

    public string GetInvalidLinkMessage()
    {
        return wait.Until(d =>
            d.FindElement(
                By.CssSelector(
                    ".reset-password-error-container p"
                )
            )
        ).Text;
    }

    public string GetPasswordMatchMessage()
    {
        return wait.Until(d =>
            d.FindElement(
                By.CssSelector(".reset-password-match")
            )
        ).Text;
    }

    public string GetFormError()
    {
        return wait.Until(d =>
            d.FindElement(
                By.CssSelector(".reset-password-error")
            )
        ).Text;
    }

    public bool IsSuccessDisplayed()
    {
        try
        {
            return wait.Until(d =>
                d.FindElement(
                    By.CssSelector(
                        ".reset-password-success"
                    )
                ).Displayed
            );
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public string GetSuccessTitle()
    {
        return wait.Until(d =>
            d.FindElement(
                By.CssSelector(
                    ".reset-password-success h3"
                )
            )
        ).Text;
    }

    public bool IsRequirementMet(string requirementText)
    {
        IWebElement requirement =
            driver.FindElements(
                By.CssSelector(
                    ".password-requirements__item"
                )
            )
            .First(x =>
                x.Text.Contains(
                    requirementText,
                    StringComparison.OrdinalIgnoreCase
                )
            );

        string classes =
            requirement.GetAttribute("class") ?? "";

        return classes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains("met");
    }

    public bool IsWeakPasswordRequirementDisplayed()
    {
        return driver
            .FindElements(By.CssSelector(".password-requirements__item:not(.met)"))
            .Any(e => e.Displayed);
    }

    public bool IsSubmitButtonEnabled()
    {
        IWebElement button = driver.FindElement(
            By.CssSelector(".reset-password-button--primary")
        );

        return button.Enabled;
    }

    public void ClickSubmit()
    {
        IWebElement button = wait.Until(d =>
            d.FindElement(By.CssSelector(".reset-password-button--primary"))
        );

        button.Click();
    }

    public bool IsResetErrorDisplayed()
    {
        try
        {
            return wait.Until(d =>
                d.FindElements(By.CssSelector(".reset-password-error[role='alert']"))
                 .Any(e => e.Displayed)
            );
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public bool IsPasswordMatchDisplayed()
    {
        return driver
            .FindElements(By.CssSelector(".reset-password-match"))
            .Any(e =>
                e.Displayed &&
                e.Text.Contains("match", StringComparison.OrdinalIgnoreCase)
            );
    }
}