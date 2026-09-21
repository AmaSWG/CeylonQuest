import React, { useState, useEffect } from 'react'
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
  const [guestCount, setGuestCount] = useState(1)

  const [availability, setAvailability] = useState(null)
  const [loadingAvail, setLoadingAvail] = useState(false)
  const [availError, setAvailError] = useState(null)

  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState(null)

  const unit =
    item?.unit ||
    (item?.type === 'Accommodation' ? 'night' : 'person')

  // Maximum capacity based on listing type
  const maxCap = item
    ? item.type === 'Accommodation'
      ? item.maxGuests || 4
      : item.type === 'Restaurant'
        ? item.seatingCapacity || 10
        : item.maxParticipants || 10
    : 10

  /*
   * Load real availability whenever the
   * listing or selected date changes.
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

        if (response.ok) {
          const data = await response.json()

          setAvailability(data)

          // Automatically select the first slot
          // that still has remaining capacity.
          const firstAvailableSlot = data.slots?.find(
            (slot) => slot.remainingCapacity > 0
          )

          setSelectedTimeSlot(
            firstAvailableSlot?.timeSlot || ''
          )
        } else {
          setAvailError(
            'Unable to check availability for this date.'
          )
        }
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

  const isNotOperating =
    availability?.isOperatingDay === false

  const isSoldOut =
    availability &&
    (
      availability.isFullyBooked ||
      (
        availability.slots?.length > 0 &&
        availability.slots.every(
          (slot) => slot.remainingCapacity <= 0
        )
      )
    )

  /*
   * Get the currently selected slot.
   */
  const selectedSlot = availability?.slots?.find(
    (slot) => slot.timeSlot === selectedTimeSlot
  )

  const selectedSlotRemaining =
    selectedSlot?.remainingCapacity ?? 0

  const isPerPerson =
    unit === 'person' || unit === 'guest'

  /*
   * Frontend estimate only.
   * Backend calculates the real total.
   */
  const estimatedTotal =
    Number(item.price) *
    (isPerPerson ? guestCount : 1)

  /*
   * Participant input validation.
   */
  const handleGuestCountChange = (e) => {
    const value = parseInt(e.target.value, 10)

    const currentMax = selectedTimeSlot
      ? Math.min(maxCap, selectedSlotRemaining)
      : maxCap

    if (isNaN(value) || value < 1) {
      setGuestCount(1)
      return
    }

    if (value > currentMax) {
      setGuestCount(currentMax)
      return
    }

    setGuestCount(value)
  }

  /*
   * Story 7.1
   * Create real booking through Booking Service.
   */
  const handleConfirmBooking = async () => {
    setSubmitError(null)

    if (!selectedDate) {
      setSubmitError('Please select a booking date.')
      return
    }

    if (!selectedTimeSlot) {
      setSubmitError(
        'Please select an available time slot.'
      )
      return
    }

    if (guestCount < 1) {
      setSubmitError(
        'Participant count must be at least 1.'
      )
      return
    }

    /*
     * Frontend capacity validation.
     * Backend validates this again.
     */
    if (
      selectedSlot &&
      guestCount > selectedSlot.remainingCapacity
    ) {
      setSubmitError(
        `Only ${selectedSlot.remainingCapacity} place(s) remain for this time slot.`
      )
      return
    }

    const token = localStorage.getItem('authToken')

    if (!token) {
      setSubmitError(
        'Your session has expired. Please log in again.'
      )
      return
    }

    /*
     * Only send booking information.
     *
     * VisitorId, UnitPrice, TotalAmount,
     * Status and PaymentStatus are controlled
     * by the backend.
     */
    const payload = {
      listingId: item.id,
      bookingDate: selectedDate,
      timeSlot: selectedTimeSlot,
      participantCount: guestCount
    }

    setSubmitting(true)

    try {
      const response = await fetch(
        bookingUrl('/api/Bookings'),
        {
          method: 'POST',

          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`
          },

          body: JSON.stringify(payload)
        }
      )

      if (response.ok) {
        const booking = await response.json()

        if (onBookingSuccess) {
          onBookingSuccess(
            `Booking created for ${item.title} on ${selectedDate} ` +
            `at ${selectedTimeSlot}. ` +
            `Total: LKR ${Number(
              booking.totalAmount
            ).toLocaleString()}. ` +
            `Status: Pending Payment.`
          )
        } else {
          onClose()
        }

        return
      }

      const errorData = await response
        .json()
        .catch(() => null)

      if (response.status === 401) {
        setSubmitError(
          'Your session has expired. Please log in again.'
        )
      } else if (response.status === 403) {
        setSubmitError(
          'You are not authorized to create this booking.'
        )
      } else {
        setSubmitError(
          errorData?.message ||
          errorData?.error ||
          'Failed to create booking. Please try again.'
        )
      }
    } catch {
      setSubmitError(
        'Network error while creating the booking. Please check your connection.'
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
        {/* Header */}
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
            Book: {item.title}
          </h2>

          <p className="vd-detail-modal__provider">
            By {item.providerBusinessName}
          </p>

        </div>

        <div className="vd-booking-modal__body">

          {/* Base Rate */}
          <div className="vd-booking-rate-card">

            <span className="vd-booking-rate-label">
              <CalendarMonthIcon size={16} />
              {' '}Base Rate
            </span>

            <span className="vd-booking-rate-price">
              LKR {Number(item.price).toLocaleString()}

              <span className="vd-booking-rate-unit">
                {' '}/ {unit}
              </span>
            </span>

          </div>

          {/* Listing Details */}
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

            {/* Accommodation */}
            {item.type === 'Accommodation' && (
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

            {/* Experience */}
            {item.type === 'Experience' && (
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

            {/* Restaurant */}
            {item.type === 'Restaurant' && (
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
              </>
            )}

          </div>

          {/* Booking Form */}
          <div className="vd-booking-form-grid">

            {/* Date */}
            <div className="vd-form-group">

              <label className="vd-form-label">
                Select Date
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
                onChange={(e) =>
                  setSelectedDate(e.target.value)
                }
              />

            </div>

            {/* Time Slot */}
            <div className="vd-form-group">

              <label className="vd-form-label">
                Select Time
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
                    disabled={slot.remainingCapacity <= 0}
                  >
                    {slot.timeSlot}
                    {' — '}
                    {slot.remainingCapacity > 0
                      ? `${slot.remainingCapacity} places left`
                      : 'Fully Booked'}
                  </option>
                ))}

              </select>

            </div>

            {/* Participants */}
            <div className="vd-form-group">

              <label className="vd-form-label">
                Guests / Participants

                <span className="vd-form-label-hint">
                  {' '}
                  (Max:{' '}
                  {selectedTimeSlot
                    ? Math.min(
                      maxCap,
                      selectedSlotRemaining
                    )
                    : maxCap}
                  )
                </span>
              </label>

              <input
                type="number"
                className="vd-form-input"
                min="1"
                max={
                  selectedTimeSlot
                    ? Math.min(
                      maxCap,
                      selectedSlotRemaining
                    )
                    : maxCap
                }
                value={guestCount}
                onChange={handleGuestCountChange}
              />

            </div>

          </div>

          {/* Availability Status */}
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
                  Listing does not operate on this date (
                  {selectedDate}).

                </div>
              )}

            {!loadingAvail &&
              !availError &&
              !isNotOperating &&
              isSoldOut && (
                <div className="vd-fully-booked-notice">

                  <DangerIcon size={16} />

                  {' '}
                  Fully booked on {selectedDate}.
                  Please select another date.

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
                    ? ` — ${selectedSlotRemaining} place(s) remaining in selected time slot`
                    : ''}

                </div>
              )}

          </div>

          {/* Booking Error */}
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

          {/* Footer */}
          <div className="vd-detail-modal__footer">

            <div className="vd-detail-price">

              <span className="vd-detail-price__label">
                Estimated Total
              </span>

              <span className="vd-detail-price__val">
                LKR {estimatedTotal.toLocaleString()}
              </span>

            </div>

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
                guestCount > selectedSlotRemaining ||
                submitting
              }
              onClick={handleConfirmBooking}
            >
              {submitting
                ? 'Creating Booking…'
                : 'Confirm Booking'}
            </button>

          </div>

        </div>
      </div>
    </div>
  )
}