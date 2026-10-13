import { apiUrl } from './client'

const notificationBase = (import.meta.env.VITE_NOTIFICATION_API_BASE_URL || '').replace(/\/$/, '')

async function request(path = '', method = 'GET', signal) {
  const token = localStorage.getItem('authToken')
  if (!token) throw new Error('Please log in to view notifications.')

  const response = await fetch(notificationBase ? `${notificationBase}/api/notifications${path}` : apiUrl(`/api/notifications${path}`), {
    method,
    headers: { Authorization: `Bearer ${token}` },
    signal,
  })

  if (!response.ok) {
    throw Object.assign(
      new Error('Unable to update or load notifications. Please try again.'),
      { status: response.status },
    )
  }

  return response.json()
}

export const getNotifications = (page = 1, signal) =>
  request(`?page=${page}&pageSize=20`, 'GET', signal)

export const getUnreadCount = signal =>
  request('/unread-count', 'GET', signal)

export const markNotificationRead = id =>
  request(`/${id}/read`, 'PATCH')

export const markAllNotificationsRead = () =>
  request('/read-all', 'PATCH')