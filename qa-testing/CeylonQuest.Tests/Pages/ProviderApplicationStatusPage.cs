using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;

namespace CeylonQuest.Tests.Pages
{
    public class ProviderApplicationStatusPage
    {
        private readonly IWebDriver driver;
        private readonly WebDriverWait wait;

        public ProviderApplicationStatusPage(IWebDriver driver)
        {
            this.driver = driver;
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        public void Open()
        {
            driver.Navigate().GoToUrl(
                "http://localhost:5173/provider-application-status"
            );
        }

        public void SearchByEmail(string email)
        {
            IWebElement emailInput = wait.Until(d =>
                d.FindElement(By.Id("pas-email"))
            );

            emailInput.Clear();
            emailInput.SendKeys(email);

            driver.FindElement(By.Id("pas-check-status-btn")).Click();
        }

        public bool WaitForResults()
        {
            try
            {
                return wait.Until(d =>
                    d.FindElements(By.Id("pas-results-section"))
                     .Any(e => e.Displayed)
                );
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }

        public string GetStatus()
        {
            IWebElement badge = wait.Until(d =>
                d.FindElement(By.CssSelector(".pas-status-badge"))
            );

            return badge.Text;
        }

        public bool IsPendingStatusDisplayed()
        {
            return driver
                .FindElements(By.CssSelector(".pas-status-badge--pending"))
                .Any(e =>
                    e.Displayed &&
                    e.Text.Contains(
                        "Pending",
                        StringComparison.OrdinalIgnoreCase
                    )
                );
        }

        public bool IsApprovedStatusDisplayed()
        {
            return driver
                .FindElements(By.CssSelector(".pas-status-badge--approved"))
                .Any(e =>
                    e.Displayed &&
                    e.Text.Contains(
                        "Approved",
                        StringComparison.OrdinalIgnoreCase
                    )
                );
        }

        public bool IsRejectedStatusDisplayed()
        {
            return driver
                .FindElements(By.CssSelector(".pas-status-badge--rejected"))
                .Any(e =>
                    e.Displayed &&
                    e.Text.Contains(
                        "Rejected",
                        StringComparison.OrdinalIgnoreCase
                    )
                );
        }

        public bool IsRejectionReasonDisplayed()
        {
            return driver
                .FindElements(By.CssSelector(".pas-rejection-reason-text"))
                .Any(e =>
                    e.Displayed &&
                    !string.IsNullOrWhiteSpace(e.Text)
                );
        }

        public string GetRejectionReason()
        {
            return wait.Until(d =>
                d.FindElement(
                    By.CssSelector(".pas-rejection-reason-text")
                )
            ).Text;
        }

        public bool IsActivationButtonDisplayed()
        {
            return driver
                .FindElements(By.Id("pas-enter-otp-btn"))
                .Any(e => e.Displayed);
        }

        public bool IsNotFoundErrorDisplayed()
        {
            return driver
                .FindElements(By.CssSelector(".pas-error-box[role='alert']"))
                .Any(e => e.Displayed);
        }

        public string GetErrorMessage()
        {
            IWebElement error = wait.Until(d =>
                d.FindElement(By.CssSelector(".pas-error-msg"))
            );

            return error.Text;
        }

        public bool IsResultsDisplayed()
        {
            return driver
                .FindElements(By.Id("pas-results-section"))
                .Any(e => e.Displayed);
        }

        public string GetDisplayedApplicantEmail()
        {
            var details = driver.FindElements(
                By.CssSelector(".pas-detail-item")
            );

            foreach (IWebElement item in details)
            {
                if (item.Text.Contains(
                    "Applicant Email",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return item
                        .FindElement(By.CssSelector(".pas-detail-val"))
                        .Text
                        .Trim();
                }
            }

            return "";
        }
    }
}