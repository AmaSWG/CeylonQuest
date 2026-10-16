using CeylonQuest.E2E.Tests.Provider.Infrastructure;
using CeylonQuest.E2E.Tests.Provider.Pages;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Provider.Tests;

[Trait("Workflow", "Provider")]
public sealed class ProviderListingTests(ITestOutputHelper output) : ProviderTestBase(output)
{
    [ProviderFact("CQ_E2E_PROVIDER_LISTING_ID")]
    [Trait("Safety", "ReadOnly")]
    [Trait("CaseId", "PROV-LIST-01")]
    public void Listings_SearchDisplaysConfiguredOwnedListing() => Run((ui, dashboard) => {
        var listings = new ProviderListingsPage(ui, dashboard);
        listings.Open();
        listings.AssertOwnedListing(ProviderSettings.Id("CQ_E2E_PROVIDER_LISTING_ID"));
    });

    [ProviderFact]
    [Trait("Safety", "ReadOnly")]
    [Trait("CaseId", "PROV-LIST-02")]
    public void EmptyListingForm_DisplaysRequiredFieldError() => Run((ui, dashboard) => {
        var listings = new ProviderListingsPage(ui, dashboard);
        listings.Open();
        listings.AssertRequiredFieldValidation();
    });

    [ProviderWriteFact]
    [Trait("Safety", "Writes")]
    [Trait("CaseId", "PROV-LIST-03")]
    public void DedicatedListing_CreateEditDeleteAndCleanup() => Run((ui, dashboard) => {
        var listings = new ProviderListingsPage(ui, dashboard);
        listings.Open();
        listings.ExerciseOwnedListingLifecycle();
    }, allowsWrites: true);
}
