import { useCallback, useEffect, useMemo, useState } from 'react'
import { bookingUrl } from '../../../api/client'
import './VisitorBookingsTab.css'

export default function VisitorBookingsTab({ onSessionExpired }) {
    const [bookings, setBookings] = useState([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)

    const [selectedBooking, setSelectedBooking] = useState(null)
    const [showCancelConfirm, setShowCancelConfirm] = useState(false)
    const [showCancelForm, setShowCancelForm] = useState(false)

    const [cancellationReason, setCancellationReason] = useState('')
    const [cancelLoading, setCancelLoading] = useState(false)
    const [cancelError, setCancelError] = useState('')
    const [successMessage, setSuccessMessage] = useState('')

    const [searchTerm, setSearchTerm] = useState('')
    const [statusFilter, setStatusFilter] = useState('all')

    // =========================================================
    // FETCH BOOKINGS
    // =========================================================

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
                onSessionExpired?.()
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

    // =========================================================
    // FORMATTERS
    // =========================================================

    const formatDate = (date) => {
        if (!date) {
            return '—'
        }

        return new Date(`${date}T00:00:00`).toLocaleDateString(
            'en-US',
            {
                year: 'numeric',
                month: 'short',
                day: 'numeric'
            }
        )
    }

    const formatDateTime = (date) => {
        if (!date) {
            return '—'
        }

        const value = new Date(date)

        if (Number.isNaN(value.getTime())) {
            return '—'
        }

        return value.toLocaleString()
    }

    const formatMoney = (amount) => {
        const value = Number(amount ?? 0)
        return `LKR ${value.toLocaleString()}`
    }

    // =========================================================
    // BOOKING TYPE HELPERS
    // =========================================================

    const isRestaurant = (booking) => {
        return booking?.bookingType === 'Restaurant Reservation'
    }

    const isAccommodation = (booking) => {
        return booking?.bookingType === 'Accommodation Booking'
    }

    const isExperience = (booking) => {
        return booking?.bookingType === 'Experience Booking'
    }

    const getBookingTypeLabel = (booking) => {
        if (isRestaurant(booking)) {
            return 'RESTAURANT'
        }

        if (isAccommodation(booking)) {
            return 'ACCOMMODATION'
        }

        return 'EXPERIENCE'
    }

    const getBookingTypeClass = (booking) => {
        if (isRestaurant(booking)) {
            return 'vb-type vb-type--restaurant'
        }

        if (isAccommodation(booking)) {
            return 'vb-type vb-type--accommodation'
        }

        return 'vb-type vb-type--experience'
    }

    const getPeopleLabel = (booking) => {
        if (isRestaurant(booking)) {
            return 'Party Size'
        }

        if (isAccommodation(booking)) {
            return 'Guests'
        }

        return 'Participants'
    }

    const getCancellationTypeName = (booking) => {
        if (isRestaurant(booking)) {
            return 'restaurant reservation'
        }

        if (isAccommodation(booking)) {
            return 'accommodation booking'
        }

        return 'experience booking'
    }

    const getShortCancellationTypeName = (booking) => {
        if (isRestaurant(booking)) {
            return 'Reservation'
        }

        return 'Booking'
    }

    // =========================================================
    // STATUS HELPERS
    // =========================================================

    const isCancelled = (booking) => {
        const status = String(booking?.status || '').toLowerCase()

        return status === 'cancelled' || status === 'canceled'
    }

    const isCompleted = (booking) => {
        return (
            String(booking?.status || '').toLowerCase() ===
            'completed'
        )
    }

    const canCancel = (booking) => {
        return !isCancelled(booking) && !isCompleted(booking)
    }

    const getStatusClass = (status) => {
        const normalized = String(status || '')
            .toLowerCase()
            .replace(/\s+/g, '-')

        return `vb-status vb-status--${normalized}`
    }

    // =========================================================
    // MODAL
    // =========================================================

    const openDetails = (booking) => {
        setSelectedBooking(booking)
        setShowCancelConfirm(false)
        setShowCancelForm(false)
        setCancellationReason('')
        setCancelError('')
    }

    const closeDetails = () => {
        if (cancelLoading) {
            return
        }

        setSelectedBooking(null)
        setShowCancelConfirm(false)
        setShowCancelForm(false)
        setCancellationReason('')
        setCancelError('')
    }

    const openCancelConfirmation = (booking) => {
        setSelectedBooking(booking)
        setShowCancelConfirm(true)
        setShowCancelForm(false)
        setCancellationReason('')
        setCancelError('')
    }

    const continueToCancellation = () => {
        setShowCancelConfirm(false)
        setShowCancelForm(true)
        setCancellationReason('')
        setCancelError('')
    }

    const keepBooking = () => {
        setShowCancelConfirm(false)
        setShowCancelForm(false)
        setCancellationReason('')
        setCancelError('')
    }

    const backToCancelConfirmation = () => {
        if (cancelLoading) {
            return
        }

        setShowCancelForm(false)
        setShowCancelConfirm(true)
        setCancellationReason('')
        setCancelError('')
    }

    // =========================================================
    // FILTERING
    // =========================================================

    const filteredBookings = useMemo(() => {
        const search = searchTerm.trim().toLowerCase()

        return bookings.filter((booking) => {
            const matchesSearch =
                !search ||
                String(booking.serviceName || '')
                    .toLowerCase()
                    .includes(search) ||
                String(booking.bookingType || '')
                    .toLowerCase()
                    .includes(search) ||
                String(booking.status || '')
                    .toLowerCase()
                    .includes(search)

            let matchesStatus = true

            if (statusFilter === 'active') {
                matchesStatus =
                    !isCancelled(booking) &&
                    !isCompleted(booking)
            }

            if (statusFilter === 'cancelled') {
                matchesStatus = isCancelled(booking)
            }

            return matchesSearch && matchesStatus
        })
    }, [bookings, searchTerm, statusFilter])

    // =========================================================
    // CANCEL BOOKING
    // =========================================================

    const handleCancelBooking = async () => {
        if (!selectedBooking) {
            return
        }

        const token = localStorage.getItem('authToken')

        if (!token) {
            onSessionExpired?.()
            return
        }

        setCancelLoading(true)
        setCancelError('')

        try {
            let endpoint

            if (isRestaurant(selectedBooking)) {
                endpoint =
                    `/api/Reservations/${selectedBooking.id}/cancel`
            } else if (isAccommodation(selectedBooking)) {
                endpoint =
                    `/api/AccommodationBookings/${selectedBooking.id}/cancel`
            } else {
                endpoint =
                    `/api/Bookings/${selectedBooking.id}/cancel`
            }

            const response = await fetch(
                bookingUrl(endpoint),
                {
                    method: 'PUT',

                    headers: {
                        Authorization: `Bearer ${token}`,
                        'Content-Type': 'application/json'
                    },

                    body: JSON.stringify({
                        reason:
                            cancellationReason.trim() || null
                    })
                }
            )

            if (response.status === 401) {
                onSessionExpired?.()
                return
            }

            const data =
                await response.json().catch(() => null)

            if (!response.ok) {
                setCancelError(
                    data?.message ||
                    'Unable to cancel this booking.'
                )
                return
            }

            const refundPercentage =
                Number(data?.refundPercentage ?? 0)

            const refundAmount =
                Number(data?.refundAmount ?? 0)

            const typeName =
                isRestaurant(selectedBooking)
                    ? 'Restaurant reservation'
                    : isAccommodation(selectedBooking)
                        ? 'Accommodation booking'
                        : 'Experience booking'

            setSelectedBooking(null)
            setShowCancelConfirm(false)
            setShowCancelForm(false)
            setCancellationReason('')
            setCancelError('')

            setSuccessMessage(
                `${typeName} cancelled successfully. ` +
                `Refund: ${refundPercentage}% ` +
                `(${formatMoney(refundAmount)}).`
            )

            await fetchBookings()
        } catch {
            setCancelError(
                'Network error. Please check your connection and try again.'
            )
        } finally {
            setCancelLoading(false)
        }
    }

    // =========================================================
    // LOADING
    // =========================================================

    if (loading) {
        return (
            <div className="vb-page">
                <div className="vb-header">
                    <div>
                        <h1>My Bookings & Reservations</h1>

                        <p>
                            Track your experiences, restaurant
                            reservations and accommodation bookings.
                        </p>
                    </div>
                </div>

                <div className="vb-state">
                    <div className="vb-loader" />

                    <p>
                        Loading your bookings and reservations...
                    </p>
                </div>
            </div>
        )
    }

    // =========================================================
    // ERROR
    // =========================================================

    if (error) {
        return (
            <div className="vb-page">
                <div className="vb-header">
                    <div>
                        <h1>My Bookings & Reservations</h1>

                        <p>
                            Track your experiences, restaurant
                            reservations and accommodation bookings.
                        </p>
                    </div>
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

    // =========================================================
    // PAGE
    // =========================================================

    return (
        <div className="vb-page">

            {/* HEADER */}

            <div className="vb-header">
                <div>
                    <h1>My Bookings & Reservations</h1>

                    <p>
                        Track your experiences, restaurant
                        reservations and accommodation bookings.
                    </p>
                </div>

                <div className="vb-count">
                    {bookings.length}{' '}
                    {bookings.length === 1
                        ? 'Booking'
                        : 'Bookings'}
                </div>
            </div>

            {/* SUCCESS */}

            {successMessage && (
                <div className="vb-success-message">
                    <span>✓</span>

                    <span>{successMessage}</span>

                    <button
                        type="button"
                        onClick={() =>
                            setSuccessMessage('')
                        }
                    >
                        ×
                    </button>
                </div>
            )}

            {/* EMPTY */}

            {bookings.length === 0 ? (
                <div className="vb-state">
                    <div className="vb-empty-icon">
                        📅
                    </div>

                    <h3>
                        No bookings or reservations yet
                    </h3>

                    <p>
                        Your experience bookings, restaurant
                        reservations and accommodation bookings
                        will appear here.
                    </p>
                </div>
            ) : (
                <>
                    {/* TOOLBAR */}

                    <div className="vb-toolbar">
                        <div className="vb-search-wrapper">
                            <span className="vb-search-icon">
                                ⌕
                            </span>

                            <input
                                type="text"
                                className="vb-search"
                                placeholder="Search your bookings..."
                                value={searchTerm}
                                onChange={(event) =>
                                    setSearchTerm(
                                        event.target.value
                                    )
                                }
                            />
                        </div>

                        <div className="vb-filters">
                            <button
                                type="button"
                                className={
                                    statusFilter === 'all'
                                        ? 'vb-filter-btn vb-filter-btn--active'
                                        : 'vb-filter-btn'
                                }
                                onClick={() =>
                                    setStatusFilter('all')
                                }
                            >
                                All
                            </button>

                            <button
                                type="button"
                                className={
                                    statusFilter === 'active'
                                        ? 'vb-filter-btn vb-filter-btn--active'
                                        : 'vb-filter-btn'
                                }
                                onClick={() =>
                                    setStatusFilter('active')
                                }
                            >
                                Active
                            </button>

                            <button
                                type="button"
                                className={
                                    statusFilter === 'cancelled'
                                        ? 'vb-filter-btn vb-filter-btn--active'
                                        : 'vb-filter-btn'
                                }
                                onClick={() =>
                                    setStatusFilter('cancelled')
                                }
                            >
                                Cancelled
                            </button>
                        </div>
                    </div>

                    {/* BOOKINGS TABLE */}

                    <div className="vb-table-card">
                        <div className="vb-table-scroll">
                            <table className="vb-table">
                                <thead>
                                    <tr>
                                        <th>TYPE</th>
                                        <th>SERVICE</th>
                                        <th>DATE</th>
                                        <th>TIME / STAY</th>
                                        <th>PEOPLE</th>
                                        <th>AMOUNT</th>
                                        <th>STATUS</th>
                                        <th>ACTIONS</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    {filteredBookings.map(
                                        (booking) => (
                                            <tr
                                                key={`${booking.bookingType}-${booking.id}`}
                                                className={
                                                    isCancelled(
                                                        booking
                                                    )
                                                        ? 'vb-row vb-row--cancelled'
                                                        : 'vb-row'
                                                }
                                            >
                                                <td>
                                                    <span
                                                        className={
                                                            getBookingTypeClass(
                                                                booking
                                                            )
                                                        }
                                                    >
                                                        {
                                                            getBookingTypeLabel(
                                                                booking
                                                            )
                                                        }
                                                    </span>
                                                </td>

                                                <td>
                                                    <div className="vb-service-cell">
                                                        <strong>
                                                            {booking.serviceName ||
                                                                'Unnamed Service'}
                                                        </strong>

                                                        {booking.paymentStatus && (
                                                            <span>
                                                                Payment:{' '}
                                                                {
                                                                    booking.paymentStatus
                                                                }
                                                            </span>
                                                        )}
                                                    </div>
                                                </td>

                                                <td className="vb-nowrap">
                                                    {isAccommodation(
                                                        booking
                                                    )
                                                        ? formatDate(
                                                            booking.checkInDate ||
                                                            booking.date
                                                        )
                                                        : formatDate(
                                                            booking.date
                                                        )}
                                                </td>

                                                <td>
                                                    <span className="vb-time-cell">
                                                        {isAccommodation(
                                                            booking
                                                        )
                                                            ? `${booking.numberOfNights ?? 0} night${Number(booking.numberOfNights ?? 0) === 1 ? '' : 's'}`
                                                            : booking.time ||
                                                            '—'}
                                                    </span>
                                                </td>

                                                <td>
                                                    <span className="vb-people">
                                                        {
                                                            booking.peopleCount
                                                        }
                                                    </span>
                                                </td>

                                                <td className="vb-nowrap">
                                                    <strong className="vb-amount">
                                                        {formatMoney(
                                                            booking.totalAmount
                                                        )}
                                                    </strong>
                                                </td>

                                                <td>
                                                    <span
                                                        className={
                                                            getStatusClass(
                                                                booking.status
                                                            )
                                                        }
                                                    >
                                                        {booking.status ||
                                                            'Unknown'}
                                                    </span>
                                                </td>

                                                <td>
                                                    <div className="vb-table-actions">
                                                        <button
                                                            type="button"
                                                            className="vb-view-btn"
                                                            onClick={() =>
                                                                openDetails(
                                                                    booking
                                                                )
                                                            }
                                                        >
                                                            View Details
                                                        </button>

                                                        {canCancel(
                                                            booking
                                                        ) && (
                                                                <button
                                                                    type="button"
                                                                    className="vb-cancel-btn"
                                                                    onClick={() =>
                                                                        openCancelConfirmation(
                                                                            booking
                                                                        )
                                                                    }
                                                                >
                                                                    Cancel
                                                                </button>
                                                            )}
                                                    </div>
                                                </td>
                                            </tr>
                                        )
                                    )}
                                </tbody>
                            </table>

                            {filteredBookings.length === 0 && (
                                <div className="vb-no-results">
                                    <div>⌕</div>

                                    <strong>
                                        No matching bookings
                                    </strong>

                                    <p>
                                        Try another search or filter.
                                    </p>
                                </div>
                            )}
                        </div>
                    </div>
                </>
            )}

            {/* MODAL */}

            {selectedBooking && (
                <div
                    className="vb-modal-overlay"
                    onMouseDown={(event) => {
                        if (
                            event.target ===
                            event.currentTarget
                        ) {
                            closeDetails()
                        }
                    }}
                >
                    <div className="vb-modal">

                        {/* MODAL HEADER */}

                        <div className="vb-modal__header">
                            <div>
                                <span
                                    className={
                                        getBookingTypeClass(
                                            selectedBooking
                                        )
                                    }
                                >
                                    {getBookingTypeLabel(
                                        selectedBooking
                                    )}
                                </span>

                                <h2>
                                    {
                                        selectedBooking.serviceName
                                    }
                                </h2>
                            </div>

                            <button
                                type="button"
                                className="vb-modal__close"
                                onClick={closeDetails}
                                disabled={cancelLoading}
                                aria-label="Close"
                            >
                                ×
                            </button>
                        </div>

                        {/* MODAL BODY */}

                        <div className="vb-modal__body">

                            {/* NORMAL DETAILS */}

                            {!showCancelForm &&
                                !showCancelConfirm && (
                                    <>
                                        <div className="vb-modal-status">
                                            <span>
                                                Status
                                            </span>

                                            <span
                                                className={
                                                    getStatusClass(
                                                        selectedBooking.status
                                                    )
                                                }
                                            >
                                                {
                                                    selectedBooking.status
                                                }
                                            </span>
                                        </div>

                                        {/* ACCOMMODATION DETAILS */}

                                        {isAccommodation(
                                            selectedBooking
                                        ) ? (
                                            <div className="vb-details-section">
                                                <h3>
                                                    Accommodation Information
                                                </h3>

                                                <div className="vb-detail-grid">
                                                    <div className="vb-detail-item">
                                                        <span>
                                                            Check-in
                                                        </span>

                                                        <strong>
                                                            {formatDate(
                                                                selectedBooking.checkInDate
                                                            )}
                                                        </strong>
                                                    </div>

                                                    <div className="vb-detail-item">
                                                        <span>
                                                            Check-out
                                                        </span>

                                                        <strong>
                                                            {formatDate(
                                                                selectedBooking.checkOutDate
                                                            )}
                                                        </strong>
                                                    </div>

                                                    <div className="vb-detail-item">
                                                        <span>
                                                            Number of Nights
                                                        </span>

                                                        <strong>
                                                            {
                                                                selectedBooking.numberOfNights ??
                                                                '—'
                                                            }
                                                        </strong>
                                                    </div>

                                                    <div className="vb-detail-item">
                                                        <span>
                                                            Guests
                                                        </span>

                                                        <strong>
                                                            {
                                                                selectedBooking.peopleCount
                                                            }
                                                        </strong>
                                                    </div>

                                                    <div className="vb-detail-item">
                                                        <span>
                                                            Price Per Night
                                                        </span>

                                                        <strong>
                                                            {formatMoney(
                                                                selectedBooking.unitPrice
                                                            )}
                                                        </strong>
                                                    </div>

                                                    <div className="vb-detail-item">
                                                        <span>
                                                            Total Amount
                                                        </span>

                                                        <strong className="vb-detail-price">
                                                            {formatMoney(
                                                                selectedBooking.totalAmount
                                                            )}
                                                        </strong>
                                                    </div>
                                                </div>
                                            </div>
                                        ) : (
                                            /* EXPERIENCE / RESTAURANT DETAILS */

                                            <div className="vb-details-section">
                                                <h3>
                                                    Booking Information
                                                </h3>

                                                <div className="vb-detail-grid">
                                                    <div className="vb-detail-item">
                                                        <span>
                                                            Date
                                                        </span>

                                                        <strong>
                                                            {formatDate(
                                                                selectedBooking.date
                                                            )}
                                                        </strong>
                                                    </div>

                                                    <div className="vb-detail-item">
                                                        <span>
                                                            Time
                                                        </span>

                                                        <strong>
                                                            {selectedBooking.time ||
                                                                '—'}
                                                        </strong>
                                                    </div>

                                                    <div className="vb-detail-item">
                                                        <span>
                                                            {getPeopleLabel(
                                                                selectedBooking
                                                            )}
                                                        </span>

                                                        <strong>
                                                            {
                                                                selectedBooking.peopleCount
                                                            }
                                                        </strong>
                                                    </div>

                                                    <div className="vb-detail-item">
                                                        <span>
                                                            Total Amount
                                                        </span>

                                                        <strong className="vb-detail-price">
                                                            {formatMoney(
                                                                selectedBooking.totalAmount
                                                            )}
                                                        </strong>
                                                    </div>

                                                    {selectedBooking.unitPrice != null && (
                                                        <div className="vb-detail-item">
                                                            <span>
                                                                Unit Price
                                                            </span>

                                                            <strong>
                                                                {formatMoney(
                                                                    selectedBooking.unitPrice
                                                                )}
                                                            </strong>
                                                        </div>
                                                    )}

                                                    {selectedBooking.paymentStatus && (
                                                        <div className="vb-detail-item">
                                                            <span>
                                                                Payment Status
                                                            </span>

                                                            <strong>
                                                                {
                                                                    selectedBooking.paymentStatus
                                                                }
                                                            </strong>
                                                        </div>
                                                    )}
                                                </div>
                                            </div>
                                        )}

                                        {/* CANCELLED DETAILS */}

                                        {isCancelled(
                                            selectedBooking
                                        ) && (
                                                <div className="vb-details-section vb-cancelled-section">
                                                    <h3>
                                                        Cancellation & Refund
                                                    </h3>

                                                    <div className="vb-detail-grid">
                                                        <div className="vb-detail-item vb-detail-item--full">
                                                            <span>
                                                                Cancellation Reason
                                                            </span>

                                                            <strong>
                                                                {selectedBooking.cancellationReason ||
                                                                    'No reason provided'}
                                                            </strong>
                                                        </div>

                                                        {selectedBooking.cancelledAt && (
                                                            <div className="vb-detail-item">
                                                                <span>
                                                                    Cancelled At
                                                                </span>

                                                                <strong>
                                                                    {formatDateTime(
                                                                        selectedBooking.cancelledAt
                                                                    )}
                                                                </strong>
                                                            </div>
                                                        )}

                                                        <div className="vb-detail-item">
                                                            <span>
                                                                Refund
                                                            </span>

                                                            <strong>
                                                                {Number(
                                                                    selectedBooking.refundPercentage ??
                                                                    0
                                                                )}
                                                                %
                                                            </strong>
                                                        </div>

                                                        <div className="vb-detail-item">
                                                            <span>
                                                                Refund Amount
                                                            </span>

                                                            <strong className="vb-refund-value">
                                                                {formatMoney(
                                                                    selectedBooking.refundAmount
                                                                )}
                                                            </strong>
                                                        </div>

                                                        {selectedBooking.refundedAt && (
                                                            <div className="vb-detail-item">
                                                                <span>
                                                                    Refunded At
                                                                </span>

                                                                <strong>
                                                                    {formatDateTime(
                                                                        selectedBooking.refundedAt
                                                                    )}
                                                                </strong>
                                                            </div>
                                                        )}
                                                    </div>
                                                </div>
                                            )}
                                    </>
                                )}

                            {/* ARE YOU SURE? */}

                            {showCancelConfirm && (
                                <div className="vb-cancel-confirmation">
                                    <div className="vb-cancel-confirmation__icon">
                                        !
                                    </div>

                                    <h3>
                                        Are you sure you want to cancel this{' '}
                                        {isRestaurant(
                                            selectedBooking
                                        )
                                            ? 'reservation'
                                            : 'booking'}
                                        ?
                                    </h3>

                                    <p className="vb-cancel-confirmation__text">
                                        You are about to cancel your{' '}
                                        {getCancellationTypeName(
                                            selectedBooking
                                        )}{' '}
                                        for{' '}

                                        <strong>
                                            {
                                                selectedBooking.serviceName
                                            }
                                        </strong>
                                        .
                                    </p>

                                    <div className="vb-confirm-booking-info">
                                        <div>
                                            <span>
                                                Service
                                            </span>

                                            <strong>
                                                {
                                                    selectedBooking.serviceName
                                                }
                                            </strong>
                                        </div>

                                        <div>
                                            <span>
                                                {isAccommodation(
                                                    selectedBooking
                                                )
                                                    ? 'Check-in'
                                                    : 'Date'}
                                            </span>

                                            <strong>
                                                {formatDate(
                                                    isAccommodation(
                                                        selectedBooking
                                                    )
                                                        ? selectedBooking.checkInDate
                                                        : selectedBooking.date
                                                )}
                                            </strong>
                                        </div>

                                        <div>
                                            <span>
                                                Amount
                                            </span>

                                            <strong>
                                                {formatMoney(
                                                    selectedBooking.totalAmount
                                                )}
                                            </strong>
                                        </div>
                                    </div>

                                    <div className="vb-confirm-warning">
                                        <strong>
                                            Important
                                        </strong>

                                        <p>
                                            Your booking will not be
                                            cancelled yet. If you
                                            continue, you can review
                                            the refund policy and
                                            enter a cancellation
                                            reason before the final
                                            confirmation.
                                        </p>
                                    </div>
                                </div>
                            )}

                            {/* CANCELLATION FORM */}

                            {showCancelForm && (
                                <div className="vb-cancel-form">
                                    <h3>
                                        Cancel{' '}
                                        {getShortCancellationTypeName(
                                            selectedBooking
                                        )}
                                    </h3>

                                    <p className="vb-cancel-intro">
                                        Please review the cancellation
                                        policy before confirming.
                                    </p>

                                    <div className="vb-cancel-summary">
                                        <div>
                                            <span>
                                                Service
                                            </span>

                                            <strong>
                                                {
                                                    selectedBooking.serviceName
                                                }
                                            </strong>
                                        </div>

                                        <div>
                                            <span>
                                                {isAccommodation(
                                                    selectedBooking
                                                )
                                                    ? 'Check-in'
                                                    : 'Date'}
                                            </span>

                                            <strong>
                                                {formatDate(
                                                    isAccommodation(
                                                        selectedBooking
                                                    )
                                                        ? selectedBooking.checkInDate
                                                        : selectedBooking.date
                                                )}
                                            </strong>
                                        </div>

                                        <div>
                                            <span>
                                                Amount
                                            </span>

                                            <strong>
                                                {formatMoney(
                                                    selectedBooking.totalAmount
                                                )}
                                            </strong>
                                        </div>
                                    </div>

                                    <div className="vb-warning">
                                        <strong>
                                            Cancellation Policy
                                        </strong>

                                        <p>
                                            48 hours or more before:
                                            100% refund.
                                        </p>

                                        <p>
                                            Between 24 and 48 hours:
                                            50% refund.
                                        </p>

                                        <p>
                                            Less than 24 hours:
                                            cancellation is not allowed.
                                        </p>
                                    </div>

                                    <label
                                        className="vb-reason-label"
                                        htmlFor="cancellationReason"
                                    >
                                        Reason for cancellation

                                        <span>
                                            {' '}(optional)
                                        </span>
                                    </label>

                                    <textarea
                                        id="cancellationReason"
                                        className="vb-reason-input"
                                        value={
                                            cancellationReason
                                        }
                                        onChange={(event) =>
                                            setCancellationReason(
                                                event.target.value
                                            )
                                        }
                                        placeholder="Tell us why you are cancelling..."
                                        maxLength={500}
                                        rows={4}
                                        disabled={cancelLoading}
                                    />

                                    <div className="vb-character-count">
                                        {cancellationReason.length}/500
                                    </div>

                                    {cancelError && (
                                        <div className="vb-cancel-error">
                                            {cancelError}
                                        </div>
                                    )}
                                </div>
                            )}
                        </div>

                        {/* MODAL FOOTER */}

                        <div className="vb-modal__footer">

                            {/* NORMAL DETAILS */}

                            {!showCancelForm &&
                                !showCancelConfirm && (
                                    <>
                                        <button
                                            type="button"
                                            className="vb-close-btn"
                                            onClick={closeDetails}
                                        >
                                            Close
                                        </button>

                                        {canCancel(
                                            selectedBooking
                                        ) && (
                                                <button
                                                    type="button"
                                                    className="vb-cancel-btn"
                                                    onClick={() =>
                                                        openCancelConfirmation(
                                                            selectedBooking
                                                        )
                                                    }
                                                >
                                                    {isRestaurant(
                                                        selectedBooking
                                                    )
                                                        ? 'Cancel Reservation'
                                                        : isAccommodation(
                                                            selectedBooking
                                                        )
                                                            ? 'Cancel Accommodation'
                                                            : 'Cancel Booking'}
                                                </button>
                                            )}
                                    </>
                                )}

                            {/* CONFIRMATION */}

                            {showCancelConfirm && (
                                <>
                                    <button
                                        type="button"
                                        className="vb-close-btn"
                                        onClick={keepBooking}
                                    >
                                        No, Keep{' '}
                                        {getShortCancellationTypeName(
                                            selectedBooking
                                        )}
                                    </button>

                                    <button
                                        type="button"
                                        className="vb-confirm-cancel-btn"
                                        onClick={
                                            continueToCancellation
                                        }
                                    >
                                        Yes, Continue to Cancel
                                    </button>
                                </>
                            )}

                            {/* FINAL CANCEL */}

                            {showCancelForm && (
                                <>
                                    <button
                                        type="button"
                                        className="vb-close-btn"
                                        onClick={
                                            backToCancelConfirmation
                                        }
                                        disabled={cancelLoading}
                                    >
                                        Back
                                    </button>

                                    <button
                                        type="button"
                                        className="vb-confirm-cancel-btn"
                                        onClick={
                                            handleCancelBooking
                                        }
                                        disabled={cancelLoading}
                                    >
                                        {cancelLoading
                                            ? 'Cancelling...'
                                            : `Confirm ${getShortCancellationTypeName(
                                                selectedBooking
                                            )} Cancellation`}
                                    </button>
                                </>
                            )}
                        </div>
                    </div>
                </div>
            )}
        </div>
    )
}