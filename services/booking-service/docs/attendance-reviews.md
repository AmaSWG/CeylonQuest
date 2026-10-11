# Attendance-based listing reviews

This extends the existing experience, restaurant and accommodation review system.
General platform reviews are unchanged.

## Workflow and policy

- BookingStatus remains separate from AttendanceStatus: Pending, Attended, NoShow.
- Passing the scheduled end does not change attendance or enable reviews.
- An owning provider can verify a confirmed/completed booking strictly after its known scheduled end.
- Verification explicitly marks the booking Completed and records attendance, the identity user who verified it and UTC time. Payment and cancellation data are preserved.
- Provider verification is allowed once. Further corrections go through an audited administrative dispute resolution.
- Review submission requires ownership, Completed, Attended, a known past scheduled end and no previous review for that booking/type.
- Cancellation/pending-payment states remain ineligible. There is no new payment-status-only exclusion for an otherwise eligible attended booking; existing cancellation/refund policy is preserved.
- The existing unique review index and transaction/outbox prevent duplicates and publication before persistence.

Visitor: My Bookings → attendance confirmed → Write Review → Review Submitted.
Public listing details display reviews and provider responses. Submission is available only through My Bookings.
Provider: Bookings → Mark Attended/NoShow → Reviews & Ratings → respond.
Visitor disputes are available for past Pending/NoShow records; Admin → Attendance Disputes → resolve with evidence and a recorded outcome.

The UI retains pagination and newest-first default order. There are no review filtering/sorting controls.
The existing API rating query remains for compatibility. Listing card summaries continue using catalog projections; review lists and immediate submission refresh use authoritative booking-service summaries.

## New endpoints

| Method | Path | Role |
| --- | --- | --- |
| PATCH | `/api/provider-bookings/{bookingType}/{bookingId}/attendance` | Provider |
| POST | `/api/user-bookings/{bookingType}/{bookingId}/attendance-dispute` | Visitor |
| GET | `/api/admin-bookings/attendance-disputes` | Admin |
| PATCH | `/api/admin-bookings/attendance-disputes/{id}` | Admin |
| PUT | `/api/bookings/reviews/{reviewId}/response` | Provider |

Booking types are exactly Experience, Restaurant or Accommodation.
Attendance payload: `{ "status": "Attended" }` or `{ "status": "NoShow" }`.
Dispute payload: `{ "reason": "I attended; please investigate." }`.
Resolution payload: `{ "status": "Attended", "resolution": "Evidence confirms attendance." }`.
Provider response payload: `{ "comment": "Thank you for your feedback." }`.
Text fields must contain non-whitespace text and have at most 2000 characters.
Authorization, ownership and schedule checks are enforced server-side; concurrency conflicts return 409.

The existing user/provider booking list endpoints now expose attendance and scheduled-end information.
The visitor list also exposes CanReview, ReviewId and ReviewIneligibilityReason.
Public review DTOs now include the response text/date and whether attendance was verified at submission.
They continue omitting visitor IDs and other private booking information.

## Events

ReviewSubmittedEvent remains version 1 with additive optional ProviderUserId and ReviewText fields.
Catalog consumers continue using the existing fields and deduplicated rating projection.
Notification-service accepts canonical SubmittedAtUtc and legacy SubmittedAt payloads.
Catalog ProviderId is not used as an identity recipient ID; the existing trusted ProviderUserId/listing lookup resolves the recipient.

Attendance verification saves `review.eligibility-granted` in the existing review outbox in the same transaction.
Only an eligible, unreviewed Attended booking gets an invitation. NoShow/Pending/cancelled records get none.
The existing notification consumer processes the new topic and deduplicates by EventId.
Ensure this Kafka topic exists when broker automatic topic creation is disabled.
Provider responses do not publish rating events or modify rating totals.

## Migration and rollout

`20261011074209_AddAttendanceVerificationAndReviewResponses` adds:

- Pending attendance and nullable verification audit columns to each of the three booking tables.
- Nullable provider-response fields and an attendance-verification snapshot to ListingReviews.
- Topic to ReviewOutboxMessages, defaulting existing messages to review.submitted.
- AttendanceDisputes with original attendance/audit details, administrative resolution and one dispute per booking/type.

It does not update historical booking/payment statuses or delete existing reviews.
Historical review attendance snapshots default false. The Verified Booking label describes the stored booking relationship, not assumed historical attendance.

Stop the locally running booking service before rebuilding its normal output, apply the migration using the configured BookingDb user secret, then restart booking and notification services and Vite:

```powershell
dotnet ef database update --project services/booking-service/booking-service.csproj
```

The migration has been generated, not applied to the database by this implementation.
The Vite proxy now routes admin-bookings attendance requests to booking-service.

## Validation and remaining integration checks

Booking-service and notification-service compile with the isolated ReviewVerification configuration; frontend production build and new review-component ESLint checks pass.
110 targeted review/attendance/booking-list tests pass and cover authorization, schedules, eligibility, NoShow, provider responses, dispute resolution, invitation retry and migration defaults.
74 notification tests pass (one MySQL test skipped) and cover the producer timestamp contract and visitor invitation deduplication.
18 catalog rating projection and booking-event checks pass.

The broader booking suite has three pre-existing revenue-report count failures, reproduced using the saved earlier assembly; reporting was left unchanged.
Notification MySQL integration testing is skipped without a configured test database.
Live browser/Swagger, MySQL concurrency and end-to-end Kafka delivery still need checking after migration and restart.
Legacy bookings missing a trustworthy ScheduledEndAtUtc/range still require a schedule backfill from actual historical booking data; the system does not invent attendance or schedules.

## Modified and added files

- `frontend/src/api/reviewApi.js`
- `frontend/src/components/AttendanceActions.jsx`
- `frontend/src/components/AttendanceDisputeAction.jsx`
- `frontend/src/components/ListingReviews.css`
- `frontend/src/components/ListingReviews.jsx`
- `frontend/src/components/ProviderReviewResponse.jsx`
- `frontend/src/features/admin/components/AdminAttendanceDisputesTab.jsx`
- `frontend/src/features/admin/pages/AdminDashboard.jsx`
- `frontend/src/features/provider/components/ProviderBookingsTab.jsx`
- `frontend/src/features/provider/components/ProviderReviewsTab.jsx`
- `frontend/src/features/provider/pages/ProviderDashboard.jsx`
- `frontend/src/features/visitor/components/VisitorBookingsTab.jsx`
- `frontend/vite.config.js`
- `services/booking-service/BookingService.Tests/AttendanceTests.cs`
- `services/booking-service/BookingService.Tests/ReviewTests.cs`
- `services/booking-service/Controllers/AttendanceController.cs`
- `services/booking-service/Controllers/ProviderBookingsController.cs`
- `services/booking-service/Controllers/ReviewsController.cs`
- `services/booking-service/Controllers/UserBookingsController.cs`
- `services/booking-service/DTOs/ProviderBookingResponse.cs`
- `services/booking-service/DTOs/ReviewResponse.cs`
- `services/booking-service/DTOs/UserBookingResponse.cs`
- `services/booking-service/Data/BookingDbContext.cs`
- `services/booking-service/Events/ReviewEligibilityGrantedEvent.cs`
- `services/booking-service/Events/ReviewSubmittedEvent.cs`
- `services/booking-service/Migrations/20261011074209_AddAttendanceVerificationAndReviewResponses.Designer.cs`
- `services/booking-service/Migrations/20261011074209_AddAttendanceVerificationAndReviewResponses.cs`
- `services/booking-service/Migrations/BookingDbContextModelSnapshot.cs`
- `services/booking-service/Models/AccommodationBooking.cs`
- `services/booking-service/Models/AttendanceDispute.cs`
- `services/booking-service/Models/AttendanceStatus.cs`
- `services/booking-service/Models/Booking.cs`
- `services/booking-service/Models/IPayableBooking.cs`
- `services/booking-service/Models/ListingReview.cs`
- `services/booking-service/Models/RestaurantReservation.cs`
- `services/booking-service/Models/ReviewOutboxMessage.cs`
- `services/booking-service/Services/AttendanceRules.cs`
- `services/booking-service/Services/ReviewOutboxPublisher.cs`
- `services/booking-service/Services/ReviewService.cs`
- `services/booking-service/docs/attendance-reviews.md`
- `services/notification-service/Events/ReviewEligibilityGrantedEvent.cs`
- `services/notification-service/Events/ReviewSubmittedEvent.cs`
- `services/notification-service/NotificationService.Tests/NotificationProcessorTests.cs`
- `services/notification-service/Services/NotificationEventConsumer.cs`
- `services/notification-service/Services/NotificationEventProcessor.cs`

- `services/booking-service/docs/reviews.md`
