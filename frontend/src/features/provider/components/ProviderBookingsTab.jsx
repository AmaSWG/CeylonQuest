import { useMemo, useState } from 'react'
import {
  CalendarMonthIcon
} from '../../../components/Icons'
import StatusBadge from '../../../components/common/StatusBadge'
import EmptyState from '../../../components/common/EmptyState'

function formatDate(value) {
  if (!value) return '—'

  const date =
    new Date(`${value}T00:00:00`)

  if (Number.isNaN(date.getTime())) {
    return value
  }

  return date.toLocaleDateString(
    'en-GB',
    {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    }
  )
}

function formatCurrency(amount) {
  const value =
    Number(amount ?? 0)

  return new Intl.NumberFormat(
    'en-LK',
    {
      style: 'currency',
      currency: 'LKR',
      maximumFractionDigits: 0
    }
  ).format(value)
}

function normalizeStatus(status) {
  return String(status || '')
    .replace(/[_-]/g, ' ')
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .trim()
}

function shortId(id) {
  if (!id) return '—'

  return String(id)
    .split('-')[0]
    .toUpperCase()
}

export default function ProviderBookingsTab({
  bookings = [],
  loading = false,
  error = null,
  onRetry
}) {
  const [filter, setFilter] =
    useState('all')

  const [search, setSearch] =
    useState('')

  const counts = useMemo(() => {
    const result = {
      all: bookings.length,
      pending: 0,
      confirmed: 0,
      completed: 0,
      cancelled: 0
    }

    bookings.forEach((booking) => {
      const status =
        String(booking.status || '')
          .toLowerCase()

      if (status.includes('pending')) {
        result.pending += 1
      }

      if (status === 'confirmed') {
        result.confirmed += 1
      }

      if (status === 'completed') {
        result.completed += 1
      }

      if (status === 'cancelled') {
        result.cancelled += 1
      }
    })

    return result
  }, [bookings])

  const filteredBookings =
    useMemo(() => {
      const query =
        search.trim().toLowerCase()

      return bookings.filter((booking) => {
        const status =
          String(booking.status || '')
            .toLowerCase()

        if (
          filter !== 'all' &&
          !status.includes(filter)
        ) {
          return false
        }

        if (!query) {
          return true
        }

        const searchable =
          [
            booking.serviceName,
            booking.bookingType,
            booking.customerName,
            booking.customerEmail,
            booking.customerId,
            booking.id
          ]
            .filter(Boolean)
            .join(' ')
            .toLowerCase()

        return searchable.includes(query)
      })
    }, [bookings, filter, search])

  if (loading) {
    return (
      <div className="pd-bookings-tab">
        <div className="pd-page-header">
          <div className="pd-page-header__left">
            <h1>Booking Management</h1>
            <p>
              Track customer bookings and reservations
              for your services.
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

  if (error) {
    return (
      <div className="pd-bookings-tab">
        <div className="pd-page-header">
          <div className="pd-page-header__left">
            <h1>Booking Management</h1>
            <p>
              Track customer bookings and reservations
              for your services.
            </p>
          </div>
        </div>

        <div className="pd-bookings-error">
          <div>
            <strong>
              Unable to load bookings
            </strong>

            <p>{error}</p>
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

  return (
    <div className="pd-bookings-tab">
      <div className="pd-page-header">
        <div className="pd-page-header__left">
          <h1>Booking Management</h1>

          <p>
            Track customer bookings and restaurant
            reservations associated with your services.
          </p>
        </div>

        <div className="pd-bookings-count">
          {bookings.length}{' '}
          {bookings.length === 1
            ? 'Booking'
            : 'Bookings'}
        </div>
      </div>

      <div className="pd-bookings-card">
        <div className="pd-bookings-toolbar">
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
              placeholder={
                'Search by service, customer ID or booking ID'
              }
              aria-label="Search bookings"
            />
          </div>

          <div
            className="pd-bookings-filters"
            aria-label="Booking status filters"
          >
            {[
              ['all', 'All'],
              ['pending', 'Pending'],
              ['confirmed', 'Confirmed'],
              ['completed', 'Completed']
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
                onClick={() => setFilter(key)}
              >
                {label}

                <span>
                  {counts[key]}
                </span>
              </button>
            ))}
          </div>
        </div>

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
                  ? 'Bookings and reservations for your services will appear here.'
                  : 'Try changing your search or status filter.'
              }
            />
          </div>
        ) : (
          <div className="pd-bookings-table-wrap">
            <table className="pd-bookings-table">
              <thead>
                <tr>
                  <th>Booking</th>
                  <th>Customer</th>
                  <th>Service</th>
                  <th>Type</th>
                  <th>Date & Time</th>
                  <th>Guests</th>
                  <th>Amount</th>
                  <th>Status</th>
                </tr>
              </thead>

              <tbody>
                {filteredBookings.map(
                  (booking) => (
                    <tr key={booking.id}>
                      <td>
                        <span className="pd-booking-id">
                          #{shortId(booking.id)}
                        </span>
                      </td>

                      <td>
                        <div className="pd-customer-cell">
                          <span className="pd-customer-avatar">
                            V
                          </span>

                          <div>
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

                      <td>
                        <strong className="pd-service-name">
                          {booking.serviceName ||
                            'Service'}
                        </strong>
                      </td>

                      <td>
                        <span
                          className={
                            booking.bookingType
                              ?.toLowerCase()
                              .includes('restaurant')
                              ? 'pd-type-badge pd-type-badge--restaurant'
                              : 'pd-type-badge pd-type-badge--experience'
                          }
                        >
                          {booking.bookingType}
                        </span>
                      </td>

                      <td>
                        <div className="pd-date-cell">
                          <strong>
                            {formatDate(
                              booking.date
                            )}
                          </strong>

                          <small>
                            {booking.time || '—'}
                          </small>
                        </div>
                      </td>

                      <td>
                        {booking.peopleCount ?? '—'}
                      </td>

                      <td>
                        <strong>
                          {formatCurrency(
                            booking.totalAmount
                          )}
                        </strong>

                        {booking.paymentStatus && (
                          <small className="pd-payment-state">
                            {normalizeStatus(
                              booking.paymentStatus
                            )}
                          </small>
                        )}
                      </td>

                      <td>
                        <StatusBadge
                          status={
                            normalizeStatus(
                              booking.status
                            ) || 'Pending'
                          }
                        />
                      </td>
                    </tr>
                  )
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}