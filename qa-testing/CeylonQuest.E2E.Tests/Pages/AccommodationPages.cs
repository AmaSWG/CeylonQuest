using System.Globalization;
using CeylonQuest.E2E.Tests.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.E2E.Tests.Pages;

internal sealed class ExplorePage(BrowserSession browser)
{
    public void OpenAccommodationDetails()
    {
        var title = TestSettings.Required("CQ_E2E_ACCOMMODATION_TITLE");
        browser.Click(By.Id("nav-explore"));
        var filter = By.XPath("//div[contains(@class,'vd-filters')]/select[option[@value='accommodation']]");
        new SelectElement(browser.Visible(filter)).SelectByValue("accommodation");
        browser.Fill(By.CssSelector(".vd-search-bar__input"), title);
        var card = browser.Until(d => d.FindElements(By.CssSelector(".vd-service-card"))
            .SingleOrDefault(e => e.FindElement(By.CssSelector(".vd-service-card__title")).Text == title)!,
            "Expected one accommodation with the configured exact title. Check catalog availability and search's 50-listing limit.");
        // CSS renders the service badge in uppercase; compare the type independently
        // of that visual styling while still rejecting a different service type.
        Assert.Equal("Accommodation", card.FindElement(By.CssSelector(".vd-type-badge")).Text.Trim(), ignoreCase: true);
        card.FindElement(By.CssSelector(".vd-btn--secondary")).Click();
        Assert.Equal(title, browser.Visible(By.CssSelector(".vd-detail-modal .vd-detail-modal__title")).Text);
        browser.Visible(By.CssSelector(".vd-detail-modal .vd-detail-price__val"));
        browser.Step("Accommodation search and details verified: " + title);
    }

    public BookingModal OpenBooking()
    {
        OpenAccommodationDetails();
        browser.Click(By.CssSelector(".vd-detail-modal .vd-btn-book"));
        browser.Visible(By.CssSelector(".vd-booking-modal"));
        return new BookingModal(browser);
    }
}

internal sealed class BookingModal(BrowserSession browser)
{
    // Inputs have no IDs or associated htmlFor: scope by the actual adjacent labels.
    public static readonly By CheckOut = By.XPath("//div[contains(@class,'vd-booking-form-grid')]//label[normalize-space()='Check-out Date']/following-sibling::input");
    private static readonly By CheckInByLabel = By.XPath("//div[contains(@class,'vd-booking-form-grid')]//label[normalize-space()='Check-in Date']/following-sibling::input");
    public static readonly By Guests = By.CssSelector(".vd-booking-modal input[type='number']");
    public static readonly By Confirm = By.CssSelector(".vd-booking-modal .vd-btn-book");

    public void SetStay(DateOnly checkIn, DateOnly checkOut)
    {
        browser.SetDate(CheckInByLabel, checkIn);
        browser.SetDate(CheckOut, checkOut);
    }

    public void SelectAvailability()
    {
        var selector = By.CssSelector(".vd-booking-form-grid select");
        browser.Until(d => {
            var select = d.FindElement(selector);
            if (!select.Enabled) return false;
            var available = new SelectElement(select).Options.FirstOrDefault(e =>
                !string.IsNullOrEmpty(e.GetDomAttribute("value")) && e.Enabled);
            if (available is null) return false;
            new SelectElement(select).SelectByValue(available.GetDomAttribute("value")!);
            return true;
        }, "No available stay slot for the configured date. Inspect availability error/operating days; no booking was submitted.");
    }

    public void PrepareValidStay()
    {
        TestSettings.ValidateStay();
        SetStay(TestSettings.Date("CQ_E2E_CHECK_IN"), TestSettings.Date("CQ_E2E_CHECK_OUT"));
        SelectAvailability();
        browser.Fill(Guests, TestSettings.Guests.ToString(CultureInfo.InvariantCulture));
        browser.Visible(Guests).SendKeys(Keys.Tab);
        browser.Until(d => d.FindElement(Confirm).Enabled,
            "Book Stay remains disabled. Check minimum nights, guest limits and availability.");
        Assert.Equal(TestSettings.Guests.ToString(CultureInfo.InvariantCulture), browser.Visible(Guests).GetDomProperty("value"));
    }

    public void AssertSubmissionDisabled() => Assert.False(browser.Visible(Confirm).Enabled);
}
