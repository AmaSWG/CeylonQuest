using CeylonQuest.E2E.Tests.Provider.Infrastructure;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Provider.Tests;

public sealed class ProviderCatalogTests(ITestOutputHelper output) : ProviderTestBase(output)
{
    [ProviderFact, Trait("CaseId", "PC_001")]
    public void ValidProviderLogin_LoadsCatalogProfileAndOwnListings() => Run((ui, dashboard, _) => {
        Assert.False(string.IsNullOrWhiteSpace(dashboard.Profile.Text("id")));
        Assert.StartsWith("/api/catalog/", dashboard.CatalogPath);
        Assert.NotNull(dashboard.Listings);
        ui.Browser.Step("Provider role, dashboard, catalog profile and category endpoint verified after valid login.");
    });

    [ProviderFact, Trait("CaseId", "PC_002")]
    public void BusinessProfile_DisplaysReturnedCatalogFields() => Run((_, dashboard, _) => dashboard.AssertBusinessProfile());

    [ProviderFact, Trait("CaseId", "PC_003")]
    public void Overview_ActiveAndTotalListingCountsMatchCatalog() => Run((_, dashboard, _) => dashboard.AssertListingSummary());

    [ProviderFact, Trait("CaseId", "PC_004")]
    public void ListingManagement_DisplaysCategorySpecificOwnedRows() => Run((_, _, listings) => listings.Open());

    [ProviderFact, Trait("CaseId", "PC_005")]
    public void ListingSearch_FindsOwnedListingAndClearsResults() => Run((_, dashboard, listings) => {
        listings.Open();
        listings.AssertSearchAndClear(dashboard.ChooseListing());
    });

    [ProviderTheory, Trait("CaseId", "PC_006")]
    [InlineData("all")]
    [InlineData("active")]
    [InlineData("inactive")]
    public void StatusFilter_DisplaysMatchingRowsOrExpectedEmptyState(string status) => Run((_, _, listings) => {
        listings.Open();
        listings.AssertStatus(status);
    });

    [ProviderFact, Trait("CaseId", "PC_007")]
    public void AddListing_ValidDraftIsDiscardedWhenCanceled() => Run((_, _, listings) => {
        listings.Open();
        listings.PrepareValidDraftAndCancel();
    });

    [ProviderFact, Trait("CaseId", "PC_008")]
    public void EditListing_PopulatesSavedCategorySpecificValues() => Run((_, dashboard, listings) => {
        listings.Open();
        var item = dashboard.ChooseListing();
        listings.OpenEdit(item);
        listings.AssertEditValues(item);
        listings.CancelForm();
        listings.AssertRows(dashboard.Listings);
    });

    [ProviderFact, Trait("CaseId", "PC_009")]
    public void EditListing_CancelRestoresOriginalValuesOnReopen() => Run((_, dashboard, listings) => {
        listings.Open();
        listings.ChangeAndCancelEdit(dashboard.ChooseListing());
    });

    [ProviderFact, Trait("CaseId", "PC_010")]
    public void DeleteListing_CancelKeepsTheOwnedListing() => Run((_, dashboard, listings) => {
        listings.Open();
        listings.CancelDelete(dashboard.ChooseListing());
    });

    [ProviderFact, Trait("CaseId", "PC_011")]
    public void CatalogJourney_CanceledActionsRetainListingsAfterNavigation() => Run((ui, dashboard, listings) => {
        listings.Open();
        var item = dashboard.ChooseListing();
        listings.AssertSearchAndClear(item);
        listings.PrepareValidDraftAndCancel();
        listings.ChangeAndCancelEdit(item);
        listings.CancelDelete(item);
        dashboard.AssertBusinessProfile();
        listings.Open();
        Assert.Equal("", ui.Browser.Visible(By.CssSelector(".pd-search-input")).GetDomProperty("value"));
        listings.OpenEdit(item);
        listings.AssertEditValues(item);
        listings.CancelForm();
        dashboard.AssertListingSummary();
        ui.Browser.Step("Catalog journey retained original rows and form values after cancellation and tab navigation.");
    });
}
