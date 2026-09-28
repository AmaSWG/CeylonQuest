using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Pages
{
    public class ProviderBookingsRevenueReportPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public ProviderBookingsRevenueReportPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
        }

        // Navigation
        private By ReportsSidebarBtn => By.Id("nav-reports");
        private By BookingsRevenueSubTabBtn => By.XPath("//button[contains(@class, 'cq-report-subtab-btn') and contains(text(), 'Bookings & Revenue')]");
        private By PageHeaderTitle => By.CssSelector(".pd-page-header h1");

        // Form Fields
        private By StartDateInput => By.Id("pbr-start");
        private By EndDateInput => By.Id("pbr-end");
        private By StatusSelect => By.Id("pbr-status");
        private By BookingTypeSelect => By.Id("pbr-type");
        private By ApplyFiltersBtn => By.CssSelector("button.pbr-button--primary");
        private By ResetFiltersBtn => By.XPath("//button[contains(@class, 'pbr-button') and text()='Reset Filters']");
        private By ValidationMsg => By.CssSelector(".pbr-validation");

        // Results
        private By TotalBookingsValue => By.CssSelector(".pbr-summary .pbr-total");
        private By TableRows => By.CssSelector("table.pd-table tbody tr");
        private By NoDataMessage => By.CssSelector(".pbr-state p");

        public void NavigateToReport()
        {
            // 1. Click Reports in sidebar
            var nav = _wait.Until(d => d.FindElement(ReportsSidebarBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", nav);

            // 2. Click Bookings & Revenue subtab
            var subTab = _wait.Until(d => d.FindElement(BookingsRevenueSubTabBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", subTab);

            // 3. Wait until the header is displayed
            _wait.Until(d => d.FindElement(PageHeaderTitle).Displayed);
        }

        public bool IsLoaded()
        {
            try
            {
                return _wait.Until(d => d.FindElement(PageHeaderTitle).Text.Contains("Bookings & Revenue Report"));
            }
            catch { return false; }
        }

        public void FilterByDates(string start, string end)
        {
            var startEl = _wait.Until(d => d.FindElement(StartDateInput));
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                @"var el = arguments[0];
                  var setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
                  if (setter) setter.call(el, arguments[1]);
                  else el.value = arguments[1];
                  el.dispatchEvent(new Event('input', { bubbles: true }));
                  el.dispatchEvent(new Event('change', { bubbles: true }));",
                startEl, start);

            var endEl = _wait.Until(d => d.FindElement(EndDateInput));
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                @"var el = arguments[0];
                  var setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
                  if (setter) setter.call(el, arguments[1]);
                  else el.value = arguments[1];
                  el.dispatchEvent(new Event('input', { bubbles: true }));
                  el.dispatchEvent(new Event('change', { bubbles: true }));",
                endEl, end);

            var applyBtn = _wait.Until(d => d.FindElement(ApplyFiltersBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", applyBtn);
        }

        public void FilterByType(string type)
        {
            var selectEl = _wait.Until(d => d.FindElement(BookingTypeSelect));
            new SelectElement(selectEl).SelectByValue(type);

            var applyBtn = _wait.Until(d => d.FindElement(ApplyFiltersBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", applyBtn);
        }

        public void FilterByStatus(string status)
        {
            var selectEl = _wait.Until(d => d.FindElement(StatusSelect));
            new SelectElement(selectEl).SelectByValue(status);

            var applyBtn = _wait.Until(d => d.FindElement(ApplyFiltersBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", applyBtn);
        }

        public void ResetFilters()
        {
            var resetBtn = _wait.Until(d => d.FindElement(ResetFiltersBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", resetBtn);
        }

        public int GetRecordRowCount()
        {
            return _driver.FindElements(TableRows).Count;
        }

        public string GetValidationError()
        {
            try
            {
                return _wait.Until(d => d.FindElement(ValidationMsg)).Text;
            }
            catch { return string.Empty; }
        }

        public string GetNoDataMessage()
        {
            try
            {
                return _wait.Until(d => d.FindElement(NoDataMessage)).Text;
            }
            catch { return string.Empty; }
        }
    }
}