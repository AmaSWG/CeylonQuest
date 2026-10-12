import { apiUrl } from './client'

export async function getAdminAuditLogs(token, filters, signal) {
  const params = new URLSearchParams()

  for (const [key, value] of Object.entries(filters)) {
    if (value !== '' && value !== undefined && value !== null) {
      params.set(key, String(value))
    }
  }

  const response = await fetch(
    apiUrl(`/api/admin/audit-logs?${params}`),
    {
      headers: { Authorization: `Bearer ${token}` },
      signal,
    },
  )

  if (!response.ok) {
    const body = await response.json().catch(() => null)
    throw new Error(
      body?.message || `Unable to load audit logs (${response.status}).`,
    )
  }

  return response.json()
}
