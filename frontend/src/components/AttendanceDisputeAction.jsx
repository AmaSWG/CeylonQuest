import { useEffect, useRef, useState } from 'react'
import { disputeAttendance } from '../api/reviewApi'

export default function AttendanceDisputeAction({ booking, onSaved, emptyFallback = null }) {
  const [open, setOpen] = useState(false)
  const [reason, setReason] = useState('')
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const busy = useRef(false)
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 30000)
    return () => clearInterval(timer)
  }, [])
  const eligible = ['Confirmed', 'Completed'].includes(booking.status) && booking.attendanceStatus !== 'Attended'
    && booking.scheduledEndAtUtc && new Date(booking.scheduledEndAtUtc).getTime() < now
  async function submit(event) {
    event.preventDefault()
    if (busy.current || !reason.trim()) return
    busy.current = true; setSaving(true); setError('')
    try { await disputeAttendance(booking, reason.trim()); setMessage('Attendance dispute submitted for administrative review.'); setOpen(false); onSaved?.() }
    catch (err) { setError(err.message) }
    finally { busy.current = false; setSaving(false) }
  }
  if (!eligible) return emptyFallback
  return <div className="cq-attendance-dispute">
    {!message && <button type="button" className="vb-view-btn" onClick={() => setOpen(v => !v)}>Report incorrect attendance</button>}
    {message && <p role="status">{message}</p>}
    {open && <form onSubmit={submit}><label>Explain what happened<textarea required maxLength={2000} value={reason} onChange={e => setReason(e.target.value)} /></label><button className="vb-view-btn" disabled={saving || !reason.trim()} type="submit">{saving ? 'Submitting…' : 'Submit dispute'}</button></form>}
    {error && <p role="alert">{error}</p>}
  </div>
}
