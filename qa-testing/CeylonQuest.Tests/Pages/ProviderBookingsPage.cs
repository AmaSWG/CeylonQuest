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
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
        }

        // Navigation & Headers
        private By BookingsNavTab => By.XPath("//button[contains(., 'Bookings') or contains(., 'Booking Management')]");
        private By PageHeaderTitle => By.XPath("//h1[contains(text(), 'Booking Management')]");
        private By SearchInput => By.CssSelector("input[placeholder*='Search by service'], .pd-bookings-search input");
        private By TableRows => By.CssSelector("table.pd-bookings-table tbody tr, table tbody tr");
        private By EmptyContainer => By.CssSelector(".pd-bookings-empty, .vb-state");

        public void NavigateToBookingsTab()
        {
            try
            {
                var tab = _wait.Until(d => d.FindElement(BookingsNavTab));
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", tab);
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
            try
            {
                _wait.Until(d => d.FindElements(TableRows).Count > 0 || d.FindElements(EmptyContainer).Count > 0);
            }
            catch { }
            return _driver.FindElements(TableRows).Count;
        }

        public bool IsEmptyStateDisplayed()
        {
            try
            {
                var empty = _driver.FindElement(EmptyContainer);
                return empty.Displayed;
            }
            catch
            {
                return false;
            }
        }

        public void Search(string query)
        {
            var input = _wait.Until(d => d.FindElement(SearchInput));
            input.Clear();
            input.SendKeys(query);
        }

        public void SelectFilter(string filterName)
        {
            var btn = _wait.Until(d => d.FindElement(By.XPath($"//button[contains(., '{filterName}')]")));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", btn);
        }

        public void ClickViewDetailsOnFirstRow()
        {
            var btn = _wait.Until(d => d.FindElement(By.XPath("//button[contains(text(), 'View Details')]")));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", btn);
        }
    }
}