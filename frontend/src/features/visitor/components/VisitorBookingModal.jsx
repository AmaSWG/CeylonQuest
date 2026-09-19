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
import { catalogUrl } from '../../../api/client'

export default function VisitorBookingModal({ item, onClose, onBookingSuccess }) {
  const [selectedDate, setSelectedDate] = useState(
    new Date(Date.now() + 86400000).toISOString().split('T')[0]
  )
  const [guestCount, setGuestCount] = useState(1)
  const [availability, setAvailability] = useState(null)
  const [loadingAvail, setLoadingAvail] = useState(false)
  const [availError, setAvailError] = useState(null)
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState(null)

  const unit = item?.unit || (item?.type === 'Accommodation' ? 'night' : 'person')

  // Calculate max capacity strictly based on listing type
  const maxCap = item
    ? (item.type === 'Accommodation'
        ? (item.maxGuests || 4)
        : item.type === 'Restaurant'
        ? (item.seatingCapacity || 10)
        : (item.maxParticipants || 10))
    : 10

  useEffect(() => {
    if (!item || !selectedDate) return

    const fetchAvail = async () => {
      setLoadingAvail(true)
      setAvailError(null)
      try {
        const resp = await fetch(catalogUrl(`/api/catalog/availability/${item.id}?date=${selectedDate}`))
        if (resp.ok) {
          const data = await resp.json()
          setAvailability(data)
        } else {
          setAvailError('Unable to check availability for this date.')
        }
      } catch {
        setAvailError('Network error checking availability.')
      } finally {
        setLoadingAvail(false)
      }
    }

    fetchAvail()
  }, [item, selectedDate])

  if (!item) return null

  const isNotOperating = availability && availability.isOperatingDay === false
  const isSoldOut = availability && (availability.isFullyBooked || (availability.slots?.length > 0 && availability.slots.every(s => s.remainingCapacity <= 0)))
  const remainingCount = availability?.slots?.reduce((sum, s) => sum + s.remainingCapacity, 0)

  const isPerPerson = unit === 'person' || unit === 'guest'
  const estimatedTotal = Number(item.price) * (isPerPerson ? guestCount : 1)

  const handleGuestCountChange = (e) => {
    const val = parseInt(e.target.value, 10)
    if (isNaN(val) || val < 1) {
      setGuestCount(1)
    } else if (val > maxCap) {
      setGuestCount(maxCap)
    } else {
      setGuestCount(val)
    }
  }

  const handleConfirmBooking = async () => {
    setSubmitting(true)
    setSubmitError(null)

    const slotName = availability?.slots?.[0]?.timeSlot || (
      item.type === 'Accommodation'
        ? `Stay (Min ${item.minStayNights || 1} Night${(item.minStayNights || 1) > 1 ? 's' : ''})`
        : item.type === 'Restaurant'
        ? (item.openingHours || '11:30 AM - 10:00 PM')
        : (item.timeSlots?.split(',')?.[0]?.trim() || '09:00 AM - 11:00 AM')
    )

    const payload = {
      listingId: item.id,
      date: selectedDate,
      timeSlot: slotName,
      guestCount: isPerPerson ? guestCount : 1
    }

    try {
      const resp = await fetch(catalogUrl('/api/catalog/availability/simulate-booking-event'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      })

      if (resp.ok) {
        if (onBookingSuccess) {
          onBookingSuccess(`Booking confirmed for ${item.title} on ${selectedDate} (${guestCount} ${unit === 'night' ? 'guest(s)' : 'person(s)'})!`)
        } else {
          onClose()
        }
      } else {
        const err = await resp.json().catch(() => null)
        setSubmitError(err?.message || 'Failed to complete booking. Please try again.')
      }
    } catch {
      setSubmitError('Network error confirming booking. Please check connection.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="vd-modal-overlay" onClick={onClose}>
      <div className="vd-booking-modal" onClick={(e) => e.stopPropagation()}>
        <div className="vd-detail-modal__header">
          <button className="vd-detail-modal__close" onClick={onClose} aria-label="Close">
            <CloseIcon size={16} />
          </button>
          <span className={`vd-type-badge vd-type-badge--${item.type.toLowerCase()}`}>
            {item.type}
          </span>
          <h2 className="vd-detail-modal__title">Book: {item.title}</h2>
          <p className="vd-detail-modal__provider">By {item.providerBusinessName}</p>
        </div>

        <div className="vd-booking-modal__body">
          <div className="vd-booking-rate-card">
            <span className="vd-booking-rate-label">
              <CalendarMonthIcon size={16} /> Base Rate
            </span>
            <span className="vd-booking-rate-price">
              LKR {Number(item.price).toLocaleString()} <span className="vd-booking-rate-unit">/ {unit}</span>
            </span>
          </div>

          <div className="vd-booking-summary-list">
            <div className="vd-detail-row">
              <span className="vd-detail-row__label">
                <LocationOnIcon size={16} /> Location:
              </span>
              <span className="vd-detail-row__val">{item.location || 'Sri Lanka'}</span>
            </div>

            {item.type === 'Accommodation' && (
              <>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <HouseIcon size={16} /> Property Type:
                  </span>
                  <span className="vd-detail-row__val">{item.propertyType || item.category || 'Stay'}</span>
                </div>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <GroupIcon size={16} /> Max Capacity:
                  </span>
                  <span className="vd-detail-row__val">Up to {maxCap} guests</span>
                </div>
              </>
            )}

            {item.type === 'Experience' && (
              <>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <AccessTimeFilledIcon size={16} /> Duration:
                  </span>
                  <span className="vd-detail-row__val">{item.duration || 'Flexible'}</span>
                </div>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <GroupIcon size={16} /> Group Size:
                  </span>
                  <span className="vd-detail-row__val">Up to {maxCap} people</span>
                </div>
              </>
            )}

            {item.type === 'Restaurant' && (
              <>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <RestaurantIcon size={16} /> Cuisine:
                  </span>
                  <span className="vd-detail-row__val">{item.cuisineType || item.category || 'Specialty Cuisine'}</span>
                </div>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <GroupIcon size={16} /> Max Seating:
                  </span>
                  <span className="vd-detail-row__val">{maxCap} seats</span>
                </div>
              </>
            )}
          </div>

          <div className="vd-booking-form-grid">
            <div className="vd-form-group">
              <label className="vd-form-label">Select Date</label>
              <input
                type="date"
                className="vd-form-input"
                value={selectedDate}
                min={new Date().toISOString().split('T')[0]}
                onChange={(e) => setSelectedDate(e.target.value)}
              />
            </div>
            <div className="vd-form-group">
              <label className="vd-form-label">
                Guests / Units <span className="vd-form-label-hint">(Max: {maxCap})</span>
              </label>
              <input
                type="number"
                className="vd-form-input"
                min="1"
                max={maxCap}
                value={guestCount}
                onChange={handleGuestCountChange}
              />
            </div>
          </div>

          <div className="vd-avail-status-card">
            {loadingAvail && (
              <span className="vd-avail-loading">Checking real-time availability…</span>
            )}
            {availError && (
              <div className="vd-avail-error">{availError}</div>
            )}
            {!loadingAvail && !availError && isNotOperating && (
              <div className="vd-fully-booked-notice">
                <DangerIcon size={16} /> Listing does not operate on this date ({selectedDate}).
              </div>
            )}
            {!loadingAvail && !availError && !isNotOperating && isSoldOut && (
              <div className="vd-fully-booked-notice">
                <DangerIcon size={16} /> Fully Booked on {selectedDate}. Please pick another date.
              </div>
            )}
            {!loadingAvail && !availError && !isNotOperating && !isSoldOut && (
              <div className="vd-avail-success">
                <CheckCircleIcon size={16} /> Available on {selectedDate}
                {remainingCount !== undefined && remainingCount !== null && remainingCount > 0 ? ` (${remainingCount} slots left)` : ''}
              </div>
            )}
          </div>

          {submitError && (
            <div className="vd-avail-error" style={{ padding: '8px 12px', background: '#fef2f2', borderRadius: '8px', border: '1px solid #fecaca' }}>
              {submitError}
            </div>
          )}

          <div className="vd-detail-modal__footer">
            <div className="vd-detail-price">
              <span className="vd-detail-price__label">Estimated Total</span>
              <span className="vd-detail-price__val">
                LKR {estimatedTotal.toLocaleString()}
              </span>
            </div>
            <button
              className="vd-btn-book"
              disabled={loadingAvail || isSoldOut || isNotOperating || submitting}
              onClick={handleConfirmBooking}
            >
              {submitting ? 'Confirming…' : 'Confirm Booking'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
