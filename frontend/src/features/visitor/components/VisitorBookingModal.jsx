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
  // =========================================================
  // STATE
  // =========================================================

  const [selectedDate, setSelectedDate] = useState(
    new Date(Date.now() + 86400000).toISOString().split('T')[0]
  )

  // Used only for Accommodation bookings
  const [checkOutDate, setCheckOutDate] = useState('')

  const [selectedTimeSlot, setSelectedTimeSlot] = useState('')
  const [guestCount, setGuestCount] = useState(1)

  const [availability, setAvailability] = useState(null)
  const [loadingAvail, setLoadingAvail] = useState(false)
  const [availError, setAvailError] = useState(null)

  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState(null)

  // =========================================================
  // LISTING TYPE
  // =========================================================

  const isRestaurant = item?.type === 'Restaurant'
  const isAccommodation = item?.type === 'Accommodation'
  const isExperience = item?.type === 'Experience'

  // =========================================================
  // BASIC LISTING VALUES
  // =========================================================

  const unit =
    item?.unit ||
    (isAccommodation ? 'night' : 'person')

  const maxCap = item
    ? isAccommodation
      ? item.maxGuests || 4
      : isRestaurant
        ? item.seatingCapacity || 1
        : item.maxParticipants || 10
    : 1

  const itemPrice = Number(item?.price || 0)

  const minStayNights = isAccommodation
    ? Number(item?.minStayNights || 1)
    : 1

  // =========================================================
  // ACCOMMODATION NIGHT CALCULATION
  // =========================================================

  const accommodationNights =
    isAccommodation && selectedDate && checkOutDate
      ? Math.max(
        0,
        Math.round(
          (
            new Date(`${checkOutDate}T00:00:00`) -
            new Date(`${selectedDate}T00:00:00`)
          ) / 86400000
        )
      )
      : 0

  // =========================================================
  // LOAD AVAILABILITY
  // =========================================================

  useEffect(() => {
    if (!item || !selectedDate) return

    const fetchAvailability = async () => {
      setLoadingAvail(true)
      setAvailError(null)
      setAvailability(null)
      setSelectedTimeSlot('')
      setSubmitError(null)

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

  // =========================================================
  // AVAILABILITY VALUES
  // =========================================================

  const isNotOperating =
    availability?.isOperatingDay === false

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

  const selectedSlot = availability?.slots?.find(
    (slot) => slot.timeSlot === selectedTimeSlot
  )

  const selectedSlotRemaining =
    selectedSlot?.remainingCapacity ?? 0

  /*
   * IMPORTANT:
   *
   * Accommodation remainingCapacity means number
   * of accommodation units/rooms available.
   *
   * It does NOT mean maximum guests.
   *
   * Therefore Accommodation uses maxGuests directly.
   */
  const currentMax = isAccommodation
    ? maxCap
    : selectedTimeSlot
      ? Math.min(maxCap, selectedSlotRemaining)
      : maxCap

  const numericGuestCount =
    guestCount === ''
      ? 0
      : Number(guestCount)

  // =========================================================
  // PRICE CALCULATIONS
  // =========================================================

  const estimatedTotal =
    isExperience
      ? itemPrice * numericGuestCount
      : isAccommodation
        ? itemPrice * accommodationNights
        : itemPrice

  const restaurantPricePerPerson =
    isRestaurant
      ? itemPrice
      : 0

  const restaurantTotal =
    isRestaurant
      ? restaurantPricePerPerson * numericGuestCount
      : 0

  // =========================================================
  // ACCOMMODATION MINIMUM CHECKOUT DATE
  // =========================================================

  const minimumCheckoutDate =
    isAccommodation && selectedDate
      ? new Date(
        new Date(`${selectedDate}T00:00:00`).getTime() +
        minStayNights * 86400000
      )
        .toISOString()
        .split('T')[0]
      : ''

  // =========================================================
  // GUEST / PARTICIPANT COUNT
  // =========================================================

  const handleGuestCountChange = (e) => {
    const rawValue = e.target.value

    setSubmitError(null)

    if (rawValue === '') {
      setGuestCount('')
      return
    }

    const value = Number(rawValue)

    if (!Number.isInteger(value)) {
      return
    }

    if (value < 0) {
      return
    }

    setGuestCount(value)

    // Listing maximum
    if (value > maxCap) {
      setSubmitError(
        isRestaurant
          ? `Maximum seating capacity is ${maxCap}.`
          : isAccommodation
            ? `This accommodation allows a maximum of ${maxCap} guest(s).`
            : `Maximum capacity is ${maxCap}.`
      )

      return
    }

    /*
     * Do NOT compare accommodation guest count with
     * remainingCapacity.
     *
     * remainingCapacity = rooms/units for Accommodation.
     */
    if (
      !isAccommodation &&
      selectedTimeSlot &&
      selectedSlot &&
      value > selectedSlotRemaining
    ) {
      setSubmitError(
        isRestaurant
          ? `Only ${selectedSlotRemaining} seat(s) are available for this time.`
          : `Only ${selectedSlotRemaining} place(s) are available for this time slot.`
      )
    }
  }

  const handleGuestCountBlur = () => {
    if (
      guestCount === '' ||
      !Number.isInteger(Number(guestCount)) ||
      Number(guestCount) < 1
    ) {
      setGuestCount(1)
      setSubmitError(null)
      return
    }

    const value = Number(guestCount)

    if (value > maxCap) {
      setSubmitError(
        isRestaurant
          ? `Maximum seating capacity is ${maxCap}.`
          : isAccommodation
            ? `This accommodation allows a maximum of ${maxCap} guest(s).`
            : `Maximum capacity is ${maxCap}.`
      )

      return
    }

    if (
      !isAccommodation &&
      selectedTimeSlot &&
      selectedSlot &&
      value > selectedSlotRemaining
    ) {
      setSubmitError(
        isRestaurant
          ? `Only ${selectedSlotRemaining} seat(s) are available for this time.`
          : `Only ${selectedSlotRemaining} place(s) are available for this time slot.`
      )
    }
  }

  // =========================================================
  // CONFIRM BOOKING
  // =========================================================

  const handleConfirmBooking = async () => {
    setSubmitError(null)

    // ---------------------------------------------------------
    // Date validation
    // ---------------------------------------------------------

    if (!selectedDate) {
      setSubmitError(
        isRestaurant
          ? 'Please select a reservation date.'
          : isAccommodation
            ? 'Please select a check-in date.'
            : 'Please select a booking date.'
      )

      return
    }

    // ---------------------------------------------------------
    // Accommodation checkout validation
    // ---------------------------------------------------------

    if (isAccommodation) {
      if (!checkOutDate) {
        setSubmitError(
          'Please select a check-out date.'
        )
        return
      }

      if (checkOutDate <= selectedDate) {
        setSubmitError(
          'Check-out date must be after the check-in date.'
        )
        return
      }

      if (accommodationNights < minStayNights) {
        setSubmitError(
          `This accommodation requires a minimum stay of ${minStayNights} night(s).`
        )
        return
      }
    }

    // ---------------------------------------------------------
    // Availability slot
    // ---------------------------------------------------------

    if (!selectedTimeSlot) {
      setSubmitError(
        isAccommodation
          ? 'No stay availability is available for the selected check-in date.'
          : 'Please select an available time slot.'
      )

      return
    }

    const normalizedGuestCount =
      Number(guestCount)

    // ---------------------------------------------------------
    // Guest / participant validation
    // ---------------------------------------------------------

    if (
      guestCount === '' ||
      !Number.isInteger(normalizedGuestCount) ||
      normalizedGuestCount < 1
    ) {
      setSubmitError(
        isRestaurant
          ? 'Party size must be at least 1.'
          : isAccommodation
            ? 'Guest count must be at least 1.'
            : 'Participant count must be at least 1.'
      )

      return
    }

    if (normalizedGuestCount > maxCap) {
      setSubmitError(
        isRestaurant
          ? `Maximum seating capacity is ${maxCap}.`
          : isAccommodation
            ? `This accommodation allows a maximum of ${maxCap} guest(s).`
            : `Maximum capacity is ${maxCap}.`
      )

      return
    }

    /*
     * Experience / Restaurant:
     * participant count consumes slot capacity.
     *
     * Accommodation:
     * one room/unit consumes capacity = 1.
     * Guest count is validated against maxGuests.
     */
    if (
      !isAccommodation &&
      selectedSlot &&
      normalizedGuestCount >
      selectedSlot.remainingCapacity
    ) {
      setSubmitError(
        isRestaurant
          ? `Only ${selectedSlot.remainingCapacity} seat(s) are available for this time.`
          : `Only ${selectedSlot.remainingCapacity} place(s) are available for this time slot.`
      )

      return
    }

    if (
      isAccommodation &&
      selectedSlot &&
      selectedSlot.remainingCapacity < 1
    ) {
      setSubmitError(
        'This accommodation is no longer available for the selected check-in date.'
      )

      return
    }

    // ---------------------------------------------------------
    // Authentication
    // ---------------------------------------------------------

    const token =
      localStorage.getItem('authToken')

    if (!token) {
      setSubmitError(
        'Your session has expired. Please log in again.'
      )

      return
    }

    // =========================================================
    // PAYLOADS
    // =========================================================

    // Restaurant
    const restaurantPayload = {
      restaurantId: item.id,
      reservationDate: selectedDate,
      timeSlot: selectedTimeSlot,
      partySize: normalizedGuestCount
    }

    // Experience
    const bookingPayload = {
      listingId: item.id,
      bookingDate: selectedDate,
      timeSlot: selectedTimeSlot,
      participantCount: normalizedGuestCount
    }

    // Accommodation
    const accommodationPayload = {
      accommodationId: item.id,
      checkInDate: selectedDate,
      checkOutDate: checkOutDate,
      guestCount: normalizedGuestCount
    }

    // =========================================================
    // CORRECT ENDPOINT
    // =========================================================

    const endpoint = isRestaurant
      ? '/api/Reservations'
      : isAccommodation
        ? '/api/AccommodationBookings'
        : '/api/Bookings'

    const payload = isRestaurant
      ? restaurantPayload
      : isAccommodation
        ? accommodationPayload
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

      // =======================================================
      // SUCCESS
      // =======================================================

      if (response.ok) {
        const result = await response.json()

        // -----------------------------------------------------
        // Restaurant success
        // -----------------------------------------------------

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
              `Price per person: LKR ${confirmedPrice.toLocaleString()}. ` +
              `Total: LKR ${confirmedTotal.toLocaleString()}. ` +
              `Status: ${result.status || 'Confirmed'
              }.`
            )
          } else {
            onClose()
          }

          return
        }

        // -----------------------------------------------------
        // Accommodation success
        // -----------------------------------------------------

        if (isAccommodation) {
          if (onBookingSuccess) {
            const confirmedPricePerNight =
              Number(
                result.pricePerNight ??
                itemPrice
              )

            const confirmedTotal =
              Number(
                result.totalPrice ??
                estimatedTotal
              )

            const confirmedNights =
              Number(
                result.numberOfNights ??
                accommodationNights
              )

            onBookingSuccess(
              `Accommodation booked successfully at ${result.accommodationName || item.title
              }. ` +
              `Check-in: ${result.checkInDate || selectedDate
              }. ` +
              `Check-out: ${result.checkOutDate || checkOutDate
              }. ` +
              `${confirmedNights} ${confirmedNights === 1
                ? 'night'
                : 'nights'
              }. ` +
              `Guests: ${result.guestCount ||
              normalizedGuestCount
              }. ` +
              `LKR ${confirmedPricePerNight.toLocaleString()} per night. ` +
              `Total: LKR ${confirmedTotal.toLocaleString()}. ` +
              `Status: ${result.status || 'Confirmed'
              }.`
            )
          } else {
            onClose()
          }

          return
        }

        // -----------------------------------------------------
        // Experience success
        // -----------------------------------------------------

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

      // =======================================================
      // ERROR RESPONSE
      // =======================================================

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
            : isAccommodation
              ? 'You are not authorized to book this accommodation.'
              : 'You are not authorized to create this booking.'
        )
      } else if (response.status === 409) {
        setSubmitError(
          errorData?.message ||
          (
            isAccommodation
              ? 'This accommodation is no longer available for the selected check-in date.'
              : 'The selected capacity is no longer available. Please check availability and try again.'
          )
        )
      } else {
        setSubmitError(
          errorData?.message ||
          errorData?.error ||
          (
            isRestaurant
              ? 'Failed to create reservation. Please try again.'
              : isAccommodation
                ? 'Failed to create accommodation booking. Please try again.'
                : 'Failed to create booking. Please try again.'
          )
        )
      }
    } catch {
      setSubmitError(
        isRestaurant
          ? 'Network error while creating the reservation. Please check your connection.'
          : isAccommodation
            ? 'Network error while creating the accommodation booking. Please check your connection.'
            : 'Network error while creating the booking. Please check your connection.'
      )
    } finally {
      setSubmitting(false)
    }
  }

  // =========================================================
  // UI
  // =========================================================

  return (
    <div
      className="vd-modal-overlay"
      onClick={onClose}
    >
      <div
        className="vd-booking-modal"
        onClick={(e) => e.stopPropagation()}
      >
        {/* HEADER */}
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

          {/* PRICE CARD */}
          <div className="vd-booking-rate-card">

            <span className="vd-booking-rate-label">
              <CalendarMonthIcon size={16} />
              {' '}
              {isRestaurant
                ? 'Price per Person'
                : isAccommodation
                  ? 'Price per Night'
                  : 'Base Rate'}
            </span>

            <span className="vd-booking-rate-price">
              LKR {itemPrice.toLocaleString()}

              <span className="vd-booking-rate-unit">
                {' '}/ {isRestaurant ? 'person' : unit}
              </span>
            </span>

          </div>

          {/* LISTING DETAILS */}
          <div className="vd-booking-summary-list">

            <div className="vd-detail-row">
              <span className="vd-detail-row__label">
                <LocationOnIcon size={16} />
                {' '}Location:
              </span>

              <span className="vd-detail-row__val">
                {item.location || 'Sri Lanka'}
              </span>
            </div>

            {/* ACCOMMODATION */}
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

                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <CalendarMonthIcon size={16} />
                    {' '}Minimum Stay:
                  </span>

                  <span className="vd-detail-row__val">
                    {minStayNights}{' '}
                    {minStayNights === 1
                      ? 'night'
                      : 'nights'}
                  </span>
                </div>
              </>
            )}

            {/* EXPERIENCE */}
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

            {/* RESTAURANT */}
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

          {/* BOOKING FORM */}
          <div className="vd-booking-form-grid">

            {/* CHECK-IN / BOOKING / RESERVATION DATE */}
            <div className="vd-form-group">

              <label className="vd-form-label">
                {isRestaurant
                  ? 'Reservation Date'
                  : isAccommodation
                    ? 'Check-in Date'
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
                  const newDate = e.target.value

                  setSelectedDate(newDate)
                  setGuestCount(1)
                  setSubmitError(null)

                  /*
                   * Reset checkout when check-in changes.
                   * This prevents an old invalid checkout date.
                   */
                  if (isAccommodation) {
                    setCheckOutDate('')
                  }
                }}
              />

            </div>

            {/* ACCOMMODATION CHECK-OUT */}
            {isAccommodation && (
              <div className="vd-form-group">

                <label className="vd-form-label">
                  Check-out Date
                </label>

                <input
                  type="date"
                  className="vd-form-input"
                  value={checkOutDate}
                  min={minimumCheckoutDate}
                  onChange={(e) => {
                    setCheckOutDate(e.target.value)
                    setSubmitError(null)
                  }}
                />

              </div>
            )}

            {/* TIME / STAY AVAILABILITY */}
            <div className="vd-form-group">

              <label className="vd-form-label">
                {isRestaurant
                  ? 'Reservation Time'
                  : isAccommodation
                    ? 'Stay Availability'
                    : 'Select Time'}
              </label>

              <select
                className="vd-form-input"
                value={selectedTimeSlot}
                onChange={(e) => {
                  setSelectedTimeSlot(e.target.value)

                  /*
                   * Do not unnecessarily reset Accommodation
                   * guest count based on room inventory.
                   */
                  if (!isAccommodation) {
                    setGuestCount(1)
                  }

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
                  {isAccommodation
                    ? 'Select stay availability'
                    : 'Select a time slot'}
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
                      : isAccommodation
                        ? `${slot.timeSlot} — ${slot.remainingCapacity > 0
                          ? 'Available'
                          : 'Fully Booked'
                        }`
                        : `${slot.timeSlot} — ${slot.remainingCapacity > 0
                          ? `${slot.remainingCapacity} places left`
                          : 'Fully Booked'
                        }`}
                  </option>
                ))}

              </select>

            </div>

            {/* PARTICIPANTS / GUESTS */}
            <div className="vd-form-group">

              <label className="vd-form-label">

                {isRestaurant
                  ? 'Party Size'
                  : isAccommodation
                    ? 'Guests'
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
                max={maxCap}
                step="1"
                value={guestCount}
                onChange={handleGuestCountChange}
                onBlur={handleGuestCountBlur}
              />

            </div>

          </div>

          {/* AVAILABILITY */}
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
                    : isAccommodation
                      ? 'Accommodation is not available'
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
                    : isAccommodation
                      ? `This accommodation is fully booked on ${selectedDate}.`
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

                  {isAccommodation
                    ? `Accommodation available for check-in on ${selectedDate}`
                    : `Available on ${selectedDate}`}

                  {selectedTimeSlot &&
                    selectedSlotRemaining > 0
                    ? isRestaurant
                      ? ` — ${selectedSlotRemaining} seat(s) available at the selected time`
                      : isAccommodation
                        ? ' — room available'
                        : ` — ${selectedSlotRemaining} place(s) remaining in selected time slot`
                    : ''}

                </div>
              )}

          </div>

          {/* VALIDATION ERROR */}
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
              <DangerIcon size={16} />
              {' '}
              {submitError}
            </div>
          )}

          {/* FOOTER */}
          <div className="vd-detail-modal__footer">

            {/* EXPERIENCE / ACCOMMODATION TOTAL */}
            {!isRestaurant && (
              <div className="vd-detail-price">

                <span className="vd-detail-price__label">
                  Estimated Total
                </span>

                <span className="vd-detail-price__val">
                  LKR {estimatedTotal.toLocaleString()}
                </span>

                {/* EXPERIENCE BREAKDOWN */}
                {isExperience && (
                  <span
                    style={{
                      display: 'block',
                      fontSize: '12px',
                      marginTop: '4px'
                    }}
                  >
                    LKR {itemPrice.toLocaleString()}
                    {' × '}
                    {numericGuestCount}
                    {' '}
                    {numericGuestCount === 1
                      ? 'participant'
                      : 'participants'}
                  </span>
                )}

                {/* ACCOMMODATION BREAKDOWN */}
                {isAccommodation && (
                  <span
                    style={{
                      display: 'block',
                      fontSize: '12px',
                      marginTop: '4px'
                    }}
                  >
                    LKR {itemPrice.toLocaleString()}
                    {' × '}
                    {accommodationNights}
                    {' '}
                    {accommodationNights === 1
                      ? 'night'
                      : 'nights'}
                  </span>
                )}

              </div>
            )}

            {/* RESTAURANT TOTAL */}
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

            {/* CONFIRM BUTTON */}
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
                numericGuestCount > maxCap ||
                numericGuestCount > currentMax ||
                (
                  isAccommodation &&
                  !checkOutDate
                ) ||
                (
                  isAccommodation &&
                  accommodationNights < minStayNights
                ) ||
                submitting
              }
              onClick={handleConfirmBooking}
            >
              {submitting
                ? isRestaurant
                  ? 'Reserving Table…'
                  : isAccommodation
                    ? 'Booking Stay…'
                    : 'Creating Booking…'
                : isRestaurant
                  ? 'Reserve Table'
                  : isAccommodation
                    ? 'Book Stay'
                    : 'Confirm Booking'}
            </button>

          </div>

        </div>
      </div>
    </div>
  )
}