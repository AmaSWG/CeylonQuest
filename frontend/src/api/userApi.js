/**
 * userApi.js — CeylonQuest User API Service
 *
 * Wraps all /api/users/* fetch calls.
 * All requests proxy through the API Gateway (http://localhost:5000)
 * via the Vite /api proxy configured in vite.config.js.
 *
 * Endpoints:
 *   GET /api/users/me
 *   PUT /api/users/me
 */

const BASE = '/api/users'

/**
 * Fetch the currently authenticated user's profile.
 * @param {string|null} token  Bearer token (optional — cookies are sent automatically)
 * @returns {Promise<object>} The user profile object
 * @throws {Error} with .status and .body on non-2xx responses
 */
export async function getMe(token = null) {
  const resp = await fetch(`${BASE}/me`, {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    credentials: 'include',
  })
  const body = await resp.json().catch(() => ({}))
  if (!resp.ok) {
    throw Object.assign(new Error(body.message || 'Failed to fetch profile'), { status: resp.status, body })
  }
  return body
}

/**
 * Update the currently authenticated user's profile.
 * @param {object} data        Fields to update
 * @param {string|null} token  Bearer token (optional — cookies are sent automatically)
 * @returns {Promise<object>} The updated user profile object
 * @throws {Error} with .status and .body on non-2xx responses
 */
export async function updateMe(data, token = null) {
  const resp = await fetch(`${BASE}/me`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    credentials: 'include',
    body: JSON.stringify(data),
  })
  const body = await resp.json().catch(() => ({}))
  if (!resp.ok) {
    throw Object.assign(new Error(body.message || 'Failed to update profile'), { status: resp.status, body })
  }
  return body
}
