import { apiUrl } from './client'

async function readResponse(response) {
  const body = await response.json().catch(() => ({}))

  if (!response.ok) {
    const validationMessage = Object.values(body.errors || {})
      .flat()
      .join(' ')

    const message =
      body.message ||
      validationMessage ||
      (response.status === 401
        ? 'Your session has expired. Please log in again.'
        : response.status === 403
          ? 'Your account cannot submit platform reviews.'
          : 'Unable to complete the review request.')

    throw new Error(message)
  }

  return body
}

export async function getPlatformReviews({
  rating = '',
  page = 1,
  pageSize = 20,
  signal,
} = {}) {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  })

  if (rating !== '') {
    params.set('rating', String(rating))
  }

  const response = await fetch(
    apiUrl(`/api/platform-reviews?${params}`),
    { signal },
  )

  return readResponse(response)
}

export async function submitPlatformReview({ rating, content }) {
  const token = localStorage.getItem('authToken')

  if (!token) {
    throw new Error('Please log in before submitting a review.')
  }

  const response = await fetch(apiUrl('/api/platform-reviews'), {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({ rating, content }),
  })

  return readResponse(response)
}