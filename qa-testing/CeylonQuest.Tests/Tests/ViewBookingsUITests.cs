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

        private void LoginAsVisitor()
        {
            _driver.Navigate().GoToUrl($"{TestConfiguration.Settings.BaseUrl}/login");
            _loginPage.Login(TestConfiguration.Settings.VisitorEmail, TestConfiguration.Settings.VisitorPassword);
            _visitorBookingsPage.NavigateToBookingsTab();
        }

        [Fact(DisplayName = "CQ-VB-01: Visitor can navigate to My Bookings and view page header")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQ-VB-01")]
        public void CQ_VB_01_VisitorNavigatesToBookings_Success()
        {
            LoginAsVisitor();
            Assert.True(_visitorBookingsPage.IsLoaded(), "Visitor Bookings page header did not load.");
        }

        [Fact(DisplayName = "CQ-VB-02: Visitor row details actually contain populated data")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQ-VB-02")]
        public void CQ_VB_02_VisitorRowDetails_ArePopulated()
        {
            LoginAsVisitor();

            var rows = _driver.FindElements(By.CssSelector("table.vb-table tbody tr.vb-row"));
            if (rows.Count > 0)
            {
                var firstRow = rows.First();
                var typeCell = firstRow.FindElement(By.CssSelector(".vb-type")).Text;
                var serviceCell = firstRow.FindElement(By.CssSelector(".vb-service-cell strong")).Text;
                var dateCell = firstRow.FindElement(By.CssSelector("td:nth-child(3)")).Text;
                var peopleCell = firstRow.FindElement(By.CssSelector(".vb-people")).Text;
                var amountCell = firstRow.FindElement(By.CssSelector(".vb-amount")).Text;
                var statusCell = firstRow.FindElement(By.CssSelector(".vb-status")).Text;

                Assert.False(string.IsNullOrWhiteSpace(typeCell), "Booking Type must not be empty.");
                Assert.False(string.IsNullOrWhiteSpace(serviceCell), "Service Name must not be empty.");
                Assert.False(string.IsNullOrWhiteSpace(dateCell), "Date must not be empty.");
                Assert.False(string.IsNullOrWhiteSpace(peopleCell), "People/Party count must not be empty.");
                Assert.Contains("LKR", amountCell);
                Assert.False(string.IsNullOrWhiteSpace(statusCell), "Status must not be empty.");
            }
            else
            {
                Assert.True(_visitorBookingsPage.IsEmptyStateDisplayed(), "Expected empty state when 0 rows are present.");
            }
        }

        [Fact(DisplayName = "CQ-VB-03: Type badges display valid booking types")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQ-VB-03")]
        public void CQ_VB_03_VisitorTypeBadges_AreValid()
        {
            LoginAsVisitor();

            var typeBadges = _driver.FindElements(By.CssSelector("table.vb-table tbody tr .vb-type"));
            foreach (var badge in typeBadges)
            {
                var text = badge.Text.Trim();
                Assert.Contains(text, new[] { "EXPERIENCE", "RESTAURANT", "ACCOMMODATION" });
            }
        }

        [Fact(DisplayName = "CQ-VB-04: Status filter correctly isolates Cancelled vs Active bookings")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQ-VB-04")]
        public void CQ_VB_04_VisitorStatusFilter_FiltersRowsCorrectly()
        {
            LoginAsVisitor();

            if (_visitorBookingsPage.GetBookingRowCount() == 0) return;

            // 1. Filter by Cancelled
            _visitorBookingsPage.FilterByStatus("Cancelled");
            Thread.Sleep(500);

            var cancelledRows = _driver.FindElements(By.CssSelector("table.vb-table tbody tr.vb-row"));
            foreach (var row in cancelledRows)
            {
                var status = row.FindElement(By.CssSelector(".vb-status")).Text.ToLower();
                Assert.True(status.Contains("cancelled") || status.Contains("canceled"),
                    $"Expected row to be Cancelled, but was '{status}'");
            }

            // 2. Filter by Active
            _visitorBookingsPage.FilterByStatus("Active");
            Thread.Sleep(500);

            var activeRows = _driver.FindElements(By.CssSelector("table.vb-table tbody tr.vb-row"));
            foreach (var row in activeRows)
            {
                var status = row.FindElement(By.CssSelector(".vb-status")).Text.ToLower();
                Assert.False(status.Contains("cancelled") || status.Contains("canceled"),
                    $"Expected row to be Active, but was Cancelled.");
            }
        }

        [Fact(DisplayName = "CQ-VB-05: Non-matching search displays empty/no-results state")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQ-VB-05")]
        public void CQ_VB_05_VisitorSearch_NoResults_DisplaysEmptyState()
        {
            LoginAsVisitor();

            _visitorBookingsPage.SearchBookings("___UNMATCHABLE_SEARCH_KEYWORD_XYZ___");
            Thread.Sleep(500);

            var noResults = _driver.FindElement(By.CssSelector(".vb-no-results"));
            Assert.True(noResults.Displayed, "Expected '.vb-no-results' container to be displayed.");
            Assert.Contains("No matching bookings", noResults.Text);
        }

        [Fact(DisplayName = "CQ-VB-06: View Details modal opens with matching service information and closes")]
        [Trait("Story", "9.1")]
        [Trait("TestCase", "CQ-VB-06")]
        public void CQ_VB_06_VisitorDetailsModal_DisplaysMatchingService()
        {
            LoginAsVisitor();

            if (_visitorBookingsPage.GetBookingRowCount() > 0)
            {
                var expectedServiceName = _driver.FindElement(By.CssSelector("table.vb-table tbody tr .vb-service-cell strong")).Text;

                _visitorBookingsPage.OpenDetailsForFirstRow();
                var modalTitle = _visitorBookingsPage.GetModalTitle();

                Assert.Equal(expectedServiceName, modalTitle);

                _visitorBookingsPage.CloseDetailsModal();
            }
        }
    }
}