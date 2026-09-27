import { useEffect, useRef, useState } from 'react'
import { bookingUrl } from '../../../api/client'
import './PaymentSuccessPage.css'

function toApiBookingType(label) {
    const map = {
        'Experience Booking': 'Experience',
        'Accommodation Booking': 'Accommodation',
        'Restaurant Reservation': 'Restaurant',
        Experience: 'Experience',
        Accommodation: 'Accommodation',
        Restaurant: 'Restaurant',
    }
    return map[label] || label
}

function formatMoney(amount, currency) {
    const value = Number(amount ?? 0)
    const cur = String(currency || 'LKR').toUpperCase()
    return cur + ' ' + value.toLocaleString()
}

function shortId(id) {
    if (!id) return '—'
    return String(id).split('-')[0].toUpperCase()
}

const MAX_RETRIES = 5
const RETRY_DELAY_MS = 2500

export default function PaymentSuccessPage({ onGoToBookings, onSessionExpired }) {
    const [phase, setPhase] = useState('verifying')
    const [paymentDetails, setPaymentDetails] = useState(null)
    const [errorMessage, setErrorMessage] = useState('')
    const hasStarted = useRef(false)

    useEffect(() => {
        if (hasStarted.current) return
        hasStarted.current = true
        verifyPayment()
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [])

    function sleep(ms) {
        return new Promise((resolve) => setTimeout(resolve, ms))
    }

    async function verifyPayment() {
        const token = localStorage.getItem('authToken')
        if (!token) { onSessionExpired?.(); return }

        let pendingRaw = null
        try { pendingRaw = sessionStorage.getItem('pendingPayment') } catch { /* ignore */ }

        if (!pendingRaw) {
            setPhase('error')
            setErrorMessage('Payment context not found. Please check My Bookings to verify your payment status.')
            return
        }

        let pending
        try { pending = JSON.parse(pendingRaw) } catch {
            setPhase('error')
            setErrorMessage('Payment context is invalid. Please check My Bookings.')
            return
        }

        const { bookingId, bookingType } = pending
        if (!bookingId || !bookingType) {
            setPhase('error')
            setErrorMessage('Missing payment information. Please check My Bookings.')
            return
        }

        const apiType = toApiBookingType(bookingType)

        for (let attempt = 1; attempt <= MAX_RETRIES; attempt++) {
            try {
                const response = await fetch(
                    bookingUrl('/api/Payments/booking/' + bookingId + '?bookingType=' + apiType),
                    { headers: { Authorization: 'Bearer ' + token } }
                )
                if (response.status === 401) { onSessionExpired?.(); return }
                if (response.ok) {
                    const data = await response.json()
                    const payStatus = String(data.paymentStatus || '').toLowerCase()
                    const bookStatus = String(data.bookingStatus || '').toLowerCase()
                    if (payStatus === 'paid' || bookStatus === 'confirmed') {
                        try { sessionStorage.removeItem('pendingPayment') } catch { /* ignore */ }
                        setPaymentDetails(data)
                        setPhase('success')
                        return
                    }
                }
                if (attempt < MAX_RETRIES) await sleep(RETRY_DELAY_MS)
            } catch {
                if (attempt < MAX_RETRIES) await sleep(RETRY_DELAY_MS)
            }
        }
        setPhase('pending')
    }

    if (phase === 'verifying') {
        return (
            <div className="psp-page">
                <div className="psp-card psp-card--verifying">
                    <div className="psp-spinner" />
                    <h1>Confirming your payment…</h1>
                    <p>Please wait while we verify your payment with our payment processor.</p>
                    <p className="psp-hint">This may take a few moments.</p>
                </div>
            </div>
        )
    }

    if (phase === 'error') {
        return (
            <div className="psp-page">
                <div className="psp-card psp-card--error">
                    <div className="psp-icon psp-icon--error">⚠️</div>
                    <h1>Payment Verification Error</h1>
                    <p>{errorMessage}</p>
                    <button type="button" className="psp-btn psp-btn--primary" onClick={onGoToBookings}>
                        Go to My Bookings
                    </button>
                </div>
            </div>
        )
    }

    if (phase === 'pending') {
        return (
            <div className="psp-page">
                <div className="psp-card psp-card--pending">
                    <div className="psp-icon psp-icon--pending">⏳</div>
                    <h1>Payment Received</h1>
                    <p>We are still verifying your payment. This usually completes within a few seconds.</p>
                    <p><strong>Please check My Bookings shortly to confirm your booking status.</strong></p>
                    <button type="button" className="psp-btn psp-btn--primary" onClick={onGoToBookings}>
                        Go to My Bookings
                    </button>
                </div>
            </div>
        )
    }

    return (
        <div className="psp-page">
            <div className="psp-card psp-card--success">
                <div className="psp-success-icon">✓</div>
                <h1 className="psp-success-title">Payment Successful</h1>
                <p className="psp-success-subtitle">
                    Your payment has been completed successfully. Your booking is now confirmed.
                </p>
                {paymentDetails && (
                    <div className="psp-details">
                        {paymentDetails.listingTitle && (
                            <div className="psp-detail-row">
                                <span>Service</span>
                                <strong>{paymentDetails.listingTitle}</strong>
                            </div>
                        )}
                        {paymentDetails.bookingType && (
                            <div className="psp-detail-row">
                                <span>Booking Type</span>
                                <strong>{paymentDetails.bookingType}</strong>
                            </div>
                        )}
                        <div className="psp-detail-row">
                            <span>Amount</span>
                            <strong className="psp-amount">
                                {formatMoney(paymentDetails.totalAmount, paymentDetails.currency)}
                            </strong>
                        </div>
                        <div className="psp-detail-row">
                            <span>Booking Status</span>
                            <span className="psp-badge psp-badge--confirmed">{paymentDetails.bookingStatus}</span>
                        </div>
                        <div className="psp-detail-row">
                            <span>Payment Status</span>
                            <span className="psp-badge psp-badge--paid">{paymentDetails.paymentStatus}</span>
                        </div>
                        {paymentDetails.paymentReference && (
                            <div className="psp-detail-row">
                                <span>Transaction Reference</span>
                                <strong className="psp-ref">{paymentDetails.paymentReference}</strong>
                            </div>
                        )}
                        {paymentDetails.bookingId && (
                            <div className="psp-detail-row">
                                <span>Booking ID</span>
                                <strong className="psp-ref">#{shortId(paymentDetails.bookingId)}</strong>
                            </div>
                        )}
                    </div>
                )}
                <button type="button" className="psp-btn psp-btn--primary" onClick={onGoToBookings}>
                    ← Back to My Bookings
                </button>
            </div>
        </div>
    )
}
