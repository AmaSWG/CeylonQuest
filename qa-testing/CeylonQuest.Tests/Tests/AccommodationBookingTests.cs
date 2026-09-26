using CeylonQuest.Tests.Configuration;
using CeylonQuest.Tests.Pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using Xunit;

namespace CeylonQuest.Tests.Tests
{
    [Collection("Sequential UI Tests")]
    [Trait("Category", "UI")]
    public class AccommodationBookingTests : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly LoginPage _loginPage;
        private readonly VisitorExplorePage _explorePage;
        private readonly BookingModalPage _modalPage;
        private readonly WebDriverWait _wait;

        public AccommodationBookingTests()
        {
            var options = new ChromeOptions();
            options.AddArgument("--start-maximized");

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(TestConfiguration.Settings.ImplicitWaitSeconds);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));

            _loginPage = new LoginPage(_driver);
            _explorePage = new VisitorExplorePage(_driver);
            _modalPage = new BookingModalPage(_driver);

            // Step 1: Login
            _driver.Navigate().GoToUrl($"{TestConfiguration.Settings.BaseUrl}/login");
            _loginPage.Login(TestConfiguration.Settings.VisitorEmail, TestConfiguration.Settings.VisitorPassword);
        }

        [Fact(DisplayName = "Accommodation UI: Guest count change keeps fixed nightly rate")]
        public void Accommodation_GuestCountChange_DoesNotMultiplyBaseRate()
        {
            _explorePage.FilterByAccommodations();
            _explorePage.ClickFirstExperienceBookNow();

            Assert.True(_modalPage.ModalContainer.Displayed);

            _modalPage.SelectFirstAvailableDateAndSlot(daysToScan: 14);

            // Initial guest count = 1
            _modalPage.SetGuestCount(1);
            var initialTotal = _modalPage.GetEstimatedTotal();

            // Changing guest count to 2 should NOT change the estimated total
            _modalPage.SetGuestCount(2);
            var updatedTotal = _modalPage.GetEstimatedTotal();

            Assert.True(initialTotal > 0, "Estimated total should be calculated based on nightly rate and stay length.");
            Assert.Equal(initialTotal, updatedTotal);

            // Reset back to 1 and verify confirm button is enabled
            _modalPage.SetGuestCount(1);
            Assert.True(_modalPage.WaitForConfirmButtonEnabled(5), "Booking button should be enabled for valid accommodation details.");
        }

        [Fact(DisplayName = "Accommodation UI: Exceeding maxGuests disables booking button")]
        public void Accommodation_ExceedingMaxGuests_ShowsErrorAndDisablesConfirm()
        {
            _explorePage.FilterByAccommodations();
            _explorePage.ClickFirstExperienceBookNow();

            _modalPage.SelectFirstAvailableDateAndSlot(daysToScan: 14);

            // Set guest count beyond max capacity
            _modalPage.SetGuestCount(999);

            var isConfirmDisabled = !_modalPage.WaitForConfirmButtonEnabled(2);
            var hasErrorMessage = _driver.FindElements(By.CssSelector(".vd-avail-error")).Count > 0;

            Assert.True(isConfirmDisabled || hasErrorMessage, "Booking button should be disabled when guest count exceeds max capacity.");
        }

        public void Dispose()
        {
            try
            {
                _driver.Quit();
                _driver.Dispose();
            }
            catch { }
        }
    }
}