# Admin Provider Catalog E2E tests

This is Admin's with the approved Provider Catalog-only
workflow: **17 scenario methods / 30 configured cases**. Every case starts with
valid Admin login through the actual UI. 


## Enter environment variables in PowerShell

From the repository root, use the same terminal for configuration and execution:

    $env:CQ_E2E_ADMIN_EMAIL = Read-Host 'Valid admin email'
    $adminCatalogPassword = Read-Host 'Valid admin password' -AsSecureString
    try {
        $env:CQ_E2E_ADMIN_PASSWORD = [System.Net.NetworkCredential]::new('', $adminCatalogPassword).Password
    }
    finally {
        $adminCatalogPassword.Dispose()
    }

These are the only mandatory environment variables. 


## Run

All current Admin catalog tests:

    .\qa-testing\CeylonQuest.E2E.Tests\Admin\Run-AdminTests.ps1

Watch Chrome:

    .\qa-testing\CeylonQuest.E2E.Tests\Admin\Run-AdminTests.ps1 -ShowBrowser

Run valid login or one scenario:

    .\qa-testing\CeylonQuest.E2E.Tests\Admin\Run-AdminTests.ps1 -CaseId AC_001
    .\qa-testing\CeylonQuest.E2E.Tests\Admin\Run-AdminTests.ps1 -CaseId AC_014

Discover without opening a browser or entering credentials:

    .\qa-testing\CeylonQuest.E2E.Tests\Admin\Run-AdminTests.ps1 -ListTests

Use another already-running frontend:

    .\qa-testing\CeylonQuest.E2E.Tests\Admin\Run-AdminTests.ps1 -BaseUrl 'http://localhost:5173'

Default target is the existing deployed Azure frontend from the shared helper.
The runner filters both the Admin namespace and Category=AdminCatalog;


## Optional fixture selection

Automatic selection usually requires no additional configuration. To choose
specific existing records instead, set only the options you need:

    $env:CQ_E2E_ADMIN_CATALOG_APPLICATION_ID = Read-Host 'Catalog application GUID for details/search'
    $env:CQ_E2E_ADMIN_CATALOG_PENDING_APPLICATION_ID = Read-Host 'Existing Pending application GUID'
    $env:CQ_E2E_ADMIN_CATALOG_DOCUMENT_APPLICATION_ID = Read-Host 'Application GUID with a valid verification document'
    $env:CQ_E2E_ADMIN_CATALOG_DOCUMENT_INDEX = '0'
    $env:CQ_E2E_ADMIN_CATALOG_LISTING_ID = Read-Host 'Existing Active catalog listing GUID'
    $env:CQ_E2E_ADMIN_CATALOG_LISTING_TYPE = Read-Host 'Experience, Restaurant or Accommodation'
    $env:CQ_E2E_ADMIN_CATALOG_LOCATION = Read-Host 'Existing inventory location'

These are Catalog IDs, not Identity user IDs. A supplied ID must match its purpose;
the suite fails on an incorrect selection instead of substituting another record.
The same application can serve multiple purposes if Pending and documented.

The listing ID/type pair selects listing searches and cancellation; use an Active
record so the full suite can exercise cancellation. Document indices are zero-based.
The download case verifies the selected document only; it does not download every
file in the application. Unset optional variables to return to automatic selection.

## Implemented scope

- Valid Admin login, role/session/navigation and catalog applications load.
- Application business/contact/category/location/status rows, business/email search,
  All/Pending/Approved/Rejected filters and legitimate empty groups.
- Application review fields, document metadata, and closing review.
- Verification document download through the actual UI: successful selected
  application/index GET, expected filename, nonempty content, matching byte length,
  and PDF/PNG/JPEG signature checks for those formats.
- Approval confirmation canceled for a Pending application.
- Rejection reason/feedback drafted then canceled for a Pending application.
- Listing identity, provider, category/status rows, service/provider/ID searches,
  category/status filters and combined filters.
- Valid removal reason and confirmation canceled, followed by a real UI Refresh GET
  verifying the selected listing remains Active and the listing snapshot unchanged.
- Inventory KPIs, category distribution, regional supply/coverage labels, valid
  date/category/location filters and reset to default Next 30 Days.
- A combined journey returning to application review and freshly reloading listings
  after canceled decisions/removal.

All returned statuses/categories are checked against the actual UI. Legitimate empty
status/category groups have explicit empty-state assertions; required positive
fixture cases never pass merely because no records exist.

## Excluded scope

Inventory is the Provider Catalog report only. It does not establish independent
booking revenue correctness or concurrent capacity behavior. Expected report values
come from the real catalog response; submitted business information is not validated
against outside sources.


## Evidence and prerequisites

Use a compatible .NET SDK (project targets net8.0), Chrome, initial NuGet/ChromeDriver
download access, and working frontend/Identity/Provider Catalog/blob storage services.
Existing Selenium Manager resolves ChromeDriver. Existing project configuration runs
cases serially. Each case gets a fresh browser and explicit waits; there are no
Thread.Sleep calls, new packages, saved credentials or data-seeding steps.

