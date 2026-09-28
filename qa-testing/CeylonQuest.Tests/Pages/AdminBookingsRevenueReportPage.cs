using System;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Pages
{
    public class AdminBookingsRevenueReportPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public AdminBookingsRevenueReportPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
        }

        private By ReportsNavBtn => By.Id("nav-reports");
        private By BookingsRevenueSubTabBtn => By.XPath("//button[contains(., 'Bookings & Revenue Report') or contains(., 'Bookings & Revenue')]");
        private By PageHeaderTitle => By.CssSelector(".cq-report-header-title, .pd-page-header h1, h1");

        // Exact selectors matching AdminBookingsRevenueReportTab.jsx
        private By TotalBookingsKpi => By.XPath("//span[contains(@class, 'cq-kpicard__label') and contains(., 'Total Bookings')]/following-sibling::span[contains(@class, 'cq-kpicard__val')]");
        private By TotalRevenueKpi => By.XPath("//span[contains(@class, 'cq-kpicard__label') and contains(., 'Total Revenue')]/following-sibling::span[contains(@class, 'cq-kpicard__val')]");
        private By KpiCardValues => By.CssSelector(".cq-kpicard__val, .brr-kpi-val");

        public void NavigateToAdminReport()
        {
            // 1. Click 'Reports & Analytics' in sidebar
            var nav = _wait.Until(d => d.FindElement(ReportsNavBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", nav);
            Thread.Sleep(500);

            // 2. Click 'Bookings & Revenue Report' subtab
            var subTab = _wait.Until(d => d.FindElement(BookingsRevenueSubTabBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", subTab);
            Thread.Sleep(500);

            // 3. Wait until the header is displayed
            _wait.Until(d => d.FindElement(PageHeaderTitle).Displayed);

            // 4. Wait for report data to load (KPI card value populated)
            _wait.Until(d =>
            {
                var elements = d.FindElements(KpiCardValues);
                return elements.Count > 0 && !string.IsNullOrWhiteSpace(elements[0].Text) ? elements[0] : null;
            });
        }

        public bool IsLoaded()
        {
            try
            {
                return _wait.Until(d => d.FindElement(PageHeaderTitle).Text.Contains("Bookings & Revenue Report"));
            }
            catch { return false; }
        }

        public string GetTotalBookings()
        {
            try
            {
                var el = _wait.Until(d => d.FindElement(TotalBookingsKpi));
                return el.Text.Trim();
            }
            catch
            {
                var firstVal = _driver.FindElements(KpiCardValues).FirstOrDefault();
                return firstVal?.Text.Trim() ?? string.Empty;
            }
        }

        public string GetTotalRevenue()
        {
            try { return _wait.Until(d => d.FindElement(TotalRevenueKpi)).Text.Trim(); }
            catch { return string.Empty; }
        }
    }
}