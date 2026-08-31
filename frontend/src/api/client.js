/**
 * client.js — CeylonQuest API base URL helper
 *
 * In development VITE_API_BASE_URL is unset, so this returns a relative path
 * and the Vite proxy in vite.config.js forwards it to the backend.
 *
 * In production the deploy workflow sets VITE_API_BASE_URL to the Identity
 * Service origin, so calls go straight there. Without this, relative paths
 * would hit the Static Web App origin — where /api/* is a reserved path — and
 * fail with 404/405 instead of reaching the backend.
 *
 * Usage:  fetch(apiUrl('/api/users/me'), { credentials: 'include' })
 */

const API_BASE = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '')

/**
 * Resolve an app-relative API path against the configured backend origin.
 * @param {string} path  A path beginning with '/', e.g. '/api/users/me'
 * @returns {string} An absolute URL in production, the path itself in dev
 */
export function apiUrl(path) {
  return `${API_BASE}${path}`
}

export { API_BASE }
