# Notification service tests

Run from the repository root:

```powershell
dotnet test services/notification-service/NotificationService.Tests/NotificationService.Tests.csproj --collect:"XPlat Code Coverage" --settings services/notification-service/NotificationService.Tests/coverage.runsettings
```

If notification service is running and locks build files, stop it first or add
`--output .verification/notification-suite` to build into a separate directory.
The command prints the generated `coverage.cobertura.xml` path. Do not commit
generated results or `.verification` build outputs.

## Test responsibilities

- `NotificationTests.cs`: provider-name messages, recipient mapping, replay handling,
  basic malformed events, and account isolation.
- `NotificationProcessorTests.cs`: required event fields, invalid amounts and ratings,
  failed-event recovery, replay deduplication across all topics, identical recipients,
  cancellation refund details, and stable failure keys.
- `RecipientResolverTests.cs`: supplied/configured identities, all catalog listing types,
  unavailable catalog endpoints, missing identities, stored booking context, and event
  identity precedence.
- `NotificationReadTests.cs`: owner-only individual/all read updates, persisted timestamps,
  repeated read calls, missing records, invalid identities, `sub` claims, pagination,
  unread totals, and empty accounts.

Read tests use isolated in-memory SQLite databases to execute the real relational
`ExecuteUpdate` operations. Mapping tests use EF InMemory and do not validate transaction
rollback. Neither requires Azure or Kafka. SQLite tests complement rather than replace
the existing MySQL integration test.

## Coverage scope and limits

The coverage settings include only the NotificationService assembly and exclude generated
migrations. Startup, the design-time context factory, and the live Kafka consumer remain
in the denominator. Controller method tests do not test HTTP middleware or JWT validation.
Live Kafka offset/reconnection behavior and MySQL concurrency/transactions need integration
tests. The existing MySQL test is skipped unless `NotificationsTestConnectionString` points
to a dedicated database whose name ends in `_test`.
