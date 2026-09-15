import React, { useState, useEffect } from 'react'
import './VisitorBookingModal.css'
import {
  CloseIcon,
  CalendarMonthIcon,
  CheckCircleIcon,
  DangerIcon
} from '../../../components/Icons'
import { catalogUrl } from '../../../api/client'

export default function VisitorBookingModal({ item, onClose }) {
  const [selectedDate, setSelectedDate] = useState(
    new Date(Date.now() + 86400000).toISOString().split('T')[0]
  )
  const [guestCount, setGuestCount] = useState(1)
  const [availability, setAvailability] = useState(null)
  const [loadingAvail, setLoadingAvail] = useState(false)
  const [availError, setAvailError] = useState(null)
  const [bookingSuccess, setBookingSuccess] = useState(false)

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

  const isSoldOut = availability && availability.availableCapacity <= 0

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

        {bookingSuccess ? (
          <div className="vd-booking-success">
            <CheckCircleIcon size={48} className="vd-booking-success__icon" />
            <h3>Booking Request Submitted!</h3>
            <p>Your booking request for {item.title} on {selectedDate} ({guestCount} guest(s)) has been received. The provider will review it shortly.</p>
            <button className="vd-btn-book" onClick={onClose}>Done</button>
          </div>
        ) : (
          <div className="vd-booking-modal__body">
            <div className="vd-booking-info-row">
              <span className="vd-booking-info-label">
                <CalendarMonthIcon size={15} /> Base Rate
              </span>
              <span className="vd-booking-info-price">
                LKR {Number(item.price).toLocaleString()} <small>/{item.priceUnit}</small>
              </span>
            </div>

            <div className="vd-booking-details-box">
              <div>
                <span className="vd-booking-box-label">Location</span>
                <span className="vd-booking-box-val">{item.location}</span>
              </div>
              <div>
                <span className="vd-booking-box-label">Region</span>
                <span className="vd-booking-box-val">{item.region}</span>
              </div>
              <div className="vd-booking-box-full">
                <span className="vd-booking-box-label">Service Type</span>
                <span className="vd-booking-box-val">{item.type} — {item.category || item.roomType || item.cuisineType}</span>
              </div>
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
                <label className="vd-form-label">Guests / Units</label>
                <input
                  type="number"
                  className="vd-form-input"
                  min="1"
                  max={item.maxParticipants || item.maxGuestsPerRoom || 20}
                  value={guestCount}
                  onChange={(e) => setGuestCount(Math.max(1, parseInt(e.target.value) || 1))}
                />
              </div>
            </div>

            <div className="vd-avail-status-card">
              {loadingAvail && (
                <span className="vd-avail-loading">Checking real-time capacity…</span>
              )}
              {availError && (
                <div className="vd-avail-error">{availError}</div>
              )}
              {availability && !loadingAvail && isSoldOut && (
                <div className="vd-fully-booked-notice">
                  <DangerIcon size={16} /> Fully Booked on {selectedDate}. Please pick another date.
                </div>
              )}
              {availability && !loadingAvail && !isSoldOut && (
                <div className="vd-avail-success">
                  <CheckCircleIcon size={16} /> Available! {availability.availableCapacity} remaining on {selectedDate}.
                </div>
              )}
            </div>

            <div className="vd-detail-modal__footer">
              <div className="vd-detail-price">
                <span className="vd-detail-price__label">Estimated Total</span>
                <span className="vd-detail-price__val">
                  LKR {(Number(item.price) * (item.priceUnit === 'person' || item.priceUnit === 'guest' ? guestCount : 1)).toLocaleString()}
                </span>
              </div>
              <button
                className="vd-btn-book"
                disabled={loadingAvail || isSoldOut}
                onClick={() => setBookingSuccess(true)}
              >
                Confirm Booking
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
