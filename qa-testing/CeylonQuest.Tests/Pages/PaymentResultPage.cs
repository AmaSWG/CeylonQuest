using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Pages
{
    public class PaymentResultPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public PaymentResultPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        }

        // Success Page Selectors
        private By SuccessCard => By.CssSelector(".psp-card--success");
        private By SuccessTitle => By.CssSelector(".psp-success-title");
        private By ConfirmedBadge => By.CssSelector(".psp-badge--confirmed");
        private By PaidBadge => By.CssSelector(".psp-badge--paid");
        private By AmountText => By.CssSelector(".psp-amount");
        private By BackToBookingsBtn => By.CssSelector(".psp-btn--primary");

        // Cancelled Page Selectors
        private By CancelledHeading => By.XPath("//h1[contains(text(), 'Payment Cancelled')]");
        private By CancelledBadge => By.CssSelector(".psp-badge--failed");

        public bool IsSuccessPageLoaded()
        {
            try
            {
                _wait.Until(d => d.FindElements(SuccessCard).Count > 0 || d.FindElements(SuccessTitle).Count > 0);
                return true;
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }

        public string GetSuccessTitle()
        {
            return _driver.FindElement(SuccessTitle).Text;
        }

        public bool IsConfirmedBadgeVisible()
        {
            var badges = _driver.FindElements(ConfirmedBadge);
            return badges.Count > 0 && badges[0].Displayed;
        }

        public bool IsPaidBadgeVisible()
        {
            var badges = _driver.FindElements(PaidBadge);
            return badges.Count > 0 && badges[0].Displayed;
        }

        public bool IsCancelledPageLoaded()
        {
            try
            {
                _wait.Until(d => d.FindElements(CancelledHeading).Count > 0);
                return true;
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }

        public void ClickBackToMyBookings()
        {
            var btn = _wait.Until(d => d.FindElement(BackToBookingsBtn));
            btn.Click();
        }
    }
}