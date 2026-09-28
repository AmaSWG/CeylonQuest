import { useEffect, useRef, useState } from 'react'
import { bookingUrl } from '../../../api/client'
import './PaymentSuccessPage.css'

export default function PaymentCancelPage({ onGoToBookings, onSessionExpired }) {
    const [phase, setPhase] = useState('processing')
    const [cancelResult, setCancelResult] = useState(null)
    const [errorMessage, setErrorMessage] = useState('')
    const hasRun = useRef(false)

    useEffect(() => {
        if (hasRun.current) return
        hasRun.current = true
        processCancellation()
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [])

    async function processCancellation() {
        const token = localStorage.getItem('authToken')
        if (!token) { onSessionExpired?.(); return }

        // Read transactionId from URL query params (set by backend cancel URL)
        const params = new URLSearchParams(window.location.search)
        const transactionId = params.get('transactionId')

        if (!transactionId) {
            setPhase('error')
            setErrorMessage('Payment cancellation details are missing. Please check My Bookings.')
            return
        }

        try {
            const response = await fetch(
                bookingUrl('/api/Payments/cancel-checkout/' + transactionId),
                {
                    method: 'POST',
                    headers: {
                        Authorization: 'Bearer ' + token,
                        'Content-Type': 'application/json',
                    },
                }
            )

            if (response.status === 401) { onSessionExpired?.(); return }

            const data = await response.json().catch(() => null)

            if (response.ok) {
                // Clear pending payment after cancellation
                try { sessionStorage.removeItem('pendingPayment') } catch { /* ignore */ }
                setCancelResult(data)
                setPhase('cancelled')
            } else {
                // Conflict means already cancelled/paid — still show cancelled message
                if (response.status === 409) {
                    try { sessionStorage.removeItem('pendingPayment') } catch { /* ignore */ }
                    setCancelResult(data)
                    setPhase('cancelled')
                } else {
                    setPhase('error')
                    setErrorMessage(
                        data?.message ||
                        'Failed to record payment cancellation. Please check My Bookings.'
                    )
                }
            }
        } catch {
            setPhase('error')
            setErrorMessage('Network error. Please check your connection and view My Bookings to verify status.')
        }
    }

    if (phase === 'processing') {
        return (
            <div className="psp-page">
                <div className="psp-card psp-card--verifying">
                    <div className="psp-spinner" />
                    <h1>Cancelling payment…</h1>
                    <p>Please wait while we update your booking.</p>
                </div>
            </div>
        )
    }

    if (phase === 'error') {
        return (
            <div className="psp-page">
                <div className="psp-card psp-card--error">
                    <div className="psp-icon psp-icon--error">⚠️</div>
                    <h1>Cancellation Error</h1>
                    <p>{errorMessage}</p>
                    <button type="button" className="psp-btn psp-btn--primary" onClick={onGoToBookings}>
                        Go to My Bookings
                    </button>
                </div>
            </div>
        )
    }

    return (
        <div className="psp-page">
            <div className="psp-card" style={{ borderTop: '4px solid #f59e0b' }}>
                <div style={{
                    width: 72, height: 72, borderRadius: '50%',
                    background: 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)',
                    color: '#fff', fontSize: 36, display: 'flex', alignItems: 'center',
                    justifyContent: 'center', margin: '0 auto 24px',
                    boxShadow: '0 4px 18px rgba(245,158,11,0.4)',
                }}>
                    ✕
                </div>

                <h1 style={{ fontSize: 28, fontWeight: 800, color: '#92400e', margin: '0 0 12px' }}>
                    Payment Cancelled
                </h1>

                <p style={{ color: '#374151', fontSize: 15, margin: '0 0 28px', lineHeight: 1.6 }}>
                    Your payment was not completed. Your booking is still pending payment.
                </p>

                {cancelResult && (
                    <div className="psp-details">
                        {cancelResult.paymentStatus && (
                            <div className="psp-detail-row">
                                <span>Payment Status</span>
                                <span className="psp-badge psp-badge--failed">
                                    {cancelResult.paymentStatus}
                                </span>
                            </div>
                        )}
                        {cancelResult.bookingStatus && (
                            <div className="psp-detail-row">
                                <span>Booking Status</span>
                                <span className="psp-badge psp-badge--unpaid">
                                    {cancelResult.bookingStatus}
                                </span>
                            </div>
                        )}
                    </div>
                )}

                <p style={{ color: '#6b7280', fontSize: 14, margin: '0 0 28px' }}>
                    You can try again from My Bookings using the <strong>Pay Now</strong> button.
                </p>

                <div style={{ display: 'flex', gap: 12, justifyContent: 'center', flexWrap: 'wrap' }}>
                    <button type="button" className="psp-btn psp-btn--primary" onClick={onGoToBookings}>
                        ← Back to My Bookings
                    </button>
                </div>
            </div>
        </div>
    )
}
