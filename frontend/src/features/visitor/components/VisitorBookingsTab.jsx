import { useCallback, useEffect, useState } from 'react'
import { bookingUrl } from '../../../api/client'
import './VisitorBookingsTab.css'

export default function VisitorBookingsTab({ onSessionExpired }) {
    const [bookings, setBookings] = useState([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)

    const fetchBookings = useCallback(async () => {
        const token = localStorage.getItem('authToken')

        if (!token) {
            onSessionExpired?.()
            return
        }

        setLoading(true)
        setError(null)

        try {
            const response = await fetch(
                bookingUrl('/api/user-bookings/my'),
                {
                    headers: {
                        Authorization: `Bearer ${token}`
                    }
                }
            )

            if (response.status === 401) {
                setError('Your session has expired. Please log in again.')
                return
            }

            if (response.status === 403) {
                setError('You are not authorized to view these bookings.')
                return
            }

            if (!response.ok) {
                const data = await response.json().catch(() => null)

                setError(
                    data?.message ||
                    'Unable to load your bookings and reservations.'
                )

                return
            }

            const data = await response.json()
            setBookings(Array.isArray(data) ? data : [])
        } catch {
            setError(
                'Network error. Please check your connection and try again.'
            )
        } finally {
            setLoading(false)
        }
    }, [onSessionExpired])

    useEffect(() => {
        fetchBookings()
    }, [fetchBookings])

    const formatDate = (date) => {
        if (!date) return '—'

        return new Date(`${date}T00:00:00`).toLocaleDateString(
            'en-US',
            {
                year: 'numeric',
                month: 'short',
                day: 'numeric'
            }
        )
    }

    const formatMoney = (amount) => {
        const value = Number(amount ?? 0)

        return `LKR ${value.toLocaleString()}`
    }

    const getStatusClass = (status) => {
        const normalized = String(status || '')
            .toLowerCase()
            .replace(/\s+/g, '-')

        return `vb-status vb-status--${normalized}`
    }

    if (loading) {
        return (
            <div className="vb-page">
                <div className="vb-header">
                    <h1>My Bookings & Reservations</h1>
                    <p>View your experience bookings and restaurant reservations.</p>
                </div>

                <div className="vb-state">
                    <div className="vb-loader" />
                    <p>Loading your bookings and reservations...</p>
                </div>
            </div>
        )
    }

    if (error) {
        return (
            <div className="vb-page">
                <div className="vb-header">
                    <h1>My Bookings & Reservations</h1>
                    <p>View your experience bookings and restaurant reservations.</p>
                </div>

                <div className="vb-state vb-state--error">
                    <h3>Unable to load bookings</h3>
                    <p>{error}</p>

                    <button
                        type="button"
                        className="vb-retry-btn"
                        onClick={fetchBookings}
                    >
                        Try Again
                    </button>
                </div>
            </div>
        )
    }

    return (
        <div className="vb-page">

            <div className="vb-header">
                <div>
                    <h1>My Bookings & Reservations</h1>
                    <p>
                        Track your upcoming and previous experiences and
                        restaurant reservations.
                    </p>
                </div>

                <div className="vb-count">
                    {bookings.length}{' '}
                    {bookings.length === 1 ? 'Booking' : 'Bookings'}
                </div>
            </div>

            {bookings.length === 0 ? (
                <div className="vb-state">
                    <div className="vb-empty-icon">📅</div>

                    <h3>No bookings or reservations yet</h3>

                    <p>
                        Your experience bookings and restaurant reservations
                        will appear here after you make them.
                    </p>
                </div>
            ) : (
                <div className="vb-grid">
                    {bookings.map((booking) => {
                        const isRestaurant =
                            booking.bookingType === 'Restaurant Reservation'

                        return (
                            <article
                                key={`${booking.bookingType}-${booking.id}`}
                                className="vb-card"
                            >
                                <div className="vb-card__top">
                                    <span
                                        className={
                                            isRestaurant
                                                ? 'vb-type vb-type--restaurant'
                                                : 'vb-type vb-type--experience'
                                        }
                                    >
                                        {booking.bookingType}
                                    </span>

                                    <span className={getStatusClass(booking.status)}>
                                        {booking.status || 'Unknown'}
                                    </span>
                                </div>

                                <h2 className="vb-card__title">
                                    {booking.serviceName || 'Unnamed Service'}
                                </h2>

                                <div className="vb-details">

                                    <div className="vb-detail">
                                        <span className="vb-detail__label">
                                            Date
                                        </span>

                                        <span className="vb-detail__value">
                                            {formatDate(booking.date)}
                                        </span>
                                    </div>

                                    <div className="vb-detail">
                                        <span className="vb-detail__label">
                                            Time
                                        </span>

                                        <span className="vb-detail__value">
                                            {booking.time || '—'}
                                        </span>
                                    </div>

                                    <div className="vb-detail">
                                        <span className="vb-detail__label">
                                            {isRestaurant
                                                ? 'Party Size'
                                                : 'Participants'}
                                        </span>

                                        <span className="vb-detail__value">
                                            {booking.peopleCount}
                                        </span>
                                    </div>

                                    <div className="vb-detail">
                                        <span className="vb-detail__label">
                                            Total Amount
                                        </span>

                                        <span className="vb-detail__value vb-price">
                                            {formatMoney(booking.totalAmount)}
                                        </span>
                                    </div>

                                </div>

                                {booking.paymentStatus && (
                                    <div className="vb-payment">
                                        <span>Payment Status</span>

                                        <strong>
                                            {booking.paymentStatus}
                                        </strong>
                                    </div>
                                )}

                            </article>
                        )
                    })}
                </div>
            )}
        </div>
    )
}