export function utcTimestamp(value) {
    if (!value) return NaN
    const text = String(value)
    return Date.parse(/(Z|[+-]\d{2}:\d{2})$/i.test(text) ? text : text + 'Z')
}

export function formatSriLankaTime(value) {
    const timestamp = utcTimestamp(value)
    if (!Number.isFinite(timestamp)) return '-'
    const date = new Date(timestamp)
    const timeZone = 'Asia/Colombo'
    return new Intl.DateTimeFormat('en-US', { timeZone, year: 'numeric', month: 'short', day: 'numeric' }).format(date)
        + ' | ' + new Intl.DateTimeFormat('en-US', { timeZone, hour: 'numeric', minute: '2-digit' }).format(date)
}

export function canRetryPaymentAt(booking, now) {
    const status = booking?.status ?? booking?.bookingStatus
    const expires = booking?.paymentExpiresAt
        ? utcTimestamp(booking.paymentExpiresAt)
        : utcTimestamp(booking?.createdAt) + 15 * 60000
    return now > 0 && status === 'PendingPayment' &&
        ['Unpaid', 'Failed'].includes(booking?.paymentStatus) && expires > now
}

export function refundPreview(booking, now) {
    const accommodation = String(booking?.bookingType).startsWith('Accommodation')
    const date = accommodation ? booking?.checkInDate : booking?.date
    const match = String(booking?.time || '').match(/^(\d{1,2}):(\d{2})\s*(AM|PM)?/i)
    let hour = accommodation ? 0 : Number(match?.[1])
    const minute = accommodation ? 0 : Number(match?.[2])
    if (match?.[3]) hour = hour % 12 + (match[3].toUpperCase() === 'PM' ? 12 : 0)
    const scheduled = Date.parse(date + 'T' + String(hour).padStart(2, '0') + ':' + String(minute).padStart(2, '0') + ':00+05:30')
    const hours = (scheduled - now) / 3600000
    const percentage = hours >= 48 ? 100 : hours >= 24 ? 50 : 0
    return {
        amount: booking?.paymentStatus === 'Paid' ? Number(booking.totalAmount) * percentage / 100 : 0,
        message: hours < 24
            ? 'This booking is within 24 hours of the scheduled time. You can cancel it, but no refund will be provided.'
            : 'Refund policy: 100% at least 48 hours before the booking; 50% from 24 to 48 hours. Only paid amounts are refundable.'
    }
}
