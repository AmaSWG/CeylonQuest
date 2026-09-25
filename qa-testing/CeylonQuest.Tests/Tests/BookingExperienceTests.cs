using CeylonQuest.Tests.Configuration;
using CeylonQuest.Tests.Pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using Xunit;

namespace CeylonQuest.Tests.Tests
{
    public class BookingExperienceTests : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly LoginPage _loginPage;
        private readonly VisitorExplorePage _explorePage;
        private readonly BookingModalPage _bookingModal;

        public BookingExperienceTests()
        {
            var options = new ChromeOptions();
            options.AddArgument("--start-maximized");

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(TestConfiguration.Settings.ImplicitWaitSeconds);

            _loginPage = new LoginPage(_driver);
            _explorePage = new VisitorExplorePage(_driver);
            _bookingModal = new BookingModalPage(_driver);

            // Step 1: Login and wait until successfully redirected
            _driver.Navigate().GoToUrl($"{TestConfiguration.Settings.BaseUrl}/login");
            _loginPage.Login(TestConfiguration.Settings.VisitorEmail, TestConfiguration.Settings.VisitorPassword);
        }

        public bool IsSuccessToastDisplayed()
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
                var toast = wait.Until(d =>
                {
                    try
                    {
                        var toasts = d.FindElements(By.CssSelector(".vd-toast"));
                        return toasts.FirstOrDefault(t => t.Displayed);
                    }
                    catch (StaleElementReferenceException) { return null; }
                });

                return toast != null && toast.Displayed;
            }
            catch (WebDriverTimeoutException)
            {
                // Diagnostic check if a backend submit error appeared inside the modal instead
                var errorBanner = _driver.FindElements(By.CssSelector(".vd-avail-error")).FirstOrDefault();
                if (errorBanner != null && errorBanner.Displayed)
                {
                    Console.WriteLine($"[DIAG:toast] Modal displayed error banner instead of toast: '{errorBanner.Text}'");
                }
                return false;
            }
        }

        [Fact(DisplayName = "Scenario 1: Select Booking Details - Allows user to select date, time and participants")]
        public void Scenario1_ShouldAllowSelectingBookingDetails()
        {
            _explorePage.FilterByExperiences();
            _explorePage.ClickFirstExperienceBookNow();

            Assert.True(_bookingModal.ModalContainer.Displayed);

            var (date, slot) = _bookingModal.SelectFirstAvailableDateAndSlot();
            _bookingModal.SetGuestCount(2);

            Assert.Equal(date, _bookingModal.DateInput.GetAttribute("value"));
            Assert.Equal(2, _bookingModal.GetCurrentGuestCount());
            Assert.True(_bookingModal.WaitForConfirmButtonEnabled(),"Confirm Booking button should be enabled for valid details.");
        }

        [Fact(DisplayName = "Scenario 2: Validate Booking Details - Prevent booking when required fields are missing")]
        public void Scenario2_ShouldValidateBookingDetails()
        {
            _explorePage.FilterByExperiences();
            _explorePage.ClickFirstExperienceBookNow();

            _bookingModal.SetGuestCount(0);
            Assert.Equal(1, _bookingModal.GetCurrentGuestCount());

            _bookingModal.SetGuestCount(-5);
            Assert.Equal(1, _bookingModal.GetCurrentGuestCount());
        }

        [Fact(DisplayName = "Scenario 3: Validate Experience Availability - Prevent overbooking capacity")]
        public void Scenario3_ShouldPreventOverbooking()
        {
            _explorePage.FilterByExperiences();
            _explorePage.ClickFirstExperienceBookNow();
            _bookingModal.SelectFirstAvailableDateAndSlot();

            _bookingModal.SetGuestCount(999);

            var rawValue = _bookingModal.GuestsInput.GetAttribute("value");
            int.TryParse(rawValue, out var clampedVal);

            Assert.True(clampedVal < 999 || !_bookingModal.ConfirmBookingButton.Enabled,
                "Overbooking must be prevented by clamping input or disabling confirm button.");
        }

        [Fact(DisplayName = "Scenario 4: Calculate Booking Amount - Dynamic calculation based on price * count")]
        public void Scenario4_ShouldCalculateTotalAmountCorrectly()
        {
            _explorePage.FilterByExperiences();
            _explorePage.ClickFirstExperienceBookNow();

            var basePrice = _bookingModal.GetBasePrice();
            _bookingModal.SelectFirstAvailableDateAndSlot();

            _bookingModal.SetGuestCount(1);
            Assert.Equal(basePrice * 1, _bookingModal.GetEstimatedTotal());

            _bookingModal.SetGuestCount(3);
            Assert.Equal(basePrice * 3, _bookingModal.GetEstimatedTotal());
        }

        [Fact(DisplayName = "Scenario 5: Create Booking - Creates booking with Pending Payment status")]
        public void Scenario5_ShouldCreateBookingWithPendingPaymentStatus()
        {
            _explorePage.FilterByExperiences();
            _explorePage.ClickFirstExperienceBookNow();

            _bookingModal.SelectFirstAvailableDateAndSlot();
            _bookingModal.SetGuestCount(1);
            _bookingModal.ClickConfirmBooking();

            Assert.True(_bookingModal.IsSuccessToastDisplayed(),
                "Expected success confirmation toast with 'Pending Payment' status.");
        }

        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
    }
}