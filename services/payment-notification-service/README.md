# Payment/Notification Service
Owner: Somarathne H.D.P.Y 
Status: Backend implemented; upstream event contracts and live integration need verification.

## Local configuration

Run commands from the repository root. The project already has a UserSecretsId.
Do not put database passwords in Program.cs or tracked appsettings files.

```powershell
dotnet user-secrets set "ConnectionStrings:PaymentNotificationDb" "Server=localhost;Port=3306;Database=ceylonquest_notifications;User=YOUR_USER;Password=YOUR_PASSWORD;" --project services/payment-notification-service
dotnet user-secrets set "Jwt:Key" "SAME_KEY_AS_IDENTITY_SERVICE" --project services/payment-notification-service
dotnet ef database update --project services/payment-notification-service --startup-project services/payment-notification-service
dotnet run --project services/payment-notification-service --launch-profile http
```

Use your actual MySQL host, database, user and password. The database user needs migration permissions
when running database update. MySQL must already be running. Adjust DatabaseServerVersion to your server.
No migrations run automatically at application startup. The design-time factory runs migrations without Kafka.
User secrets live outside the repository and are for development; they are not encrypted.
Production should supply ConnectionStrings__PaymentNotificationDb and Jwt__Key through deployment secrets.
Double underscores map to configuration separators. JWT issuer, audience and the SHA-256-derived signing
key must match identity-service. The development fallback key is rejected outside Development.

## Recipient integration

Subscriptions: booking.created, payment.completed, booking.canceled, review.submitted.
The group defaults to payment-notification-service and is distinct from catalog consumer groups.
DTOs match existing producer payloads; optional extra fields support future enriched contracts.
ReviewSubmittedEvent is a proposed contract: no review publisher currently exists in booking-service.

ProviderUserId means the identity-service user ID, not the catalog Provider.Id.
Current booking.created payloads lack that ID. Configure an explicit listing mapping for local integration:

```powershell
dotnet user-secrets set "Notifications:ListingProviders:ACTUAL_LISTING_GUID" "PROVIDER_IDENTITY_USER_GUID" --project services/payment-notification-service
```

Successful booking creation stores the visitor/provider pair in BookingContexts. Cancellation uses this
context when its payload lacks recipient IDs. For bookings predating consumption, configure
Notifications:BookingVisitors:BOOKING_GUID plus the listing provider mapping, or supply enriched events.
Missing recipients fail processing and are retried. There are no invented lookup endpoints or direct reads
of other service databases. Static mappings are an integration fallback; production needs agreed enriched
events or a real service lookup contract. Cross-topic ordering is not guaranteed: a cancellation without
context may arrive before creation. Supply recipient IDs/mappings to resolve this; the consumer currently
restarts rather than skipping the unresolved record and can delay other notifications.

Cancellation notifications include refund status/amount/currency only when supplied. The current producer
does not supply these fields. Payment currency also needs upstream enrichment. Never infer refund success.

## Persistence, retry and replay

Notification insertion, booking context, and ProcessedEvent insertion share a transaction.
Unique event keys and (EventKey, RecipientUserId) constraints protect against replay and concurrent delivery.
Offsets commit only after the database transaction commits. Database/lookup/commit errors recreate the
consumer after Kafka:RetryDelaySeconds, starting from committed offsets. No later record is committed past
a failed record. Every attempt uses a fresh dependency-injection scope.

Prefer a stable producer EventId. Existing contracts fall back to topic + BookingId for creation,
topic + PaymentId for payment, topic + ReviewId for reviews, and topic + BookingId + CanceledAt for
cancellation. These assume one creation/payment-completion/review-submission per corresponding business ID,
and a stable cancellation timestamp. Missing required timestamps and empty IDs are rejected.

FailedEvents stores raw payload, latest error and attempt count when the database is available.
Successful retry resolves its failure record. If failure persistence fails, the Kafka offset still remains
uncommitted. Permanently malformed records remain uncommitted and block progress; an operator must repair
the producer/recipient configuration or arrange a controlled corrected replay and offset recovery.
There is no automatic dead-letter-and-skip policy. Kafka retention must cover outages and recovery.

## API

All routes require an identity-service JWT with a GUID NameIdentifier/sub claim:

- GET /api/notifications?page=1&pageSize=20 (maximum pageSize 100)
- GET /api/notifications/unread-count
- PATCH /api/notifications/{id}/read
- PATCH /api/notifications/read-all

GET returns items, pagination, totalCount and unreadCount. Read responses return updatedCount and
unreadCount. Reads are idempotent and preserve the first read timestamp. Other users' IDs return 404.
PaymentNotificationService.http contains request examples. No frontend or gateway files were changed.

## Verification

```powershell
dotnet test services/payment-notification-service/PaymentNotificationService.Tests/PaymentNotificationService.Tests.csproj
```

Unit tests verify event mapping, replays, missing-recipient recovery, invalid events and user isolation.
The in-memory provider does not verify MySQL transactions, indexes, migrations or ExecuteUpdate.
To enable the separate MySQL integration test, set NotificationsTestConnectionString to a dedicated
database whose name ends in _test. It applies migrations, tests duplicate constraints and read operations,
and rolls back its records. Do not use a production database. Live Kafka restart/rebalance and concurrent
consumer integration still need testing against the deployed infrastructure.
