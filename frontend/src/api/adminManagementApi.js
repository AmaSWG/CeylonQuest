import { apiUrl, catalogUrl } from './client'

async function request(url, token, options = {}) {
  if (!token) throw new Error('Please sign in again.')

  const response = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
      ...options.headers,
    },
  })

  const text = await response.text()
  let data = null
  try {
    data = text ? JSON.parse(text) : null
  } catch {
    // A proxy may return an HTML error page.
  }

  if (!response.ok) {
    const validationErrors = data?.errors
      ? Object.values(data.errors).flat().join(' ')
      : ''
    const fallback = response.status === 401
      ? 'Your session has expired. Please sign in again.'
      : response.status === 403
        ? 'You do not have permission to perform this action.'
        : `Request failed (${response.status}). Please try again.`
    throw new Error(data?.message || validationErrors || fallback)
  }

  return data
}

export function getAdminUsers(token, signal) {
  return request(apiUrl('/api/admin/users'), token, { signal })
}

export function updateAdminUserStatus(token, userId, isActive) {
  return request(
    apiUrl(`/api/admin/users/${encodeURIComponent(userId)}/status`),
    token,
    { method: 'PUT', body: JSON.stringify({ isActive }) },
  )
}

export function getAdminListings(token, signal) {
  return request(catalogUrl('/api/catalog/reports/admin-listings'), token, { signal })
}

export function removeAdminListing(token, listing, reason) {
  return request(
    catalogUrl(`/api/catalog/admin/listings/${encodeURIComponent(listing.bookingType)}/${encodeURIComponent(listing.id)}/remove`),
    token,
    {
      method: 'POST',
      body: JSON.stringify({ confirmRemoval: true, reason: reason.trim() }),
    },
  )
}