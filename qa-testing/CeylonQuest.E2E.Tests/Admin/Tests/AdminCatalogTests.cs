using CeylonQuest.E2E.Tests.Admin.Infrastructure;
using CeylonQuest.E2E.Tests.Admin.Pages;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Admin.Tests;

public sealed class AdminCatalogTests(ITestOutputHelper output) : AdminTestBase(output)
{
    [AdminFact, Trait("CaseId", "AC_001")]
    public void ValidAdminLogin_LoadsCatalogApplicationsAndNavigation() => Run((ui, dashboard) => {
        Assert.NotNull(dashboard.Applications);
        ui.Browser.Step("Admin role/session and catalog application GET verified after valid login.");
    });

    [AdminFact, Trait("CaseId", "AC_002")]
    public void Applications_DisplayCatalogBusinessContactCategoryAndStatus() => Run((ui, dashboard) =>
        new AdminApplicationsPage(ui, dashboard).Open());

    [AdminTheory, Trait("CaseId", "AC_003")]
    [InlineData("businessName")]
    [InlineData("email")]
    public void ApplicationSearch_FindsExistingRecordsAndClears(string property) => Run((ui, dashboard) => {
        var page = new AdminApplicationsPage(ui, dashboard);
        page.Open();
        page.SearchAndClear(property);
    });

    [AdminTheory, Trait("CaseId", "AC_004")]
    [InlineData("All")]
    [InlineData("Pending")]
    [InlineData("Approved")]
    [InlineData("Rejected")]
    public void ApplicationStatus_DisplaysMatchingRowsOrExpectedEmptyState(string status) => Run((ui, dashboard) => {
        var page = new AdminApplicationsPage(ui, dashboard);
        page.Open();
        page.AssertStatus(status);
    });

    [AdminFact, Trait("CaseId", "AC_005")]
    public void ApplicationDetails_DisplaySelectedBusinessAndDocuments() => Run((ui, dashboard) => {
        var page = new AdminApplicationsPage(ui, dashboard);
        page.Open();
        page.OpenReview(page.Choose());
        page.CloseReview();
    });

    [AdminFact, Trait("CaseId", "AC_006")]
    public void VerificationDocument_DownloadsExpectedNonemptyFile() => Run((ui, dashboard) => {
        var page = new AdminApplicationsPage(ui, dashboard);
        page.Open();
        page.DownloadDocument(page.Choose("document"));
    });

    [AdminFact, Trait("CaseId", "AC_007")]
    public void Approval_CancelRetainsPendingApplication() => Run((ui, dashboard) => {
        var page = new AdminApplicationsPage(ui, dashboard);
        page.Open();
        page.CancelApproval(page.Choose("pending"));
    });

    [AdminFact, Trait("CaseId", "AC_008")]
    public void Rejection_ValidReasonCanceledWithoutSubmitting() => Run((ui, dashboard) => {
        var page = new AdminApplicationsPage(ui, dashboard);
        page.Open();
        page.CancelRejection(page.Choose("pending"));
    });

    [AdminFact, Trait("CaseId", "AC_009")]
    public void Listings_DisplayCatalogIdentitiesProvidersCategoriesAndStatuses() => Run((ui, dashboard) =>
        new AdminCatalogListingsPage(ui, dashboard).Open());

    [AdminTheory, Trait("CaseId", "AC_010")]
    [InlineData("serviceName")]
    [InlineData("providerName")]
    [InlineData("id")]
    public void ListingSearch_MatchesNameProviderOrIdAndClears(string property) => Run((ui, dashboard) => {
        var page = new AdminCatalogListingsPage(ui, dashboard);
        page.Open();
        page.SearchAndClear(property);
    });

    [AdminTheory, Trait("CaseId", "AC_011")]
    [InlineData("Experience")]
    [InlineData("Restaurant")]
    [InlineData("Accommodation")]
    public void ListingCategory_DisplaysMatchingCatalogRows(string category) => Run((ui, dashboard) => {
        var page = new AdminCatalogListingsPage(ui, dashboard);
        page.Open();
        page.Category(category);
        page.Reset();
    });

    [AdminTheory, Trait("CaseId", "AC_012")]
    [InlineData("all")]
    [InlineData("Active")]
    [InlineData("Inactive")]
    [InlineData("Removed")]
    public void ListingStatus_DisplaysMatchingCatalogRowsOrEmptyState(string status) => Run((ui, dashboard) => {
        var page = new AdminCatalogListingsPage(ui, dashboard);
        page.Open();
        page.FilterStatus(status);
    });

    [AdminFact, Trait("CaseId", "AC_013")]
    public void ListingFilters_CombineCategoryStatusAndSearch() => Run((ui, dashboard) => {
        var page = new AdminCatalogListingsPage(ui, dashboard);
        page.Open();
        page.CombinedFilters();
    });

    [AdminFact, Trait("CaseId", "AC_014")]
    public void ListingRemoval_ValidReasonCanceledKeepsActiveListing() => Run((ui, dashboard) => {
        var page = new AdminCatalogListingsPage(ui, dashboard);
        page.Open();
        page.CancelRemoval(page.Choose(active: true));
    });

    [AdminFact, Trait("CaseId", "AC_015")]
    public void InventoryReport_DisplaysCatalogSummaryCategoryAndLocationData() => Run((ui, dashboard) =>
        new AdminInventoryPage(ui, dashboard).Open());

    [AdminTheory, Trait("CaseId", "AC_016")]
    [InlineData("dates")]
    [InlineData("category")]
    [InlineData("location")]
    public void InventoryFilters_ApplyValidCriteriaAndReset(string kind) => Run((ui, dashboard) => {
        var page = new AdminInventoryPage(ui, dashboard);
        page.Open();
        page.FiltersAndReset(kind);
    });

    [AdminFact, Trait("CaseId", "AC_017")]
    public void CatalogJourney_CanceledActionsRetainApplicationAndListingState() => Run((ui, dashboard) => {
        var applications = new AdminApplicationsPage(ui, dashboard);
        applications.Open();
        var pending = applications.Choose("pending");
        applications.CancelApproval(pending);
        applications.CancelRejection(pending);
        var listings = new AdminCatalogListingsPage(ui, dashboard);
        listings.Open();
        var original = listings.Records.ToArray();
        listings.CancelRemoval(listings.Choose(active: true));
        new AdminInventoryPage(ui, dashboard).Open();
        applications.Open();
        applications.OpenReview(pending);
        applications.CloseReview();
        listings.Open();
        listings.AssertSameSnapshot(original);
        ui.Browser.Step("Catalog journey retained the pending application UI state and fresh listing GET state after canceled actions.");
    });
}
