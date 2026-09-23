import React, { useEffect, useState } from 'react'
import './VisitorBookingModal.css'

import {
  CloseIcon,
  CalendarMonthIcon,
  CheckCircleIcon,
  DangerIcon,
  LocationOnIcon,
  HouseIcon,
  RestaurantIcon,
  AccessTimeFilledIcon,
  GroupIcon
} from '../../../components/Icons'

import { catalogUrl, bookingUrl } from '../../../api/client'

export default function VisitorBookingModal({
  item,
  onClose,
  onBookingSuccess
}) {
  const [selectedDate, setSelectedDate] = useState(
    new Date(Date.now() + 86400000).toISOString().split('T')[0]
  )

  const [selectedTimeSlot, setSelectedTimeSlot] = useState('')

  // Can temporarily be '' while the user is editing the number input.
  const [guestCount, setGuestCount] = useState(1)

  const [availability, setAvailability] = useState(null)
  const [loadingAvail, setLoadingAvail] = useState(false)
  const [availError, setAvailError] = useState(null)

  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState(null)

  const isRestaurant = item?.type === 'Restaurant'
  const isAccommodation = item?.type === 'Accommodation'
  const isExperience = item?.type === 'Experience'

  /*
   * Decide how the listing price is measured.
   */
  const unit =
    item?.unit ||
    (isAccommodation ? 'night' : 'person')

  /*
   * Maximum capacity depends on listing type.
   */
  const maxCap = item
    ? isAccommodation
      ? item.maxGuests || 4
      : isRestaurant
        ? item.seatingCapacity || 1
        : item.maxParticipants || 10
    : 1

  /*
   * Load availability whenever the listing
   * or selected date changes.
   */
  useEffect(() => {
    if (!item || !selectedDate) return

    const fetchAvailability = async () => {
      setLoadingAvail(true)
      setAvailError(null)
      setAvailability(null)
      setSelectedTimeSlot('')

      try {
        const response = await fetch(
          catalogUrl(
            `/api/catalog/availability/${item.id}?date=${selectedDate}`
          )
        )

        if (!response.ok) {
          setAvailError(
            'Unable to check availability for this date.'
          )
          return
        }

        const data = await response.json()

        setAvailability(data)

        /*
         * Automatically choose the first time slot
         * that still has capacity.
         */
        const firstAvailableSlot = data.slots?.find(
          (slot) =>
            !slot.isFullyBooked &&
            slot.remainingCapacity > 0
        )

        setSelectedTimeSlot(
          firstAvailableSlot?.timeSlot || ''
        )
      } catch {
        setAvailError(
          'Network error while checking availability.'
        )
      } finally {
        setLoadingAvail(false)
      }
    }

    fetchAvailability()
  }, [item, selectedDate])

  if (!item) return null

  /*
   * Check whether the listing operates
   * on the selected date.
   */
  const isNotOperating =
    availability?.isOperatingDay === false

  /*
   * Check whether every time slot is full.
   */
  const isSoldOut =
    availability &&
    (
      availability.isFullyBooked ||
      (
        availability.slots?.length > 0 &&
        availability.slots.every(
          (slot) =>
            slot.isFullyBooked ||
            slot.remainingCapacity <= 0
        )
      )
    )

  /*
   * Find currently selected time slot.
   */
  const selectedSlot = availability?.slots?.find(
    (slot) => slot.timeSlot === selectedTimeSlot
  )

  const selectedSlotRemaining =
    selectedSlot?.remainingCapacity ?? 0

  /*
   * Current maximum allowed for selected slot.
   */
  const currentMax = selectedTimeSlot
    ? Math.min(maxCap, selectedSlotRemaining)
    : maxCap

  /*
   * Convert guestCount safely.
   *
   * guestCount may temporarily be an empty string
   * while the user is typing.
   */
  const numericGuestCount =
    guestCount === ''
      ? 0
      : Number(guestCount)

  /*
   * Price calculation.
   *
   * The visitor listing data already exposes its
   * display price through item.price.
   *
   * Restaurant:
   * Price per person × party size.
   *
   * Experience:
   * Existing per-person calculation remains.
   *
   * Accommodation:
   * Existing listing-price calculation remains.
   */
  const itemPrice = Number(item?.price || 0)

  const isPerPerson =
    unit === 'person' || unit === 'guest'

  const estimatedTotal =
    itemPrice *
    (isPerPerson ? numericGuestCount : 1)

  const restaurantPricePerPerson =
    isRestaurant
      ? itemPrice
      : 0

  const restaurantTotal =
    isRestaurant
      ? restaurantPricePerPerson * numericGuestCount
      : 0

  /*
   * Guest / participant / party-size input.
   *
   * IMPORTANT:
   * Allow an empty string while editing.
   *
   * This means the user can:
   *
   * 1 -> delete -> "" -> type 4
   *
   * without React immediately putting 1 back.
   */
  const handleGuestCountChange = (e) => {
    const rawValue = e.target.value

    setSubmitError(null)

    /*
     * Allow temporary empty value.
     */
    if (rawValue === '') {
      setGuestCount('')
      return
    }

    const value = Number(rawValue)

    /*
     * Only whole numbers are valid.
     */
    if (!Number.isInteger(value)) {
      return
    }

    /*
     * Do not allow negative values.
     *
     * Zero can temporarily appear in the input,
     * but submission remains disabled and validation
     * prevents reservation creation.
     */
    if (value < 0) {
      return
    }

    /*
     * Do not allow a number above current
     * available capacity.
     */
    if (currentMax > 0 && value > currentMax) {
      setGuestCount(currentMax)
      return
    }

    setGuestCount(value)
  }

  /*
   * If the user leaves the party-size field empty
   * or with zero, restore it to 1.
   */
  const handleGuestCountBlur = () => {
    if (
      guestCount === '' ||
      !Number.isInteger(Number(guestCount)) ||
      Number(guestCount) < 1
    ) {
      setGuestCount(1)
    }
  }

  /*
   * Story 7.1:
   * Create Experience / Accommodation booking.
   *
   * Story 8.1:
   * Create Restaurant reservation.
   */
  const handleConfirmBooking = async () => {
    setSubmitError(null)

    /*
     * Validate date.
     */
    if (!selectedDate) {
      setSubmitError(
        isRestaurant
          ? 'Please select a reservation date.'
          : 'Please select a booking date.'
      )
      return
    }

    /*
     * Validate time.
     */
    if (!selectedTimeSlot) {
      setSubmitError(
        'Please select an available time slot.'
      )
      return
    }

    /*
     * Normalize guest count.
     */
    const normalizedGuestCount =
      Number(guestCount)

    /*
     * Validate party size / participant count.
     */
    if (
      guestCount === '' ||
      !Number.isInteger(normalizedGuestCount) ||
      normalizedGuestCount < 1
    ) {
      setSubmitError(
        isRestaurant
          ? 'Party size must be at least 1.'
          : 'Participant count must be at least 1.'
      )
      return
    }

    /*
     * Validate against listing maximum.
     */
    if (normalizedGuestCount > maxCap) {
      setSubmitError(
        isRestaurant
          ? `Maximum seating capacity is ${maxCap}.`
          : `Maximum capacity is ${maxCap}.`
      )
      return
    }

    /*
     * Frontend capacity validation.
     *
     * Backend validates this again to prevent
     * overbooking.
     */
    if (
      selectedSlot &&
      normalizedGuestCount >
      selectedSlot.remainingCapacity
    ) {
      setSubmitError(
        isRestaurant
          ? `Only ${selectedSlot.remainingCapacity} seat(s) are available for this time.`
          : `Only ${selectedSlot.remainingCapacity} place(s) remain for this time slot.`
      )
      return
    }

    /*
     * Check login token.
     */
    const token =
      localStorage.getItem('authToken')

    if (!token) {
      setSubmitError(
        'Your session has expired. Please log in again.'
      )
      return
    }

    /*
     * Restaurant reservation payload.
     *
     * IMPORTANT:
     * Do NOT send PricePerPerson or TotalPrice.
     *
     * The backend retrieves the trusted restaurant
     * price from Provider Catalog and calculates
     * the real total.
     */
    const restaurantPayload = {
      restaurantId: item.id,
      reservationDate: selectedDate,
      timeSlot: selectedTimeSlot,
      partySize: normalizedGuestCount
    }

    /*
     * Experience / Accommodation payload.
     */
    const bookingPayload = {
      listingId: item.id,
      bookingDate: selectedDate,
      timeSlot: selectedTimeSlot,
      participantCount: normalizedGuestCount
    }

    /*
     * Different endpoint depending on listing type.
     */
    const endpoint = isRestaurant
      ? '/api/Reservations'
      : '/api/Bookings'

    const payload = isRestaurant
      ? restaurantPayload
      : bookingPayload

    setSubmitting(true)

    try {
      const response = await fetch(
        bookingUrl(endpoint),
        {
          method: 'POST',

          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`
          },

          body: JSON.stringify(payload)
        }
      )

      /*
       * Successful response.
       */
      if (response.ok) {
        const result =
          await response.json()

        /*
         * Story 8.1:
         * Restaurant reservation confirmation.
         */
        if (isRestaurant) {
          if (onBookingSuccess) {
            const confirmedPrice =
              Number(
                result.pricePerPerson ??
                restaurantPricePerPerson
              )

            const confirmedTotal =
              Number(
                result.totalPrice ??
                restaurantTotal
              )

            onBookingSuccess(
              `Table reserved successfully at ${result.restaurantName || item.title
              } ` +
              `for ${result.reservationDate || selectedDate
              } ` +
              `at ${result.timeSlot || selectedTimeSlot
              }. ` +
              `Party size: ${result.partySize ||
              normalizedGuestCount
              }. ` +
              `Price per person: LKR ${confirmedPrice.toLocaleString()
              }. ` +
              `Total: LKR ${confirmedTotal.toLocaleString()
              }. ` +
              `Status: ${result.status || 'Confirmed'
              }.`
            )
          } else {
            onClose()
          }

          return
        }

        /*
         * Story 7.1:
         * Experience / Accommodation confirmation.
         */
        if (onBookingSuccess) {
          onBookingSuccess(
            `Booking created for ${item.title} on ${selectedDate} ` +
            `at ${selectedTimeSlot}. ` +
            `Total: LKR ${Number(
              result.totalAmount
            ).toLocaleString()}. ` +
            'Status: Pending Payment.'
          )
        } else {
          onClose()
        }

        return
      }

      /*
       * Read backend validation/error message.
       */
      const errorData = await response
        .json()
        .catch(() => null)

      if (response.status === 401) {
        setSubmitError(
          'Your session has expired. Please log in again.'
        )
      } else if (response.status === 403) {
        setSubmitError(
          isRestaurant
            ? 'You are not authorized to create this reservation.'
            : 'You are not authorized to create this booking.'
        )
      } else if (response.status === 409) {
        setSubmitError(
          errorData?.message ||
          'The selected capacity is no longer available. Please check availability and try again.'
        )
      } else {
        setSubmitError(
          errorData?.message ||
          errorData?.error ||
          (
            isRestaurant
              ? 'Failed to create reservation. Please try again.'
              : 'Failed to create booking. Please try again.'
          )
        )
      }
    } catch {
      setSubmitError(
        isRestaurant
          ? 'Network error while creating the reservation. Please check your connection.'
          : 'Network error while creating the booking. Please check your connection.'
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div
      className="vd-modal-overlay"
      onClick={onClose}
    >
      <div
        className="vd-booking-modal"
        onClick={(e) => e.stopPropagation()}
      >
        {/* =====================================================
            HEADER
        ===================================================== */}
        <div className="vd-detail-modal__header">

          <button
            className="vd-detail-modal__close"
            onClick={onClose}
            aria-label="Close"
          >
            <CloseIcon size={16} />
          </button>

          <span
            className={`vd-type-badge vd-type-badge--${item.type.toLowerCase()}`}
          >
            {item.type}
          </span>

          <h2 className="vd-detail-modal__title">
            {isRestaurant
              ? 'Reserve a Table'
              : `Book: ${item.title}`}
          </h2>

          {isRestaurant && (
            <p className="vd-detail-modal__provider">
              {item.title}
            </p>
          )}

          <p className="vd-detail-modal__provider">
            By {item.providerBusinessName}
          </p>

        </div>

        <div className="vd-booking-modal__body">

          {/* =====================================================
              PRICE CARD
          ===================================================== */}

          <div className="vd-booking-rate-card">

            <span className="vd-booking-rate-label">
              <CalendarMonthIcon size={16} />
              {' '}
              {isRestaurant
                ? 'Price per Person'
                : 'Base Rate'}
            </span>

            <span className="vd-booking-rate-price">
              LKR {itemPrice.toLocaleString()}

              <span className="vd-booking-rate-unit">
                {' '}/ {isRestaurant ? 'person' : unit}
              </span>
            </span>

          </div>

          {/* =====================================================
              LISTING DETAILS
          ===================================================== */}

          <div className="vd-booking-summary-list">

            {/* Location */}
            <div className="vd-detail-row">

              <span className="vd-detail-row__label">
                <LocationOnIcon size={16} />
                {' '}Location:
              </span>

              <span className="vd-detail-row__val">
                {item.location || 'Sri Lanka'}
              </span>

            </div>

            {/* =================================================
                ACCOMMODATION
            ================================================= */}

            {isAccommodation && (
              <>
                <div className="vd-detail-row">

                  <span className="vd-detail-row__label">
                    <HouseIcon size={16} />
                    {' '}Property Type:
                  </span>

                  <span className="vd-detail-row__val">
                    {item.propertyType ||
                      item.category ||
                      'Stay'}
                  </span>

                </div>

                <div className="vd-detail-row">

                  <span className="vd-detail-row__label">
                    <GroupIcon size={16} />
                    {' '}Max Capacity:
                  </span>

                  <span className="vd-detail-row__val">
                    Up to {maxCap} guests
                  </span>

                </div>
              </>
            )}

            {/* =================================================
                EXPERIENCE
            ================================================= */}

            {isExperience && (
              <>
                <div className="vd-detail-row">

                  <span className="vd-detail-row__label">
                    <AccessTimeFilledIcon size={16} />
                    {' '}Duration:
                  </span>

                  <span className="vd-detail-row__val">
                    {item.duration || 'Flexible'}
                  </span>

                </div>

                <div className="vd-detail-row">

                  <span className="vd-detail-row__label">
                    <GroupIcon size={16} />
                    {' '}Group Size:
                  </span>

                  <span className="vd-detail-row__val">
                    Up to {maxCap} people
                  </span>

                </div>
              </>
            )}

            {/* =================================================
                RESTAURANT
            ================================================= */}

            {isRestaurant && (
              <>
                <div className="vd-detail-row">

                  <span className="vd-detail-row__label">
                    <RestaurantIcon size={16} />
                    {' '}Cuisine:
                  </span>

                  <span className="vd-detail-row__val">
                    {item.cuisineType ||
                      item.category ||
                      'Specialty Cuisine'}
                  </span>

                </div>

                <div className="vd-detail-row">

                  <span className="vd-detail-row__label">
                    <GroupIcon size={16} />
                    {' '}Max Seating:
                  </span>

                  <span className="vd-detail-row__val">
                    {maxCap} seats
                  </span>

                </div>

                <div className="vd-detail-row">

                  <span className="vd-detail-row__label">
                    Price per Person:
                  </span>

                  <span className="vd-detail-row__val">
                    LKR {restaurantPricePerPerson.toLocaleString()}
                  </span>

                </div>
              </>
            )}

          </div>

          {/* =====================================================
              RESERVATION / BOOKING FORM
          ===================================================== */}

          <div className="vd-booking-form-grid">

            {/* Date */}
            <div className="vd-form-group">

              <label className="vd-form-label">
                {isRestaurant
                  ? 'Reservation Date'
                  : 'Select Date'}
              </label>

              <input
                type="date"
                className="vd-form-input"
                value={selectedDate}
                min={
                  new Date()
                    .toISOString()
                    .split('T')[0]
                }
                onChange={(e) => {
                  setSelectedDate(e.target.value)
                  setGuestCount(1)
                  setSubmitError(null)
                }}
              />

            </div>

            {/* Time */}
            <div className="vd-form-group">

              <label className="vd-form-label">
                {isRestaurant
                  ? 'Reservation Time'
                  : 'Select Time'}
              </label>

              <select
                className="vd-form-input"
                value={selectedTimeSlot}
                onChange={(e) => {
                  setSelectedTimeSlot(e.target.value)
                  setGuestCount(1)
                  setSubmitError(null)
                }}
                disabled={
                  loadingAvail ||
                  !availability ||
                  isNotOperating ||
                  isSoldOut
                }
              >

                <option value="">
                  Select a time slot
                </option>

                {availability?.slots?.map((slot) => (
                  <option
                    key={slot.timeSlot}
                    value={slot.timeSlot}
                    disabled={
                      slot.isFullyBooked ||
                      slot.remainingCapacity <= 0
                    }
                  >
                    {isRestaurant
                      ? slot.timeSlot
                      : (
                        <>
                          {slot.timeSlot}
                          {' — '}
                          {slot.remainingCapacity > 0
                            ? `${slot.remainingCapacity} places left`
                            : 'Fully Booked'}
                        </>
                      )}
                  </option>
                ))}

              </select>

            </div>

            {/* Party Size / Participants */}
            <div className="vd-form-group">

              <label className="vd-form-label">

                {isRestaurant
                  ? 'Party Size'
                  : 'Guests / Participants'}

                <span className="vd-form-label-hint">
                  {' '}
                  (Max: {currentMax})
                </span>

              </label>

              <input
                type="number"
                className="vd-form-input"
                min="1"
                max={currentMax}
                step="1"
                value={guestCount}
                onChange={handleGuestCountChange}
                onBlur={handleGuestCountBlur}
              />

            </div>

          </div>

          {/* =====================================================
              AVAILABILITY STATUS
          ===================================================== */}

          <div className="vd-avail-status-card">

            {loadingAvail && (
              <span className="vd-avail-loading">
                Checking real-time availability…
              </span>
            )}

            {availError && (
              <div className="vd-avail-error">
                {availError}
              </div>
            )}

            {!loadingAvail &&
              !availError &&
              isNotOperating && (
                <div className="vd-fully-booked-notice">

                  <DangerIcon size={16} />

                  {' '}
                  {isRestaurant
                    ? 'Restaurant is not accepting reservations'
                    : 'Listing does not operate'}{' '}
                  on this date ({selectedDate}).

                </div>
              )}

            {!loadingAvail &&
              !availError &&
              !isNotOperating &&
              isSoldOut && (
                <div className="vd-fully-booked-notice">

                  <DangerIcon size={16} />

                  {' '}
                  {isRestaurant
                    ? `No tables are available on ${selectedDate}.`
                    : `Fully booked on ${selectedDate}.`}

                  {' '}Please select another date.

                </div>
              )}

            {!loadingAvail &&
              !availError &&
              !isNotOperating &&
              !isSoldOut &&
              availability && (
                <div className="vd-avail-success">

                  <CheckCircleIcon size={16} />

                  {' '}
                  Available on {selectedDate}

                  {selectedTimeSlot &&
                    selectedSlotRemaining > 0
                    ? isRestaurant
                      ? ` — ${selectedSlotRemaining} seat(s) available at the selected time`
                      : ` — ${selectedSlotRemaining} place(s) remaining in selected time slot`
                    : ''}

                </div>
              )}

          </div>

          {/* =====================================================
              VALIDATION / API ERROR
          ===================================================== */}

          {submitError && (
            <div
              className="vd-avail-error"
              style={{
                padding: '8px 12px',
                background: '#fef2f2',
                borderRadius: '8px',
                border: '1px solid #fecaca'
              }}
            >
              {submitError}
            </div>
          )}

          {/* =====================================================
              FOOTER
          ===================================================== */}

          <div className="vd-detail-modal__footer">

            {/* Experience / Accommodation Total */}
            {!isRestaurant && (
              <div className="vd-detail-price">

                <span className="vd-detail-price__label">
                  Estimated Total
                </span>

                <span className="vd-detail-price__val">
                  LKR {estimatedTotal.toLocaleString()}
                </span>

              </div>
            )}

            {/* Restaurant Total */}
            {isRestaurant && (
              <div className="vd-detail-price">

                <span className="vd-detail-price__label">
                  Estimated Total
                </span>

                <span className="vd-detail-price__val">
                  LKR {restaurantTotal.toLocaleString()}
                </span>

                <span
                  style={{
                    display: 'block',
                    fontSize: '12px',
                    marginTop: '4px'
                  }}
                >
                  LKR{' '}
                  {restaurantPricePerPerson.toLocaleString()}
                  {' × '}
                  {numericGuestCount}
                  {' '}
                  {numericGuestCount === 1
                    ? 'guest'
                    : 'guests'}
                </span>

              </div>
            )}

            <button
              className="vd-btn-book"
              disabled={
                loadingAvail ||
                !availability ||
                !!availError ||
                isSoldOut ||
                isNotOperating ||
                !selectedTimeSlot ||
                selectedSlotRemaining <= 0 ||
                guestCount === '' ||
                !Number.isInteger(numericGuestCount) ||
                numericGuestCount < 1 ||
                numericGuestCount > currentMax ||
                submitting
              }
              onClick={handleConfirmBooking}
            >
              {submitting
                ? isRestaurant
                  ? 'Reserving Table…'
                  : 'Creating Booking…'
                : isRestaurant
                  ? 'Reserve Table'
                  : 'Confirm Booking'}
            </button>

          </div>

        </div>
      </div>
    </div>
  )
}