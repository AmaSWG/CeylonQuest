using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        // Nav / Header
        private By BookingsNavBtn => By.Id("nav-bookings");
        private By PageHeaderTitle => By.CssSelector(".vb-header h1");
        private By TotalCountBadge => By.CssSelector(".vb-count");
        private By BookingRows => By.CssSelector("table.vb-table tbody tr.vb-row");
        private By SearchInput => By.CssSelector("input.vb-search, input[placeholder*='Search your bookings']");
        private By LoaderElement => By.CssSelector(".vb-loader");

        // Filter Pills: All | Confirmed | Pending Payment | Cancelled
        private By FilterAllBtn => By.XPath("//button[contains(@class, 'vb-filter-btn') and (text()='All' or contains(., 'All'))]");
        private By FilterConfirmedBtn => By.XPath("//button[contains(@class, 'vb-filter-btn') and (text()='Confirmed' or contains(., 'Confirmed'))]");
        private By FilterPendingPaymentBtn => By.XPath("//button[contains(@class, 'vb-filter-btn') and (contains(text(), 'Pending') or contains(text(), 'Active'))]");
        private By FilterCancelledBtn => By.XPath("//button[contains(@class, 'vb-filter-btn') and (text()='Cancelled' or contains(., 'Cancelled'))]");
        private By EmptyStateContainer => By.CssSelector(".vb-state");

        // Modal Elements
        private By ModalOverlay => By.CssSelector(".vb-modal-overlay");
        private By ModalTitle => By.CssSelector(".vb-modal__header h2, .vb-modal h2");
        private By ModalCloseBtn => By.CssSelector(".vb-modal__close, .vb-close-btn");

        // Actions
        private By FirstCancelBtn => By.CssSelector("table.vb-table tbody tr .vb-cancel-btn");
        private By FirstDeleteBtn => By.CssSelector("table.vb-table tbody tr .vb-delete-btn");
        private By ConfirmCancelContinueBtn => By.CssSelector(".vb-confirm-cancel-btn");
        private By CancellationReasonInput => By.Id("cancellationReason");
        private By FinalConfirmCancelBtn => By.XPath("//button[contains(@class, 'vb-confirm-cancel-btn') and contains(text(), 'Confirm')]");
        private By SuccessMessageBanner => By.CssSelector(".vb-success-message");
        private By CancelledSection => By.CssSelector(".vb-cancelled-section");

        public void NavigateToBookingsTab()
        {
            var tab = _wait.Until(d => d.FindElement(BookingsNavBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", tab);
            _wait.Until(d => d.FindElement(PageHeaderTitle).Displayed);

            WaitForLoadingToComplete();
        }

        private void WaitForLoadingToComplete()
        {
            try
            {
                _wait.Until(d => d.FindElements(LoaderElement).Count == 0);
            }
            catch { }
        }

        public bool IsLoaded()
        {
            try
            {
                return _wait.Until(d => d.FindElement(PageHeaderTitle).Text.Contains("My Bookings"));
            }
            catch
            {
                return false;
            }
        }

        public int GetBookingRowCount()
        {
            WaitForLoadingToComplete();
            try
            {
                _wait.Until(d => d.FindElements(BookingRows).Count > 0 || d.FindElements(EmptyStateContainer).Count > 0);
            }
            catch { }

            return _driver.FindElements(BookingRows).Count;
        }

        public bool IsEmptyStateDisplayed()
        {
            WaitForLoadingToComplete();
            try
            {
                var empty = _driver.FindElement(EmptyStateContainer);
                return empty.Displayed;
            }
            catch
            {
                return false;
            }
        }

        public string GetTotalCountBadgeText()
        {
            try
            {
                return _wait.Until(d => d.FindElement(TotalCountBadge)).Text;
            }
            catch { return ""; }
        }

        public void SearchBookings(string query)
        {
            WaitForLoadingToComplete();
            var search = _wait.Until(d => d.FindElement(SearchInput));
            search.Clear();
            search.SendKeys(query);
            Thread.Sleep(300);
        }

        public void FilterByStatus(string status)
        {
            WaitForLoadingToComplete();

            if (GetBookingRowCount() == 0) return;

            By target = status.ToLower() switch
            {
                "confirmed" => FilterConfirmedBtn,
                "pending" or "pending payment" or "active" => FilterPendingPaymentBtn,
                "cancelled" => FilterCancelledBtn,
                _ => FilterAllBtn
            };

            try
            {
                var btn = _wait.Until(d => d.FindElement(target));
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", btn);
                Thread.Sleep(300);
            }
            catch (WebDriverTimeoutException)
            {
                var fallback = _driver.FindElements(By.XPath($"//button[contains(., '{status}')]")).FirstOrDefault();
                if (fallback != null)
                {
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", fallback);
                    Thread.Sleep(300);
                }
            }
        }

        public void OpenDetailsForFirstRow()
        {
            WaitForLoadingToComplete();

            var firstViewBtn = _wait.Until(d =>
            {
                var btns = d.FindElements(By.CssSelector("table.vb-table tbody tr .vb-view-btn"));
                return btns.Count > 0 && btns[0].Displayed && btns[0].Enabled ? btns[0] : null;
            });

            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({behavior: 'instant', block: 'center'});", firstViewBtn);
            Thread.Sleep(200);

            try
            {
                firstViewBtn.Click();
            }
            catch
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", firstViewBtn);
            }

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
            WaitForLoadingToComplete();
            var cancelBtn = _wait.Until(d => d.FindElement(FirstCancelBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", cancelBtn);

            var continueBtn = _wait.Until(d => d.FindElement(ConfirmCancelContinueBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", continueBtn);

            if (!string.IsNullOrEmpty(reason))
            {
                var reasonField = _wait.Until(d => d.FindElement(CancellationReasonInput));
                reasonField.Clear();
                reasonField.SendKeys(reason);
            }

            var finalBtn = _wait.Until(d => d.FindElement(FinalConfirmCancelBtn));
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", finalBtn);

            _wait.Until(d => d.FindElement(SuccessMessageBanner).Displayed);
        }

        public bool IsSuccessBannerDisplayed()
        {
            try { return _wait.Until(d => d.FindElement(SuccessMessageBanner).Displayed); }
            catch { return false; }
        }

        public bool IsCancellationDetailsDisplayedInModal()
        {
            try { return _wait.Until(d => d.FindElement(CancelledSection).Displayed); }
            catch { return false; }
        }
    }
}