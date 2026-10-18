# Provider Catalog Selenium E2E tests

The Provider folder now contains only the approved Provider Catalog workflow:
**11 scenario methods / 13 configured cases**. Every case starts with valid provider
login in a fresh Chrome session. The status-filter scenario has three data rows.

## Enter environment variables in your terminal

Open PowerShell in the repository root. Use the same terminal for configuration
and execution. No settings/credential file is created or loaded.

    $env:CQ_E2E_PROVIDER_EMAIL = Read-Host 'Valid provider email'
    $providerCatalogPassword = Read-Host 'Valid provider password' -AsSecureString
    try {
        $env:CQ_E2E_PROVIDER_PASSWORD = [System.Net.NetworkCredential]::new('', $providerCatalogPassword).Password
    }
    finally {
        $providerCatalogPassword.Dispose()
    }

These two values are sufficient to run the full suite against a provider with at
least one existing listing. The default fixture is the first owned listing in the
UI's actual catalog response. You can optionally choose one particular owned listing:

    $env:CQ_E2E_PROVIDER_LISTING_ID = Read-Host 'Existing owned listing GUID'

To return to automatic selection:

    Remove-Item Env:CQ_E2E_PROVIDER_LISTING_ID -ErrorAction SilentlyContinue

Use an active approved provider account. The optional listing GUID must belong to
that account and its registered category; an incorrect configured ID fails rather
than silently substituting another record. Listing names must uniquely identify
rendered rows because the UI does not expose listing IDs on those rows.

## Run

From the repository root:

    .\qa-testing\CeylonQuest.E2E.Tests\Provider\Run-ProviderTests.ps1

Watch Chrome:

    .\qa-testing\CeylonQuest.E2E.Tests\Provider\Run-ProviderTests.ps1 -ShowBrowser

Run valid login first, or another specific scenario:

    .\qa-testing\CeylonQuest.E2E.Tests\Provider\Run-ProviderTests.ps1 -CaseId PC_001
    .\qa-testing\CeylonQuest.E2E.Tests\Provider\Run-ProviderTests.ps1 -CaseId PC_007

PC_001 output lists owned listing GUIDs and names so you can optionally configure
the desired record without database access.

Discover cases without credentials or opening a browser:

    .\qa-testing\CeylonQuest.E2E.Tests\Provider\Run-ProviderTests.ps1 -ListTests

The default target remains the existing deployed frontend. For an already-running
local frontend:

    .\qa-testing\CeylonQuest.E2E.Tests\Provider\Run-ProviderTests.ps1 -BaseUrl 'http://localhost:5173'


## Implemented checks

1. Valid login: Provider role/session, dashboard, successful catalog profile and
   category-specific owned-listing response.
2. Business profile: name, service category, location, description, phone and email.
3. Overview: active and total registered listing counts.
4. Owned listing table: heading, names, descriptions, location, price, applicable
   category/capacity/schedule fields, status and Edit/Delete controls.
5. Search: known listing title in uppercase, correct matching results, clear to restore.
6. Status: All, Active and Inactive counts/rows; legitimate empty groups have the
   expected empty message and no rows.
7. Add: fill valid category-specific draft values, inspect retained inputs, cancel,
   verify original rows and no creation request.
8. Edit: inspect saved category-specific values, including dates/time slots where
   present, cancel.
9. Cancel edit: change name/description locally, cancel, reopen and verify originals.
10. Cancel delete: inspect the selected row's confirmation, cancel, verify row retained.
11. Journey: search, draft/cancel, edit/cancel, delete/cancel, navigate through business
    profile and back, verify original rows/form values and listing summary.

The selected account exercises its registered Experience, Restaurant or Accommodation
category. To cover all three, rerun with valid accounts of those respective types.

## Category-specific fields

| Category | Listing table and edit-form coverage |
| --- | --- |
| Experience | Title, description, location, price/unit, max participants, duration, available days, validity dates, time-slot inputs, active state |
| Restaurant | Name, description, cuisine, dining style, location, price/person and range, group category, seating capacity, opening hours, reservation slots, menu, dietary options, active state |
| Accommodation | Room/property type, description, location, price/night, guests, beds, minimum stay, amenities, bathroom details, active state |

Native selects and time inputs must be representable by the current form. A legacy
saved value that the actual form fails to populate is an assertion failure, not
automatically accepted as correct.


## Scope and evidence

The provider tests cover valid login, viewing business details, checking listing counts, viewing and searching owned listings, and filtering them by Active or Inactive status. They also check that listing forms display the correct information and that canceling a new listing, an edit, or a deletion leaves the original data unchanged. The tests support experience, restaurant, and accommodation providers according to the account’s category.

Runtime prerequisites: compatible .NET SDK (target net8.0), Chrome, initial NuGet
and Selenium Manager download access, and a working frontend/Identity/Catalog
deployment. Existing project configuration runs cases serially, with a fresh
browser and explicit waits per case. No Thread.Sleep or new package is added.

The runner writes TRX to Provider/TestResults/provider-catalog-results.trx and
screenshots to Provider/TestResults/screenshots. Existing git rules ignore those
artifacts. Screenshots/logs may contain account/listing data; keep test credentials
out of source and chat. Discovery-time skips are not browser passes.


