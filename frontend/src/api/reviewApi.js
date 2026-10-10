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

async function request(path, { auth = false, body, signal } = {}) {
  const headers = {}
  const token = localStorage.getItem('authToken')

  if (auth) {
    if (!token) throw new Error('Sign in to submit a review.')
    headers.Authorization = `Bearer ${token}`
  }

  if (body) headers['Content-Type'] = 'application/json'

  const response = await fetch(bookingUrl(path), {
    method: body ? 'POST' : 'GET',
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
        ? 'Only eligible visitors can submit reviews.'
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
  listingId, bookingType, { rating = '', page = 1, signal } = {}
) {
  const query = new URLSearchParams({
    bookingType: normalizeBookingType(bookingType),
    page: String(page),
    pageSize: '10'
  })

  if (rating !== '') query.set('rating', String(rating))

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