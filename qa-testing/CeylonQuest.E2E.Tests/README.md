# CeylonQuest Selenium UI/E2E tests

An isolated .NET 8 / xUnit / Selenium 4 project for the existing React application.
No application project references, application file changes, API shortcuts, seeded
accounts, or database writes are needed to build it. Each test gets a fresh Chrome
session, explicit waits, failure screenshots, and browser disposal. Tests run serially.
Selenium Manager resolves ChromeDriver; no ChromeDriver NuGet package is pinned.

## Run from the repository root

Prerequisites: .NET 8 SDK (or a compatible installed SDK), Chrome, access to NuGet
and ChromeDriver downloads on first use, and network access to the deployed app.

```powershell
$env:CQ_E2E_EVIDENCE_DIR = Join-Path (Get-Location) 'qa-testing\CeylonQuest.E2E.Tests\TestResults\screenshots'
dotnet test .\qa-testing\CeylonQuest.E2E.Tests\CeylonQuest.E2E.Tests.csproj --logger "trx;LogFileName=ceylonquest-e2e.trx" --logger "console;verbosity=normal" --results-directory .\qa-testing\CeylonQuest.E2E.Tests\TestResults
```

The default target is https://jolly-field-0aaea8a00.7.azurestaticapps.net.
Without credentials, the login navigation and two negative login cases run, and
authenticated cases are **Skipped with reasons**. A successful `dotnet test` exit
code with skips does not establish that the complete booking workflow passed.
Build/discovery, skips, partial stage logs, and complete journey passes are distinct.

Run only the home-to-login smoke check:

```powershell
dotnet test .\qa-testing\CeylonQuest.E2E.Tests\CeylonQuest.E2E.Tests.csproj --filter "Category=Smoke" --logger "trx;LogFileName=smoke.trx" --results-directory .\qa-testing\CeylonQuest.E2E.Tests\TestResults
```

## Credentials and test data

Use a dedicated visitor account. Set environment variables in the same terminal
that runs `dotnet test`. Keep passwords out of source, command history, and chat:

```powershell
$env:CQ_E2E_VISITOR_EMAIL = Read-Host 'Test visitor email'
$testVisitorPassword = Read-Host 'Test visitor password' -AsSecureString
$env:CQ_E2E_VISITOR_PASSWORD = [System.Net.NetworkCredential]::new('', $testVisitorPassword).Password
$testVisitorPassword.Dispose()
$env:CQ_E2E_ACCOMMODATION_TITLE = Read-Host 'Exact dedicated test accommodation title'
$env:CQ_E2E_CHECK_IN = Read-Host 'Future Sri Lanka check-in date (yyyy-MM-dd)'
$env:CQ_E2E_CHECK_OUT = Read-Host 'Later check-out date, respecting minimum stay (yyyy-MM-dd)'
$env:CQ_E2E_GUESTS = '1'
```

For the supplied test scenario, the non-secret data is:

```powershell
$env:CQ_E2E_ACCOMMODATION_TITLE = 'Mountain View Deluxe Room'
$env:CQ_E2E_CHECK_IN = '2026-10-18'
$env:CQ_E2E_CHECK_OUT = '2026-10-20'
$env:CQ_E2E_GUESTS = '1'
```

Environment variables set in a terminal apply to that terminal and its child
processes; an already-running editor or agent does not automatically inherit them.
Run the commands below from the same terminal in which credentials were entered.

The title must uniquely identify an accommodation in the frontend's first 50
catalog search results. Select dates on which its provider operates and inventory
is available. Configure valid dates/guest count for validation scenarios too:
those scenarios establish a valid available form before introducing one defect.
Search alone does not prove availability; the booking modal checks it.

To run authenticated checks without creating bookings:

```powershell
dotnet test .\qa-testing\CeylonQuest.E2E.Tests\CeylonQuest.E2E.Tests.csproj --filter "Category!=Booking" --logger "trx;LogFileName=authenticated-ui.trx" --results-directory .\qa-testing\CeylonQuest.E2E.Tests\TestResults
```

## Booking journey: manual Stripe test checkout

This case creates **one actual test booking** using the real Book Stay button.
Enable it only with a designated test listing and an approved Stripe test
integration. Confirm the server's deployed Stripe configuration before enabling
the flag; the frontend configuration alone cannot establish test mode.
The checked-in Booking service `Program.cs` reads `Stripe:SecretKey` and rejects
keys other than `sk_test_` / `rk_test_`. That server setting belongs to the existing
deployment; do not put any Stripe secret key in this Selenium project. Confirm the
deployed instance uses that configuration, then confirm the hosted page says test
mode. The flags below acknowledge test mode; they do not switch Stripe into it.

Stripe documents that its hosted payment interfaces have protections against
automated testing. This suite deliberately uses a **manual payment step**, with
Selenium automating the application before and after it. It is not a fully
unattended payment E2E test. No Stripe field selectors are invented, payments
are not mocked, and confirmed/paid statuses are never forced.

```powershell
$env:CQ_E2E_ALLOW_BOOKING = 'true'
$env:CQ_E2E_STRIPE_TEST_MODE_CONFIRMED = 'true'
$env:CQ_E2E_HEADLESS = 'false'
dotnet test .\qa-testing\CeylonQuest.E2E.Tests\CeylonQuest.E2E.Tests.csproj --filter "Category=Booking" --logger "trx;LogFileName=booking-journey.trx" --logger "console;verbosity=detailed" --results-directory .\qa-testing\CeylonQuest.E2E.Tests\TestResults
```

1. Selenium logs in, searches, verifies details, selects dates/availability/guests,
   and clicks Book Stay once.
2. In the visible Chrome window, confirm Stripe displays test mode. The test also
   checks that the hosted URL contains `cs_test_`; live/unknown sessions fail.
3. Complete the checkout using an approved Stripe test card, never a real card.
   There is a 240-second explicit wait by default. Do not close the window.
4. Selenium resumes on the real `/payment/success` return. It requires **Payment
   Successful**, **Confirmed**, and **Paid**, verifies the new My Bookings record,
   and waits up to 100 seconds for a rendered notification for that booking.

Do not blindly rerun after failure at checkout or notifications: the booking
may already exist. The suite does not cancel, delete, refund, or retry bookings.
Review the recorded booking ID and evidence before deciding how to clean up
dedicated test data using the application's ordinary workflow.

## Configuration reference

| Variable | Default / purpose |
| --- | --- |
| `CQ_E2E_BASE_URL` | Deployed Azure Static Web App URL above |
| `CQ_E2E_VISITOR_EMAIL`, `CQ_E2E_VISITOR_PASSWORD` | Required for authenticated cases |
| `CQ_E2E_ACCOMMODATION_TITLE` | Exact, unique dedicated accommodation title |
| `CQ_E2E_CHECK_IN`, `CQ_E2E_CHECK_OUT` | `yyyy-MM-dd`; future check-in, later check-out |
| `CQ_E2E_GUESTS` | `1`; must respect the listing's guest limit |
| `CQ_E2E_HEADLESS` | `true`; set `false` for the manual payment journey |
| `CQ_E2E_ALLOW_BOOKING` | Disabled unless `true` |
| `CQ_E2E_STRIPE_TEST_MODE_CONFIRMED` | Disabled unless `true` |
| `CQ_E2E_WAIT_SECONDS` | `35` per ordinary explicit wait |
| `CQ_E2E_PAYMENT_WAIT_SECONDS` | `240` for manual test payment |
| `CQ_E2E_NOTIFICATION_WAIT_SECONDS` | `100`; allows asynchronous delivery and 30-second UI refresh |
| `CQ_E2E_EVIDENCE_DIR` | Otherwise `TestResults/screenshots` below the test binary directory |

Missing required variables produce discovery-time skips. Invalid configured
dates, unavailable inventory, service errors, failed assertions, and notification
timeouts fail the affected test. No `Thread.Sleep` is used.

## How correlation works

The success screen renders an abbreviated booking ID. Booking table rows and
notification cards do not render full IDs. After payment, a passive browser-local
observer reads successful GET response copies from the **actual UI's own fetch
requests** to `/api/user-bookings/my` and `/api/notifications`. It makes no extra
requests and does not alter payloads, auth, tokens, validation, or server state.
It uniquely resolves the success ID to a fresh booking and verifies its rendered
row/modal; notifications must have that exact full booking ID and a matching
rendered title/message. Response bodies and tokens are not written to artifacts.
This supplementary correlation is documented rather than presented as DOM-only
verification. A backend or DOM contract change should fail visibly.

## Evidence and repository hygiene

TRX and screenshots go under this new project's `TestResults/` directory. Existing
repository ignore rules already exclude `bin/`, `obj/`, and `TestResults/`; no
changes to `.gitignore` are needed. Screenshots may contain test account/booking
details; review them before including them in a submission. No `.env` is created.

See [test cases](docs/test-cases.md) and [execution report](docs/selenium-e2e-report.md).
After a rerun, update the report from that run's TRX; do not carry earlier results
forward as proof that changed tests passed.

References: [Selenium Manager](https://www.selenium.dev/documentation/selenium_manager/),
[explicit waits](https://www.selenium.dev/documentation/webdriver/waits/),
[Stripe automated testing](https://docs.stripe.com/automated-testing),
[Stripe test cards](https://docs.stripe.com/testing).
