using System.Globalization;
using System.Text.Json;
using CeylonQuest.E2E.Tests.Provider.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace CeylonQuest.E2E.Tests.Provider.Pages;

internal sealed class ProviderReviewsPage(ProviderUi ui, ProviderDashboardPage dashboard)
{
    private JsonElement data;
    private string listingId = "";
    private string originUrl = "";
    public JsonElement[] Reviews => data.Items();
    private string Path => "/api/bookings/reviews/" + listingId;

    public void Open()
    {
        var before = ui.Sequence;
        dashboard.Navigate("reviews");
        ProviderUi.EqualText("Reviews & Ratings", ui.Browser.Visible(By.CssSelector(".pd-reviews-view h1")).Text);
        ProviderUi.EqualText("Customer Reviews & Ratings", ui.Browser.Visible(By.Id("provider-reviews-title")).Text);
        Assert.NotEmpty(dashboard.Listings); // A dropdown test needs real owned listings.
        var select = new SelectElement(ui.Browser.Visible(By.Id("provider-review-listing")));
        var expectedIds = dashboard.Listings.Select(item => item.Text("id")).Order().ToArray();
        Assert.Equal(expectedIds, select.Options.Select(option => option.GetDomAttribute("value")).Order().ToArray());
        foreach (var listing in dashboard.Listings)
        {
            var option = Assert.Single(select.Options, option => option.GetDomAttribute("value") == listing.Text("id"));
            ProviderUi.EqualText(listing.ListingName(), option.Text);
        }
        listingId = select.SelectedOption.GetDomAttribute("value")!;
        Load(before, 1);
    }

    public void Select(string id)
    {
        Assert.Contains(dashboard.Listings, item => item.Text("id") == id);
        var before = ui.Sequence;
        var changed = listingId != id;
        new SelectElement(ui.Browser.Visible(By.Id("provider-review-listing"))).SelectByValue(id);
        listingId = id;
        Load(changed ? before : -1, 1);
        Assert.Equal(id, new SelectElement(ui.Browser.Visible(By.Id("provider-review-listing"))).SelectedOption.GetDomAttribute("value"));
    }

    public void AssertReviews(bool requireReviews = false)
    {
        var reviews = Reviews;
        if (requireReviews) Assert.NotEmpty(reviews);
        Assert.InRange(data.GetProperty("averageRating").GetDouble(), 0, 5);
        Assert.True(data.Int("reviewCount") >= reviews.Length);
        Assert.True(data.Int("totalCount") >= reviews.Length);
        foreach (var review in reviews)
        {
            Assert.Equal(listingId, review.Text("listingId"));
            Assert.Equal(dashboard.ListingType, review.Text("bookingType"));
            Assert.InRange(review.Int("rating"), 1, 5);
        }
        var score = data.GetProperty("averageRating").GetDouble().ToString("F2", CultureInfo.InvariantCulture) + " / 5";
        ProviderUi.EqualText(score, ui.Browser.Visible(By.CssSelector(".pd-reviews-summary-score")).Text);
        Assert.Contains(data.Int("reviewCount") + " review", ui.Browser.Visible(By.CssSelector(".pd-reviews-summary-bar")).Text);
        ui.Browser.Until(d => d.FindElements(By.CssSelector(".pd-review-item")).Count == reviews.Length,
            "Rendered review count differs from the selected listing response.");
        var cards = ui.Browser.Driver.FindElements(By.CssSelector(".pd-review-item"));
        for (var index = 0; index < reviews.Length; index++) AssertCard(cards[index], reviews[index]);
        if (reviews.Length == 0)
            ui.Browser.Visible(By.XPath("//section[@aria-labelledby='provider-reviews-title']//p[normalize-space()='No customer reviews for this listing yet.']"));
    }

    public void AssertSwitchingIsolated(string firstId, string secondId)
    {
        Assert.NotEqual(firstId, secondId);
        Select(firstId);
        AssertReviews(true);
        var first = Reviews;
        ui.Browser.Capture("reviews-listing-a");
        Select(secondId);
        AssertReviews(true);
        var second = Reviews;
        Assert.Empty(first.Select(item => item.Text("id")).Intersect(second.Select(item => item.Text("id"))));
        // Full listing IDs and review IDs are checked in the real response; each
        // rendered card is also matched to comment, reviewer, rating and timestamp.
        // Identical legitimate comments across listings are not treated as leakage.
        ui.Browser.Capture("reviews-listing-b");
        Select(firstId);
        AssertReviews(true);
        Assert.Equal(first.Select(item => item.Text("id")), Reviews.Select(item => item.Text("id")));
    }

    public void AssertEmptyListing(string id)
    {
        Select(id);
        Assert.Equal(0, data.Int("totalCount"));
        Assert.Equal(0, data.Int("reviewCount"));
        Assert.Empty(Reviews);
        AssertReviews();
    }

    public void ToggleLocalReadStatus()
    {
        AssertReviews(true);
        var review = Reviews[0];
        var card = Card(review.Text("id"));
        card.FindElement(By.CssSelector(".pd-review-actions-bar .pd-btn-read-toggle")).Click();
        ui.Browser.Until(_ => Card(review.Text("id")).GetDomAttribute("class")!.Contains("pd-review-item--read"),
            "Review did not change to its local read state.");
        ProviderUi.EqualText("Read", Card(review.Text("id")).FindElement(By.CssSelector(".pd-read-status-badge")).Text, true);
        Card(review.Text("id")).FindElement(By.CssSelector(".pd-review-actions-bar .pd-btn-read-toggle")).Click();
        ProviderUi.EqualText("Unread", Card(review.Text("id")).FindElement(By.CssSelector(".pd-read-status-badge")).Text, true);
        ui.Browser.Step("Review read/unread toggles verified as component state only; no persistence claimed.");
    }

    public void RespondAndVerifyPersistence(string id)
    {
        FindReview(id);
        ProviderSettings.AssertWriteTarget(originUrl);
        var original = Assert.Single(Reviews, review => review.Text("id") == id).Text("providerResponse");
        var response = "Selenium dedicated review response " + Guid.NewGuid().ToString("N");
        try
        {
            SaveResponse(id, response);
            SaveResponse(id, response + " updated");
            var selected = listingId;
            dashboard.Navigate("overview");
            Open();
            Select(selected);
            FindReview(id);
            Assert.Equal(response + " updated", Assert.Single(Reviews, review => review.Text("id") == id).Text("providerResponse"));
            Assert.Equal(response + " updated", Card(id).FindElement(By.CssSelector(".cq-review-form textarea")).GetDomProperty("value"));
            ui.Browser.Capture("persisted-provider-response");
        }
        finally
        {
            // There is no response-delete operation. Restore an existing response;
            // an initially blank dedicated test review retains the test response.
            if (!string.IsNullOrWhiteSpace(original))
            {
                FindReview(id);
                SaveResponse(id, original);
                ui.Browser.Step("Restored the dedicated review's original provider response.");
            }
            else ui.Browser.Step("Dedicated test review " + id + " retains the response; no response-delete UI/API exists.");
        }
    }

    private void FindReview(string id)
    {
        while (!Reviews.Any(review => review.Text("id") == id) && data.Int("page") < data.Int("totalPages"))
        {
            var next = data.Int("page") + 1;
            var before = ui.Sequence;
            ui.ClickText("//section[@aria-labelledby='provider-reviews-title']", "Next");
            Load(before, next);
        }
        Assert.Contains(Reviews, review => review.Text("id") == id);
    }

    private void SaveResponse(string id, string text)
    {
        ProviderSettings.AssertWriteTarget(originUrl);
        var card = Card(id);
        var input = card.FindElement(By.CssSelector(".cq-review-form textarea"));
        input.SendKeys(Keys.Control + "a");
        input.SendKeys(text);
        var before = ui.Sequence;
        var page = data.Int("page");
        card.FindElement(By.CssSelector(".cq-review-form button[type='submit']")).Click();
        Load(before, page);
        Assert.Equal(text, Assert.Single(Reviews, review => review.Text("id") == id).Text("providerResponse"));
        Assert.Equal(text, Card(id).FindElement(By.CssSelector(".cq-review-form textarea")).GetDomProperty("value"));
    }

    private IWebElement Card(string id)
    {
        var index = Array.FindIndex(Reviews, review => review.Text("id") == id);
        Assert.True(index >= 0, "Configured review is not on the current page.");
        var card = ui.Browser.Until(d => {
            var cards = d.FindElements(By.CssSelector(".pd-review-item"));
            return cards.Count == Reviews.Length ? cards[index] : null!;
        }, "Review cards are not loaded.");
        AssertCard(card, Reviews[index]);
        return card;
    }

    private static void AssertCard(IWebElement card, JsonElement review)
    {
        ProviderUi.EqualText(review.Text("comment"), card.FindElement(By.CssSelector(".pd-review-comment")).Text);
        ProviderUi.EqualText(review.Text("reviewerDisplayName", "Customer"), card.FindElement(By.CssSelector(".pd-review-reviewer")).Text);
        ProviderUi.EqualText(review.Int("rating") + " / 5", card.FindElement(By.CssSelector(".pd-review-rating-score")).Text);
        Assert.Equal(DateTimeOffset.Parse(review.Text("createdAtUtc"), CultureInfo.InvariantCulture),
            DateTimeOffset.Parse(card.FindElement(By.TagName("time")).GetDomAttribute("datetime")!, CultureInfo.InvariantCulture));
        Assert.Equal(review.Text("providerResponse"), card.FindElement(By.CssSelector(".cq-review-form textarea")).GetDomProperty("value"));
    }

    private void Load(int after, int page)
    {
        var response = ui.Wait(Path, after, uri => ProviderUi.Query(uri, "bookingType") == dashboard.ListingType &&
            ProviderUi.Query(uri, "page") == page.ToString(CultureInfo.InvariantCulture));
        data = response.Body;
        originUrl = response.Url;
        Assert.Equal(page, data.Int("page"));
        ui.Browser.Until(d => !d.FindElements(By.CssSelector(".pd-reviews-card [role='status']")).Any(element => element.Displayed) &&
            d.FindElements(By.CssSelector(".pd-reviews-summary-bar")).Count == 1, "Selected listing reviews did not finish loading.");
        ui.AssertNoVisibleError(".pd-reviews-card");
    }
}
