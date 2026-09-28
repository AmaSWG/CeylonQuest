using System;
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
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
        }

        private By ReportsNavBtn => By.Id("nav-reports");
        private By BookingsRevenueSubTabBtn => By.XPath("//button[contains(@class, 'cq-report-subtab-btn') and contains(text(), 'Bookings & Revenue')]");
        private By PageHeaderTitle => By.CssSelector(".cq-report-header-title");
        private By TotalBookingsKpi => By.XPath("//span[text()='TOTAL BOOKINGS' or text()='Total Bookings']/following-sibling::span");
        private By TotalRevenueKpi => By.XPath("//span[text()='TOTAL REVENUE' or text()='Total Revenue']/following-sibling::span");

        public void NavigateToAdminReport()
        {
            var nav = _wait.Until(d => d.FindElement(ReportsNavBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", nav);

            var subTab = _wait.Until(d => d.FindElement(BookingsRevenueSubTabBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", subTab);

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

        public string GetTotalBookings()
        {
            try { return _wait.Until(d => d.FindElement(TotalBookingsKpi)).Text; }
            catch { return string.Empty; }
        }

        public string GetTotalRevenue()
        {
            try { return _wait.Until(d => d.FindElement(TotalRevenueKpi)).Text; }
            catch { return string.Empty; }
        }
    }
}