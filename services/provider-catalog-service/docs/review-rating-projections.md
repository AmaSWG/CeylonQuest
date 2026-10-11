# Catalog review rating projections

Booking-service remains the source of truth for reviews and review eligibility.
Catalog-service now consumes `review.submitted` and maintains `RatingSum`, `ReviewCount`
and `AverageRating` on activity, restaurant and accommodation listings. Public listing,
provider listing, detail and unified search responses include `averageRating` and
`reviewCount`. Review text, star filtering and pagination remain in booking-service.

## Processing and consistency

The consumer uses its own group, `provider-catalog-service-review-ratings`, so notification
consumers receive the same events independently. It starts at the earliest retained
offset when this group has no committed offsets. It accepts event contract version 1
and validates the identifiers, supported booking type and rating before processing.
Malformed or unsupported events are logged and ignored without logging review text.

`ReviewRatingContributions` is a durable inbox keyed by ReviewId, with a unique EventId.
Within one MySQL transaction, the service inserts a contribution, increments the listing's
sum/count and calculates the average from exact integer totals. Repeated events, including
the same review published with a different EventId, do not increment totals twice.
Concurrent updates use SQL increments rather than a read/overwrite calculation.
Both catalog and booking summaries round to two decimal places, with halfway values
rounded away from zero. Listing updates use sum/count concurrency tokens.

Kafka offsets commit only after processing succeeds. Database errors propagate to the
existing Kafka consumer retry loop. Broker outages leave booking-service's review outbox
pending. Ratings are eventually consistent: publication runs every five seconds and
consumer/broker outages can delay updates further. Use the booking summary endpoint for
an immediate refresh after submitting a review.

If a listing is physically deleted while its event is in transit, the contribution is
retained and logged without blocking other events in the partition. A restored listing
can rebuild its totals from those retained contributions. Inactive listings still receive
rating updates even though they do not appear in public discovery.

## Migration and operation

Configure `ConnectionStrings__ProviderCatalogDb` for the intended catalog database and
apply the migration before starting the updated service:

```powershell
dotnet ef database update --project services/provider-catalog-service/ProviderCatalogService.csproj
```

`AddReviewRatingProjections` adds the rating columns with zero defaults and the durable
contribution table. It does not recreate BookingCapacityReleases: an earlier migration
already created that table although the older model snapshot omitted it. The new model
snapshot includes the existing table so subsequent migrations remain accurate.

Existing ratings initialize to zero until retained review events are consumed. Verify
that both services use the same Kafka cluster and that `review.submitted` is available
and permits the producer/consumer to access it. The migration has not been applied
automatically, and no real-broker/MySQL integration verification has been performed.

## Rebuild and recovery

An authenticated Admin can repair a listing from the durable contribution ledger:

```http
POST /api/catalog/review-ratings/{listingId}/rebuild?bookingType=Experience
Authorization: Bearer <admin-token>
```

BookingType must be `Experience`, `Restaurant` or `Accommodation`. Success returns 204;
invalid input 400; a missing listing 404. The rebuild runs transactionally and preserves
deduplication records. It repairs a corrupted listing projection but cannot recover
events that catalog-service has never received.

For missing historical events, replay the original `review.submitted` events. Published
booking-service outbox rows retain their original EventId/payload; an operator can
requeue the required rows by clearing PublishedAtUtc in a controlled maintenance window.
This also redelivers to notification consumers, which must deduplicate EventId. Verify
that the original outbox rows are retained before using this recovery method. Existing
catalog contributions ensure replay does not double count previously consumed reviews.
Do not overwrite catalog totals from a summary while leaving missing deduplication
records: later replay of those reviews would increment those totals again.

Keep the booking summary endpoint as the authoritative comparison for reconciliation.
No changes to review creation or listing ownership are required for this projection.

## Verification

```powershell
dotnet test services/provider-catalog-service/ProviderCatalogService.Tests/ProviderCatalogService.Tests.csproj
dotnet test services/booking-service/BookingService.Tests/BookingService.Tests.csproj --filter FullyQualifiedName~ReviewTests
```

Tests cover all listing types, duplicates, exact totals/rounding, projection rebuilds,
deleted listings, input validation, consumer deserialization/failure behavior, public
listing/search responses and schema metadata. The catalog test project's EF packages
are aligned with the service's 8.0.30 version. In-memory tests do not establish MySQL
transaction isolation or concurrent unique-index enforcement; verify concurrent delivery,
rollback and broker restart against the integration environment before deployment.
