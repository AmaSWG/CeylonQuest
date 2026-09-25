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

        public string GetMinimumBookableDate()
        {
            // React/HTML date inputs often expose the earliest selectable date via the `min` attribute.
            // If the app doesn't set `min`, fall back to a date comfortably in the future.
            var min = DateInput.GetAttribute("min");
            if (!string.IsNullOrWhiteSpace(min))
                return min;

            return DateTime.Now.AddDays(7).ToString("yyyy-MM-dd");
        }

        public (string date, string slot) SelectFirstAvailableDateAndSlot()
        {
            var startDate = DateTime.Parse(GetMinimumBookableDate());

            for (int dayOffset = 0; dayOffset < 14; dayOffset++)
            {
                var candidate = startDate.AddDays(dayOffset).ToString("yyyy-MM-dd");
                SetBookingDate(candidate);

                try
                {
                    _wait.Until(d =>
                    {
                        try
                        {
                            // Re-find the select on every poll so we never hold a stale reference
                            var selectEl = d.FindElement(By.CssSelector("select.vd-form-input"));
                            var select = new SelectElement(selectEl);

                            return select.Options.Any(o =>
                            {
                                try
                                {
                                    return !string.IsNullOrEmpty(o.GetAttribute("value")) && o.Enabled;
                                }
                                catch (StaleElementReferenceException)
                                {
                                    return false; // re-poll
                                }
                            });
                        }
                        catch (StaleElementReferenceException)
                        {
                            return false;
                        }
                        catch (NoSuchElementException)
                        {
                            return false;
                        }
                    });

                    // Re-query once more for the actual selection
                    var selectEl2 = _driver.FindElement(By.CssSelector("select.vd-form-input"));
                    var select2 = new SelectElement(selectEl2);
                    var slot = select2.Options.First(o =>
                    {
                        try { return !string.IsNullOrEmpty(o.GetAttribute("value")) && o.Enabled; }
                        catch (StaleElementReferenceException) { return false; }
                    });
                    var slotValue = slot.GetAttribute("value");
                    select2.SelectByValue(slotValue);

                    return (candidate, slotValue);
                }
                catch (WebDriverTimeoutException)
                {
                    Console.WriteLine($"[DIAG:avail] No slots on {candidate}, trying next day");
                }
            }

            throw new Exception($"No available slot found in 14 days from {startDate:yyyy-MM-dd}");
        }

        /*public void SelectFirstAvailableTimeSlot()
        {
            // Wait for availability slots to finish loading from the catalog API
            _wait.Until(d =>
            {
                var select = new SelectElement(TimeSlotSelect);
                return select.Options.Any(o => !string.IsNullOrEmpty(o.GetAttribute("value")) && o.Enabled);
            });

            var selectElement = new SelectElement(TimeSlotSelect);
            var availableOption = selectElement.Options.FirstOrDefault(o => !string.IsNullOrEmpty(o.GetAttribute("value")) && o.Enabled);
            if (availableOption != null)
            {
                selectElement.SelectByValue(availableOption.GetAttribute("value"));
            }
        }*/
        /*public void SelectFirstAvailableTimeSlot()
        {
            // Give React a moment to fire the availability request after we set the date
            System.Threading.Thread.Sleep(1500);

            try
            {
                _wait.Until(d =>
                {
                    var select = new SelectElement(TimeSlotSelect);
                    var opts = select.Options
                        .Select(o => $"value='{o.GetAttribute("value")}' text='{o.Text}' enabled={o.Enabled}")
                        .ToList();

                    Console.WriteLine($"[DIAG:slot] options ({opts.Count}): {string.Join(" | ", opts)}");

                    return select.Options.Any(o =>
                        !string.IsNullOrEmpty(o.GetAttribute("value")) && o.Enabled);
                });
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine($"[DIAG:slot] TIMEOUT. URL = {_driver.Url}");
                Console.WriteLine($"[DIAG:slot] date input value = '{DateInput.GetAttribute("value")}'");
                var src = _driver.PageSource;
                var idx = src.IndexOf("vd-booking-modal");
                if (idx >= 0)
                    Console.WriteLine("[DIAG:slot] modal html:\n" +
                        src.Substring(idx, Math.Min(3000, src.Length - idx)));
                throw;
            }

            var selectElement = new SelectElement(TimeSlotSelect);
            var availableOption = selectElement.Options.FirstOrDefault(o =>
                !string.IsNullOrEmpty(o.GetAttribute("value")) && o.Enabled);
            if (availableOption != null)
                selectElement.SelectByValue(availableOption.GetAttribute("value"));
        }*/

        /*public (string date, string slot) SelectFirstAvailableDateAndSlot()
        {
            var startDate = DateTime.Parse(GetMinimumBookableDate());

            for (int dayOffset = 0; dayOffset < 14; dayOffset++)
            {
                var candidate = startDate.AddDays(dayOffset).ToString("yyyy-MM-dd");
                SetBookingDate(candidate);

                // Give React time to fetch availability for this date
                try
                {
                    _wait.Until(d =>
                    {
                        var select = new SelectElement(TimeSlotSelect);
                        return select.Options.Any(o =>
                            !string.IsNullOrEmpty(o.GetAttribute("value")) && o.Enabled);
                    });

                    var select2 = new SelectElement(TimeSlotSelect);
                    var slot = select2.Options.First(o =>
                        !string.IsNullOrEmpty(o.GetAttribute("value")) && o.Enabled);
                    var slotValue = slot.GetAttribute("value");

                    select2.SelectByValue(slotValue);
                    return (candidate, slotValue);
                }
                catch (WebDriverTimeoutException)
                {
                    // No slots for this date, try the next one
                    Console.WriteLine($"[DIAG:avail] No slots on {candidate}, trying next day");
                }
            }

            throw new Exception($"No available slot found in 14 days from {startDate:yyyy-MM-dd}");
        }*/

        /*public void SetGuestCount(int count)
        {
            GuestsInput.Clear();
            GuestsInput.SendKeys(count.ToString());
        }*/
        /*public void SetGuestCount(int count)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                @"var el = arguments[0];
                  var setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
                  setter.call(el, arguments[1]);
                  el.dispatchEvent(new Event('input',  { bubbles: true }));
                  el.dispatchEvent(new Event('change', { bubbles: true }));",
                GuestsInput, count.ToString());
        }*/
        /*public void SetGuestCount(int count)
        {
            // Clear, then type the value so React's real keyboard handlers fire
            GuestsInput.Click();                      // focus
            GuestsInput.SendKeys(Keys.Control + "a"); // select all
            GuestsInput.SendKeys(Keys.Delete);        // clear
            GuestsInput.SendKeys(count.ToString());   // type
        }*/
        /*public void SetGuestCount(int count)
        {
            GuestsInput.Click();
            System.Threading.Thread.Sleep(100);

            GuestsInput.SendKeys(Keys.Control + "a");
            System.Threading.Thread.Sleep(50);
            GuestsInput.SendKeys(Keys.Delete);
            System.Threading.Thread.Sleep(50);

            GuestsInput.SendKeys(count.ToString());
            System.Threading.Thread.Sleep(400);
        }*/

        public void SetGuestCount(int count)
        {
            GuestsInput.SendKeys(Keys.Control + "a");
            GuestsInput.SendKeys(Keys.Backspace);
            GuestsInput.SendKeys(count.ToString());
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

        public void ClickConfirmBooking()
        {
            _wait.Until(d => ConfirmBookingButton.Enabled);
            ConfirmBookingButton.Click();
        }

        /*public bool IsSuccessToastDisplayed()
        {
            try
            {
                var toast = _wait.Until(d => d.FindElement(By.XPath("//*[contains(text(), 'Booking created') or contains(text(), 'Pending Payment')]")));
                return toast.Displayed;
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }*/

        public bool IsSuccessToastDisplayed()
        {
            try
            {
                var toast = _wait.Until(d => d.FindElement(By.XPath(
                    "//*[contains(., 'Booking created') or contains(., 'Pending Payment')]")));
                return toast.Displayed;
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
        }
    }
}