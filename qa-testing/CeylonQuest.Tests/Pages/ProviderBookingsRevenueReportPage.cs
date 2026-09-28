using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
        }

        // Navigation
        private By ReportsSidebarBtn => By.XPath("//button[@id='nav-reports' or contains(., 'Reports')] | //a[contains(., 'Reports')]");
        private By BookingsRevenueSubTabBtn => By.XPath("//button[contains(@class, 'cq-report-subtab-btn') and contains(., 'Bookings & Revenue')] | //button[contains(., 'Bookings & Revenue')]");
        private By PageHeaderTitle => By.CssSelector(".pd-page-header h1");

        // Form Fields
        private By StartDateInput => By.Id("pbr-start");
        private By EndDateInput => By.Id("pbr-end");
        private By StatusSelect => By.Id("pbr-status");
        private By BookingTypeSelect => By.Id("pbr-type");
        private By ApplyFiltersBtn => By.CssSelector("button.pbr-button--primary, button.pbr-button[type='submit']");
        private By ResetFiltersBtn => By.XPath("//button[contains(@class, 'pbr-button') and contains(., 'Reset Filters')]");
        private By ValidationMsg => By.CssSelector(".pbr-validation");

        // Results
        private By TotalBookingsValue => By.CssSelector(".pbr-summary .pbr-total");
        private By TableRows => By.CssSelector("table.pd-table tbody tr");
        private By NoDataMessage => By.XPath("//div[contains(@class, 'pbr-state') or contains(@class, 'brr-empty-state')]//p | //div[contains(@class, 'pbr-state') or contains(@class, 'brr-empty-state')]");
        private By SummaryContainer => By.CssSelector(".pbr-summary");

        public void NavigateToReport()
        {
            // 1. Click Reports in sidebar
            var nav = _wait.Until(d =>
            {
                var el = d.FindElements(ReportsSidebarBtn).FirstOrDefault(e => e.Displayed);
                return el;
            });
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", nav);

            // 2. Click Bookings & Revenue subtab
            var subTab = _wait.Until(d =>
            {
                var el = d.FindElements(BookingsRevenueSubTabBtn).FirstOrDefault(e => e.Displayed);
                return el;
            });
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", subTab);

            // 3. Wait until the header is displayed
            _wait.Until(d => d.FindElement(PageHeaderTitle).Displayed);

            // 4. Wait for report data loading to complete
            WaitForReportToLoad();
        }

        public void WaitForReportToLoad()
        {
            try
            {
                _wait.Until(d => d.FindElements(SummaryContainer).Count > 0 || d.FindElements(NoDataMessage).Count > 0);
            }
            catch { }
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
            Thread.Sleep(300);
            WaitForReportToLoad();
        }

        public void FilterByType(string type)
        {
            var selectEl = _wait.Until(d => d.FindElement(BookingTypeSelect));
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                @"var sel = arguments[0];
                  var val = arguments[1];
                  var setter = Object.getOwnPropertyDescriptor(window.HTMLSelectElement.prototype, 'value').set;
                  if (setter) setter.call(sel, val);
                  else sel.value = val;
                  sel.dispatchEvent(new Event('input', { bubbles: true }));
                  sel.dispatchEvent(new Event('change', { bubbles: true }));",
                selectEl, type);
            Thread.Sleep(200);

            var applyBtn = _wait.Until(d => d.FindElement(ApplyFiltersBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", applyBtn);
            Thread.Sleep(500);
            WaitForReportToLoad();
        }

        public void FilterByStatus(string status)
        {
            var selectEl = _wait.Until(d => d.FindElement(StatusSelect));
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                @"var sel = arguments[0];
                  var val = arguments[1];
                  var setter = Object.getOwnPropertyDescriptor(window.HTMLSelectElement.prototype, 'value').set;
                  if (setter) setter.call(sel, val);
                  else sel.value = val;
                  sel.dispatchEvent(new Event('input', { bubbles: true }));
                  sel.dispatchEvent(new Event('change', { bubbles: true }));",
                selectEl, status);
            Thread.Sleep(200);

            var applyBtn = _wait.Until(d => d.FindElement(ApplyFiltersBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", applyBtn);
            Thread.Sleep(500);
            WaitForReportToLoad();
        }

        public void ResetFilters()
        {
            var resetBtn = _wait.Until(d => d.FindElement(ResetFiltersBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", resetBtn);
            Thread.Sleep(300);
            WaitForReportToLoad();
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
                var msgEl = _wait.Until(d =>
                {
                    var elements = d.FindElements(NoDataMessage);
                    var match = elements.FirstOrDefault(e => e.Displayed && e.Text.Contains("No data available"));
                    return match;
                });
                return msgEl?.Text ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}