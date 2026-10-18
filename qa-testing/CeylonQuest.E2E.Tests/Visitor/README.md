# Visitor catalog E2E tests

An isolated visitor suite inside the existing .NET 8 / xUnit / Selenium project.
Every case starts with valid visitor login and opens Explore and Search.
The current suite contains **15 scenario methods / 26 configured cases**.


## Save configuration once

Add the Environmental variable before running the test.

## Run

Run all 26 configured visitor catalog cases:

    .\qa-testing\CeylonQuest.E2E.Tests\Visitor\Run-VisitorCatalogTests.ps1

Show the browser:

    .\qa-testing\CeylonQuest.E2E.Tests\Visitor\Run-VisitorCatalogTests.ps1 -ShowBrowser

Run a group, or one retained scenario:

    .\qa-testing\CeylonQuest.E2E.Tests\Visitor\Run-VisitorCatalogTests.ps1 -Area Browse
    .\qa-testing\CeylonQuest.E2E.Tests\Visitor\Run-VisitorCatalogTests.ps1 -Area Details
    .\qa-testing\CeylonQuest.E2E.Tests\Visitor\Run-VisitorCatalogTests.ps1 -Area Availability
    .\qa-testing\CeylonQuest.E2E.Tests\Visitor\Run-VisitorCatalogTests.ps1 -CaseId VC_003

Discovery only, without decrypting the saved password or opening Chrome:

    .\qa-testing\CeylonQuest.E2E.Tests\Visitor\Run-VisitorCatalogTests.ps1 -ListTests

For another already-running target:

    .\qa-testing\CeylonQuest.E2E.Tests\Visitor\Run-VisitorCatalogTests.ps1 -BaseUrl 'http://localhost:5173'


## Current scope

Covered: valid visitor login; catalog cards; Experiences, Restaurants and
Accommodations; case-insensitive title/description/location/provider search;
clearing search; location filtering; Budget and Mid-range price filtering; combined
filters; newest/ascending-price/descending-price sorting; category-specific details;
card entry to details; reviewed/unreviewed card summaries; valid experience,
restaurant and accommodation availability; and direct card entry to availability.


## Required data and settings

Prerequisites: compatible .NET SDK (project targets .NET 8), Chrome, initial package
and ChromeDriver download access, and working frontend/Identity/Catalog services.
Selenium Manager resolves ChromeDriver. No package or project-reference change is
needed. The deployed Azure frontend is the default target.

The loaded dataset needs all three categories, different prices and creation dates,
a Budget listing and a Mid-range listing, a supported dropdown city, reviewed and
unreviewed listings, and a field-specific keyword witness for each searchable field.

| JSON setting | Environment override | Purpose |
| --- | --- | --- |
| BaseUrl | CQ_E2E_BASE_URL | Target frontend |
| VisitorEmail | CQ_E2E_VISITOR_EMAIL | Valid visitor account |
| VisitorPasswordEncrypted | CQ_E2E_VISITOR_PASSWORD | Encrypted saved password; environment override is the ordinary password |
| ExperienceTitle | CQ_E2E_CATALOG_EXPERIENCE_TITLE | Exact experience title |
| ExperienceDate | CQ_E2E_CATALOG_EXPERIENCE_DATE | Future operating date within validity |
| RestaurantTitle | CQ_E2E_CATALOG_RESTAURANT_TITLE | Exact restaurant title |
| RestaurantDate | CQ_E2E_CATALOG_RESTAURANT_DATE | Future reservation date with available seats |
| AccommodationTitle | CQ_E2E_CATALOG_ACCOMMODATION_TITLE | Exact accommodation title |
| AccommodationDate | CQ_E2E_CATALOG_ACCOMMODATION_DATE | Future check-in with an available room |
| CheckOut | CQ_E2E_CATALOG_CHECK_OUT | Later date satisfying minimum stay |
| Guests | CQ_E2E_CATALOG_GUESTS | Valid positive count; default 1 |

Dates use yyyy-MM-dd. Required fixtures must be in Explore's first 50 results.
Configured titles must identify one listing within their category. Cards must have
unambiguous type/title/provider combinations. Experience dates must be operating
days; restaurant slots must have parseable future times; accommodation guests use
the room's guest limit rather than the number of available rooms.

CQ_E2E_HEADLESS defaults to true. CQ_E2E_WAIT_SECONDS defaults to 35. The runner
stores TRX and success/failure screenshots in Visitor/TestResults/. A full run needs
all availability settings; Browse/Details need visitor credentials without dates.

## Evidence and limits

The browser drives the real UI. A passive observer copies the UI's search and
availability GET responses; it issues no API setup requests and changes no payloads.
It records unexpected non-GET API calls after login as failures. Credentials, auth
headers, and request bodies are not written to artifacts.

Expected cards, details and availability options are checked against the actual
responses. This verifies rendering and behavior, not whether provider-entered
business descriptions are factually correct. Explore loads at most 50 listings,
then filters/sorts/paginates locally in groups of nine. Server-side filtering,
the complete API dataset, Kafka events, and capacity deduction/restoration are
outside this suite.



