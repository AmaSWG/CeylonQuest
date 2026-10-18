using System.Runtime.CompilerServices;
using CeylonQuest.E2E.Tests.Visitor.Infrastructure;
using CeylonQuest.E2E.Tests.Visitor.Pages;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Visitor.Tests;

public sealed class VisitorCatalogAvailabilityTests(ITestOutputHelper output) : VisitorCatalogTestBase(output)
{
    [CatalogAvailabilityFact("Experience"), Trait("CaseId", "VC_015")]
    public void Experience_ValidDateAndParticipants_DisplayAvailableSlots() => AvailabilityJourney("Experience");

    [CatalogAvailabilityFact("Restaurant"), Trait("CaseId", "VC_016")]
    public void Restaurant_ValidFutureReservation_DisplaysAvailableSeats() => AvailabilityJourney("Restaurant");

    [CatalogAvailabilityFact("Accommodation"), Trait("CaseId", "VC_017")]
    public void Accommodation_ValidStay_DisplaysAvailableRoomAndGuestCapacity() => AvailabilityJourney("Accommodation");

    [CatalogAvailabilityFact("Experience"), Trait("CaseId", "VC_018")]
    public void CardBookNow_OpensAvailabilityForTheSelectedListing() => AvailabilityJourney("Experience", fromCard: true);

    private void AvailabilityJourney(string type, bool fromCard = false, [CallerMemberName] string name = "AvailabilityJourney") => Run((ui, explore) => {
        var listing = explore.Choose(type);
        VisitorAvailabilityPage availability;
        if (fromCard) availability = explore.OpenAvailabilityFromCard(listing);
        else
        {
            var details = explore.OpenDetails(listing);
            details.AssertDetails();
            availability = details.OpenAvailability();
        }
        var filterValues = explore.FilterValues;
        var search = explore.SearchTerm;
        var page = explore.ActivePage;
        availability.PrepareAndAssertValidSelection();
        availability.Close();
        Assert.Equal(filterValues, explore.FilterValues);
        Assert.Equal(search, explore.SearchTerm);
        explore.AssertPage(VisitorExplorePage.Newest(explore.Listings.Where(item => item.Type == type && item.Matches(listing.Title))), page);
        ui.Browser.Step("Returned to the same filtered catalog after closing availability.");
    }, name);
}
