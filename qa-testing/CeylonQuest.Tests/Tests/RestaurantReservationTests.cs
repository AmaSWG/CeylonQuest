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
    public class RestaurantReservationTests : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly LoginPage _loginPage;
        private readonly VisitorExplorePage _explorePage;
        private readonly BookingModalPage _modalPage;
        private readonly WebDriverWait _wait;

        public RestaurantReservationTests()
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

        [Fact(DisplayName = "Restaurant UI: Select details and calculate price per person")]
        public void Restaurant_SelectDetails_CalculatesPerPersonPrice()
        {
            _explorePage.FilterByRestaurants();
            _explorePage.ClickFirstExperienceBookNow();

            Assert.True(_modalPage.ModalContainer.Displayed);

            _modalPage.SelectFirstAvailableDateAndSlot(daysToScan: 14);
            _modalPage.SetGuestCount(3);

            var basePrice = _modalPage.GetBasePrice();
            var total = _modalPage.GetEstimatedTotal();

            // Total = Price per Person × Party Size (3)
            Assert.Equal(basePrice * 3, total);
            Assert.True(_modalPage.WaitForConfirmButtonEnabled(5), "Reserve Table button should be enabled for valid party size.");
        }

        [Fact(DisplayName = "Restaurant UI: Exceeding seating capacity disables reservation button")]
        public void Restaurant_ExceedingCapacity_DisablesButton()
        {
            _explorePage.FilterByRestaurants();
            _explorePage.ClickFirstExperienceBookNow();

            _modalPage.SelectFirstAvailableDateAndSlot(daysToScan: 14);
            _modalPage.SetGuestCount(999);

            var isButtonDisabled = !_modalPage.WaitForConfirmButtonEnabled(2);
            var hasErrorMessage = _driver.FindElements(By.CssSelector(".vd-avail-error")).Count > 0;

            Assert.True(isButtonDisabled || hasErrorMessage, "UI must prevent overcapacity reservation.");
        }

        [Fact(DisplayName = "Restaurant UI: Reserve table confirms successfully")]
        public void Restaurant_ValidReservation_CreatesConfirmedReservation()
        {
            _explorePage.FilterByRestaurants();
            _explorePage.ClickFirstExperienceBookNow();

            _modalPage.SelectFirstAvailableDateAndSlot(daysToScan: 14);
            _modalPage.SetGuestCount(2);

            _modalPage.ClickConfirmBooking();

            var toastFound = _wait.Until(d =>
            {
                var toasts = d.FindElements(By.XPath(
                    "//*[contains(., 'Table reserved successfully') or contains(., 'Confirmed') or contains(., 'Reservation confirmed')]"));
                return toasts.Count > 0 && toasts[0].Displayed;
            });

            Assert.True(toastFound, "Expected confirmation toast with Confirmed status.");
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