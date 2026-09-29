import { useCallback, useEffect, useMemo, useState } from 'react'
import { bookingUrl } from '../../../api/client'
import { formatSriLankaTime, canRetryPaymentAt, refundPreview } from '../../../api/bookingPayment'
import ConfirmModal from '../../../components/ConfirmModal'
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
    const [deleteTarget, setDeleteTarget] = useState(null)
    const [deletingBookingId, setDeletingBookingId] = useState(null)
    const [deleteError, setDeleteError] = useState('')
    const [retryingBookingId, setRetryingBookingId] = useState(null)
    const [paymentError, setPaymentError] = useState('')
    const [now, setNow] = useState(0)

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

    useEffect(() => {
        const initial = setTimeout(() => setNow(Date.now()), 0)
        const timer = setInterval(() => setNow(Date.now()), 30000)
        return () => {
            clearTimeout(initial)
            clearInterval(timer)
        }
    }, [])

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

    const formatDateTime = formatSriLankaTime

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
    // PAYMENT HELPERS
    // =========================================================

    const getPaymentStatusClass = (paymentStatus) => {
        const s = String(paymentStatus || '').toLowerCase()
        if (s === 'paid') return 'vb-pay-status vb-pay-status--paid'
        if (s === 'failed') return 'vb-pay-status vb-pay-status--failed'
        if (s === 'refunded') return 'vb-pay-status vb-pay-status--refunded'
        if (s === 'unpaid') return 'vb-pay-status vb-pay-status--unpaid'
        return 'vb-pay-status vb-pay-status--unpaid'
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

    const canDelete = (booking) => {
        return isCancelled(booking) || isCompleted(booking)
    }

    const canRetryPayment = (booking) => canRetryPaymentAt(booking, now)

    const retryPayment = async (booking) => {
        const token = localStorage.getItem('authToken')
        if (!token) { onSessionExpired?.(); return }
        setRetryingBookingId(booking.id)
        setPaymentError('')
        const bookingType = isRestaurant(booking) ? 'Restaurant' : isAccommodation(booking) ? 'Accommodation' : 'Experience'
        try {
            const response = await fetch(bookingUrl('/api/Payments/create-checkout-session'), {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
                body: JSON.stringify({ bookingId: booking.id, bookingType })
            })
            const data = await response.json().catch(() => null)
            if (!response.ok || !data?.checkoutUrl) throw new Error(data?.message || 'Unable to retry payment.')
            sessionStorage.setItem('pendingPayment', JSON.stringify({ bookingId: booking.id, bookingType, transactionId: data.transactionId, checkoutSessionId: data.checkoutSessionId }))
            window.location.assign(data.checkoutUrl)
        } catch (error) {
            setPaymentError(error.message || 'Unable to retry payment.')
            setRetryingBookingId(null)
        }
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

            if (statusFilter === 'confirmed') {
                matchesStatus =
                    String(booking.status || '').toLowerCase() === 'confirmed'
            }

            if (statusFilter === 'cancelled') {
                matchesStatus = isCancelled(booking)
            }

            if (statusFilter === 'pending-payment') {
                matchesStatus = String(booking.status || '').toLowerCase() === 'pendingpayment'
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

            if (refundAmount > 0) {
                setSuccessMessage(
                    `${typeName} cancelled successfully. ` +
                    `Refund: ${refundPercentage}% ` +
                    `(${formatMoney(refundAmount)}).`
                )
            } else {
                setSuccessMessage(
                    `${typeName} cancelled successfully. No refund was applicable.`
                )
            }

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
    // DELETE EXPERIENCE BOOKING FROM HISTORY
    // =========================================================

    const openDeleteConfirmation = (booking) => {
        if (!canDelete(booking) || deletingBookingId) return

        setDeleteError('')
        setSuccessMessage('')
        setDeleteTarget(booking)
    }

    const closeDeleteConfirmation = () => {
        if (deletingBookingId) return
        setDeleteTarget(null)
    }

    const handleDeleteBooking = async () => {
        if (!deleteTarget || deletingBookingId) return

        const token = localStorage.getItem('authToken')

        if (!token) {
            onSessionExpired?.()
            return
        }

        const bookingId = deleteTarget.id
        const endpoint = isRestaurant(deleteTarget)
            ? `/api/Reservations/${bookingId}`
            : isAccommodation(deleteTarget)
                ? `/api/AccommodationBookings/${bookingId}`
                : `/api/bookings/${bookingId}`
        setDeletingBookingId(bookingId)
        setDeleteError('')

        try {
            const response = await fetch(
                bookingUrl(endpoint),
                {
                    method: 'DELETE',
                    headers: {
                        Authorization: `Bearer ${token}`
                    }
                }
            )

            if (response.status === 401) {
                onSessionExpired?.()
                return
            }

            if (response.status === 204) {
                setBookings((current) =>
                    current.filter((booking) =>
                        !(
                            booking.bookingType === deleteTarget.bookingType &&
                            booking.id === bookingId
                        )
                    )
                )
                setDeleteTarget(null)
                setSuccessMessage('Booking deleted from history.')
                return
            }

            const data = await response.json().catch(() => null)
            setDeleteTarget(null)
            setDeleteError(
                response.status === 404
                    ? 'This booking could not be found. It may already have been removed.'
                    : data?.message ||
                    'Unable to delete this booking from your history.'
            )
        } catch {
            setDeleteTarget(null)
            setDeleteError(
                'Network error. Please check your connection and try again.'
            )
        } finally {
            setDeletingBookingId(null)
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
                        <h1>My Bookings</h1>

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
                        <h1>My Bookings</h1>

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
                    <h1>My Bookings</h1>

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
                <div
                    className="vb-success-modal-overlay"
                    onMouseDown={(event) => {
                        if (event.target === event.currentTarget) {
                            setSuccessMessage('')
                        }
                    }}
                >
                    <div
                        className="vb-success-modal"
                        role="dialog"
                        aria-modal="true"
                        aria-labelledby="vb-success-title"
                    >
                        <button
                            type="button"
                            className="vb-success-modal__close"
                            onClick={() => setSuccessMessage('')}
                            aria-label="Close"
                        >
                            ×
                        </button>

                        <div className="vb-success-modal__icon">
                            ✓
                        </div>

                        <h3 id="vb-success-title">
                            Cancellation Successful
                        </h3>

                        <p>{successMessage}</p>

                        <button
                            type="button"
                            className="vb-success-modal__button"
                            onClick={() => setSuccessMessage('')}
                        >
                            Done
                        </button>
                    </div>
                </div>
            )}

            {deleteError && (
                <div className="vb-error-message" role="alert">
                    <span>{deleteError}</span>
                    <button
                        type="button"
                        onClick={() => setDeleteError('')}
                        aria-label="Dismiss delete error"
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
                                    statusFilter === 'confirmed'
                                        ? 'vb-filter-btn vb-filter-btn--active'
                                        : 'vb-filter-btn'
                                }
                                onClick={() =>
                                    setStatusFilter('confirmed')
                                }
                            >
                                Confirmed
                            </button>

                            <button
                                type="button"
                                className={statusFilter === 'pending-payment' ? 'vb-filter-btn vb-filter-btn--active' : 'vb-filter-btn'}
                                onClick={() => setStatusFilter('pending-payment')}
                            >
                                Pending Payment
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
                                                            <span className={getPaymentStatusClass(booking.paymentStatus)}>
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

                                                        {canRetryPayment(booking) && (
                                                            <button type="button" className="vb-payment-retry-btn" disabled={Boolean(retryingBookingId)} onClick={() => retryPayment(booking)}>Retry Payment</button>
                                                        )}
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

                                                        {canDelete(booking) && (
                                                            <button
                                                                type="button"
                                                                className="vb-delete-btn"
                                                                disabled={
                                                                    deletingBookingId === booking.id
                                                                }
                                                                onClick={() =>
                                                                    openDeleteConfirmation(booking)
                                                                }
                                                            >
                                                                {deletingBookingId === booking.id
                                                                    ? 'Deleting...'
                                                                    : 'Delete'}
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

                                                    <div className="vb-detail-item">
                                                        <span>Created At</span>
                                                        <strong>{formatDateTime(selectedBooking.createdAt)}</strong>
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

                                                    <div className="vb-detail-item">
                                                        <span>Created At</span>
                                                        <strong>{formatDateTime(selectedBooking.createdAt)}</strong>
                                                    </div>
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
                                        {isRestaurant(selectedBooking)
                                            ? 'reservation'
                                            : 'booking'}
                                        ?
                                    </h3>

                                    <p className="vb-cancel-confirmation__text">
                                        You are about to cancel your{' '}
                                        {getCancellationTypeName(selectedBooking)}{' '}
                                        for{' '}
                                        <strong>
                                            {selectedBooking.serviceName}
                                        </strong>
                                        .
                                    </p>

                                    <div className="vb-confirm-booking-info">
                                        <div>
                                            <span>Service</span>
                                            <strong>
                                                {selectedBooking.serviceName}
                                            </strong>
                                        </div>

                                        <div>
                                            <span>
                                                {isAccommodation(selectedBooking)
                                                    ? 'Check-in'
                                                    : 'Date'}
                                            </span>
                                            <strong>
                                                {formatDate(
                                                    isAccommodation(selectedBooking)
                                                        ? selectedBooking.checkInDate
                                                        : selectedBooking.date
                                                )}
                                            </strong>
                                        </div>

                                        <div>
                                            <span>Amount</span>
                                            <strong>
                                                {formatMoney(
                                                    selectedBooking.totalAmount
                                                )}
                                            </strong>
                                        </div>
                                    </div>

                                    <div className="vb-confirm-warning">
                                        <strong>Important</strong>

                                        <p>
                                            Your booking will not be cancelled yet.
                                            If you continue, you can review the
                                            cancellation details and enter a
                                            cancellation reason before the final
                                            confirmation.
                                        </p>

                                        <p>
                                            {refundPreview(
                                                selectedBooking,
                                                now
                                            ).message}
                                        </p>

                                        <p className="vb-confirm-warning__refund">
                                            Refund Amount:{' '}
                                            {formatMoney(
                                                refundPreview(
                                                    selectedBooking,
                                                    now
                                                ).amount
                                            )}
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
                                        Please review the cancellation policy
                                        before confirming.
                                    </p>

                                    <div className="vb-cancel-summary">
                                        <div>
                                            <span>Service</span>
                                            <strong>
                                                {selectedBooking.serviceName}
                                            </strong>
                                        </div>

                                        <div>
                                            <span>
                                                {isAccommodation(selectedBooking)
                                                    ? 'Check-in'
                                                    : 'Date'}
                                            </span>
                                            <strong>
                                                {formatDate(
                                                    isAccommodation(selectedBooking)
                                                        ? selectedBooking.checkInDate
                                                        : selectedBooking.date
                                                )}
                                            </strong>
                                        </div>

                                        <div>
                                            <span>Amount</span>
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
                                            48 hours or more before: 100% refund.
                                        </p>

                                        <p>
                                            Between 24 and 48 hours: 50% refund.
                                        </p>

                                        <p>
                                            Less than 24 hours: cancellation is
                                            allowed with no refund.
                                        </p>
                                    </div>

                                    <label
                                        className="vb-reason-label"
                                        htmlFor="cancellationReason"
                                    >
                                        Reason for cancellation
                                        <span>{' '}(optional)</span>
                                    </label>

                                    <textarea
                                        id="cancellationReason"
                                        className="vb-reason-input"
                                        value={cancellationReason}
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

                                        {canRetryPayment(selectedBooking) && (
                                            <button
                                                type="button"
                                                className="vb-payment-retry-btn"
                                                disabled={retryingBookingId === selectedBooking.id}
                                                onClick={() => retryPayment(selectedBooking)}
                                            >
                                                {retryingBookingId === selectedBooking.id ? 'Starting Payment...' : 'Retry Payment'}
                                            </button>
                                        )}

                                        {paymentError && <div className="vb-cancel-error">{paymentError}</div>}

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

            <ConfirmModal
                isOpen={Boolean(deleteTarget)}
                title="Delete this booking from your history?"
                message="This will remove the booking from your booking history. This action will not cancel an active booking."
                confirmText="Delete"
                cancelText="Cancel"
                confirmVariant="danger"
                onConfirm={handleDeleteBooking}
                onCancel={closeDeleteConfirmation}
                loading={Boolean(deletingBookingId)}
            />
        </div>
    )
}
