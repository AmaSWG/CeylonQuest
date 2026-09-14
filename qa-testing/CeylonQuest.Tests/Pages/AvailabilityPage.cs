using System.Text.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Pages;

public class AvailabilityPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public sealed record ApiResult(int Status, string Body);

    public AvailabilityPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
    }

    // =========================================================
    // API HELPERS
    // =========================================================

    public ApiResult BrowserApiRequest(
        string method,
        string path,
        string? bodyJson = null,
        bool useAuth = true)
    {
        var script = @"
            const done = arguments[arguments.length - 1];
            const method = arguments[0];
            const path = arguments[1];
            const body = arguments[2];
            const useAuth = arguments[3];

            const headers = {
                'Accept': 'application/json'
            };

            if (body) {
                headers['Content-Type'] = 'application/json';
            }

            if (useAuth) {
                const token =
                    window.localStorage.getItem('authToken');

                if (token) {
                    headers['Authorization'] =
                        'Bearer ' + token;
                }
            }

            fetch(path, {
                method,
                headers,
                body: body || undefined
            })
            .then(async response => {
                const text = await response.text();

                done(JSON.stringify({
                    status: response.status,
                    body: text
                }));
            })
            .catch(error => {
                done(JSON.stringify({
                    status: 0,
                    body: String(error)
                }));
            });
        ";

        var raw =
            (string?)((IJavaScriptExecutor)_driver)
                .ExecuteAsyncScript(
                    script,
                    method,
                    path,
                    bodyJson,
                    useAuth);

        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "Browser API request returned no result.");
        }

        using var doc =
            JsonDocument.Parse(raw);

        return new ApiResult(
            doc.RootElement
                .GetProperty("status")
                .GetInt32(),

            doc.RootElement
                .GetProperty("body")
                .GetString() ?? string.Empty
        );
    }

    public JsonDocument GetAvailability(
        string listingId,
        string date)
    {
        var result =
            BrowserApiRequest(
                "GET",
                $"/api/catalog/availability/{listingId}?date={date}",
                null,
                false);

        if (result.Status != 200)
        {
            throw new InvalidOperationException(
                $"Availability GET failed. " +
                $"HTTP {result.Status}: {result.Body}");
        }

        return JsonDocument.Parse(
            result.Body);
    }

    public ApiResult SetCapacity(
        string listingId,
        string date,
        string timeSlot,
        int capacity)
    {
        var body =
            JsonSerializer.Serialize(
                new
                {
                    date,
                    timeSlot,
                    capacity
                });

        return BrowserApiRequest(
            "PUT",
            $"/api/catalog/availability/{listingId}",
            body,
            true);
    }

    public ApiResult SetRawCapacityPayload(
        string listingId,
        string rawJson)
    {
        return BrowserApiRequest(
            "PUT",
            $"/api/catalog/availability/{listingId}",
            rawJson,
            true);
    }

    public ApiResult SimulateBooking(
        string listingId,
        string date,
        string timeSlot,
        int guestCount)
    {
        var body =
            JsonSerializer.Serialize(
                new
                {
                    listingId,
                    date,
                    timeSlot,
                    guestCount
                });

        return BrowserApiRequest(
            "POST",
            "/api/catalog/availability/simulate-booking-event",
            body,
            false);
    }

    public (
        string TimeSlot,
        int Total,
        int Remaining,
        bool FullyBooked)
        FirstSlot(JsonDocument doc)
    {
        var slots =
            doc.RootElement
                .GetProperty("slots");

        if (slots.GetArrayLength() == 0)
        {
            throw new InvalidOperationException(
                "The selected listing/date returned no availability slots.");
        }

        var slot = slots[0];

        return (
            slot.GetProperty("timeSlot")
                .GetString() ?? string.Empty,

            slot.GetProperty("totalCapacity")
                .GetInt32(),

            slot.GetProperty("remainingCapacity")
                .GetInt32(),

            slot.GetProperty("isFullyBooked")
                .GetBoolean()
        );
    }

    // =========================================================
    // VISITOR EXPLORE PAGE
    // =========================================================

    public void OpenVisitorExplore()
    {
        var explore =
            _wait.Until(d =>
                d.FindElements(
                    By.Id("nav-explore"))
                 .FirstOrDefault(e =>
                     e.Displayed &&
                     e.Enabled));

        if (explore == null)
        {
            throw new NoSuchElementException(
                "Visitor Explore navigation was not found.");
        }

        explore.Click();

        // Wait until Explore page is actually loaded.
        _wait.Until(d =>
        {
            var searchInputs =
                d.FindElements(
                    By.CssSelector("input"));

            var searchReady =
                searchInputs.Any(input =>
                {
                    try
                    {
                        var placeholder =
                            input.GetAttribute(
                                "placeholder") ?? "";

                        return input.Displayed &&
                               input.Enabled &&
                               placeholder.Contains(
                                   "search",
                                   StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }
                });

            var cardsReady =
                d.FindElements(
                    By.CssSelector(
                        ".vd-service-card"))
                 .Any(c =>
                 {
                     try
                     {
                         return c.Displayed;
                     }
                     catch
                     {
                         return false;
                     }
                 });

            return searchReady ||
                   cardsReady;
        });
    }

    // =========================================================
    // SEARCH + OPEN LISTING
    // =========================================================

    public void OpenAvailabilityForListingTitle(
        string listingTitle)
    {
        if (string.IsNullOrWhiteSpace(
                listingTitle))
        {
            throw new ArgumentException(
                "Listing title cannot be empty.",
                nameof(listingTitle));
        }

        var expectedTitle =
            listingTitle.Trim();

        // -----------------------------------------------------
        // STEP 1:
        // Find Explore search input
        // -----------------------------------------------------

        var searchInput =
            _wait.Until(d =>
                d.FindElements(
                    By.CssSelector("input"))
                 .FirstOrDefault(input =>
                 {
                     try
                     {
                         if (!input.Displayed ||
                             !input.Enabled)
                         {
                             return false;
                         }

                         var placeholder =
                             input.GetAttribute(
                                 "placeholder") ?? "";

                         var type =
                             input.GetAttribute(
                                 "type") ?? "";

                         return
                             placeholder.Contains(
                                 "search",
                                 StringComparison.OrdinalIgnoreCase)
                             ||
                             type.Equals(
                                 "search",
                                 StringComparison.OrdinalIgnoreCase);
                     }
                     catch
                     {
                         return false;
                     }
                 }));

        if (searchInput == null)
        {
            throw new NoSuchElementException(
                "Explore search input was not found.");
        }

        // -----------------------------------------------------
        // STEP 2:
        // Search exact listing title
        // -----------------------------------------------------

        searchInput.Click();

        searchInput.SendKeys(
            Keys.Control + "a");

        searchInput.SendKeys(
            Keys.Backspace);

        searchInput.SendKeys(
            expectedTitle);

        // Try Enter first.
        searchInput.SendKeys(
            Keys.Enter);

        Thread.Sleep(800);

        // -----------------------------------------------------
        // STEP 3:
        // If React page requires clicking the search icon,
        // click the last visible button near the search input.
        // -----------------------------------------------------

        IWebElement? matchedCard =
            FindListingCard(
                expectedTitle);

        if (matchedCard == null)
        {
            try
            {
                var parent =
                    searchInput.FindElement(
                        By.XPath("./.."));

                var nearbyButtons =
                    parent.FindElements(
                        By.CssSelector("button"))
                    .Where(button =>
                    {
                        try
                        {
                            return button.Displayed &&
                                   button.Enabled;
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .ToList();

                // In your Explore UI:
                // first button = clear
                // last button = search icon
                if (nearbyButtons.Count > 0)
                {
                    nearbyButtons.Last().Click();

                    Thread.Sleep(800);
                }
            }
            catch
            {
                // Do not fail here.
                // Final wait below will give useful error.
            }
        }

        // -----------------------------------------------------
        // STEP 4:
        // Wait for "new tester" / configured listing
        // -----------------------------------------------------

        try
        {
            matchedCard =
                _wait.Until(d =>
                    FindListingCard(
                        expectedTitle));
        }
        catch (WebDriverTimeoutException)
        {
            var visibleTitles =
                GetVisibleListingTitles();

            var foundTitles =
                visibleTitles.Count == 0
                    ? "(no visible listing titles)"
                    : string.Join(
                        ", ",
                        visibleTitles);

            throw new NoSuchElementException(
                $"Listing '{expectedTitle}' was not found after searching. " +
                $"Visible listings: {foundTitles}");
        }

        if (matchedCard == null)
        {
            throw new NoSuchElementException(
                $"Listing '{expectedTitle}' was not found after searching.");
        }

        // -----------------------------------------------------
        // STEP 5:
        // Scroll listing into view
        // -----------------------------------------------------

        ((IJavaScriptExecutor)_driver)
            .ExecuteScript(
                "arguments[0].scrollIntoView({block:'center'});",
                matchedCard);

        Thread.Sleep(300);

        // -----------------------------------------------------
        // STEP 6:
        // Find Book button inside matched card
        // -----------------------------------------------------

        IWebElement? bookButton = null;

        try
        {
            bookButton =
                matchedCard
                    .FindElements(
                        By.CssSelector(
                            ".vd-book-btn"))
                    .FirstOrDefault(b =>
                    {
                        try
                        {
                            return b.Displayed &&
                                   b.Enabled;
                        }
                        catch
                        {
                            return false;
                        }
                    });

            // Fallback if CSS class changes slightly:
            if (bookButton == null)
            {
                bookButton =
                    matchedCard
                        .FindElements(
                            By.CssSelector("button"))
                        .FirstOrDefault(b =>
                        {
                            try
                            {
                                return b.Displayed &&
                                       b.Enabled &&
                                       b.Text.Contains(
                                           "book",
                                           StringComparison.OrdinalIgnoreCase);
                            }
                            catch
                            {
                                return false;
                            }
                        });
            }
        }
        catch (StaleElementReferenceException)
        {
            matchedCard =
                FindListingCard(
                    expectedTitle);

            if (matchedCard != null)
            {
                bookButton =
                    matchedCard
                        .FindElements(
                            By.CssSelector(
                                ".vd-book-btn"))
                        .FirstOrDefault(b =>
                            b.Displayed &&
                            b.Enabled);
            }
        }

        if (bookButton == null)
        {
            throw new NoSuchElementException(
                $"Book button was not found for listing '{expectedTitle}'.");
        }

        // -----------------------------------------------------
        // STEP 7:
        // Scroll to and click Book
        // -----------------------------------------------------

        ((IJavaScriptExecutor)_driver)
            .ExecuteScript(
                "arguments[0].scrollIntoView({block:'center'});",
                bookButton);

        Thread.Sleep(300);

        try
        {
            bookButton.Click();
        }
        catch (ElementClickInterceptedException)
        {
            ((IJavaScriptExecutor)_driver)
                .ExecuteScript(
                    "arguments[0].click();",
                    bookButton);
        }
        catch (StaleElementReferenceException)
        {
            matchedCard =
                FindListingCard(
                    expectedTitle);

            if (matchedCard == null)
            {
                throw new NoSuchElementException(
                    $"Listing '{expectedTitle}' disappeared before booking.");
            }

            bookButton =
                matchedCard
                    .FindElements(
                        By.CssSelector(
                            ".vd-book-btn"))
                    .FirstOrDefault(b =>
                        b.Displayed &&
                        b.Enabled);

            if (bookButton == null)
            {
                throw new NoSuchElementException(
                    $"Book button disappeared for '{expectedTitle}'.");
            }

            ((IJavaScriptExecutor)_driver)
                .ExecuteScript(
                    "arguments[0].click();",
                    bookButton);
        }

        // -----------------------------------------------------
        // STEP 8:
        // Wait until booking modal appears
        // -----------------------------------------------------

        _wait.Until(d =>
        {
            // Current expected selector
            var modalTitles =
                d.FindElements(
                    By.CssSelector(
                        ".vd-detail-modal__title"));

            if (modalTitles.Any(e =>
            {
                try
                {
                    return e.Displayed &&
                           e.Text.Contains(
                               "Book:",
                               StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            }))
            {
                return true;
            }

            // Fallback:
            // booking modal may contain date selector
            return d.FindElements(
                       By.CssSelector(
                           "input[type='date']"))
                    .Any(e =>
                    {
                        try
                        {
                            return e.Displayed;
                        }
                        catch
                        {
                            return false;
                        }
                    });
        });
    }

    // =========================================================
    // LISTING CARD HELPER
    // =========================================================

    private IWebElement? FindListingCard(
        string listingTitle)
    {
        var cards =
            _driver.FindElements(
                By.CssSelector(
                    ".vd-service-card"));

        foreach (var card in cards)
        {
            try
            {
                if (!card.Displayed)
                {
                    continue;
                }

                var titleElements =
                    card.FindElements(
                        By.CssSelector(
                            ".vd-service-card__title"));

                if (titleElements.Count > 0)
                {
                    var title =
                        titleElements[0]
                            .Text
                            .Trim();

                    if (title.Equals(
                        listingTitle,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return card;
                    }
                }

                // Fallback:
                // if CSS title class changes,
                // inspect the entire card text.
                var cardText =
                    card.Text?.Trim() ?? "";

                var lines =
                    cardText
                        .Split(
                            new[]
                            {
                                '\r',
                                '\n'
                            },
                            StringSplitOptions
                                .RemoveEmptyEntries)
                        .Select(x =>
                            x.Trim());

                if (lines.Any(line =>
                        line.Equals(
                            listingTitle,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    return card;
                }
            }
            catch (StaleElementReferenceException)
            {
                // Page updated while searching.
                return null;
            }
            catch
            {
                // Ignore one broken card.
            }
        }

        return null;
    }

    // =========================================================
    // DEBUG HELPER
    // =========================================================

    private List<string> GetVisibleListingTitles()
    {
        var result =
            new List<string>();

        var cards =
            _driver.FindElements(
                By.CssSelector(
                    ".vd-service-card"));

        foreach (var card in cards)
        {
            try
            {
                if (!card.Displayed)
                {
                    continue;
                }

                var title =
                    card.FindElements(
                            By.CssSelector(
                                ".vd-service-card__title"))
                        .FirstOrDefault();

                if (title != null &&
                    !string.IsNullOrWhiteSpace(
                        title.Text))
                {
                    result.Add(
                        title.Text.Trim());
                }
            }
            catch
            {
                // Only used for debug message.
            }
        }

        return result;
    }

    // =========================================================
    // DATE
    // =========================================================

    public void SelectDate(
        string yyyyMmDd)
    {
        var date =
            _wait.Until(d =>
                d.FindElements(
                    By.CssSelector(
                        "input[type='date']"))
                 .FirstOrDefault(e =>
                     e.Displayed &&
                     e.Enabled));

        if (date == null)
        {
            throw new NoSuchElementException(
                "Booking date input was not found.");
        }

        ((IJavaScriptExecutor)_driver)
            .ExecuteScript(@"
                const el = arguments[0];
                const value = arguments[1];

                const setter =
                    Object.getOwnPropertyDescriptor(
                        HTMLInputElement.prototype,
                        'value'
                    ).set;

                setter.call(el, value);

                el.dispatchEvent(
                    new Event(
                        'input',
                        { bubbles: true }
                    )
                );

                el.dispatchEvent(
                    new Event(
                        'change',
                        { bubbles: true }
                    )
                );
            ",
            date,
            yyyyMmDd);

        // Wait until React has accepted the selected date.
        _wait.Until(d =>
        {
            try
            {
                var current =
                    date.GetAttribute("value");

                return string.Equals(
                    current,
                    yyyyMmDd,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        });

        Thread.Sleep(500);
    }

    // =========================================================
    // AVAILABILITY MESSAGE
    // =========================================================

    public string AvailabilityMessage()
    {
        // Prefer the smallest visible element that actually contains
        // the availability state. The old XPath could match a large
        // ancestor (even the whole page) before the modal message.
        var message =
            _wait.Until(d =>
            {
                var candidates =
                    d.FindElements(
                        By.XPath(
                            "//*[contains(normalize-space(.),'Fully Booked') " +
                            "or contains(normalize-space(.),'spots available') " +
                            "or contains(normalize-space(.),'does not operate')]"));

                var visible =
                    candidates
                        .Where(e =>
                        {
                            try
                            {
                                return e.Displayed &&
                                       !string.IsNullOrWhiteSpace(e.Text);
                            }
                            catch
                            {
                                return false;
                            }
                        })
                        .ToList();

                if (visible.Count == 0)
                    return null;

                // Prefer a leaf/small element rather than a page container.
                return visible
                    .OrderBy(e =>
                    {
                        try
                        {
                            return e.FindElements(By.XPath("./*")).Count;
                        }
                        catch
                        {
                            return int.MaxValue;
                        }
                    })
                    .ThenBy(e =>
                    {
                        try
                        {
                            return e.Text.Length;
                        }
                        catch
                        {
                            return int.MaxValue;
                        }
                    })
                    .FirstOrDefault();
            });

        if (message == null)
        {
            throw new NoSuchElementException(
                "Availability message was not found.");
        }

        return message.Text.Trim();
    }

    // =========================================================
    // CONFIRM BUTTON
    // =========================================================

    public bool ConfirmSelectionEnabled()
    {
        IWebElement? button = null;

        try
        {
            button =
                _wait.Until(d =>
                    d.FindElements(
                        By.CssSelector(
                            ".vd-save-btn"))
                     .LastOrDefault(e =>
                     {
                         try
                         {
                             return e.Displayed;
                         }
                         catch
                         {
                             return false;
                         }
                     }));
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }

        return button != null &&
               button.Enabled &&
               !button.Text.Contains(
                   "Fully Booked",
                   StringComparison.OrdinalIgnoreCase);
    }
}