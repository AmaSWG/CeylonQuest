using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Pages
{
    public class VisitorBookingsPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public VisitorBookingsPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
        }

        // Nav / Tab
        private By BookingsNavBtn => By.Id("nav-bookings");
        private By PageHeaderTitle => By.CssSelector(".vb-header h1");
        private By BookingRows => By.CssSelector("table.vb-table tbody tr.vb-row");
        private By SearchInput => By.CssSelector("input.vb-search");
        private By FilterAllBtn => By.XPath("//button[contains(@class, 'vb-filter-btn') and text()='All']");
        private By FilterActiveBtn => By.XPath("//button[contains(@class, 'vb-filter-btn') and text()='Active']");
        private By FilterCancelledBtn => By.XPath("//button[contains(@class, 'vb-filter-btn') and text()='Cancelled']");
        private By EmptyStateContainer => By.CssSelector(".vb-state");

        // Modal
        private By ModalOverlay => By.CssSelector(".vb-modal-overlay");
        private By ModalTitle => By.CssSelector(".vb-modal__header h2");
        private By ModalCloseBtn => By.CssSelector(".vb-modal__close, .vb-close-btn");

        // Cancellation Elements
        private By FirstCancelBtn => By.CssSelector("table.vb-table tbody tr .vb-cancel-btn");
        private By ConfirmCancelContinueBtn => By.CssSelector(".vb-confirm-cancel-btn");
        private By CancellationReasonInput => By.Id("cancellationReason");
        private By FinalConfirmCancelBtn => By.XPath("//button[contains(@class, 'vb-confirm-cancel-btn') and contains(text(), 'Confirm')]");
        private By SuccessMessageBanner => By.CssSelector(".vb-success-message");
        private By CancelledSection => By.CssSelector(".vb-cancelled-section");


        public void NavigateToBookingsTab()
        {
            // Wait for navigation sidebar to be ready
            var tab = _wait.Until(d => d.FindElement(BookingsNavBtn));

            // Perform JavaScript click to ensure it triggers regardless of viewport scroll state
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", tab);

            // Wait until the header is rendered
            _wait.Until(d => d.FindElement(PageHeaderTitle).Displayed);
        }

        public bool IsLoaded()
        {
            try
            {
                return _wait.Until(d => d.FindElement(PageHeaderTitle).Text.Contains("Bookings & Reservations"));
            }
            catch
            {
                return false;
            }
        }

        public int GetBookingRowCount()
        {
            // Short wait for rows or empty container to appear
            try
            {
                _wait.Until(d => d.FindElements(BookingRows).Count > 0 || d.FindElements(EmptyStateContainer).Count > 0);
            }
            catch { }

            return _driver.FindElements(BookingRows).Count;
        }

        public bool IsEmptyStateDisplayed()
        {
            try
            {
                var empty = _driver.FindElement(EmptyStateContainer);
                return empty.Displayed && empty.Text.Contains("No bookings or reservations yet");
            }
            catch
            {
                return false;
            }
        }

        public void SearchBookings(string query)
        {
            var search = _wait.Until(d => d.FindElement(SearchInput));
            search.Clear();
            search.SendKeys(query);
        }

        public void FilterByStatus(string status)
        {
            By target = status.ToLower() switch
            {
                "active" => FilterActiveBtn,
                "cancelled" => FilterCancelledBtn,
                _ => FilterAllBtn
            };
            var btn = _wait.Until(d => d.FindElement(target));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", btn);
        }

        public void OpenDetailsForFirstRow()
        {
            var firstViewBtn = _wait.Until(d => d.FindElement(By.CssSelector("table.vb-table tbody tr .vb-view-btn")));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", firstViewBtn);
            _wait.Until(d => d.FindElement(ModalOverlay).Displayed);
        }

        public string GetModalTitle()
        {
            return _wait.Until(d => d.FindElement(ModalTitle)).Text;
        }

        public void CloseDetailsModal()
        {
            var closeBtn = _wait.Until(d => d.FindElement(ModalCloseBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", closeBtn);
            _wait.Until(d => d.FindElements(ModalOverlay).Count == 0 || !d.FindElement(ModalOverlay).Displayed);
        }

        public void CancelFirstActiveBooking(string reason = "Schedule changed")
        {
            // 1. Click 'Cancel' button on first active row
            var cancelBtn = _wait.Until(d => d.FindElement(FirstCancelBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", cancelBtn);
            // 2. Step 1 confirmation: Click 'Yes, Continue to Cancel'
            var continueBtn = _wait.Until(d => d.FindElement(ConfirmCancelContinueBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", continueBtn);
            // 3. Step 2 form: Enter optional reason
            if (!string.IsNullOrEmpty(reason))
            {
                var reasonField = _wait.Until(d => d.FindElement(CancellationReasonInput));
                reasonField.Clear();
                reasonField.SendKeys(reason);
            }
            // 4. Click 'Confirm Booking Cancellation'
            var finalBtn = _wait.Until(d => d.FindElement(FinalConfirmCancelBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", finalBtn);
            // 5. Wait for success banner
            _wait.Until(d => d.FindElement(SuccessMessageBanner).Displayed);
        }
        public bool IsSuccessBannerDisplayed()
        {
            try
            {
                return _wait.Until(d => d.FindElement(SuccessMessageBanner).Displayed);
            }
            catch { return false; }
        }
        public bool IsCancellationDetailsDisplayedInModal()
        {
            try
            {
                return _wait.Until(d => d.FindElement(CancelledSection).Displayed);
            }
            catch { return false; }
        }
    }
}