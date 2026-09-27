using System;
using System.Threading;
using CeylonQuest.Tests.Configuration;
using CeylonQuest.Tests.Pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace CeylonQuest.Tests.Tests
{
    public class ViewBookingsUITests : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly LoginPage _loginPage;
        private readonly VisitorBookingsPage _visitorBookingsPage;

        public ViewBookingsUITests()
        {
            var options = new ChromeOptions();
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--start-maximized");
            options.AddArgument("--disable-notifications");

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(TestConfiguration.Settings.ImplicitWaitSeconds);

            _loginPage = new LoginPage(_driver);
            _visitorBookingsPage = new VisitorBookingsPage(_driver);
        }

        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }

        [Fact(DisplayName = "CQVB-01: Visitor can navigate to My Bookings and view list")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQVB-01")]
        public void CQVB_01_VisitorNavigatesToBookings_Success()
        {
            _driver.Navigate().GoToUrl($"{TestConfiguration.Settings.BaseUrl}/login");
            _loginPage.Login(TestConfiguration.Settings.VisitorEmail, TestConfiguration.Settings.VisitorPassword);

            _visitorBookingsPage.NavigateToBookingsTab();
            Assert.True(_visitorBookingsPage.IsLoaded(), "Visitor Bookings page header did not load.");
        }

        [Fact(DisplayName = "CQVB-02: Visitor bookings table renders columns and item details")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQVB-02")]
        public void CQVB_02_VisitorBookings_DisplaysDetailsAndColumns()
        {
            _driver.Navigate().GoToUrl($"{TestConfiguration.Settings.BaseUrl}/login");
            _loginPage.Login(TestConfiguration.Settings.VisitorEmail, TestConfiguration.Settings.VisitorPassword);

            _visitorBookingsPage.NavigateToBookingsTab();

            var count = _visitorBookingsPage.GetBookingRowCount();
            if (count > 0)
            {
                var tableHeader = _driver.FindElement(By.CssSelector("table.vb-table thead tr")).Text;
                Assert.Contains("TYPE", tableHeader);
                Assert.Contains("SERVICE", tableHeader);
                Assert.Contains("DATE", tableHeader);
                Assert.Contains("PEOPLE", tableHeader);
                Assert.Contains("AMOUNT", tableHeader);
                Assert.Contains("STATUS", tableHeader);
            }
            else
            {
                Assert.True(_visitorBookingsPage.IsEmptyStateDisplayed());
            }
        }

        [Fact(DisplayName = "CQVB-03: Visitor can filter bookings by status and search by query")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQVB-03")]
        public void CQVB_03_VisitorBookings_FilterAndSearch()
        {
            _driver.Navigate().GoToUrl($"{TestConfiguration.Settings.BaseUrl}/login");
            _loginPage.Login(TestConfiguration.Settings.VisitorEmail, TestConfiguration.Settings.VisitorPassword);

            _visitorBookingsPage.NavigateToBookingsTab();

            if (_visitorBookingsPage.GetBookingRowCount() > 0)
            {
                _visitorBookingsPage.FilterByStatus("Active");
                Thread.Sleep(300);

                _visitorBookingsPage.FilterByStatus("Cancelled");
                Thread.Sleep(300);

                _visitorBookingsPage.FilterByStatus("All");
                Thread.Sleep(300);

                _visitorBookingsPage.SearchBookings("NonExistentBookingQuery12345");
                Thread.Sleep(300);

                var noResults = _driver.FindElement(By.CssSelector(".vb-no-results"));
                Assert.True(noResults.Displayed, "Expected 'No matching bookings' when search term doesn't match.");
            }
        }

        [Fact(DisplayName = "CQVB-04: Visitor can open and close the booking details modal")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQVB-04")]
        public void CQVB_04_VisitorBookings_OpenDetailsModal()
        {
            _driver.Navigate().GoToUrl($"{TestConfiguration.Settings.BaseUrl}/login");
            _loginPage.Login(TestConfiguration.Settings.VisitorEmail, TestConfiguration.Settings.VisitorPassword);

            _visitorBookingsPage.NavigateToBookingsTab();

            if (_visitorBookingsPage.GetBookingRowCount() > 0)
            {
                _visitorBookingsPage.OpenDetailsForFirstRow();
                var title = _visitorBookingsPage.GetModalTitle();
                Assert.False(string.IsNullOrWhiteSpace(title), "Modal title should not be empty.");

                _visitorBookingsPage.CloseDetailsModal();
            }
        }
    }
}