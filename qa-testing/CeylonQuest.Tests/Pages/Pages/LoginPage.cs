using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;

namespace CeylonQuest.Tests.Pages
{

    public class LoginPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public LoginPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        }

        private IWebElement EmailInput => _wait.Until(d => d.FindElement(By.CssSelector("input[type='email'], input[name='email']")));
        private IWebElement PasswordInput => _driver.FindElement(By.CssSelector("input[type='password'], input[name='password']"));
        private IWebElement LoginButton => _driver.FindElement(By.CssSelector("button[type='submit']"));

        public void Login(string email, string password)
        {
            EmailInput.Clear();
            EmailInput.SendKeys(email);
            PasswordInput.Clear();
            PasswordInput.SendKeys(password);
            LoginButton.Click();

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

            // Fail fast if the app shows an invalid-credentials banner
            try
            {
                var error = _driver.FindElement(By.XPath(
                    "//*[contains(text(),'Invalid credentials')]"));
                if (error.Displayed)
                    throw new Exception($"Login failed for {email}: {error.Text}");
            }
            catch (NoSuchElementException) { /* no error banner, good */ }

            // Wait until we're no longer on the login page
            wait.Until(d => !d.Url.Contains("/me"));
        }
    }
}