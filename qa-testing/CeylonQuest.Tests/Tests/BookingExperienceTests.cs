using CeylonQuest.Tests.Configuration;
using CeylonQuest.Tests.Pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
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
            // Uncomment for headless CI/CD execution:
            // options.AddArgument("--headless");

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(TestConfiguration.Settings.ImplicitWaitSeconds);

            _loginPage = new LoginPage(_driver);
            _explorePage = new VisitorExplorePage(_driver);
            _bookingModal = new BookingModalPage(_driver);

            // Step 1: Login as Visitor and navigate to Explore
            _driver.Navigate().GoToUrl($"{TestConfiguration.Settings.BaseUrl}/login");
            _loginPage.Login(TestConfiguration.Settings.VisitorEmail, TestConfiguration.Settings.VisitorPassword);

            // Navigate to visitor dashboard explore
            _driver.Navigate().GoToUrl($"{TestConfiguration.Settings.BaseUrl}/catalog/search");
        }

        [Fact(DisplayName = "Scenario 1: Select Booking Details - Allows user to select date, time and participants")]
        public void Scenario1_ShouldAllowSelectingBookingDetails()
        {
            _explorePage.FilterByExperiences();
            _explorePage.ClickFirstExperienceBookNow();

            Assert.True(_bookingModal.ModalContainer.Displayed);

            // Set tomorrow's date
            /*var tomorrow = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            _bookingModal.SetBookingDate(tomorrow);*/
            var bookableDate = _bookingModal.GetMinimumBookableDate();
            //_bookingModal.SetBookingDate(bookableDate);
            _bookingModal.SelectFirstAvailableDateAndSlot();
            _bookingModal.SetGuestCount(2);

            Assert.True(_bookingModal.ConfirmBookingButton.Enabled);
        }

        [Fact(DisplayName = "Scenario 2: Validate Booking Details - Prevent booking when required fields are missing")]
        public void Scenario2_ShouldValidateBookingDetails()
        {
            _explorePage.FilterByExperiences();
            _explorePage.ClickFirstExperienceBookNow();

            // Set invalid guest count
            _bookingModal.SetGuestCount(0);

            // Guest count input automatically resets to min 1 or button disables
            Assert.True(_bookingModal.GuestsInput.GetAttribute("value") == "1" || !_bookingModal.ConfirmBookingButton.Enabled);
        }

        /*[Fact(DisplayName = "Scenario 3: Validate Experience Availability - Prevent overbooking capacity")]
        public void Scenario3_ShouldPreventOverbooking()
        {
            _explorePage.FilterByExperiences();
            _explorePage.ClickFirstExperienceBookNow();

            _bookingModal.SelectFirstAvailableDateAndSlot();

            // Attempt to enter 999 guests (exceeding max capacity)
            _bookingModal.SetGuestCount(999);

            // The form input clamps to maximum remaining capacity
            var clampedVal = int.Parse(_bookingModal.GuestsInput.GetAttribute("value"));
            Assert.True(clampedVal < 999, "Guest count should clamp to maximum capacity");
        }*/
        [Fact(DisplayName = "Scenario 3: Validate Experience Availability - Prevent overbooking capacity")]
        public void Scenario3_ShouldPreventOverbooking()
        {
            _explorePage.FilterByExperiences();
            _explorePage.ClickFirstExperienceBookNow();

            _bookingModal.SelectFirstAvailableDateAndSlot();

            // Attempt to enter 999 guests (exceeding max capacity)
            _bookingModal.SetGuestCount(999);

            // Give React a moment to process and re-render
            System.Threading.Thread.Sleep(1000);

            var rawValue = _bookingModal.GuestsInput.GetAttribute("value");
            int.TryParse(rawValue, out var currentVal);

            // Overbooking must be prevented: either by clamping the number OR by disabling submit
            Assert.True(
                currentVal < 999 || !_bookingModal.ConfirmBookingButton.Enabled,
                $"Overbooking must be prevented. Current value: {rawValue}, " +
                $"Button enabled: {_bookingModal.ConfirmBookingButton.Enabled}"
            );
        }

        [Fact(DisplayName = "Scenario 4: Calculate Booking Amount - Dynamic calculation based on price * count")]
        public void Scenario4_ShouldCalculateTotalAmountCorrectly()
        {
            _explorePage.FilterByExperiences();
            _explorePage.ClickFirstExperienceBookNow();

            var basePrice = _bookingModal.GetBasePrice();
            int guests = 3;

            //
            Console.WriteLine($"[DIAG:total] base price = {basePrice}");
            Console.WriteLine($"[DIAG:total] guests before set = '{_bookingModal.GuestsInput.GetAttribute("value")}'");
            //

            _bookingModal.SelectFirstAvailableDateAndSlot();
            _bookingModal.SetGuestCount(guests);

            //
            Console.WriteLine($"[DIAG:total] guests after set = '{_bookingModal.GuestsInput.GetAttribute("value")}'");
            Console.WriteLine($"[DIAG:total] total text = '{_bookingModal.EstimatedTotalElement.Text}'");
            Console.WriteLine($"[DIAG:total] total value attr = '{_bookingModal.EstimatedTotalElement.GetAttribute("value")}'");
            //

            var estimatedTotal = _bookingModal.GetEstimatedTotal();
            var expectedTotal = basePrice * guests;

            Console.WriteLine($"[DIAG:total] expected = {expectedTotal}, actual = {estimatedTotal}");

            Assert.Equal(expectedTotal, estimatedTotal);
        }

        /*[Fact(DisplayName = "Scenario 5: Create Booking - Creates booking with Pending Payment status")]
        public void Scenario5_ShouldCreateBookingWithPendingPaymentStatus()
        {
            _explorePage.FilterByExperiences();
            _explorePage.ClickFirstExperienceBookNow();

            var futureDate = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd");
            //_bookingModal.SetBookingDate(futureDate);
            _bookingModal.SelectFirstAvailableDateAndSlot();
            _bookingModal.SetGuestCount(1);

            _bookingModal.ClickConfirmBooking();

            // Verify success toast with "Pending Payment" status
            Assert.True(_bookingModal.IsSuccessToastDisplayed(), "Expected success confirmation toast with 'Pending Payment' status.");
        }*/

        /*[Fact(DisplayName = "Scenario 3: Validate Experience Availability - Prevent overbooking capacity")]
        public void Scenario3_ShouldPreventOverbooking()
        {
            _explorePage.FilterByExperiences();
            _explorePage.ClickFirstExperienceBookNow();

            _bookingModal.SelectFirstAvailableDateAndSlot();

            // Attempt to enter 999 guests (exceeding max capacity)
            _bookingModal.SetGuestCount(999);

            var rawValue = _bookingModal.GuestsInput.GetAttribute("value");
            int.TryParse(rawValue, out var currentVal);

            // Overbooking must be blocked: either by clamping the number OR by disabling submit
            Assert.True(currentVal < 999 || !_bookingModal.ConfirmBookingButton.Enabled,
                "Overbooking must be prevented by clamping input or disabling confirm button.");
        }*/

        [Fact(DisplayName = "Scenario 5: Create Booking - Creates booking with Pending Payment status")]
        public void Scenario5_ShouldCreateBookingWithPendingPaymentStatus()
        {
            _explorePage.FilterByExperiences();
            _explorePage.ClickFirstExperienceBookNow();

            _bookingModal.SelectFirstAvailableDateAndSlot();
            _bookingModal.SetGuestCount(1);

            _bookingModal.ClickConfirmBooking();

            // Verify success toast with "Pending Payment" status
            Assert.True(_bookingModal.IsSuccessToastDisplayed(), "Expected success confirmation toast with 'Pending Payment' status.");
        }

        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
    }
}