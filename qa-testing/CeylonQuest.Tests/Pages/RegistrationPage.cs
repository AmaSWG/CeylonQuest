using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using CeylonQuest.Tests.Configuration;

namespace CeylonQuest.Tests.Pages;

public class RegistrationPage
{
    private readonly IWebDriver driver;
    private readonly WebDriverWait wait;

    public RegistrationPage(IWebDriver driver)
    {
        this.driver = driver;
        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    private IWebElement FirstName =>
        driver.FindElement(By.Id("firstName"));

    private IWebElement LastName =>
        driver.FindElement(By.Id("lastName"));

    private IWebElement Email =>
        driver.FindElement(By.Id("email"));

    private IWebElement PhoneNumber =>
        driver.FindElement(By.Id("phoneNumber"));

    private IWebElement Nationality =>
        driver.FindElement(By.Id("nationality"));

    private IWebElement Password =>
        driver.FindElement(By.Id("password"));

    private IWebElement ConfirmPassword =>
        driver.FindElement(By.Id("confirmPassword"));

    private IWebElement CreateAccountButton =>
        driver.FindElement(By.Id("create-account"));

    public void Open()
    {
        driver.Navigate().GoToUrl(TestConfiguration.BaseUrl);
    }

    public void EnterFirstName(string value)
    {
        FirstName.SendKeys(value);
    }

    public void EnterLastName(string value)
    {
        LastName.SendKeys(value);
    }

    public void EnterEmail(string value)
    {
        Email.SendKeys(value);
    }

    public void EnterPhoneNumber(string value)
    {
        PhoneNumber.SendKeys(value);
    }

    public void EnterNationality(string value)
    {
        Nationality.SendKeys(value);
    }

    public void EnterPassword(string value)
    {
        Password.SendKeys(value);
    }

    public void EnterConfirmPassword(string value)
    {
        ConfirmPassword.SendKeys(value);
    }

    public void ClickCreateAccount()
    {
        CreateAccountButton.Click();
    }

    public void ClickLogin()
    {
        driver.FindElement(By.LinkText("Login")).Click();
    }

    public void RegisterUser(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string nationality,
        string password)
    {
        EnterFirstName(firstName);
        EnterLastName(lastName);
        EnterEmail(email);
        EnterPhoneNumber(phoneNumber);
        EnterNationality(nationality);
        EnterPassword(password);
        EnterConfirmPassword(password);

        ClickCreateAccount();
    }

    public bool IsRegistrationSuccessful()
    {
        try
        {
            wait.Until(d =>
                d.FindElement(By.CssSelector(".reg-toast--success")).Displayed
            );

            return true;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public string GetSuccessMessage()
    {
        IWebElement message = wait.Until(d =>
            d.FindElement(By.CssSelector(".reg-toast__title"))
        );

        return message.Text;
    }

    public string GetErrorMessage()
    {
        IWebElement error = wait.Until(d =>
            d.FindElement(By.CssSelector(".form-error"))
        );

        return error.Text;
    }
}