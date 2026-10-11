import { useEffect, useRef, useState } from 'react'
import { verifyAttendance } from '../api/reviewApi'

export default function AttendanceActions({ booking, onSaved }) {
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const busy = useRef(false)
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 30000)
    return () => clearInterval(timer)
  }, [])
  const pending = (booking.attendanceStatus || 'Pending') === 'Pending'
  const eligible = pending && ['Confirmed', 'Completed'].includes(booking.status)
    && booking.scheduledEndAtUtc && new Date(booking.scheduledEndAtUtc).getTime() < now
  async function mark(status) {
    if (busy.current) return
    busy.current = true; setSaving(true); setError('')
    try { await verifyAttendance(booking, status); onSaved?.() }
    catch (err) { setError(err.message) }
    finally { busy.current = false; setSaving(false) }
  }
  return <div className="cq-attendance-actions">
    <p>Attendance: {booking.attendanceStatus || 'Pending'}</p>
    {booking.attendanceVerifiedAtUtc && <small>Verified {new Date(booking.attendanceVerifiedAtUtc).toLocaleString()}</small>}
    {eligible && <><button type="button" className="pd-bookings-view-btn" disabled={saving} onClick={() => mark('Attended')}>Mark Attended</button><button type="button" className="pd-bookings-view-btn" disabled={saving} onClick={() => mark('NoShow')}>Mark NoShow</button></>}
    {error && <p role="alert">{error}</p>}
  </div>
}
