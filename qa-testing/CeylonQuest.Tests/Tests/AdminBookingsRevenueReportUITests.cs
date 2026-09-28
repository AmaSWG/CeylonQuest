using System;
using CeylonQuest.Tests.Configuration;
using CeylonQuest.Tests.Pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace CeylonQuest.Tests.Tests
{
    public class AdminBookingsRevenueReportUITests : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly LoginPage _loginPage;
        private readonly AdminBookingsRevenueReportPage _adminReportPage;

        public AdminBookingsRevenueReportUITests()
        {
            var options = new ChromeOptions();
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--start-maximized");
            options.AddArgument("--disable-notifications");

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(TestConfiguration.Settings.ImplicitWaitSeconds);

            _loginPage = new LoginPage(_driver);
            _adminReportPage = new AdminBookingsRevenueReportPage(_driver);
        }

        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }

        private void LoginAsAdmin()
        {
            _driver.Navigate().GoToUrl($"{TestConfiguration.Settings.BaseUrl}/login");
            _loginPage.Login("adminceylonquest@gmail.com", "AdminPassword123!");
            _adminReportPage.NavigateToAdminReport();
        }

        [Fact(DisplayName = "CQ-ADM-RP-01: Admin can navigate to Bookings & Revenue Report and view system-wide KPI metrics")]
        [Trait("Story", "9.3")]
        [Trait("TestCase", "CQ-ADM-RP-01")]
        public void CQ_ADM_RP_01_AdminNavigatesToReport_Success()
        {
            LoginAsAdmin();
            Assert.True(_adminReportPage.IsLoaded(), "Admin Bookings & Revenue Report failed to load.");

            var bookings = _adminReportPage.GetTotalBookings();
            var revenue = _adminReportPage.GetTotalRevenue();

            Assert.False(string.IsNullOrWhiteSpace(bookings), "Total Bookings KPI value should not be empty.");
            Assert.Contains("LKR", revenue);
        }
    }
}