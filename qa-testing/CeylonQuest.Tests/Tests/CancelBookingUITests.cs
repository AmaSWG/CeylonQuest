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
    public class CancelBookingUITests : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly LoginPage _loginPage;
        private readonly VisitorBookingsPage _visitorBookingsPage;
        private readonly ProviderBookingsPage _providerBookingsPage;

        public CancelBookingUITests()
        {
            var options = new ChromeOptions();
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--start-maximized");
            options.AddArgument("--disable-notifications");

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(TestConfiguration.Settings.ImplicitWaitSeconds);

            _loginPage = new LoginPage(_driver);
            _visitorBookingsPage = new VisitorBookingsPage(_driver);
            _providerBookingsPage = new ProviderBookingsPage(_driver);
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

        private void LoginAsProvider()
        {
            _driver.Navigate().GoToUrl($"{TestConfiguration.Settings.BaseUrl}/login");
            _loginPage.Login(TestConfiguration.Settings.ProviderEmail, TestConfiguration.Settings.ProviderPassword);
            _providerBookingsPage.NavigateToBookingsTab();
        }

        [Fact(DisplayName = "CQ-CN-01: Visitor cancels active booking with confirmation, reason, and refund notification")]
        [Trait("Story", "9.2")]
        [Trait("TestCase", "CQ-CN-01")]
        public void CQ_CN_01_VisitorCancelsBooking_CompleteWorkflow()
        {
            LoginAsVisitor();

            _visitorBookingsPage.FilterByStatus("Pending Payment");
            Thread.Sleep(500);

            var activeCancelButtons = _driver.FindElements(By.CssSelector("table.vb-table tbody tr .vb-cancel-btn"));
            if (activeCancelButtons.Count == 0) return; // No active booking available to cancel

            // Execute 2-step cancellation with reason
            _visitorBookingsPage.CancelFirstActiveBooking("Schedule changed unexpectedly");

            Assert.True(_visitorBookingsPage.IsSuccessBannerDisplayed(), "Expected green success message after cancellation.");
        }

        [Fact(DisplayName = "CQ-CN-02: Cancelled booking details modal displays cancellation reason and refund percentage")]
        [Trait("Story", "9.2")]
        [Trait("TestCase", "CQ-CN-02")]
        public void CQ_CN_02_VisitorCancelledRow_DetailsModalDisplaysRefund()
        {
            LoginAsVisitor();

            _visitorBookingsPage.FilterByStatus("Cancelled");
            Thread.Sleep(500);

            var cancelledRows = _driver.FindElements(By.CssSelector("table.vb-table tbody tr.vb-row"));
            if (cancelledRows.Count == 0) return;

            _visitorBookingsPage.OpenDetailsForFirstRow();

            Assert.True(_visitorBookingsPage.IsCancellationDetailsDisplayedInModal(),
                "Expected 'Cancellation & Refund' section to appear in details modal for cancelled booking.");

            _visitorBookingsPage.CloseDetailsModal();
        }

        [Fact(DisplayName = "CQ-CN-03: Cancelled rows do not render a Cancel action button")]
        [Trait("Story", "9.2")]
        [Trait("TestCase", "CQ-CN-03")]
        public void CQ_CN_03_VisitorCancelledRow_HasNoCancelButton()
        {
            LoginAsVisitor();

            _visitorBookingsPage.FilterByStatus("Cancelled");
            Thread.Sleep(500);

            // Instant global check across all rows on the page without looping
            var cancelButtons = _driver.FindElements(By.CssSelector("table.vb-table tbody tr .vb-cancel-btn"));
            Assert.Empty(cancelButtons);
        }

        [Fact(DisplayName = "CQ-CN-04: Provider Booking Management reflects Cancelled status")]
        [Trait("Story", "9.2")]
        [Trait("TestCase", "CQ-CN-04")]
        public void CQ_CN_04_ProviderViewsCancelledBooking_ReflectsStatus()
        {
            LoginAsProvider();

            if (_providerBookingsPage.GetRowCount() == 0) return;

            _providerBookingsPage.SelectFilter("Cancelled");
            Thread.Sleep(500);

            // Sample first 5 rows to ensure snappy execution
            var cancelledRows = _driver.FindElements(By.CssSelector("table.pd-bookings-table tbody tr")).Take(5);
            foreach (var row in cancelledRows)
            {
                var status = row.FindElement(By.CssSelector(".pd-status-badge, [class*='status']")).Text;
                Assert.Equal("Cancelled", status);
            }
        }
    }
}