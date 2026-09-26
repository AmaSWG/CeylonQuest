import { useMemo, useState } from 'react'
import {
  CalendarMonthIcon
} from '../../../components/Icons'

import StatusBadge from '../../../components/common/StatusBadge'
import EmptyState from '../../../components/common/EmptyState'


/* =========================================================
   Helpers
   ========================================================= */

function formatDate(value) {
  if (!value) return '—'

  const date = new Date(`${value}T00:00:00`)

  if (Number.isNaN(date.getTime())) {
    return value
  }

  return date.toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'short',
    year: 'numeric'
  })
}


function formatDateTime(value) {
  if (!value) return '—'

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return '—'
  }

  return date.toLocaleString('en-GB')
}


function formatCurrency(amount) {
  const value = Number(amount ?? 0)

  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    maximumFractionDigits: 0
  }).format(value)
}


function normalizeStatus(status) {
  return String(status || '')
    .replace(/[_-]/g, ' ')
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .trim()
}


function getStatusGroup(status) {
  const value = String(status || '')
    .replace(/[\s_-]/g, '')
    .toLowerCase()

  if (
    value === 'pendingpayment' ||
    value === 'pending'
  ) {
    return 'pending'
  }

  if (value === 'confirmed') {
    return 'confirmed'
  }

  if (value === 'completed') {
    return 'completed'
  }

  if (
    value === 'cancelled' ||
    value === 'canceled'
  ) {
    return 'cancelled'
  }

  return value
}


function getDisplayStatus(status) {
  const group = getStatusGroup(status)

  if (group === 'pending') {
    return 'Pending'
  }

  if (group === 'confirmed') {
    return 'Confirmed'
  }

  if (group === 'completed') {
    return 'Completed'
  }

  if (group === 'cancelled') {
    return 'Cancelled'
  }

  return normalizeStatus(status) || 'Pending'
}


function shortId(id) {
  if (!id) return '—'

  return String(id)
    .split('-')[0]
    .toUpperCase()
}


function isRestaurant(booking) {
  return booking?.bookingType === 'Restaurant Reservation'
}


function isAccommodation(booking) {
  return booking?.bookingType === 'Accommodation Booking'
}


function getTypeClass(booking) {
  if (isRestaurant(booking)) {
    return 'pd-type-badge pd-type-badge--restaurant'
  }

  if (isAccommodation(booking)) {
    return 'pd-type-badge pd-type-badge--accommodation'
  }

  return 'pd-type-badge pd-type-badge--experience'
}


function getTypeLabel(booking) {
  if (isRestaurant(booking)) {
    return 'Restaurant'
  }

  if (isAccommodation(booking)) {
    return 'Accommodation'
  }

  return 'Experience'
}


/* =========================================================
   Provider Bookings Tab
   ========================================================= */

export default function ProviderBookingsTab({
  bookings = [],
  loading = false,
  error = null,
  onRetry
}) {
  const [filter, setFilter] = useState('all')
  const [search, setSearch] = useState('')
  const [selectedBooking, setSelectedBooking] = useState(null)


  /* =========================================================
     Counts
     ========================================================= */

  const counts = useMemo(() => {
    const result = {
      all: bookings.length,
      pending: 0,
      confirmed: 0,
      completed: 0,
      cancelled: 0
    }

    bookings.forEach((booking) => {
      const statusGroup = getStatusGroup(
        booking.status
      )

      if (
        Object.prototype.hasOwnProperty.call(
          result,
          statusGroup
        )
      ) {
        result[statusGroup] += 1
      }
    })

    return result
  }, [bookings])


  /* =========================================================
     Filter + Search
     ========================================================= */

  const filteredBookings = useMemo(() => {
    const query = search
      .trim()
      .toLowerCase()

    return bookings.filter((booking) => {
      const statusGroup = getStatusGroup(
        booking.status
      )

      if (
        filter !== 'all' &&
        statusGroup !== filter
      ) {
        return false
      }

      if (!query) {
        return true
      }

      const searchable = [
        booking.serviceName,
        booking.bookingType,
        booking.customerName,
        booking.customerEmail,
        booking.customerId,
        booking.id,
        booking.status
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()

      return searchable.includes(query)
    })
  }, [bookings, filter, search])


  /* =========================================================
     Loading
     ========================================================= */

  if (loading) {
    return (
      <div className="pd-bookings-tab">

        <div className="pd-page-header">
          <div className="pd-page-header__left">

            <h1>
              Booking Management
            </h1>

            <p>
              Track customer bookings, reservations and
              accommodation stays for your services.
            </p>

          </div>
        </div>


        <div className="pd-bookings-loading">

          <div className="pd-bookings-spinner" />

          <div>

            <strong>
              Loading bookings
            </strong>

            <p>
              Retrieving your latest customer activity…
            </p>

          </div>

        </div>

      </div>
    )
  }


  /* =========================================================
     Error
     ========================================================= */

  if (error) {
    return (
      <div className="pd-bookings-tab">

        <div className="pd-page-header">

          <div className="pd-page-header__left">

            <h1>
              Booking Management
            </h1>

            <p>
              Track customer bookings, reservations and
              accommodation stays for your services.
            </p>

          </div>

        </div>


        <div className="pd-bookings-error">

          <div>

            <strong>
              Unable to load bookings
            </strong>

            <p>
              {error}
            </p>

          </div>

          <button
            type="button"
            className="pd-bookings-retry"
            onClick={onRetry}
          >
            Try Again
          </button>

        </div>

      </div>
    )
  }


  /* =========================================================
     Main
     ========================================================= */

  return (
    <div className="pd-bookings-tab">


      {/* =====================================================
          HEADER
         ===================================================== */}

      <div className="pd-page-header">

        <div className="pd-page-header__left">

          <h1>
            Booking Management
          </h1>

          <p>
            Track customer experience bookings,
            restaurant reservations and accommodation
            bookings associated with your services.
          </p>

        </div>


        <div className="pd-bookings-count">

          {bookings.length}{' '}

          {bookings.length === 1
            ? 'Booking'
            : 'Bookings'}

        </div>

      </div>


      {/* =====================================================
          CARD
         ===================================================== */}

      <div className="pd-bookings-card">


        {/* =====================================================
            TOOLBAR
           ===================================================== */}

        <div className="pd-bookings-toolbar">


          {/* Search */}

          <div className="pd-bookings-search">

            <span
              className="pd-bookings-search__icon"
              aria-hidden="true"
            >
              ⌕
            </span>

            <input
              type="search"
              value={search}
              onChange={(event) =>
                setSearch(event.target.value)
              }
              placeholder="Search by service, customer or booking ID"
              aria-label="Search bookings"
            />

          </div>


          {/* Filters */}

          <div
            className="pd-bookings-filters"
            aria-label="Booking status filters"
          >

            {[
              ['all', 'All'],
              ['pending', 'Pending'],
              ['confirmed', 'Confirmed'],
              ['completed', 'Completed'],
              ['cancelled', 'Cancelled']
            ].map(([key, label]) => (

              <button
                type="button"
                key={key}
                className={
                  `pd-bookings-filter ${filter === key
                    ? 'active'
                    : ''
                  }`
                }
                onClick={() =>
                  setFilter(key)
                }
              >

                {label}

                <span>
                  {counts[key]}
                </span>

              </button>

            ))}

          </div>

        </div>


        {/* =====================================================
            EMPTY STATE
           ===================================================== */}

        {filteredBookings.length === 0 ? (

          <div className="pd-bookings-empty">

            <EmptyState
              icon={CalendarMonthIcon}
              title={
                bookings.length === 0
                  ? 'No customer bookings yet'
                  : 'No matching bookings'
              }
              message={
                bookings.length === 0
                  ? 'Experience bookings, restaurant reservations and accommodation bookings for your services will appear here.'
                  : 'Try changing your search or status filter.'
              }
            />

          </div>

        ) : (


          /* =====================================================
             TABLE
             ===================================================== */

          <div className="pd-bookings-table-wrap">

            <table className="pd-bookings-table">

              <thead>

                <tr>
                  <th>Booking</th>
                  <th>Customer</th>
                  <th>Service</th>
                  <th>Type</th>
                  <th>Date / Stay</th>
                  <th>Guests</th>
                  <th>Amount</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>

              </thead>


              <tbody>

                {filteredBookings.map(
                  (booking) => (

                    <tr key={booking.id}>


                      {/* BOOKING ID */}

                      <td>

                        <span className="pd-booking-id">
                          #{shortId(booking.id)}
                        </span>

                      </td>


                      {/* CUSTOMER - AVATAR REMOVED */}

                      <td>

                        <div className="pd-customer-cell">

                          <div className="pd-customer-info">

                            <strong>
                              {booking.customerName ||
                                'Visitor'}
                            </strong>

                            <small>
                              {booking.customerEmail ||
                                `ID ${shortId(
                                  booking.customerId
                                )}`}
                            </small>

                          </div>

                        </div>

                      </td>


                      {/* SERVICE */}

                      <td>

                        <strong className="pd-service-name">

                          {booking.serviceName ||
                            'Service'}

                        </strong>


                        {isAccommodation(booking) &&
                          booking.numberOfNights != null && (

                            <small
                              style={{
                                display: 'block',
                                marginTop: '4px'
                              }}
                            >

                              {booking.numberOfNights}{' '}

                              {Number(
                                booking.numberOfNights
                              ) === 1
                                ? 'night'
                                : 'nights'}

                            </small>

                          )}

                      </td>


                      {/* TYPE */}

                      <td>

                        <span
                          className={
                            getTypeClass(booking)
                          }
                        >

                          {getTypeLabel(booking)}

                        </span>

                      </td>


                      {/* DATE / STAY */}

                      <td>

                        <div className="pd-date-cell">

                          {isAccommodation(
                            booking
                          ) ? (

                            <>

                              <strong>

                                {formatDate(
                                  booking.checkInDate ||
                                  booking.date
                                )}

                              </strong>

                              <small>

                                to{' '}

                                {formatDate(
                                  booking.checkOutDate
                                )}

                              </small>

                            </>

                          ) : (

                            <>

                              <strong>

                                {formatDate(
                                  booking.date
                                )}

                              </strong>

                              <small>
                                {booking.time || '—'}
                              </small>

                            </>

                          )}

                        </div>

                      </td>


                      {/* PEOPLE */}

                      <td>
                        {booking.peopleCount ?? '—'}
                      </td>


                      {/* AMOUNT */}

                      <td>

                        <strong>

                          {formatCurrency(
                            booking.totalAmount
                          )}

                        </strong>


                        {isAccommodation(
                          booking
                        ) &&
                          booking.unitPrice != null && (

                            <small className="pd-payment-state">

                              {formatCurrency(
                                booking.unitPrice
                              )}{' '}
                              / night

                            </small>

                          )}


                        {!isAccommodation(
                          booking
                        ) &&
                          booking.paymentStatus && (

                            <small className="pd-payment-state">

                              {normalizeStatus(
                                booking.paymentStatus
                              )}

                            </small>

                          )}

                      </td>


                      {/* STATUS */}

                      <td>

                        <StatusBadge
                          status={
                            getDisplayStatus(
                              booking.status
                            )
                          }
                        />

                      </td>


                      {/* ACTION */}

                      <td>

                        <button
                          type="button"
                          className="pd-bookings-view-btn"
                          onClick={() =>
                            setSelectedBooking(
                              booking
                            )
                          }
                        >
                          View Details
                        </button>

                      </td>

                    </tr>

                  )
                )}

              </tbody>

            </table>

          </div>

        )}

      </div>


      {/* =====================================================
          DETAILS MODAL
         ===================================================== */}

      {selectedBooking && (

        <div
          className="pd-booking-modal-overlay"
          role="presentation"
          onMouseDown={(event) => {

            if (
              event.target ===
              event.currentTarget
            ) {
              setSelectedBooking(null)
            }

          }}
        >

          <div
            className="pd-booking-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="booking-details-title"
          >


            {/* HEADER */}

            <div className="pd-booking-modal__header">

              <div>

                <span
                  className={
                    getTypeClass(
                      selectedBooking
                    )
                  }
                >

                  {getTypeLabel(
                    selectedBooking
                  )}

                </span>

                <h2 id="booking-details-title">

                  {selectedBooking.serviceName ||
                    'Booking Details'}

                </h2>

              </div>


              <button
                type="button"
                className="pd-booking-modal__close"
                onClick={() =>
                  setSelectedBooking(null)
                }
                aria-label="Close booking details"
              >
                ×
              </button>

            </div>


            {/* BODY */}

            <div className="pd-booking-modal__body">


              {/* STATUS */}

              <div className="pd-booking-detail-status">

                <span>
                  Status
                </span>

                <StatusBadge
                  status={
                    getDisplayStatus(
                      selectedBooking.status
                    )
                  }
                />

              </div>


              {/* CUSTOMER */}

              <div className="pd-booking-detail-section">

                <h3>
                  Customer Information
                </h3>

                <div className="pd-booking-detail-grid">


                  <div className="pd-booking-detail-item">

                    <span>
                      Customer Name
                    </span>

                    <strong>

                      {selectedBooking.customerName ||
                        'Visitor'}

                    </strong>

                  </div>


                  <div className="pd-booking-detail-item">

                    <span>
                      Email
                    </span>

                    <strong>

                      {selectedBooking.customerEmail ||
                        '—'}

                    </strong>

                  </div>


                  <div className="pd-booking-detail-item">

                    <span>
                      Booking ID
                    </span>

                    <strong>
                      {selectedBooking.id}
                    </strong>

                  </div>


                  <div className="pd-booking-detail-item">

                    <span>
                      Customer ID
                    </span>

                    <strong>

                      {selectedBooking.customerId ||
                        '—'}

                    </strong>

                  </div>

                </div>

              </div>


              {/* =================================================
                  ACCOMMODATION
                 ================================================= */}

              {isAccommodation(
                selectedBooking
              ) ? (

                <div className="pd-booking-detail-section">

                  <h3>
                    Accommodation Details
                  </h3>

                  <div className="pd-booking-detail-grid">


                    <div className="pd-booking-detail-item">

                      <span>
                        Check-in
                      </span>

                      <strong>

                        {formatDate(
                          selectedBooking.checkInDate
                        )}

                      </strong>

                    </div>


                    <div className="pd-booking-detail-item">

                      <span>
                        Check-out
                      </span>

                      <strong>

                        {formatDate(
                          selectedBooking.checkOutDate
                        )}

                      </strong>

                    </div>


                    <div className="pd-booking-detail-item">

                      <span>
                        Number of Nights
                      </span>

                      <strong>

                        {selectedBooking.numberOfNights ??
                          '—'}

                      </strong>

                    </div>


                    <div className="pd-booking-detail-item">

                      <span>
                        Guests
                      </span>

                      <strong>

                        {selectedBooking.peopleCount ??
                          '—'}

                      </strong>

                    </div>


                    <div className="pd-booking-detail-item">

                      <span>
                        Price Per Night
                      </span>

                      <strong>

                        {formatCurrency(
                          selectedBooking.unitPrice
                        )}

                      </strong>

                    </div>


                    <div className="pd-booking-detail-item">

                      <span>
                        Total Amount
                      </span>

                      <strong>

                        {formatCurrency(
                          selectedBooking.totalAmount
                        )}

                      </strong>

                    </div>

                  </div>

                </div>

              ) : (


                /* =================================================
                   EXPERIENCE / RESTAURANT
                   ================================================= */

                <div className="pd-booking-detail-section">

                  <h3>
                    {isRestaurant(selectedBooking)
                      ? 'Reservation Details'
                      : 'Booking Details'}
                  </h3>

                  <div className="pd-booking-detail-grid">


                    <div className="pd-booking-detail-item">

                      <span>
                        Date
                      </span>

                      <strong>

                        {formatDate(
                          selectedBooking.date
                        )}

                      </strong>

                    </div>


                    <div className="pd-booking-detail-item">

                      <span>
                        Time
                      </span>

                      <strong>

                        {selectedBooking.time ||
                          '—'}

                      </strong>

                    </div>


                    <div className="pd-booking-detail-item">

                      <span>

                        {isRestaurant(
                          selectedBooking
                        )
                          ? 'Party Size'
                          : 'Participants'}

                      </span>

                      <strong>

                        {selectedBooking.peopleCount ??
                          '—'}

                      </strong>

                    </div>


                    <div className="pd-booking-detail-item">

                      <span>
                        Total Amount
                      </span>

                      <strong>

                        {formatCurrency(
                          selectedBooking.totalAmount
                        )}

                      </strong>

                    </div>


                    {selectedBooking.unitPrice != null && (

                      <div className="pd-booking-detail-item">

                        <span>
                          Unit Price
                        </span>

                        <strong>

                          {formatCurrency(
                            selectedBooking.unitPrice
                          )}

                        </strong>

                      </div>

                    )}


                    {selectedBooking.paymentStatus && (

                      <div className="pd-booking-detail-item">

                        <span>
                          Payment Status
                        </span>

                        <strong>

                          {normalizeStatus(
                            selectedBooking.paymentStatus
                          )}

                        </strong>

                      </div>

                    )}

                  </div>

                </div>

              )}


              {/* =================================================
                  CANCELLATION / REFUND
                 ================================================= */}

              {getStatusGroup(
                selectedBooking.status
              ) === 'cancelled' && (

                  <div className="pd-booking-detail-section pd-booking-cancelled">

                    <h3>
                      Cancellation & Refund
                    </h3>

                    <div className="pd-booking-detail-grid">


                      <div className="pd-booking-detail-item pd-booking-detail-item--full">

                        <span>
                          Cancellation Reason
                        </span>

                        <strong>

                          {selectedBooking.cancellationReason ||
                            'No reason provided'}

                        </strong>

                      </div>


                      <div className="pd-booking-detail-item">

                        <span>
                          Cancelled At
                        </span>

                        <strong>

                          {formatDateTime(
                            selectedBooking.cancelledAt
                          )}

                        </strong>

                      </div>


                      <div className="pd-booking-detail-item">

                        <span>
                          Refund Percentage
                        </span>

                        <strong>

                          {Number(
                            selectedBooking.refundPercentage ??
                            0
                          )}
                          %

                        </strong>

                      </div>


                      <div className="pd-booking-detail-item">

                        <span>
                          Refund Amount
                        </span>

                        <strong>

                          {formatCurrency(
                            selectedBooking.refundAmount
                          )}

                        </strong>

                      </div>


                      <div className="pd-booking-detail-item">

                        <span>
                          Refunded At
                        </span>

                        <strong>

                          {formatDateTime(
                            selectedBooking.refundedAt
                          )}

                        </strong>

                      </div>

                    </div>

                  </div>

                )}

            </div>


            {/* FOOTER */}

            <div className="pd-booking-modal__footer">

              <button
                type="button"
                onClick={() =>
                  setSelectedBooking(null)
                }
              >
                Close
              </button>

            </div>

          </div>

        </div>

      )}

    </div>
  )
}