# Provider Selenium workflow

This adds Provider coverage to the existing .NET 8 / xUnit / Selenium 4 project.
**All additions are inside `Provider/`; Visitor files and application files are unchanged.**
There are 23 Provider cases: 18 read-only cases and 5 opt-in write cases.
The read-only end-to-end case visits the dashboard, listings, a booking, customer
reviews, notifications, and logout in one browser session.

Revenue-report testing is excluded from the current scope at the user's request.
The three report cases and report Page Object were removed, and the combined
journey no longer visits Reports. The previously discovered completed-Restaurant
revenue inconsistency remains unresolved; excluding it does not establish report
correctness. Historical results/evidence are preserved in the execution report.

## Start here on the current deployed website

The user confirmed the provider account uses the same deployed website, with no
separate environment. Run read-only cases there. Persistent changes are blocked
on that shared frontend and on the known production backend origins.

From the repository root, run:

```powershell
& .\qa-testing\CeylonQuest.E2E.Tests\Provider\Run-ProviderTests.ps1
```

The launcher prompts first for **the provider email**, then for **the provider
password with hidden input**. Enter values at the prompts; do not replace the
prompt labels with a password. It runs headless Chrome, uses the existing
Selenium Manager setup, and saves TRX/screenshots under `Provider/TestResults/`.
It does not set mutation opt-ins or create credential files. Credentials entered
by the launcher are removed/restored in its environment afterwards.

Run the login case first to verify the account and print its own listing IDs:

```powershell
& .\qa-testing\CeylonQuest.E2E.Tests\Provider\Run-ProviderTests.ps1 -CaseId PROV-AUTH-01
```

Use `-ShowBrowser` to watch a case. Each case uses a fresh browser; tests are serial
under the existing project configuration. Login uses the actual home/login buttons
and the existing `LoginPage.OpenFromHome` / `Submit` methods. Provider credentials
do not activate Visitor cases.

## Direct dotnet commands and independent execution

Alternatively set process environment variables in the same PowerShell terminal:

```powershell
$env:CQ_E2E_PROVIDER_EMAIL = Read-Host 'Enter provider email'
$providerPassword = Read-Host 'Enter provider password' -AsSecureString
$env:CQ_E2E_PROVIDER_PASSWORD = [System.Net.NetworkCredential]::new('', $providerPassword).Password
$providerPassword.Dispose()
$env:CQ_E2E_EVIDENCE_DIR = Join-Path (Get-Location) 'qa-testing\CeylonQuest.E2E.Tests\Provider\TestResults\screenshots'

dotnet test .\qa-testing\CeylonQuest.E2E.Tests\CeylonQuest.E2E.Tests.csproj --filter "Workflow=Provider&Safety=ReadOnly" --logger "trx;LogFileName=provider-readonly.trx" --logger "console;verbosity=detailed" --results-directory .\qa-testing\CeylonQuest.E2E.Tests\Provider\TestResults
```

Useful filters, without editing any Visitor tests:

```powershell
# All Provider cases (write cases skip unless every isolation prerequisite is met).
dotnet test .\qa-testing\CeylonQuest.E2E.Tests\CeylonQuest.E2E.Tests.csproj --filter "Workflow=Provider" --logger "trx;LogFileName=provider-all.trx" --results-directory .\qa-testing\CeylonQuest.E2E.Tests\Provider\TestResults

# Only Provider review cases.
dotnet test .\qa-testing\CeylonQuest.E2E.Tests\CeylonQuest.E2E.Tests.csproj --filter "FullyQualifiedName~CeylonQuest.E2E.Tests.Provider.Tests.ProviderReviewTests" --logger "trx;LogFileName=provider-reviews.trx" --results-directory .\qa-testing\CeylonQuest.E2E.Tests\Provider\TestResults

# Existing Visitor cases only, excluding every new Provider namespace.
dotnet test .\qa-testing\CeylonQuest.E2E.Tests\CeylonQuest.E2E.Tests.csproj --filter "FullyQualifiedName!~CeylonQuest.E2E.Tests.Provider" --logger "trx;LogFileName=visitor-only.trx" --results-directory .\qa-testing\CeylonQuest.E2E.Tests\TestResults
```

`dotnet test` without a filter now discovers both workflows. A successful runner
exit with skipped cases is not proof those workflows passed. Missing prerequisite
variables are discovery-time **Skipped** results; incorrect configured IDs, service
errors, ownership mismatches, assertion failures and timeouts are failures.

## Provider positive/negative authentication checks

New cases PROV-AUTH-03 (wrong password for a verified Provider) and PROV-AUTH-04
(Visitor denied Provider UI/API access) reuse the existing BrowserSession and
LoginPage. Existing Provider positive case PROV-AUTH-01 is unchanged. Unregistered
email and empty-field checks already exist on the shared login form in
`Tests/AuthenticationTests.cs`; the focused filter below runs those original cases
instead of adding duplicate Provider copies. This selection contains five cases.

Configure credentials locally in the same PowerShell terminal:

```powershell
$env:CQ_E2E_PROVIDER_EMAIL = Read-Host 'Enter provider email'
$providerPassword = Read-Host 'Enter provider password' -AsSecureString
$env:CQ_E2E_PROVIDER_PASSWORD = [System.Net.NetworkCredential]::new('', $providerPassword).Password
$providerPassword.Dispose()

$env:CQ_E2E_VISITOR_EMAIL = Read-Host 'Enter visitor email'
$visitorPassword = Read-Host 'Enter visitor password' -AsSecureString
$env:CQ_E2E_VISITOR_PASSWORD = [System.Net.NetworkCredential]::new('', $visitorPassword).Password
$visitorPassword.Dispose()

$authFilter = 'CaseId=PROV-AUTH-01|CaseId=PROV-AUTH-03|CaseId=PROV-AUTH-04|FullyQualifiedName~AuthenticationTests.InvalidCredentials_DisplayAuthenticationError|FullyQualifiedName~AuthenticationTests.EmptyLoginFields_DisplayValidationError'
$env:CQ_E2E_EVIDENCE_DIR = Join-Path (Get-Location) 'qa-testing\CeylonQuest.E2E.Tests\Provider\TestResults\authentication-screenshots'
dotnet test .\qa-testing\CeylonQuest.E2E.Tests\CeylonQuest.E2E.Tests.csproj --filter $authFilter --logger "trx;LogFileName=provider-authentication.trx" --logger "console;verbosity=normal" --results-directory .\qa-testing\CeylonQuest.E2E.Tests\Provider\TestResults
```

Prompt text is a label; type the email/password when asked. Do not paste passwords
into chat/source. No new wrong-password variable is needed; that value is generated
at runtime. Set `CQ_E2E_HEADLESS=false` before the direct command to watch Chrome.
No listing, booking, review or notification IDs are required for this focused run.

The normal Provider launcher remains unchanged. Its Provider-only filter includes
the two new cases; the Visitor-boundary case skips without Visitor credentials.
The focused filter also includes the two existing shared validation cases, which
are not part of `Workflow=Provider` by themselves. See the
[authentication coverage matrix](docs/test-cases.md) for the actual behavior tested.

## Configure existing test data

Do not paste passwords or tokens into chat/source. Environment variables entered
in one terminal are not inherited by an already-running agent or another terminal.

| Variable | Meaning / prerequisite |
| --- | --- |
| `CQ_E2E_PROVIDER_EMAIL`, `CQ_E2E_PROVIDER_PASSWORD` | Active approved Provider account, distinct from Visitor credentials |
| `CQ_E2E_VISITOR_EMAIL`, `CQ_E2E_VISITOR_PASSWORD` | Required only for PROV-AUTH-04 (Visitor denied Provider access) and existing Visitor cases |
| `CQ_E2E_BASE_URL` | Defaults to the current deployed Azure website; optional isolated frontend override |
| `CQ_E2E_PROVIDER_LISTING_ID` | Full GUID of an existing listing owned by this provider |
| `CQ_E2E_PROVIDER_BOOKING_ID` | Full GUID of a known customer booking for this provider |
| `CQ_E2E_PROVIDER_REVIEW_LISTING_A` | Own listing with at least one customer review |
| `CQ_E2E_PROVIDER_REVIEW_LISTING_B` | A different own listing with at least one review, for switching/isolation |
| `CQ_E2E_PROVIDER_EMPTY_REVIEW_LISTING` | Own listing with no reviews |
| `CQ_E2E_PROVIDER_NOTIFICATION_ID` | Full GUID of a known provider notification; must be unread for the write case |
| `CQ_E2E_WAIT_SECONDS` | Existing explicit wait timeout, default 35 seconds |
| `CQ_E2E_HEADLESS` | Existing shared setting; launcher selects headless unless `-ShowBrowser` |
| `CQ_E2E_EVIDENCE_DIR` | Existing shared screenshot directory setting |

Login output prints owned listing IDs/names. The overview case loads bookings and
prints their full IDs/service/status. The notifications load case prints current-page
notification IDs/titles/read state. Review listing IDs are the same owned listing
IDs, not review IDs. Use those logs to configure known records without database edits.
An ID that does not belong to the logged-in provider fails; another listing is
never silently substituted. A/B isolation cases require actual reviews in both.

For example, after obtaining real IDs:

```powershell
$env:CQ_E2E_PROVIDER_LISTING_ID = Read-Host 'Own listing GUID'
$env:CQ_E2E_PROVIDER_BOOKING_ID = Read-Host 'Provider booking GUID'
$env:CQ_E2E_PROVIDER_REVIEW_LISTING_A = Read-Host 'Reviewed listing A GUID'
$env:CQ_E2E_PROVIDER_REVIEW_LISTING_B = Read-Host 'Reviewed listing B GUID'
$env:CQ_E2E_PROVIDER_EMPTY_REVIEW_LISTING = Read-Host 'Listing with zero reviews GUID'
$env:CQ_E2E_PROVIDER_NOTIFICATION_ID = Read-Host 'Provider notification GUID'
& .\qa-testing\CeylonQuest.E2E.Tests\Provider\Run-ProviderTests.ps1
```

Report date variables are no longer needed. Missing IDs do not prevent
credential-only cases (login, dashboard, listing validation and
notification loading) from running.

## Implemented behavior and assertions

- Provider tabs use `nav-overview`, `nav-services`, `nav-bookings`, `nav-reviews`,
  `nav-notifications`, etc.; there are no invented provider URL routes.
- The dashboard's booking statistics are populated after visiting Bookings. Tests
  explicitly load those records before verifying overview counts/estimated earnings.
  Estimated Earnings is the overview UI's confirmed/completed booking value.
  This overview-card assertion does not verify the excluded report page.
- Provider category is obtained from the real profile. Listing forms support the
  actual Accommodation, Restaurant, or Experience fields and validation messages.
- Bookings are matched by full GUID and checked for customer, service, status,
  amount, dates and guest counts. Filters must select a populated status group.
- Review dropdown IDs/labels must exactly match the provider's own catalog list.
  Every loaded review must have the selected listing ID/type, a 1–5 rating, and
  matching rendered reviewer, comment, rating, timestamp and response field. A/B
  switching verifies disjoint review IDs and correct rendering after switching back.
- Empty reviews assert **No customer reviews for this listing yet.** with zero
  total/review counts, not a loading error. Review read/unread is temporary UI state
  only. Saved responses are shown in the actual response textarea, not an invented
  separate response/details page.
- Notification cards are checked against real title/message/timestamp/read values;
  the configured notification case requires an actual record rather than accepting
  an empty panel. No separate notification details screen is assumed.
- Logout clears token/role and removes provider navigation after refresh/reopening
  the actual entry URL. This tests the UI boundary, not server-side JWT revocation.

The new Provider-only passive observer copies the real UI's GET response bodies
to browser memory. It issues no extra API calls, bypasses no authentication, and
does not modify responses or persistent state. It is needed to correlate full
listing/review/notification IDs absent from the rendered cards. Assertions always
also verify the actual UI; successful navigation alone does not pass a case.
No credential/token or complete response body is printed to artifacts.
Read-only cases also assert that the observed UI issued no listing, attendance,
review-response or notification mutation requests during their scenario. The
observer records only method/path metadata for this check; it does not intercept
or alter requests. This verifies that empty-form validation remains local.

## Write cases: only for a future isolated deployment

**Do not enable these for the current shared website.** Source coverage exists for
the five write cases, but they cannot be claimed as tested until actually executed
in an isolated frontend/backend deployment with dedicated data.

Every write case requires ALL of:

- An isolated `CQ_E2E_BASE_URL` (the current deployed frontend is hard-blocked).
- `CQ_E2E_PROVIDER_ALLOW_WRITES=true`.
- `CQ_E2E_PROVIDER_TEST_ENVIRONMENT_CONFIRMED=true`.
- `CQ_E2E_PROVIDER_TEST_ACCOUNT_CONFIRMED=true`.
- `CQ_E2E_PROVIDER_TEST_API_ORIGINS`: semicolon-separated isolated API origins.
  Each mutation verifies the actual observed backend origin against this allowlist
  and rejects the known production Identity, Catalog and Booking origins. A different
  frontend pointing to those same production APIs does not qualify.

Write-specific data:

| Variable | Use |
| --- | --- |
| `CQ_E2E_PROVIDER_ATTENDED_BOOKING_ID` | Dedicated past Confirmed/Completed booking with Pending attendance |
| `CQ_E2E_PROVIDER_NOSHOW_BOOKING_ID` | A different dedicated past booking with Pending attendance |
| `CQ_E2E_PROVIDER_RESPONSE_REVIEW_ID` | Dedicated review on configured reviewed listing A; paginated lookup supported |

Use the launcher with `-Mode Writes` or `-Mode All` only after independently
configuring those prerequisites. The launcher never enables them itself.

Listing CRUD creates a unique **inactive** `CQ-E2E-...` record, edits that same
record, deletes it, and attempts cleanup on assertion failure when its confirmed
ID is known. It refuses to edit/delete other records. If creation succeeds but
the refresh fails before the ID is known, inspect the logged unique name and
evidence; do not guess which record to delete.

Attendance changes have no reverse action in this UI; use fresh dedicated bookings.
Read notifications cannot be marked unread by the implemented API. Responses have
no delete operation: a pre-existing nonblank response is restored when possible;
an initially blank dedicated review retains the test response. These limitations
are logged, not bypassed using database edits or invented endpoints.

No Provider approve/reject or cancellation actions are implemented or tested.
Booking creation/cancellation belongs to the Visitor workflow.

## Files and evidence

```text
Provider/
  Infrastructure/ProviderSettings.cs, ProviderUi.cs, ProviderTestBase.cs
  Pages/ProviderDashboardPage.cs, ProviderListingsPage.cs, ProviderBookingsPage.cs
        ProviderReviewsPage.cs, ProviderNotificationsPage.cs
        ProviderAuthenticationPage.cs
  Tests/ProviderDashboardTests.cs, ProviderListingTests.cs, ProviderBookingTests.cs
        ProviderReviewTests.cs, ProviderNotificationTests.cs
        ProviderJourneyTests.cs, ProviderAuthenticationTests.cs
  Run-ProviderTests.ps1
  docs/test-cases.md
  docs/execution-report.md
  TestResults/  (generated; existing ignore rules apply)
```

The existing BrowserSession handles failure screenshots and Chrome disposal.
Provider cases also capture success/stage screenshots. Screenshots and logs can
contain test account names and business/customer details; review before submission.
See [case definitions](docs/test-cases.md) and [actual execution results](docs/execution-report.md).
