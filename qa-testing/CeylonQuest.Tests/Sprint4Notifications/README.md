# Story 11.1 "Receive Event Notifications" - QA Automation (Sprint 4 / CEYQ-219)

Selenium (UI) + API integration tests for the `notification-service` story, covering the
`TC-11.1-UI-01..12` and `TC-11.1-API-01..12` test cases from the sprint test plan.

## Prerequisites

Run these before executing the suite:

1. **Kafka** - `docker compose up kafka` (from the repo root). Default: `localhost:9092`.
2. **identity-service** - `dotnet run` (default: `http://localhost:5278`).
3. **notification-service** - `dotnet run` (default: `http://localhost:5185`).
4. **frontend** - `npm run dev` in `frontend/` (default: `http://localhost:5173`).
5. *Optional, only for the "(bonus)" tests* - **booking-service** (`http://localhost:5229`) and
   **provider-catalog-service** (`http://localhost:5141`), with at least one active, bookable
   experience listing. These tests auto-skip (with a logged reason) if that data isn't present.

No provider account, Stripe keys, or seed data are required for the core 24 test cases - every
test provisions its own fresh visitor account(s) via `POST /api/auth/register` (no OTP needed for
visitor accounts).

## Running

```powershell
dotnet test qa-testing/CeylonQuest.Tests --filter "FullyQualifiedName~Sprint4Notifications"
```

Or run the `Api` and `Ui` folders independently from the Test Explorer.

### Useful environment variables

| Variable | Default | Purpose |
|---|---|---|
| `CQ_FRONTEND_URL` | `http://localhost:5173` | Frontend dev server |
| `CQ_IDENTITY_API_URL` | `http://localhost:5278` | identity-service |
| `CQ_NOTIFICATION_API_URL` | `http://localhost:5185` | notification-service |
| `CQ_BOOKING_API_URL` | `http://localhost:5229` | booking-service (bonus tests only) |
| `CQ_CATALOG_API_URL` | `http://localhost:5141` | provider-catalog-service (bonus tests only) |
| `CQ_KAFKA_BOOTSTRAP` | `localhost:9092` | Kafka bootstrap servers |
| `CQ_HEADLESS` | `true` | Set to `false` to watch Chrome while debugging |

## Why events are published directly to Kafka

Several scenarios in the test plan depend on systems this harness cannot reliably control:

- **Provider accounts** need OTP email activation *and* admin approval
  (`ProviderActivationService.cs`) - there is no automatable path to a logged-in provider.
- **Stripe payment completion** relies on a webhook call back into the developer's machine
  (`stripe listen --forward-to .../api/Payments/webhook`), which isn't guaranteed to be running.
- **Review submission** has no UI or API yet anywhere in the codebase (confirmed by searching
  `frontend/src` and both services) - it's explicitly flagged `⚠️` in the test plan.

Instead, these tests publish the exact JSON contracts `booking-service` emits (see
`services/notification-service/Events/*.cs`) straight to Kafka, using two or three real,
self-registered visitor accounts as recipients (`notification-service` doesn't check roles - only
that the token's identity matches the notification's recipient GUID). This exercises the real
`notification-service` consumer, database, and REST API end-to-end, which is what Story 11.1 is
actually responsible for, independent of `booking-service`/Stripe/review bugs that belong to other
stories.

A handful of "(bonus)" tests additionally call the real `booking-service` API (and, for cancellation,
drive the real "My Bookings" cancel UI) when the environment has bookable data, for genuine
cross-service coverage.

## Known gaps found while writing these tests

These are QA findings against the story's acceptance criteria, not test bugs - several assertions
are written to match the *story's* expected result and will fail until the product addresses them:

- `booking.created` notification text includes the provider's business name, date, time slot and
  participant count, but **not** the experience/listing title, and **no human-readable booking
  reference** is ever shown (only the raw `bookingId` field, which the panel doesn't render).
- `booking.canceled` notification text includes a refund **amount** and a `RefundStatus` string
  (e.g. `PartialRefund`), but **no refund percentage**, even though several test cases expect one.
- `PATCH /api/notifications/{id}/read` uses a `{id:guid}` route constraint; a malformed GUID most
  likely falls through to a plain **404** rather than the **400** the test plan expects, since ASP.NET
  Core route constraints are evaluated before any controller code runs.
- Provider-initiated cancellation (`TC-11.1-UI-07`) and review submission (`TC-11.1-UI-08`,
  `TC-11.1-API-06`'s UI side) are not implemented anywhere yet - tests are marked skipped per the
  test plan's own `⚠️` notes.
