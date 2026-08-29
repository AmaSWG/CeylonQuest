/**
 * authApi.js — CeylonQuest Auth API Service
 *
 * Centralises all authentication fetch calls.
 * All requests proxy through the API Gateway (http://localhost:5000)
 * via the Vite /api proxy configured in vite.config.js.
 *
 * Endpoints:
 *   POST /api/auth/login
 *   POST /api/auth/register
 *   POST /api/auth/forgot-password
 *   POST /api/auth/reset-password
 */

const BASE = `${ import.meta.env.VITE_IDENTITY_API_URL || ''}/api/auth`

/**
 * Log in an existing user.
 * @param {string} email
 * @param {string} password
 * @returns {Promise<{ accessToken: string, token: string, role: string }>}
 * @throws {Error} with .status and .body on non-2xx responses
 */
export async function login(email, password) {
  const resp = await fetch(`${BASE}/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ email, password }),
  })
  const body = await resp.json().catch(() => ({}))
  if (!resp.ok) {
    throw Object.assign(new Error(body.message || 'Login failed'), { status: resp.status, body })
  }
  return body
}

/**
 * Register a new Visitor account.
 * @param {{ firstName: string, lastName: string, email: string, phoneNumber: string,
 *            nationality: string, password: string, confirmPassword: string,
 *            registrationType: string }} data
 * @returns {Promise<{ message: string }>}
 * @throws {Error} with .status and .body on non-2xx responses
 */
export async function register(data) {
  const resp = await fetch(`${BASE}/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })
  const body = await resp.json().catch(() => ({}))
  if (!resp.ok) {
    throw Object.assign(new Error(body.message || 'Registration failed'), { status: resp.status, body })
  }
  return body
}

/**
 * Request a password-reset email.
 * @param {string} email
 * @returns {Promise<{ message: string }>}
 * @throws {Error} with .status and .body on non-2xx responses
 */
export async function forgotPassword(email) {
  const resp = await fetch(`${BASE}/forgot-password`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email }),
  })
  const body = await resp.json().catch(() => ({}))
  if (!resp.ok) {
    throw Object.assign(new Error(body.message || 'Request failed'), { status: resp.status, body })
  }
  return body
}

/**
 * Reset a user's password using a token from the reset email.
 * @param {string} token
 * @param {string} newPassword
 * @param {string} confirmPassword
 * @returns {Promise<{ message: string }>}
 * @throws {Error} with .status and .body on non-2xx responses
 */
export async function resetPassword(token, newPassword, confirmPassword) {
  const resp = await fetch(`${BASE}/reset-password`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ token, newPassword, confirmPassword }),
  })
  const body = await resp.json().catch(() => ({}))
  if (!resp.ok) {
    throw Object.assign(new Error(body.message || 'Reset failed'), { status: resp.status, body })
  }
  return body
}
