using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Pages
{
    public class ProviderBookingsPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public ProviderBookingsPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        private By BookingsNavTab => By.XPath("//button[contains(text(), 'Bookings') or contains(text(), 'Booking Management')]");
        private By PageHeaderTitle => By.CssSelector(".pd-page-header h1");
        private By SearchInput => By.CssSelector(".pd-bookings-search input");
        private By FilterButtons => By.CssSelector(".pd-bookings-filter");
        private By TableRows => By.CssSelector("table.pd-bookings-table tbody tr");
        private By EmptyContainer => By.CssSelector(".pd-bookings-empty");

        public void NavigateToBookingsTab()
        {
            try
            {
                var tab = _wait.Until(d => d.FindElement(BookingsNavTab));
                tab.Click();
            }
            catch
            {
                _driver.Navigate().GoToUrl("http://localhost:5173/#provider/bookings");
            }
            _wait.Until(d => d.FindElement(PageHeaderTitle).Displayed);
        }

        public bool IsLoaded()
        {
            try
            {
                return _wait.Until(d => d.FindElement(PageHeaderTitle).Text.Contains("Booking Management"));
            }
            catch
            {
                return false;
            }
        }

        public int GetRowCount()
        {
            return _driver.FindElements(TableRows).Count;
        }

        public void Search(string query)
        {
            var input = _wait.Until(d => d.FindElement(SearchInput));
            input.Clear();
            input.SendKeys(query);
        }

        public void SelectFilter(string filterName)
        {
            var btn = _wait.Until(d => d.FindElement(By.XPath($"//button[contains(@class, 'pd-bookings-filter') and contains(text(), '{filterName}')]")));
            btn.Click();
        }

        public bool IsEmptyStateDisplayed()
        {
            try
            {
                return _driver.FindElement(EmptyContainer).Displayed;
            }
            catch
            {
                return false;
            }
        }
    }
}