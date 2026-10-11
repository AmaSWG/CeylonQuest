import { bookingUrl } from './client'

export function normalizeBookingType(value) {
  const types = {
    Experience: 'Experience',
    Restaurant: 'Restaurant',
    Accommodation: 'Accommodation',
    'Experience Booking': 'Experience',
    'Restaurant Reservation': 'Restaurant',
    'Accommodation Booking': 'Accommodation'
  }

  const type = types[value]
  if (!type) throw new Error('Unsupported booking type.')
  return type
}

async function request(path, { auth = false, body, signal, method } = {}) {
  const headers = {}
  const token = localStorage.getItem('authToken')

  if (auth) {
    if (!token) throw new Error('Sign in to continue.')
    headers.Authorization = `Bearer ${token}`
  }

  if (body) headers['Content-Type'] = 'application/json'

  const response = await fetch(bookingUrl(path), {
    method: method || (body ? 'POST' : 'GET'),
    headers,
    body: body ? JSON.stringify(body) : undefined,
    signal
  })

  const data = await response.json().catch(() => null)

  if (!response.ok) {
    const validation = data?.errors
      ? Object.values(data.errors).flat().join(' ')
      : null

    const fallback = response.status === 401
      ? 'Your session has expired. Please sign in again.'
      : response.status === 403
        ? 'You are not authorized to perform this action.'
        : 'Reviews could not be loaded. Please try again.'

    const error = new Error(
      validation || data?.detail || data?.message || fallback
    )
    error.status = response.status
    throw error
  }

  return data
}

export function getReviewEligibility(listingId, bookingType, signal) {
  const query = new URLSearchParams({
    listingId,
    bookingType: normalizeBookingType(bookingType)
  })

  return request(`/api/bookings/reviews/eligibility?${query}`, {
    auth: true,
    signal
  })
}

export function getReviews(
  listingId, bookingType, { page = 1, signal } = {}
) {
  const query = new URLSearchParams({
    bookingType: normalizeBookingType(bookingType),
    page: String(page),
    pageSize: '10'
  })


  return request(`/api/bookings/reviews/${listingId}?${query}`, { signal })
}

export function submitReview(body) {
  return request('/api/bookings/reviews', {
    auth: true,
    body: {
      ...body,
      bookingType: normalizeBookingType(body.bookingType)
    }
  })
}
export function verifyAttendance(booking, status) {
  return request(`/api/provider-bookings/${normalizeBookingType(booking.bookingType)}/${booking.id}/attendance`, { auth: true, method: 'PATCH', body: { status } })
}
export function disputeAttendance(booking, reason) {
  return request(`/api/user-bookings/${normalizeBookingType(booking.bookingType)}/${booking.id}/attendance-dispute`, { auth: true, body: { reason } })
}
export function respondToReview(id, comment) {
  return request(`/api/bookings/reviews/${id}/response`, { auth: true, method: 'PUT', body: { comment } })
}
export function getAttendanceDisputes() {
  return request('/api/admin-bookings/attendance-disputes', { auth: true })
}
export function resolveAttendanceDispute(id, status, resolution) {
  return request(`/api/admin-bookings/attendance-disputes/${id}`, { auth: true, method: 'PATCH', body: { status, resolution } })
}
