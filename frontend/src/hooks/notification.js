import { useCallback, useEffect, useRef, useState } from 'react'
import { getNotifications, markNotificationRead, markAllNotificationsRead } from '../api/notification'

export default function useNotifications(token, isOpen = false) {
  const [data, setData] = useState({ items: [], unreadCount: 0, totalCount: 0, pageSize: 20 })
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [busy, setBusy] = useState(false)
  const controller = useRef(null)
  const generation = useRef(0)
  const writing = useRef(false)

  const refresh = useCallback(async () => {
    if (!token || writing.current) return
    controller.current?.abort()
    const request = new AbortController()
    controller.current = request
    setLoading(true)
    try {
      const result = await getNotifications(page, request.signal)
      if (!request.signal.aborted) { setData({ ...result, owner: token }); setError(null) }
    } catch (err) {
      if (!request.signal.aborted) setError(err.message)
    } finally {
      if (!request.signal.aborted) setLoading(false)
    }
  }, [token, page])

  useEffect(() => {
    generation.current += 1
    const start = setTimeout(refresh, 0)
    const timer = setInterval(refresh, 30000)
    return () => {
      generation.current += 1
      clearTimeout(start)
      clearInterval(timer)
      controller.current?.abort()
    }
  }, [refresh, isOpen])

  const markRead = async (id) => {
    if (!token || writing.current) return
    writing.current = true
    setBusy(true)
    controller.current?.abort()
    const current = generation.current
    try {
      const result = await (id ? markNotificationRead(id) : markAllNotificationsRead())
      if (current !== generation.current) return
      setData(previous => ({ ...previous, unreadCount: result.unreadCount,
        items: previous.items.map(item => !id || item.id === id ? { ...item, isRead: true } : item) }))
      setError(null)
    } catch (err) {
      if (current === generation.current) setError(err.message)
    } finally {
      writing.current = false
      setBusy(false)
      setLoading(false)
    }
  }

  const visible = data.owner === token ? data : { items: [], unreadCount: 0, totalCount: 0, pageSize: 20 }
  return { ...visible, loading, error, busy, page, setPage, refresh,
    markRead, markAllRead: () => markRead(), authenticated: Boolean(token) }
}
