using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Globalization;
using System.Linq;

namespace CeylonQuest.Tests.Pages
{
    public class BookingModalPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public BookingModalPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        }

        public IWebElement ModalContainer => _wait.Until(d => d.FindElement(By.CssSelector(".vd-booking-modal")));
        public IWebElement DateInput => _wait.Until(d => d.FindElement(By.CssSelector("input[type='date'].vd-form-input")));
        public IWebElement TimeSlotSelect => _wait.Until(d => d.FindElement(By.CssSelector("select.vd-form-input")));
        public IWebElement GuestsInput => _wait.Until(d => d.FindElement(By.CssSelector("input[type='number'].vd-form-input")));
        public IWebElement EstimatedTotalElement => _wait.Until(d => d.FindElement(By.CssSelector(".vd-detail-price__val")));
        public IWebElement BaseRatePriceElement => _wait.Until(d => d.FindElement(By.CssSelector(".vd-booking-rate-price")));
        public IWebElement ConfirmBookingButton => _wait.Until(d => d.FindElement(By.CssSelector(".vd-btn-book")));
        public IWebElement CloseButton => _wait.Until(d => d.FindElement(By.CssSelector(".vd-detail-modal__close")));

        public void SetBookingDate(string yyyyMmDd)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                    @"var el = arguments[0];
                      var setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
                      setter.call(el, arguments[1]);
                      el.dispatchEvent(new Event('input',  { bubbles: true }));
                      el.dispatchEvent(new Event('change', { bubbles: true }));",
                    DateInput, yyyyMmDd);
        }

        public void SetCheckOutDate(string yyyyMmDd)
        {
            var dateInputs = _driver.FindElements(By.CssSelector("input[type='date'].vd-form-input"));
            if (dateInputs.Count > 1)
            {
                var checkOutEl = dateInputs[1];
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                        @"var el = arguments[0];
                          var setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
                          setter.call(el, arguments[1]);
                          el.dispatchEvent(new Event('input',  { bubbles: true }));
                          el.dispatchEvent(new Event('change', { bubbles: true }));",
                        checkOutEl, yyyyMmDd);
                System.Threading.Thread.Sleep(200);
            }
        }

        public string GetMinimumBookableDate()
        {
            var min = DateInput.GetAttribute("min");
            if (!string.IsNullOrWhiteSpace(min))
                return min;

            return DateTime.Now.AddDays(7).ToString("yyyy-MM-dd");
        }

        public (string date, string slot) SelectFirstAvailableDateAndSlot(int daysToScan = 14)
        {
            var startDate = DateTime.Today.AddDays(1);
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(4));
            for (int dayOffset = 0; dayOffset < daysToScan; dayOffset++)
            {
                var candidate = startDate.AddDays(dayOffset).ToString("yyyy-MM-dd");
                SetBookingDate(candidate);
                System.Threading.Thread.Sleep(250);

                // If accommodation modal, set check-out date (e.g. +3 nights to satisfy minStay)
                var dateInputs = _driver.FindElements(By.CssSelector("input[type='date'].vd-form-input"));
                if (dateInputs.Count > 1)
                {
                    var checkOut = startDate.AddDays(dayOffset + 3).ToString("yyyy-MM-dd");
                    SetCheckOutDate(checkOut);
                    System.Threading.Thread.Sleep(250);
                }

                try
                {
                    var chosenOpt = wait.Until(d =>
                    {
                        var selects = d.FindElements(By.CssSelector("select.vd-form-input"));
                        if (selects.Count == 0) return null;
                        var sel = new SelectElement(selects[0]);
                        return sel.Options.FirstOrDefault(o =>
                        {
                            try
                            {
                                var val = o.GetAttribute("value");
                                if (string.IsNullOrWhiteSpace(val)) return false;
                                if (!o.Enabled) return false;
                                if (o.Text.Contains("Fully Booked", StringComparison.OrdinalIgnoreCase)) return false;
                                return true;
                            }
                            catch (StaleElementReferenceException) { return false; }
                        });
                    });
                    if (chosenOpt != null)
                    {
                        var slotValue = chosenOpt.GetAttribute("value")!;
                        var selectEl = _driver.FindElement(By.CssSelector("select.vd-form-input"));
                        var select = new SelectElement(selectEl);
                        select.SelectByValue(slotValue);
                        ((IJavaScriptExecutor)_driver).ExecuteScript(
                            @"var sel = arguments[0];
                              var setter = Object.getOwnPropertyDescriptor(window.HTMLSelectElement.prototype, 'value').set;
                              if (setter) setter.call(sel, arguments[1]);
                              else sel.value = arguments[1];
                              sel.dispatchEvent(new Event('input', { bubbles: true }));
                              sel.dispatchEvent(new Event('change', { bubbles: true }));",
                            selectEl, slotValue);
                        System.Threading.Thread.Sleep(300);
                        return (candidate, slotValue);
                    }
                }
                catch (WebDriverTimeoutException)
                {
                    Console.WriteLine($"[DIAG:avail] No slots available on {candidate}, checking next day...");
                }
            }
            throw new InvalidOperationException($"No available slot found in {daysToScan} days starting from {startDate:yyyy-MM-dd}");
        }

        public void ClickConfirmBooking()
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
            wait.Until(d =>
            {
                try
                {
                    var btn = d.FindElement(By.CssSelector(".vd-btn-book"));
                    return btn.Displayed && btn.Enabled;
                }
                catch (StaleElementReferenceException) { return false; }
                catch (NoSuchElementException) { return false; }
            });
            var confirmBtn = _driver.FindElement(By.CssSelector(".vd-btn-book"));
            confirmBtn.Click();
        }

        public bool WaitForConfirmButtonEnabled(int timeoutSeconds = 5)
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                return wait.Until(d =>
                {
                    try
                    {
                        var btn = d.FindElement(By.CssSelector(".vd-btn-book"));
                        return btn.Displayed && btn.Enabled;
                    }
                    catch (StaleElementReferenceException) { return false; }
                    catch (NoSuchElementException) { return false; }
                });
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }

        public void SetGuestCount(int count)
        {
            GuestsInput.Click();
            GuestsInput.SendKeys(Keys.Control + "a");
            GuestsInput.SendKeys(Keys.Backspace);
            GuestsInput.SendKeys(count.ToString());
            GuestsInput.SendKeys(Keys.Tab);
            System.Threading.Thread.Sleep(150);
        }

        public int GetCurrentGuestCount()
        {
            var val = GuestsInput.GetAttribute("value");
            return int.TryParse(val, out var count) ? count : 1;
        }

        public decimal GetBasePrice()
        {
            var text = BaseRatePriceElement.Text;
            var digits = System.Text.RegularExpressions.Regex.Match(text.Replace(",", ""), @"\d+(\.\d+)?").Value;
            return decimal.Parse(digits, CultureInfo.InvariantCulture);
        }

        public decimal GetEstimatedTotal()
        {
            var text = EstimatedTotalElement.Text;
            var digits = System.Text.RegularExpressions.Regex.Match(text.Replace(",", ""), @"\d+(\.\d+)?").Value;
            return decimal.Parse(digits, CultureInfo.InvariantCulture);
        }

        public bool IsSuccessToastDisplayed(int timeoutSeconds = 15)
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                return wait.Until(d =>
                {
                    try
                    {
                        var toasts = d.FindElements(By.CssSelector(".vd-toast"));
                        return toasts.Any(t => t.Displayed);
                    }
                    catch (StaleElementReferenceException) { return false; }
                });
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }
    }
}