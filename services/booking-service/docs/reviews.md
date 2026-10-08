# Review API

Booking-service owns reviews for experiences, restaurant reservations and accommodation stays.
Public responses omit booking IDs, visitor IDs, provider IDs and payment information.

## Endpoints

| Method | Path | Access |
| --- | --- | --- |
| POST | `/api/bookings/reviews` | JWT with Visitor role |
| GET | `/api/bookings/reviews/{listingId}` | Public |
| GET | `/api/bookings/reviews/{listingId}/summary` | Public |
| POST | `/api/bookings/platform-reviews` | JWT with Visitor role |
| GET | `/api/bookings/platform-reviews` | Public |

Listing-review request:

```json
{
  "bookingId": "00000000-0000-0000-0000-000000000001",
  "bookingType": "Restaurant",
  "rating": 5,
  "comment": "Great food and friendly service."
}
```

`bookingType` is exactly `Experience`, `Restaurant`, or `Accommodation`.
The booking must exist, belong to the JWT visitor, be Completed, have a known scheduled
end strictly before now, and have no existing review. Rating is an integer from 1 to 5;
comment must contain text and be at most 2000 characters. The service derives listing
and provider IDs from trusted booking/catalog information.

GET query parameters: `bookingType` (optional for listing reviews), `rating` (1–5),
`page` (1–1000000, default 1), `pageSize` (1–100, default 10).
Results sort by creation timestamp then ID, descending. `totalCount` and `totalPages`
describe filtered results. `averageRating` and `reviewCount` always describe all
reviews for the selected listing/type. An unreviewed listing returns zero average/count.
The summary is calculated from saved reviews, so concurrent submissions cannot lose
aggregate updates; averages round to two decimal places.

Creation returns 201. Invalid input returns 400; missing authentication 401;
wrong role/ownership 403; missing or deleted booking 404; eligibility/duplicate conflicts
409; unavailable provider resolution 503. Domain errors use ProblemDetails.

## Scheduled ends and completion

New bookings snapshot provider ID and scheduled UTC end. Experience end is calculated
from the selected slot range or a start time plus catalog duration. Restaurant end uses
the slot range, including overnight slots. Formats include `09:00 - 11:00` and
`9:00 AM - 11:00 AM`; simple duration strings such as `2 Hours` are supported.
Unknown formats leave the end unset and reviews are rejected until it is corrected.

Accommodation checkout defaults to **12:00 noon Asia/Colombo**. Override configuration
`Reviews:AccommodationCheckoutTime` (environment variable
`Reviews__AccommodationCheckoutTime`) with a time such as `11:00` before creating
bookings. The chosen value is stored with the booking and later configuration edits
do not change existing booking eligibility. Invalid configured times fail closed.
This is a platform-wide default until per-property checkout times are implemented.

For existing bookings, an explicit booked slot range can establish an end without
consulting a mutable listing. Existing start-only experiences and accommodation stays
need `ScheduledEndAtUtc` backfilled from their actual booking schedule. Do not backfill
an experience from a listing duration that has changed since the booking.
Missing provider snapshots can be resolved from catalog-service; unresolved providers
are rejected so notifications always have a recipient identifier.

Review submission never completes a booking. The booking-completion workflow must
mark all three service types Completed independently. Restaurant's Completed status
was already added to ReservationStatus in the working tree. Existing restaurant
reporting and status consumers should be checked when integrating that workflow.

Booking relationships are validated against the three existing booking tables by
booking type and ID. A single polymorphic BookingId has no database foreign key to
those three tables. `(BookingType, BookingId)` has a database unique index.

## General platform feedback

Platform requests contain only `rating` and `comment`. Any authenticated Visitor can
submit feedback; a completed booking is not required. Multiple submissions are allowed
because no one-per-visitor rule was specified. Platform feedback is stored separately,
does not affect listing ratings, and does not publish a provider review notification.
GET supports star filtering and pagination; `bookingType` is rejected.

## Kafka

Listing creation saves a review and `ReviewOutboxMessage` in the same transaction.
The worker publishes committed messages to `review.submitted` every five seconds and
marks them published only after Kafka acknowledges. Failed sends remain pending.
Delivery is at least once; consumers must deduplicate using `EventId`.

Event fields: `EventId`, `EventVersion`, `ReviewId`, `BookingId`, `BookingType`,
`ListingId`, `ListingTitle`, `VisitorId`, `ProviderId`, `Rating`, `SubmittedAtUtc`.
ProviderId is the catalog provider ID; notification-service must resolve its
IdentityUserId when delivering an account notification. No booking/payment secrets
are included. Notification-service implementation and broker verification are separate
integration work.

## Database and validation

Two migrations add review/outbox tables and booking schedule/provider snapshots.
The listing migration intentionally does not recreate BookingCancellationMessages:
the older cancellation migration already creates that table, although the old model
snapshot omitted it. The new snapshot includes it for future migration correctness.

From this service directory, configure `ConnectionStrings__BookingDb` for the intended
database, then run:

```powershell
dotnet ef database update
dotnet test BookingService.Tests/BookingService.Tests.csproj --filter FullyQualifiedName~ReviewTests
```

Migrations have not been applied automatically. The design-time factory allows EF
commands without starting Stripe/Kafka workers. Review tests cover the controller,
eligibility for all three booking types, validation, duplicates, summaries, pagination,
provider resolution, Kafka retry behavior, schedule parsing, model constraints, and
generated Swagger schemas. In-memory tests do not verify MySQL transaction isolation
or simultaneous unique-index enforcement; exercise those against the integration DB.

Swagger is available in Development at `/swagger`. The frontend can read the summary
endpoint for listing cards/details and use the returned creation response immediately.
Catalog-service now maintains rating projections from `review.submitted` and includes
average/count in its listing and search responses. See
`../../provider-catalog-service/docs/review-rating-projections.md` for migration,
eventual consistency and recovery instructions. The booking summary remains authoritative.
Frontend wiring, gateway routing and notification consumers remain separate integration work.
