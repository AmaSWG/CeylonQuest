using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Pages;

public class LoginPage
{
    private readonly IWebDriver driver;
    private readonly WebDriverWait wait;

    public LoginPage(IWebDriver driver)
    {
        this.driver = driver;
        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    private IWebElement Email =>
        driver.FindElement(By.Id("login-email"));

    private IWebElement Password =>
        driver.FindElement(By.Id("login-password"));

    private IWebElement LoginButton =>
        driver.FindElement(By.Id("login-button"));

    public void Login(string email, string password)
    {
        Email.Clear();
        Email.SendKeys(email);

        Password.Clear();
        Password.SendKeys(password);

        LoginButton.Click();
    }

    public bool HasAuthToken()
    {
        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

        object? token = js.ExecuteScript(
            "return window.localStorage.getItem('authToken');"
        );

        return token != null &&
               !string.IsNullOrWhiteSpace(token.ToString());
    }

    public string? GetUserRole()
    {
        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

        object? role = js.ExecuteScript(
            "return window.localStorage.getItem('userRole');"
        );

        return role?.ToString();
    }

    public bool IsLoginErrorDisplayed()
    {
        try
        {
            return wait.Until(d =>
                d.FindElement(By.CssSelector(".login-error")).Displayed
            );
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public string GetLoginError()
    {
        IWebElement error = wait.Until(d =>
            d.FindElement(By.CssSelector(".login-error"))
        );

        return error.Text;
    }

    public void EnterEmail(string email)
    {
        IWebElement emailInput =
            driver.FindElement(By.Id("login-email"));

        emailInput.Clear();
        emailInput.SendKeys(email);
    }

    public void EnterPassword(String password)
    {
        IWebElement passwordInput =
            driver.FindElement(By.Id("login-password"));

        passwordInput.Clear();
        passwordInput.SendKeys(password);
    }

    public void ClickLogin()
    {
        driver.FindElement(By.Id("login-button")).Click();
    }

    public string? GetLocalStorageValue(string key)
    {
        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

        object? value = js.ExecuteScript(
            "return window.localStorage.getItem(arguments[0]);",
            key
        );

        return value?.ToString();
    }

    //wait for login success
    public bool WaitForLoginSuccess()
    {
        try
        {
            return wait.Until(d =>
            {
                IJavaScriptExecutor js = (IJavaScriptExecutor)d;

                object? token = js.ExecuteScript(
                    "return window.localStorage.getItem('authToken');"
                );

                object? role = js.ExecuteScript(
                    "return window.localStorage.getItem('userRole');"
                );

                return token != null &&
                       !string.IsNullOrWhiteSpace(token.ToString()) &&
                       role != null &&
                       !string.IsNullOrWhiteSpace(role.ToString());
            });
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    //extract the real JWT from localStorage
    public string? GetAuthToken()
    {
        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

        object? token = js.ExecuteScript(
            "return window.localStorage.getItem('authToken');"
        );

        return token?.ToString();
    }

    public bool HasUserRole()
    {
        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

        object? role = js.ExecuteScript(
            "return window.localStorage.getItem('userRole');"
        );

        return role != null &&
               !string.IsNullOrWhiteSpace(role.ToString());
    }
}