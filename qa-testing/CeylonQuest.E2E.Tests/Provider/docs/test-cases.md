# Provider UI/E2E case definitions

All cases have `Workflow=Provider` and a `CaseId` trait. `Safety=ReadOnly` cases
make no persistent business-data changes. `Safety=Writes` cases require the
isolated environment/account/backend allowlist described in [README](../README.md).
Each case starts in its own browser, authenticates normally, and cleans up its
browser even on failure. Current execution statuses are in [execution report](execution-report.md).

## Cases

Current scope: **23 Provider cases, 18 read-only and 5 opt-in write cases**, plus
two existing shared-login cases reused for the focused authentication run. Report testing
is excluded by user request. PROV-REPORT-01/02/03 are retired identifiers, not
passing cases. The known revenue inconsistency is preserved in the execution report.

| Case ID | Description and meaningful assertions | Safety | Extra test data |
| --- | --- | --- | --- |
| PROV-AUTH-01 | Login through home/login form; require Provider role/token, successful provider profile/catalog loads, Overview active, all provider navigation options present | ReadOnly | Provider credentials |
| PROV-AUTH-02 | Logout; token/role absent; home visible; provider navigation absent after refresh and reopening entry URL | ReadOnly | Credentials |
| PROV-AUTH-03 | Verify valid Provider login first; logout; submit the same email with a generated unequal password; require invalid-credentials banner, retained login fields, no token/role, and no Provider navigation | ReadOnly | `CQ_E2E_PROVIDER_EMAIL`, `CQ_E2E_PROVIDER_PASSWORD` |
| PROV-AUTH-04 | Login with real Visitor credentials; require Visitor role/UI and no Provider navigation/content; reopen entry URL and retain Visitor-only routing; actual Provider bookings GET with the Visitor session must return 403 | ReadOnly | `CQ_E2E_VISITOR_EMAIL`, `CQ_E2E_VISITOR_PASSWORD`; does not require Provider credentials |
| PROV-DASH-01 | Load real booking/listing data, then verify overview sections, active listing count, total bookings and estimated earnings against those records | ReadOnly | Credentials |
| PROV-DASH-02 | Overview Add New Listing and Account Settings navigate to the actual management tabs and content | ReadOnly | Credentials |
| PROV-LIST-01 | Find a configured provider-owned listing by full ID; search its title; verify exact row title, location and editing controls | ReadOnly | `PROVIDER_LISTING_ID` |
| PROV-LIST-02 | Submit empty create form; verify actual category-specific required-name error; listing data unchanged; close modal without saving | ReadOnly | Credentials |
| PROV-LIST-03 | Create unique inactive listing with valid category-specific fields; verify new ID/row; edit title; verify same ID updated; confirm deletion; verify absence; cleanup only confirmed test-created ID on failure | Writes | Isolated test deployment/account; no pre-existing listing is edited/deleted |
| PROV-BOOK-01 | Search configured full booking ID; verify row and details modal match customer, service, status, amount, dates, nights/guest counts as applicable | ReadOnly | `PROVIDER_BOOKING_ID` |
| PROV-BOOK-02 | Select the known booking's status filter; require populated results; verify count and every displayed row status; full-ID search returns exact booking | ReadOnly | `PROVIDER_BOOKING_ID` |
| PROV-BOOK-03 | On dedicated eligible past booking, click Verify Attended; verify saved attendance response and rendered state | Writes | `PROVIDER_ATTENDED_BOOKING_ID`, Pending attendance and elapsed scheduled end |
| PROV-BOOK-04 | On different dedicated eligible past booking, click Mark No-Show; verify persisted NoShow and rendered state | Writes | `PROVIDER_NOSHOW_BOOKING_ID`, Pending attendance and elapsed scheduled end |
| PROV-NOTIF-01 | Load notifications successfully; verify every current-page title, message, Sri Lanka timestamp, read status and unread count; genuine zero-record response requires empty state | ReadOnly | Credentials |
| PROV-NOTIF-02 | Locate configured notification, including pagination; require actual notification and verify its displayed fields/status | ReadOnly | `PROVIDER_NOTIFICATION_ID` |
| PROV-NOTIF-03 | Mark dedicated unread notification as read; verify visible read state, persistence on reopening, lower unread count | Writes | `PROVIDER_NOTIFICATION_ID`, initially unread |
| PROV-REV-01 | Open Reviews & Ratings / Customer Reviews & Ratings; dropdown IDs/labels exactly match own catalog listings; select configured listing; validate summary/cards | ReadOnly | `PROVIDER_LISTING_ID` |
| PROV-REV-02 | Select reviewed listing A; require reviews; every response item belongs to selected ID/type; verify summary, reviewer, comment, rating, time and response textarea | ReadOnly | `PROVIDER_REVIEW_LISTING_A`, at least one review |
| PROV-REV-03 | Select A → B → A; both distinct listings have reviews; validate each selected ID, disjoint review IDs, complete corresponding rendered cards and return to A | ReadOnly | `PROVIDER_REVIEW_LISTING_A`, `PROVIDER_REVIEW_LISTING_B` |
| PROV-REV-04 | Select known empty listing; require zero total/review counts, no cards and exact `No customer reviews for this listing yet.` message | ReadOnly | `PROVIDER_EMPTY_REVIEW_LISTING` |
| PROV-REV-05 | Mark a review read and unread; verify class/badge changes; explicitly claim only temporary component state | ReadOnly | `PROVIDER_REVIEW_LISTING_A` with reviews |
| PROV-REV-06 | Locate dedicated review by ID; submit response; update it; verify saved payload and actual textarea; navigate away/back and verify persistence; restore earlier nonblank response when possible | Writes | `PROVIDER_REVIEW_LISTING_A`, `PROVIDER_RESPONSE_REVIEW_ID` |
| PROV-E2E-01 | One read-only Login → loaded Overview → owned listing → customer booking details → reviewed listing → known notification → Logout journey, with stage assertions/screenshots | ReadOnly | Listing ID, booking ID, reviewed listing A, notification ID |

All configuration names in the table have the prefix `CQ_E2E_`, for example
`PROVIDER_BOOKING_ID` means `CQ_E2E_PROVIDER_BOOKING_ID`. Every case needs Provider
credentials except PROV-AUTH-04, which needs only Visitor credentials. Missing variables are Skipped; invalid configured data and actual
service/DOM/assertion failures are Fail. No navigation-only or missing-data passes.

## Provider authentication coverage without duplicate tests

The application has **one shared login form**, not separate Visitor and Provider
login URLs. The five requested scenarios are covered by the following existing/new
cases. The two common validation scenarios are selected from the original Visitor
authentication class; they are not copied, wrapped or relabeled as new passing tests.

| Requested scenario | Test ID | Executable case / coverage |
| --- | --- | --- |
| Valid Provider login | PROV-AUTH-01 | Existing `ProviderDashboardTests.ProviderLogin_DisplaysRoleAndNavigation`; unchanged |
| Wrong password for a valid Provider | PROV-AUTH-03 | New `ProviderAuthenticationTests.IncorrectProviderPassword_RemainsOnLoginWithoutSession` |
| Unregistered email | AUTH-02 (shared) | Existing `AuthenticationTests.InvalidCredentials_DisplayAuthenticationError`; unique `example.invalid` email and generated password, invalid error and unauthenticated state |
| Empty email/password | AUTH-03 (shared) | Existing `AuthenticationTests.EmptyLoginFields_DisplayValidationError`; actual Identity API validation banner and unauthenticated state |
| Visitor denied Provider access | PROV-AUTH-04 | New `ProviderAuthenticationTests.VisitorCredentials_CannotAccessProviderDashboardOrBookings`; actual UI role boundary plus read-only HTTP 403 check |

AUTH-02/AUTH-03 are the original shared case IDs from the Visitor documentation,
not additional Provider test methods or `CaseId` traits. The focused command
selects their existing fully qualified method names exactly once.

The wrong-password scenario verifies the real email/password as a Provider first,
so an unregistered or inactive account cannot accidentally satisfy that case. Its
incorrect password is generated at runtime, compared against the valid password,
never printed, and submitted only once. No account records are changed.

The shared form uses `noValidate`; empty-field rejection is performed by the real
Identity API and displayed as `Please check your login details.`. The tests do not
invent native browser validation or change the application to add it.

For the Visitor scenario, successful Visitor authentication is expected. The
negative assertion is **no Provider access**, not an erroneous claim that valid
Visitor credentials must be rejected by the shared login form. There is no
implemented Provider dashboard URL to force. Tests reopen the actual app entry
point, verify Visitor routing/no Provider controls, then discover the Booking
service origin from the Visitor UI's own `/api/user-bookings/my` request and send
one read-only GET to the implemented `/api/provider-bookings/my` endpoint using
the existing Visitor session. Only HTTP 403 passes; 200, 401, errors and timeouts
fail. No tokens/roles are forged, printed or written to artifacts.

### Focused authentication execution

Set Provider and Visitor credentials through environment variables in the terminal
running these commands; see [README](../README.md). Unregistered-email/empty-field
cases need no account credentials. Existing reporting and explicit waits are reused.

```powershell
$authFilter = 'CaseId=PROV-AUTH-01|CaseId=PROV-AUTH-03|CaseId=PROV-AUTH-04|FullyQualifiedName~AuthenticationTests.InvalidCredentials_DisplayAuthenticationError|FullyQualifiedName~AuthenticationTests.EmptyLoginFields_DisplayValidationError'
$env:CQ_E2E_EVIDENCE_DIR = Join-Path (Get-Location) 'qa-testing\CeylonQuest.E2E.Tests\Provider\TestResults\authentication-screenshots'
dotnet test .\qa-testing\CeylonQuest.E2E.Tests\CeylonQuest.E2E.Tests.csproj --filter $authFilter --logger "trx;LogFileName=provider-authentication.trx" --logger "console;verbosity=normal" --results-directory .\qa-testing\CeylonQuest.E2E.Tests\Provider\TestResults
```

### Actual focused authentication results

Executed **16 October 2026, 21:22:24–21:22:49 Asia/Colombo** against the deployed
site: **5 selected, 2 passed, 0 failed, 3 skipped**, exit code 0, runner time
24.56 seconds. Compilation completed without warnings/errors. No account-dependent
scenario is claimed as passing merely because it skipped.

| Scenario / ID | Actual result | Evidence or reason |
| --- | --- | --- |
| Valid Provider login / PROV-AUTH-01 | Skipped | `CQ_E2E_PROVIDER_EMAIL`, `CQ_E2E_PROVIDER_PASSWORD` absent in this process |
| Incorrect Provider password / PROV-AUTH-03 | Skipped | Provider credentials absent; a valid Provider identity cannot be established |
| Unregistered email / AUTH-02 shared | Pass | Existing invalid-credentials test executed against the real login UI |
| Empty fields / AUTH-03 shared | Pass | Existing empty-fields test executed and verified validation/no authenticated session |
| Visitor denied Provider access / PROV-AUTH-04 | Skipped | `CQ_E2E_VISITOR_EMAIL`, `CQ_E2E_VISITOR_PASSWORD` absent in this process |

TRX: `Provider/TestResults/provider-authentication.trx` (relative to the project).
Screenshots:

- `Provider/TestResults/authentication-screenshots/InvalidCredentials_DisplayAuthenticationError-invalid-login-20261016T155239518Z.png`
- `Provider/TestResults/authentication-screenshots/EmptyLoginFields_DisplayValidationError-empty-login-20261016T155249022Z.png`

Provider credentials and Visitor credentials must be set in the same terminal
running the command to exercise the three skipped cases. Existing values in a
different terminal are not inherited by this already-running agent process.
All 25 pre-existing `.cs`, `.csproj` and `.ps1` files in the test project were
verified byte-for-byte unchanged by SHA-256 after this extension. Only the two new
Provider authentication source files and Provider documentation were added/updated.

## Actual implemented routes and controls

| Feature | Actual UI | Actual service interaction |
| --- | --- | --- |
| Login | Home `nav-login-btn`; `login-email`, `login-password`, `login-button` | Identity `POST /api/auth/login` |
| Provider data | Dashboard React state; `nav-overview`, `nav-services` | Catalog `GET /api/catalog/provider/profile`; GET of provider's category-specific listing endpoint |
| Listing management | `add-listing-btn`; real `hotel-*`, `rest-*`, `exp-*` fields; actual modal, Edit, Delete/confirmation | Catalog accommodation/restaurant/activity-listings GET/POST/PUT/DELETE |
| Bookings | `nav-bookings`; Search bookings; status filters; View Details | Booking `GET /api/provider-bookings/my` |
| Attendance | Verify Attended / Mark No-Show, only when implemented eligibility is satisfied | `PATCH /api/provider-bookings/{bookingType}/{bookingId}/attendance` |
| Reviews | `nav-reviews`; `provider-review-listing`; actual cards, summary and response form | `GET /api/bookings/reviews/{listingId}?bookingType=...&page=...&pageSize=10` |
| Provider response | Provider response textarea; Respond to review / Update response | `PUT /api/bookings/reviews/{reviewId}/response` |
| Notifications | Shared `NotificationPanel`; inline title/message/time/read status, pages, Mark as read | `GET /api/notifications`; `PATCH /api/notifications/{id}/read` |
| Logout | `pd-logout-btn`; home page | Current UI clears local token/role; tests do not claim JWT revocation |

## Not automated or restricted

- Revenue/report pages, report date/status/type filters and report arithmetic:
  removed from the active suite at the user's request. Their prior failures are
  historical evidence of an unresolved report inconsistency, not current passes.
- Provider booking approve/reject/cancel: not present in the Provider UI. Visitors
  create/cancel bookings; those actions are not invented on the Provider side.
- Notification-details navigation: no separate screen/button; details are inline.
- Persistent review read/unread state: not implemented; the toggle is component state.
- Response deletion and notification mark-unread: no supported UI/API; tests do
  not simulate cleanup through database edits or nonexistent endpoints.
- Mark all notifications read: implemented but deliberately not exercised against
  the shared account because it changes unrelated notifications. One dedicated
  unread notification is used for the isolated write case.
- Review-side Confirm Attendance is implemented, but separate duplicate coverage is
  omitted; the two booking attendance cases cover the primary attendance actions.
- Listing photo uploads, business/account edits, inventory-report internals, platform
  reviews, and server-side token revocation are outside these Provider workflow cases.
- All persistent write cases are blocked on the current shared deployment. Their
  source exists but actual write behavior remains unverified without isolation.
- Review A/B and empty-listing checks need the corresponding existing data. An empty
  dataset cannot pass the reviewed-listing or isolation cases.

## Evidence and interpretation

Actual test results come from TRX and assertions. Build success, discovered tests,
skipped prerequisites, stage screenshots and fully passed cases are reported
separately. Existing BrowserSession captures failure screenshots; Provider tests
also capture stage/success evidence. Generated files remain ignored.

The passive Provider GET observer gives supplementary ID/data correlation where
cards do not expose identifiers. It never substitutes a direct API-only test for
the rendered UI, mocks responses, injects authentication, or forces business state.
