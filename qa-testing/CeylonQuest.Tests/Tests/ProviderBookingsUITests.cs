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
    public class ProviderBookingsUITests : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly LoginPage _loginPage;
        private readonly ProviderBookingsPage _providerBookingsPage;

        public ProviderBookingsUITests()
        {
            var options = new ChromeOptions();
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--start-maximized");
            options.AddArgument("--disable-notifications");

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(TestConfiguration.Settings.ImplicitWaitSeconds);

            _loginPage = new LoginPage(_driver);
            _providerBookingsPage = new ProviderBookingsPage(_driver);
        }

        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }

        private void LoginAsProvider()
        {
            // Use provider credentials from configuration
            _driver.Navigate().GoToUrl($"{TestConfiguration.Settings.BaseUrl}/login");
            _loginPage.Login(TestConfiguration.Settings.ProviderEmail, TestConfiguration.Settings.ProviderPassword);
            _providerBookingsPage.NavigateToBookingsTab();
        }

        [Fact(DisplayName = "CQ-PB-01: Provider can navigate to Booking Management dashboard")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQ-PB-01")]
        public void CQ_PB_01_ProviderNavigatesToBookings_Success()
        {
            LoginAsProvider();
            Assert.True(_providerBookingsPage.IsLoaded(), "Provider Booking Management page failed to load.");
        }

        [Fact(DisplayName = "CQ-PB-02: Provider bookings table displays customer and service details")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQ-PB-02")]
        public void CQ_PB_02_ProviderRowDetails_DisplaysCustomerAndService()
        {
            LoginAsProvider();

            var rowCount = _providerBookingsPage.GetRowCount();
            if (rowCount > 0)
            {
                var firstRow = _driver.FindElement(By.CssSelector("table.pd-bookings-table tbody tr"));

                var customerName = firstRow.FindElement(By.CssSelector(".pd-customer-info strong")).Text;
                var serviceName = firstRow.FindElement(By.CssSelector(".pd-service-name")).Text;
                var typeBadge = firstRow.FindElement(By.CssSelector(".pd-type-badge")).Text;
                var status = firstRow.FindElement(By.CssSelector(".pd-status-badge, [class*='status']")).Text;

                Assert.False(string.IsNullOrWhiteSpace(customerName), "Customer name must be displayed.");
                Assert.False(string.IsNullOrWhiteSpace(serviceName), "Service name must be displayed.");
                Assert.Contains(typeBadge, new[] { "Experience", "Restaurant", "Accommodation" });
                Assert.False(string.IsNullOrWhiteSpace(status), "Status must be displayed.");
            }
            else
            {
                Assert.True(_providerBookingsPage.IsEmptyStateDisplayed(), "Expected empty state when provider has 0 bookings.");
            }
        }

        [Fact(DisplayName = "CQ-PB-03: Provider can filter customer bookings by status")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQ-PB-03")]
        public void CQ_PB_03_ProviderFilter_ByStatus()
        {
            LoginAsProvider();

            if (_providerBookingsPage.GetRowCount() == 0) return;

            // Filter by Confirmed
            _providerBookingsPage.SelectFilter("Confirmed");
            Thread.Sleep(500);

            var confirmedRows = _driver.FindElements(By.CssSelector("table.pd-bookings-table tbody tr"));
            foreach (var row in confirmedRows)
            {
                var status = row.FindElement(By.CssSelector(".pd-status-badge, [class*='status']")).Text;
                Assert.Equal("Confirmed", status);
            }
        }

        [Fact(DisplayName = "CQ-PB-04: Provider can search bookings by query")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQ-PB-04")]
        public void CQ_PB_04_ProviderSearch_FiltersByQuery()
        {
            LoginAsProvider();

            if (_providerBookingsPage.GetRowCount() > 0)
            {
                var firstServiceName = _driver.FindElement(By.CssSelector(".pd-service-name")).Text;

                _providerBookingsPage.Search(firstServiceName);
                Thread.Sleep(500);

                var searchRows = _driver.FindElements(By.CssSelector("table.pd-bookings-table tbody tr"));
                Assert.True(searchRows.Count > 0, "Expected matching rows after searching by service name.");
            }
        }
    }
}