using System;
using System.Linq;
using System.Threading;
using CeylonQuest.Tests.Configuration;
using CeylonQuest.Tests.Pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace CeylonQuest.Tests.Tests
{
    public class BookingsRevenueReportUITests : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly LoginPage _loginPage;
        private readonly ProviderBookingsRevenueReportPage _reportPage;

        public BookingsRevenueReportUITests()
        {
            var options = new ChromeOptions();
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--start-maximized");
            options.AddArgument("--disable-notifications");

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(TestConfiguration.Settings.ImplicitWaitSeconds);

            _loginPage = new LoginPage(_driver);
            _reportPage = new ProviderBookingsRevenueReportPage(_driver);
        }

        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }

        private void LoginAsProvider()
        {
            _driver.Navigate().GoToUrl($"{TestConfiguration.Settings.BaseUrl}/login");
            _loginPage.Login(TestConfiguration.Settings.ProviderEmail, TestConfiguration.Settings.ProviderPassword);
            _reportPage.NavigateToReport();
        }

        [Fact(DisplayName = "CQ-RP-01: Provider can navigate to Bookings & Revenue Report and view summary KPI cards")]
        [Trait("Story", "9.3")]
        [Trait("TestCase", "CQ-RP-01")]
        public void CQ_RP_01_ProviderNavigatesToReport_RendersKPIsAndTable()
        {
            LoginAsProvider();
            Assert.True(_reportPage.IsLoaded(), "Bookings & Revenue Report page failed to load.");

            var kpis = _driver.FindElements(By.CssSelector(".pbr-summary h2")).Select(e => e.Text).ToList();
            Assert.Contains("Total Bookings", kpis);
            Assert.Contains("Total Revenue", kpis);
        }

        [Fact(DisplayName = "CQ-RP-02: Provider can filter report data by Booking Type")]
        [Trait("Story", "9.3")]
        [Trait("TestCase", "CQ-RP-02")]
        public void CQ_RP_02_ProviderFiltersReport_ByBookingType()
        {
            LoginAsProvider();

            _reportPage.FilterByType("Experience");
            Thread.Sleep(600);

            var rows = _driver.FindElements(By.CssSelector("table.pd-table tbody tr"));
            foreach (var row in rows)
            {
                var typeCol = row.FindElement(By.CssSelector("td:first-child")).Text;
                Assert.Equal("Experience", typeCol);
            }
        }

        [Fact(DisplayName = "CQ-RP-03: Provider can filter report by Cancelled status")]
        [Trait("Story", "9.3")]
        [Trait("TestCase", "CQ-RP-03")]
        public void CQ_RP_03_ProviderFiltersReport_ByStatus()
        {
            LoginAsProvider();

            _reportPage.FilterByStatus("Cancelled");
            Thread.Sleep(600);

            var rows = _driver.FindElements(By.CssSelector("table.pd-table tbody tr"));
            foreach (var row in rows)
            {
                var statusCol = row.FindElement(By.CssSelector(".pbr-status")).Text;
                Assert.Equal("Cancelled", statusCol);
            }
        }

        [Fact(DisplayName = "CQ-RP-04: Report with no matching criteria displays clear no-data state")]
        [Trait("Story", "9.3")]
        [Trait("TestCase", "CQ-RP-04")]
        public void CQ_RP_04_ProviderReport_FutureDateRange_DisplaysNoData()
        {
            LoginAsProvider();

            // Filter for year 2099 where no bookings exist
            _reportPage.FilterByDates("2099-01-01", "2099-12-31");
            Thread.Sleep(600);

            var msg = _reportPage.GetNoDataMessage();
            Assert.Contains("No data available for the selected criteria", msg);
        }

        [Fact(DisplayName = "CQ-RP-05: Start date after end date displays client validation error")]
        [Trait("Story", "9.3")]
        [Trait("TestCase", "CQ-RP-05")]
        public void CQ_RP_05_ProviderReport_InvalidDateRange_ShowsValidationError()
        {
            LoginAsProvider();

            _reportPage.FilterByDates("2026-12-31", "2026-01-01");
            var error = _reportPage.GetValidationError();
            Assert.Contains("cannot be earlier", error, StringComparison.OrdinalIgnoreCase);
        }
    }
}