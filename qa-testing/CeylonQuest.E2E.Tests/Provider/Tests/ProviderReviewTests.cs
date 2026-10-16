using CeylonQuest.E2E.Tests.Provider.Infrastructure;
using CeylonQuest.E2E.Tests.Provider.Pages;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Provider.Tests;

[Trait("Workflow", "Provider")]
public sealed class ProviderReviewTests(ITestOutputHelper output) : ProviderTestBase(output)
{
    [ProviderFact("CQ_E2E_PROVIDER_LISTING_ID")]
    [Trait("Safety", "ReadOnly")]
    [Trait("CaseId", "PROV-REV-01")]
    public void ReviewsPage_DropdownMatchesOnlyProvidersOwnListings() => Run((ui, dashboard) => {
        var reviews = new ProviderReviewsPage(ui, dashboard);
        reviews.Open();
        reviews.Select(ProviderSettings.Id("CQ_E2E_PROVIDER_LISTING_ID"));
        reviews.AssertReviews();
    });

    [ProviderFact("CQ_E2E_PROVIDER_REVIEW_LISTING_A")]
    [Trait("Safety", "ReadOnly")]
    [Trait("CaseId", "PROV-REV-02")]
    public void SelectedListing_DisplaysAssociatedCustomerReviewsAndRatings() => Run((ui, dashboard) => {
        var reviews = new ProviderReviewsPage(ui, dashboard);
        reviews.Open();
        reviews.Select(ProviderSettings.Id("CQ_E2E_PROVIDER_REVIEW_LISTING_A"));
        reviews.AssertReviews(true);
    });

    [ProviderFact("CQ_E2E_PROVIDER_REVIEW_LISTING_A", "CQ_E2E_PROVIDER_REVIEW_LISTING_B")]
    [Trait("Safety", "ReadOnly")]
    [Trait("CaseId", "PROV-REV-03")]
    public void SwitchingListings_UpdatesContentAndExcludesOtherListingReviews() => Run((ui, dashboard) => {
        var reviews = new ProviderReviewsPage(ui, dashboard);
        reviews.Open();
        reviews.AssertSwitchingIsolated(ProviderSettings.Id("CQ_E2E_PROVIDER_REVIEW_LISTING_A"),
            ProviderSettings.Id("CQ_E2E_PROVIDER_REVIEW_LISTING_B"));
    });

    [ProviderFact("CQ_E2E_PROVIDER_EMPTY_REVIEW_LISTING")]
    [Trait("Safety", "ReadOnly")]
    [Trait("CaseId", "PROV-REV-04")]
    public void ListingWithoutReviews_DisplaysExactEmptyMessage() => Run((ui, dashboard) => {
        var reviews = new ProviderReviewsPage(ui, dashboard);
        reviews.Open();
        reviews.AssertEmptyListing(ProviderSettings.Id("CQ_E2E_PROVIDER_EMPTY_REVIEW_LISTING"));
    });

    [ProviderFact("CQ_E2E_PROVIDER_REVIEW_LISTING_A")]
    [Trait("Safety", "ReadOnly")]
    [Trait("CaseId", "PROV-REV-05")]
    public void ReviewReadToggle_ChangesOnlyLocalReadState() => Run((ui, dashboard) => {
        var reviews = new ProviderReviewsPage(ui, dashboard);
        reviews.Open();
        reviews.Select(ProviderSettings.Id("CQ_E2E_PROVIDER_REVIEW_LISTING_A"));
        reviews.ToggleLocalReadStatus();
    });

    [ProviderWriteFact("CQ_E2E_PROVIDER_REVIEW_LISTING_A", "CQ_E2E_PROVIDER_RESPONSE_REVIEW_ID")]
    [Trait("Safety", "Writes")]
    [Trait("CaseId", "PROV-REV-06")]
    public void DedicatedReview_ResponseAndUpdatePersist() => Run((ui, dashboard) => {
        var reviews = new ProviderReviewsPage(ui, dashboard);
        reviews.Open();
        reviews.Select(ProviderSettings.Id("CQ_E2E_PROVIDER_REVIEW_LISTING_A"));
        reviews.RespondAndVerifyPersistence(ProviderSettings.Id("CQ_E2E_PROVIDER_RESPONSE_REVIEW_ID"));
    }, allowsWrites: true);
}
