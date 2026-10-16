# CeylonQuest visitor UI/E2E test cases

Priority: complete visitor booking journey, execution evidence, then extra validation.
Implementation is isolated to `qa-testing/CeylonQuest.E2E.Tests/`.

## Repository workflow verified from source

| Stage | Actual component / navigation | Backend interaction |
| --- | --- | --- |
| Home → Login | `HomeNavbar`: `#nav-login-btn`; `Login`: `#login-email`, `#login-password`, `#login-button` | Identity `POST /api/auth/login` |
| Visitor authentication | `App` switches React state to visitor dashboard; dashboard initially opens My Profile | Identity profile load; authenticated token/role stored by the real app |
| Accommodation search | `#nav-explore`; `.vd-search-bar__input`; service-type select; `.vd-service-card` | Catalog `GET /api/catalog/search?pageSize=50`; browser filters the response |
| Accommodation details | `.vd-detail-modal`; View Details / Book Now buttons | Details are a modal, not a separate listing URL |
| Booking form | `.vd-booking-modal`; Check-in Date, Check-out Date, Stay Availability, Guests | Catalog `GET /api/catalog/availability/{listingId}?date=...` |
| Booking submission | Book Stay | Booking `POST /api/AccommodationBookings`, then `POST /api/Payments/create-checkout-session` |
| Payment confirmation | Hosted Stripe checkout → `/payment/success`; real app verifies payment | Booking `GET /api/Payments/booking/{id}?bookingType=Accommodation` |
| Visitor bookings | Back to My Bookings / `#nav-bookings`; table and detail modal | Booking `GET /api/user-bookings/my`; UI refreshes every 5 seconds |
| Notifications | `#nav-notifications`; `section[aria-label='Notifications']` | `GET /api/notifications`; UI refreshes every 30 seconds; notification service processes Kafka events |

Authentication, catalog, and booking use separately configured API origins.
The notification client uses `VITE_NOTIFICATION_API_BASE_URL` when present,
otherwise falls back to the Identity API origin. The checked-in frontend production
environment and static-site deployment workflow do not define that notification
variable. This is a configuration risk requiring live verification, not proof that
the deployed notification service is broken. No configuration files are changed.

## Scenarios

| ID | Test / prerequisites | Steps | Expected result |
| --- | --- | --- | --- |
| UI-01 | `Home_LoginNavigation_DisplaysLoginForm`; public site reachable | Open home; click Login | Visible email/password inputs and submit button with correct field types |
| AUTH-01 | `ValidVisitorLogin_DisplaysVisitorDashboard`; environment credentials for an active Visitor | Navigate from home; submit credentials | Visitor dashboard rendered; real app stores token and Visitor role; visitor navigation available |
| AUTH-02 | `InvalidCredentials_DisplayAuthenticationError`; Identity service reachable | Submit a unique reserved invalid email and random invalid password | Actual invalid-credentials error; no token or visitor navigation |
| AUTH-03 | `EmptyLoginFields_DisplayValidationError`; Identity service reachable | Submit empty login form | Backend 400 causes actual login validation banner; remains unauthenticated |
| SEARCH-01 | `AccommodationSearch_OpensMatchingDetails`; visitor credentials and unique accommodation title | Login; Explore and Search; select Accommodations; search; View Details | Exact title/type match and price shown in details modal |
| BOOK-VAL-01 | `MissingCheckoutDate_DisablesBookingSubmission`; valid available stay configuration | Login; open booking; establish valid available form; clear checkout date | Checkout is empty; booking submission disabled; no booking POST |
| BOOK-VAL-02 | `CheckoutBeforeCheckin_DisablesBookingSubmission`; valid available stay configuration | Establish valid available form; enter checkout before check-in | Invalid date retained for validation; booking submission disabled; no booking POST |
| E2E-01 | `VisitorBooking_ConfirmsPaymentBookingAndNotification_ManualStripeStep`; all visitor/stay settings, booking opt-in, approved Stripe test mode, visible browser and operator | Login → search → details → dates/availability/guests → Book Stay → manually complete Stripe test payment → success → My Bookings → Notifications | New booking is Confirmed/Paid with correct dates/guests; rendered booking details match; rendered booking/payment notification correlates to exact new booking ID |

Every executable test uses a fresh browser and can run independently. E2E-01 is
one journey, not four ordered test methods. Its stage logs/screenshots show how
far execution reached; partial completion is not a full journey pass.

## Manual follow-up cases

These are documented cases, not automated passes:

| ID | Steps | Expected result |
| --- | --- | --- |
| MAN-01 | Enter guest count above listing maximum | App rejects or clamps the value with appropriate feedback; invalid booking cannot be submitted |
| MAN-02 | Choose an unavailable/provider-closed check-in date | Unavailable state is visible; submission disabled |
| MAN-03 | Cancel the approved test Stripe checkout | App shows payment cancellation; booking is not falsely shown as confirmed/paid |
| MAN-04 | Submit a valid form while another visitor consumes the final unit | Conflict is shown; no duplicate confirmed booking or forced inventory bypass |
| MAN-05 | Observe delayed/missing notifications after confirmed payment | Bounded wait; notification issue recorded with booking ID and evidence, never a false pass |

## Execution assessment

- Pass: all assertions for the case were actually executed successfully.
- Fail: application/driver/service error, incorrect UI state, invalid configured data, or timeout.
- Skipped: explicit missing prerequisites or booking/payment opt-in absent.
- Not run: manual follow-up or an execution not attempted; never count as a pass.

The payment provider step is explicitly manual. Stripe's hosted checkout is not
claimed as unattended Selenium coverage. Application notifications are asynchronous;
an unavailable deployment is recorded as a limitation and a failed attempted journey.
