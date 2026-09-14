using System.Text.Json;
using CeylonQuest.Tests.Pages;
using CeylonQuest.Tests.Utilities;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.Tests.Tests;

public class AvailabilityTests : BaseTest
{
    private sealed record ListingInfo(
        string Id,
        string Title,
        string ProviderEmail,
        DateTime? ValidFrom,
        DateTime? ValidUntil,
        string AvailableDays);

    private sealed record SlotState(
        string TimeSlot,
        int Total,
        int Remaining,
        bool FullyBooked);

    private sealed record PreparedSlot(
        AvailabilityPage Page,
        string ListingId,
        string ListingTitle,
        string Date,
        string TimeSlot,
        int Total,
        int Remaining);

    private string Env(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"{name} is not configured.");

        return value;
    }

    private string ListingTitle =>
        Env("QA_AVAILABILITY_LISTING_TITLE");

    private string ProviderEmail =>
        Environment.GetEnvironmentVariable(
            "CQ_APPROVED_PROVIDER_EMAIL")
        ?? Env("QA_PROVIDER_EMAIL");

    private string ProviderPassword =>
        Environment.GetEnvironmentVariable(
            "CQ_APPROVED_PROVIDER_PASSWORD")
        ?? Env("QA_PROVIDER_PASSWORD");

    private void ClearSession()
    {
        Driver.Navigate().GoToUrl(BaseUrl);

        ((IJavaScriptExecutor)Driver).ExecuteScript(@"
            window.localStorage.clear();
            window.sessionStorage.clear();
        ");
    }

    private void Login(
        string email,
        string password,
        string expectedNavId)
    {
        ClearSession();

        Driver.Navigate().GoToUrl(
            $"{BaseUrl}/login");

        var wait =
            new WebDriverWait(
                Driver,
                TimeSpan.FromSeconds(30));

        var emailInput =
            wait.Until(d =>
                d.FindElements(By.Id("login-email"))
                 .FirstOrDefault(e =>
                     e.Displayed && e.Enabled));

        if (emailInput == null)
            throw new NoSuchElementException(
                "Login email field was not found.");

        emailInput.Clear();
        emailInput.SendKeys(email);

        var passwordInput =
            wait.Until(d =>
                d.FindElements(By.Id("login-password"))
                 .FirstOrDefault(e =>
                     e.Displayed && e.Enabled));

        if (passwordInput == null)
            throw new NoSuchElementException(
                "Login password field was not found.");

        passwordInput.Clear();
        passwordInput.SendKeys(password);

        var loginButton =
            wait.Until(d =>
                d.FindElements(By.Id("login-button"))
                 .FirstOrDefault(e =>
                     e.Displayed && e.Enabled));

        if (loginButton == null)
            throw new NoSuchElementException(
                "Login button was not found.");

        loginButton.Click();

        wait.Until(d =>
            d.FindElements(By.Id(expectedNavId))
             .Any(e => e.Displayed));
    }

    private void LoginAsProvider() =>
        Login(
            ProviderEmail,
            ProviderPassword,
            "pd-nav-services");

    private void LoginAsVisitor() =>
        Login(
            Env("QA_VISITOR_EMAIL"),
            Env("QA_VISITOR_PASSWORD"),
            "nav-explore");

    private List<ListingInfo> GetActiveExperienceListings(
        AvailabilityPage page,
        string? search = null)
    {
        var path =
            "/api/catalog/activity-listings/public";

        if (!string.IsNullOrWhiteSpace(search))
        {
            path +=
                $"?search={Uri.EscapeDataString(search)}";
        }

        var result =
            page.BrowserApiRequest(
                "GET",
                path,
                null,
                false);

        if (result.Status != 200)
        {
            throw new InvalidOperationException(
                $"Could not load active public experience listings. " +
                $"HTTP {result.Status}: {result.Body}");
        }

        using var doc =
            JsonDocument.Parse(result.Body);

        if (doc.RootElement.ValueKind
            != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                "Public experience listings API did not return an array.");
        }

        var listings =
            new List<ListingInfo>();

        foreach (var item
                 in doc.RootElement.EnumerateArray())
        {
            var id =
                item.TryGetProperty("id", out var idProp)
                    ? idProp.GetString()
                    : null;

            var title =
                item.TryGetProperty(
                    "title",
                    out var titleProp)
                    ? titleProp.GetString()
                    : null;

            if (string.IsNullOrWhiteSpace(id)
                || string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var providerEmail =
                item.TryGetProperty(
                    "providerEmail",
                    out var emailProp)
                    ? emailProp.GetString()
                        ?? string.Empty
                    : string.Empty;

            DateTime? validFrom = null;
            if (item.TryGetProperty(
                    "validFrom",
                    out var fromProp)
                && fromProp.ValueKind
                    != JsonValueKind.Null
                && DateTime.TryParse(
                    fromProp.GetString(),
                    out var from))
            {
                validFrom = from;
            }

            DateTime? validUntil = null;
            if (item.TryGetProperty(
                    "validUntil",
                    out var untilProp)
                && untilProp.ValueKind
                    != JsonValueKind.Null
                && DateTime.TryParse(
                    untilProp.GetString(),
                    out var until))
            {
                validUntil = until;
            }

            var availableDays =
                item.TryGetProperty(
                    "availableDays",
                    out var daysProp)
                    && daysProp.ValueKind
                        != JsonValueKind.Null
                    ? daysProp.GetString()
                        ?? "Daily"
                    : "Daily";

            listings.Add(
                new ListingInfo(
                    id,
                    title,
                    providerEmail,
                    validFrom,
                    validUntil,
                    availableDays));
        }

        return listings;
    }

    private ListingInfo ResolveOwnActiveListing(
        AvailabilityPage page)
    {
        var expectedTitle =
            ListingTitle.Trim();

        var providerEmail =
            ProviderEmail;

        var matches =
            GetActiveExperienceListings(
                page,
                expectedTitle);

        var exactOwned =
            matches.FirstOrDefault(l =>
                l.Title.Trim().Equals(
                    expectedTitle,
                    StringComparison.OrdinalIgnoreCase)
                &&
                l.ProviderEmail.Equals(
                    providerEmail,
                    StringComparison.OrdinalIgnoreCase));

        if (exactOwned != null)
            return exactOwned;

        var anyOwned =
            GetActiveExperienceListings(page)
                .FirstOrDefault(l =>
                    l.ProviderEmail.Equals(
                        providerEmail,
                        StringComparison.OrdinalIgnoreCase));

        if (anyOwned != null)
            return anyOwned;

        throw new InvalidOperationException(
            $"No ACTIVE experience listing owned by " +
            $"'{providerEmail}' was found. " +
            $"Use an approved provider that owns an active experience listing.");
    }

    private ListingInfo ResolveOtherProviderListing(
        AvailabilityPage page,
        string ownListingId)
    {
        var ownProviderEmail =
            ProviderEmail;

        var configuredId =
            Environment.GetEnvironmentVariable(
                "QA_OTHER_PROVIDER_LISTING_ID");

        var all =
            GetActiveExperienceListings(page);

        if (!string.IsNullOrWhiteSpace(
                configuredId))
        {
            var configured =
                all.FirstOrDefault(l =>
                    l.Id.Equals(
                        configuredId,
                        StringComparison.OrdinalIgnoreCase));

            if (configured != null
                && !configured.Id.Equals(
                    ownListingId,
                    StringComparison.OrdinalIgnoreCase)
                && !configured.ProviderEmail.Equals(
                    ownProviderEmail,
                    StringComparison.OrdinalIgnoreCase))
            {
                return configured;
            }
        }

        var other =
            all.FirstOrDefault(l =>
                !l.Id.Equals(
                    ownListingId,
                    StringComparison.OrdinalIgnoreCase)
                &&
                !l.ProviderEmail.Equals(
                    ownProviderEmail,
                    StringComparison.OrdinalIgnoreCase));

        return other
               ?? throw new InvalidOperationException(
                   "TC60-12 needs an ACTIVE experience " +
                   "listing owned by another provider. " +
                   "Set QA_OTHER_PROVIDER_LISTING_ID to " +
                   "that listing ID.");
    }

    // Matches the schedule rules used by AvailabilityService.
    private static bool IsOperatingDate(
        DateTime date,
        ListingInfo listing)
    {
        if (listing.ValidFrom.HasValue
            && date.Date
                < listing.ValidFrom.Value.Date)
        {
            return false;
        }

        if (listing.ValidUntil.HasValue
            && date.Date
                > listing.ValidUntil.Value.Date)
        {
            return false;
        }

        var raw =
            listing.AvailableDays?.Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(raw)
            || raw.Equals(
                "Daily",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        raw = raw.ToLowerInvariant();

        if (raw.Contains("weekday")
            && date.DayOfWeek
                >= DayOfWeek.Monday
            && date.DayOfWeek
                <= DayOfWeek.Friday)
        {
            return true;
        }

        if (raw.Contains("weekend")
            && (date.DayOfWeek
                    == DayOfWeek.Saturday
                || date.DayOfWeek
                    == DayOfWeek.Sunday))
        {
            return true;
        }

        var day =
            date.DayOfWeek
                .ToString()
                .ToLowerInvariant();

        return raw.Contains(day)
               || raw.Contains(
                   day[..3]);
    }

    // IMPORTANT:
    // We use ONE genuine future operating date.
    // We do NOT search for 14 "clean" dates.
    // Existing booked quantity is handled separately by each test.
    private static string GetOperatingDate(
        ListingInfo listing)
    {
        var start =
            DateTime.Today.AddDays(1);

        if (listing.ValidFrom.HasValue
            && listing.ValidFrom.Value.Date
                > start.Date)
        {
            start =
                listing.ValidFrom.Value.Date;
        }

        var end =
            listing.ValidUntil?.Date
            ?? start.AddYears(1);

        for (var date = start.Date;
             date <= end;
             date = date.AddDays(1))
        {
            if (IsOperatingDate(
                    date,
                    listing))
            {
                return date.ToString(
                    "yyyy-MM-dd");
            }
        }

        throw new InvalidOperationException(
            $"The active listing '{listing.Title}' " +
            "has no future operating date inside its " +
            "ValidFrom/ValidUntil schedule.");
    }

    private static List<SlotState> ReadSlots(
        JsonDocument doc)
    {
        var result =
            new List<SlotState>();

        foreach (var slot
                 in doc.RootElement
                     .GetProperty("slots")
                     .EnumerateArray())
        {
            result.Add(
                new SlotState(
                    slot.GetProperty(
                            "timeSlot")
                        .GetString()
                    ?? string.Empty,
                    slot.GetProperty(
                            "totalCapacity")
                        .GetInt32(),
                    slot.GetProperty(
                            "remainingCapacity")
                        .GetInt32(),
                    slot.GetProperty(
                            "isFullyBooked")
                        .GetBoolean()));
        }

        return result;
    }

    private static int BookedCount(
        SlotState slot) =>
        Math.Max(
            0,
            slot.Total - slot.Remaining);

    // Makes the slot have exactly desiredRemaining spots,
    // even if an earlier test run already consumed capacity.
    private PreparedSlot PrepareProviderSlot(
        int desiredRemaining)
    {
        LoginAsProvider();

        var page =
            new AvailabilityPage(Driver);

        var listing =
            ResolveOwnActiveListing(page);

        var testDate =
            GetOperatingDate(listing);

        using var baseline =
            page.GetAvailability(
                listing.Id,
                testDate);

        Assert.True(
            baseline.RootElement
                .GetProperty("isOperatingDay")
                .GetBoolean(),
            $"Chosen test date {testDate} is not an operating day.");

        var first =
            ReadSlots(baseline).First();

        var alreadyBooked =
            BookedCount(first);

        var targetTotal =
            alreadyBooked
            + desiredRemaining;

        var set =
            page.SetCapacity(
                listing.Id,
                testDate,
                first.TimeSlot,
                targetTotal);

        Assert.Equal(
            200,
            set.Status);

        using var afterSet =
            page.GetAvailability(
                listing.Id,
                testDate);

        var prepared =
            ReadSlots(afterSet)
                .Single(s =>
                    s.TimeSlot.Equals(
                        first.TimeSlot,
                        StringComparison.OrdinalIgnoreCase));

        Assert.Equal(
            targetTotal,
            prepared.Total);

        Assert.Equal(
            desiredRemaining,
            prepared.Remaining);

        return new PreparedSlot(
            page,
            listing.Id,
            listing.Title,
            testDate,
            first.TimeSlot,
            prepared.Total,
            prepared.Remaining);
    }

    // TC60-01
    [Fact]
    public void TC60_01_SetValidSlotCapacity()
    {
        var prepared =
            PrepareProviderSlot(5);

        Assert.Equal(
            5,
            prepared.Remaining);

        Assert.True(
            prepared.Total >= 5);
    }

    // TC60-02
    [Fact]
    public void TC60_02_Visitor_CanSeeAvailableCapacity_InUI()
    {
        var prepared =
            PrepareProviderSlot(10);

        LoginAsVisitor();

        var page =
            new AvailabilityPage(Driver);

        page.OpenVisitorExplore();

        page.OpenAvailabilityForListingTitle(
            prepared.ListingTitle);

        page.SelectDate(
            prepared.Date);

        var message =
            page.AvailabilityMessage();

        Assert.Contains(
            "spots available",
            message,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "Fully Booked",
            message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "10",
            message,
            StringComparison.OrdinalIgnoreCase);

        Assert.True(
            page.ConfirmSelectionEnabled());
    }

    // TC60-03
    [Fact]
    public void TC60_03_UpdateExistingSlotCapacity()
    {
        var prepared =
            PrepareProviderSlot(5);

        using var before =
            prepared.Page.GetAvailability(
                prepared.ListingId,
                prepared.Date);

        var beforeState =
            ReadSlots(before)
                .Single(s =>
                    s.TimeSlot.Equals(
                        prepared.TimeSlot,
                        StringComparison.OrdinalIgnoreCase));

        var booked =
            BookedCount(beforeState);

        var newTotal =
            booked + 8;

        var update =
            prepared.Page.SetCapacity(
                prepared.ListingId,
                prepared.Date,
                prepared.TimeSlot,
                newTotal);

        Assert.Equal(
            200,
            update.Status);

        using var after =
            prepared.Page.GetAvailability(
                prepared.ListingId,
                prepared.Date);

        var state =
            ReadSlots(after)
                .Single(s =>
                    s.TimeSlot.Equals(
                        prepared.TimeSlot,
                        StringComparison.OrdinalIgnoreCase));

        Assert.Equal(
            newTotal,
            state.Total);

        Assert.Equal(
            8,
            state.Remaining);
    }

    // TC60-04
    [Fact]
    public void TC60_04_ZeroCapacity_IsRejected()
    {
        LoginAsProvider();

        var page =
            new AvailabilityPage(Driver);

        var listing =
            ResolveOwnActiveListing(page);

        var testDate =
            GetOperatingDate(listing);

        using var baseline =
            page.GetAvailability(
                listing.Id,
                testDate);

        var slot =
            ReadSlots(baseline)
                .First()
                .TimeSlot;

        var result =
            page.SetCapacity(
                listing.Id,
                testDate,
                slot,
                0);

        Assert.InRange(
            result.Status,
            400,
            499);
    }

    // TC60-05
    [Fact]
    public void TC60_05_NegativeCapacity_IsRejected()
    {
        LoginAsProvider();

        var page =
            new AvailabilityPage(Driver);

        var listing =
            ResolveOwnActiveListing(page);

        var testDate =
            GetOperatingDate(listing);

        using var baseline =
            page.GetAvailability(
                listing.Id,
                testDate);

        var slot =
            ReadSlots(baseline)
                .First()
                .TimeSlot;

        var result =
            page.SetCapacity(
                listing.Id,
                testDate,
                slot,
                -2);

        Assert.InRange(
            result.Status,
            400,
            499);
    }

    // TC60-06
    [Fact]
    public void TC60_06_FullyBookedState_IsShown_InVisitorUI()
    {
        var prepared =
            PrepareProviderSlot(3);

        var simulate =
            prepared.Page.SimulateBooking(
                prepared.ListingId,
                prepared.Date,
                prepared.TimeSlot,
                3);

        Assert.Equal(
            200,
            simulate.Status);

        using var after =
            prepared.Page.GetAvailability(
                prepared.ListingId,
                prepared.Date);

        var state =
            ReadSlots(after)
                .Single(s =>
                    s.TimeSlot.Equals(
                        prepared.TimeSlot,
                        StringComparison.OrdinalIgnoreCase));

        Assert.Equal(
            0,
            state.Remaining);

        Assert.True(
            state.FullyBooked);

        LoginAsVisitor();

        var visitorPage =
            new AvailabilityPage(Driver);

        visitorPage.OpenVisitorExplore();

        visitorPage.OpenAvailabilityForListingTitle(
            prepared.ListingTitle);

        visitorPage.SelectDate(
            prepared.Date);

        var message =
            visitorPage.AvailabilityMessage();

        Assert.Contains(
            "Fully Booked",
            message,
            StringComparison.OrdinalIgnoreCase);

        Assert.False(
            visitorPage.ConfirmSelectionEnabled());
    }

    // TC60-07
    [Fact]
    public void TC60_07_InvalidNonNumericCapacity_IsRejected()
    {
        LoginAsProvider();

        var page =
            new AvailabilityPage(Driver);

        var listing =
            ResolveOwnActiveListing(page);

        var testDate =
            GetOperatingDate(listing);

        using var baseline =
            page.GetAvailability(
                listing.Id,
                testDate);

        var slot =
            ReadSlots(baseline)
                .First()
                .TimeSlot
                .Replace(
                    "\"",
                    "\\\"");

        var raw =
            $"{{\"date\":\"{testDate}\"," +
            $"\"timeSlot\":\"{slot}\"," +
            $"\"capacity\":\"abc\"}}";

        var result =
            page.SetRawCapacityPayload(
                listing.Id,
                raw);

        Assert.InRange(
            result.Status,
            400,
            499);
    }

    // TC60-08
    [Fact]
    public void TC60_08_AvailabilityOverride_PersistsAfterRefresh()
    {
        var prepared =
            PrepareProviderSlot(7);

        using var before =
            prepared.Page.GetAvailability(
                prepared.ListingId,
                prepared.Date);

        var beforeState =
            ReadSlots(before)
                .Single(s =>
                    s.TimeSlot.Equals(
                        prepared.TimeSlot,
                        StringComparison.OrdinalIgnoreCase));

        Assert.Equal(
            7,
            beforeState.Remaining);

        Driver.Navigate().Refresh();

        using var after =
            prepared.Page.GetAvailability(
                prepared.ListingId,
                prepared.Date);

        var afterState =
            ReadSlots(after)
                .Single(s =>
                    s.TimeSlot.Equals(
                        prepared.TimeSlot,
                        StringComparison.OrdinalIgnoreCase));

        Assert.Equal(
            beforeState.Total,
            afterState.Total);

        Assert.Equal(
            7,
            afterState.Remaining);
    }

    // TC60-09
    [Fact]
    public void TC60_09_DifferentTimeSlots_KeepIndependentCapacities()
    {
        LoginAsProvider();

        var page =
            new AvailabilityPage(Driver);

        var providerEmail =
            ProviderEmail;

        var candidates =
            GetActiveExperienceListings(page)
                .Where(l =>
                    l.ProviderEmail.Equals(
                        providerEmail,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        ListingInfo? selectedListing = null;
        string? testDate = null;
        List<SlotState>? baselineSlots = null;

        foreach (var candidate
                 in candidates)
        {
            string candidateDate;

            try
            {
                candidateDate =
                    GetOperatingDate(candidate);
            }
            catch
            {
                continue;
            }

            using var availability =
                page.GetAvailability(
                    candidate.Id,
                    candidateDate);

            var slots =
                ReadSlots(availability);

            if (slots.Count >= 2)
            {
                selectedListing =
                    candidate;

                testDate =
                    candidateDate;

                baselineSlots =
                    slots;

                break;
            }
        }

        Assert.NotNull(
            selectedListing);

        Assert.NotNull(
            testDate);

        Assert.NotNull(
            baselineSlots);

        var slotA =
            baselineSlots![0];

        var slotB =
            baselineSlots[1];

        var targetA =
            BookedCount(slotA) + 4;

        var targetB =
            BookedCount(slotB) + 9;

        var setA =
            page.SetCapacity(
                selectedListing!.Id,
                testDate!,
                slotA.TimeSlot,
                targetA);

        var setB =
            page.SetCapacity(
                selectedListing.Id,
                testDate!,
                slotB.TimeSlot,
                targetB);

        Assert.Equal(
            200,
            setA.Status);

        Assert.Equal(
            200,
            setB.Status);

        using var after =
            page.GetAvailability(
                selectedListing.Id,
                testDate!);

        var states =
            ReadSlots(after);

        var stateA =
            states.Single(s =>
                s.TimeSlot.Equals(
                    slotA.TimeSlot,
                    StringComparison.OrdinalIgnoreCase));

        var stateB =
            states.Single(s =>
                s.TimeSlot.Equals(
                    slotB.TimeSlot,
                    StringComparison.OrdinalIgnoreCase));

        Assert.Equal(
            targetA,
            stateA.Total);

        Assert.Equal(
            4,
            stateA.Remaining);

        Assert.Equal(
            targetB,
            stateB.Total);

        Assert.Equal(
            9,
            stateB.Remaining);
    }

    // TC60-10
    [Fact]
    public void TC60_10_RemainingCapacity_IsNotMarkedFullyBooked()
    {
        var prepared =
            PrepareProviderSlot(5);

        var simulate =
            prepared.Page.SimulateBooking(
                prepared.ListingId,
                prepared.Date,
                prepared.TimeSlot,
                2);

        Assert.Equal(
            200,
            simulate.Status);

        using var after =
            prepared.Page.GetAvailability(
                prepared.ListingId,
                prepared.Date);

        var state =
            ReadSlots(after)
                .Single(s =>
                    s.TimeSlot.Equals(
                        prepared.TimeSlot,
                        StringComparison.OrdinalIgnoreCase));

        Assert.Equal(
            3,
            state.Remaining);

        Assert.False(
            state.FullyBooked);

        LoginAsVisitor();

        var visitorPage =
            new AvailabilityPage(Driver);

        visitorPage.OpenVisitorExplore();

        visitorPage.OpenAvailabilityForListingTitle(
            prepared.ListingTitle);

        visitorPage.SelectDate(
            prepared.Date);

        var message =
            visitorPage.AvailabilityMessage();

        Assert.Contains(
            "spots available",
            message,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "Fully Booked",
            message,
            StringComparison.OrdinalIgnoreCase);

        Assert.True(
            visitorPage.ConfirmSelectionEnabled());
    }

    // TC60-11
    [Fact]
    public void TC60_11_Visitor_CannotModifyCapacity()
    {
        LoginAsVisitor();

        var page =
            new AvailabilityPage(Driver);

        var listing =
            ResolveOwnActiveListing(page);

        var testDate =
            GetOperatingDate(listing);

        using var baseline =
            page.GetAvailability(
                listing.Id,
                testDate);

        var first =
            ReadSlots(baseline).First();

        var result =
            page.SetCapacity(
                listing.Id,
                testDate,
                first.TimeSlot,
                first.Total + 1);

        Assert.True(
            result.Status is 401 or 403,
            $"Expected 401/403 but got " +
            $"{result.Status}. Body: {result.Body}");
    }

    // TC60-12
    // Requirement: one provider must NOT be able to change
    // another provider's listing availability.
    [Fact]
    public void TC60_12_Provider_CannotModifyAnotherProvidersAvailability()
    {
        LoginAsProvider();

        var page =
            new AvailabilityPage(Driver);

        var own =
            ResolveOwnActiveListing(page);

        var other =
            ResolveOtherProviderListing(
                page,
                own.Id);

        var testDate =
            GetOperatingDate(other);

        using var baseline =
            page.GetAvailability(
                other.Id,
                testDate);

        var first =
            ReadSlots(baseline).First();

        var result =
            page.SetCapacity(
                other.Id,
                testDate,
                first.TimeSlot,
                first.Total + 3);

        Assert.True(
            result.Status is 401 or 403 or 404,
            $"SECURITY DEFECT: Provider changed " +
            $"another provider's availability. " +
            $"HTTP {result.Status}: {result.Body}");
    }

    // TC60-13
    // Requirement: a provider should not create/update
    // availability for a date in the past.
    [Fact]
    public void TC60_13_PastDateAvailability_IsRejected()
    {
        LoginAsProvider();

        var page =
            new AvailabilityPage(Driver);

        var listing =
            ResolveOwnActiveListing(page);

        // Use a date safely in the past for both local time
        // and UTC-based backend validation.
        var pastDate =
            DateTime.UtcNow
                .AddDays(-2)
                .ToString("yyyy-MM-dd");

        using var baseline =
            page.GetAvailability(
                listing.Id,
                pastDate);

        var slot =
            ReadSlots(baseline)
                .First()
                .TimeSlot;

        var result =
            page.SetCapacity(
                listing.Id,
                pastDate,
                slot,
                5);

        Assert.InRange(
            result.Status,
            400,
            499);
    }

    // TC60-14
    [Fact]
    public void TC60_14_DecimalCapacity_IsRejected()
    {
        LoginAsProvider();

        var page =
            new AvailabilityPage(Driver);

        var listing =
            ResolveOwnActiveListing(page);

        var testDate =
            GetOperatingDate(listing);

        using var baseline =
            page.GetAvailability(
                listing.Id,
                testDate);

        var slot =
            ReadSlots(baseline)
                .First()
                .TimeSlot
                .Replace(
                    "\"",
                    "\\\"");

        var raw =
            $"{{\"date\":\"{testDate}\"," +
            $"\"timeSlot\":\"{slot}\"," +
            $"\"capacity\":2.5}}";

        var result =
            page.SetRawCapacityPayload(
                listing.Id,
                raw);

        Assert.InRange(
            result.Status,
            400,
            499);
    }

    // TC60-15
    // Requirement: total capacity must not be reduced
    // below the number of places already consumed/booked.
    [Fact]
    public void TC60_15_CannotReduceCapacityBelowAlreadyConsumedQuantity()
    {
        var prepared =
            PrepareProviderSlot(5);

        var simulate =
            prepared.Page.SimulateBooking(
                prepared.ListingId,
                prepared.Date,
                prepared.TimeSlot,
                4);

        Assert.Equal(
            200,
            simulate.Status);

        using var afterBooking =
            prepared.Page.GetAvailability(
                prepared.ListingId,
                prepared.Date);

        var state =
            ReadSlots(afterBooking)
                .Single(s =>
                    s.TimeSlot.Equals(
                        prepared.TimeSlot,
                        StringComparison.OrdinalIgnoreCase));

        Assert.Equal(
            1,
            state.Remaining);

        var consumed =
            state.Total
            - state.Remaining;

        Assert.True(
            consumed >= 4);

        // Positive but smaller than the already-consumed amount.
        var invalidNewCapacity =
            consumed - 1;

        var reduce =
            prepared.Page.SetCapacity(
                prepared.ListingId,
                prepared.Date,
                prepared.TimeSlot,
                invalidNewCapacity);

        Assert.InRange(
            reduce.Status,
            400,
            499);
    }
}

