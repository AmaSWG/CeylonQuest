# Booking service tests (Story 12.1)

Run from the repository root:

```powershell
dotnet test services/booking-service/BookingService.Tests/BookingService.Tests.csproj --no-build --filter "FullyQualifiedName~ReviewTests" --collect:"XPlat Code Coverage" --settings services/booking-service/BookingService.Tests/coverage.runsettings
```

Generate the HTML and Summary report using ReportGenerator:

```powershell
reportgenerator -reports:"services/booking-service/BookingService.Tests/TestResults/**/coverage.cobertura.xml" -targetdir:"services/booking-service/BookingService.Tests/coverage-report" -assemblyfilters:"+booking-service" -classfilters:"+*Review*;-*Migrations*;-*MigrationContextFactory*" -reporttypes:"Html;TextSummary"
```

The generated coverage report is available at:
`services/booking-service/BookingService.Tests/coverage-report/index.html`

## Test responsibilities

- `ReviewTests.cs`: 
  - `ReviewsController`: Role-based visitor access validation for submission, public anonymous listing review reads, and exception mapping (409 Conflict, 400 Bad Request, etc.).
  - `ReviewService`: Ownership checks, completed status validation, schedule/slot completion checks, duplicate review prevention, ratings calculation (count and average), reviewer display name enrichment, and transactional outbox event queuing.
  - `ReviewSchedule`: Sri Lanka time zone parsing, overnight experience slot calculation, and accommodation checkout rules.
  - `ReviewOutboxPublisher`: Reading unpublished outbox messages, publishing `review.submitted` events to Kafka, and marking outbox entries as processed.
