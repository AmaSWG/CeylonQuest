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
  const datePassed = booking.scheduledEndAtUtc
    ? new Date(booking.scheduledEndAtUtc).getTime() <= now
    : booking.checkOutDate
      ? new Date(`${booking.checkOutDate}T12:00:00`).getTime() <= now
      : booking.date
        ? new Date(`${booking.date}T23:59:59`).getTime() <= now
        : false
  const eligible = pending && ['Confirmed', 'Completed'].includes(booking.status) && datePassed

  async function mark(status) {
    if (busy.current) return
    busy.current = true; setSaving(true); setError('')
    try { await verifyAttendance(booking, status); onSaved?.() }
    catch (err) { setError(err.message) }
    finally { busy.current = false; setSaving(false) }
  }
  return <div className="cq-attendance-actions">
    <p style={{ margin: '0 0 4px', fontSize: '13px' }}>
      Attendance: <strong style={{ color: booking.attendanceStatus === 'Attended' ? '#166534' : booking.attendanceStatus === 'NoShow' ? '#991b1b' : '#64748b' }}>
        {booking.attendanceStatus || 'Pending'}
      </strong>
    </p>
    {booking.attendanceVerifiedAtUtc && (
      <small style={{ display: 'block', color: '#94a3b8', fontSize: '11px', marginBottom: '4px' }}>
        Verified {new Date(booking.attendanceVerifiedAtUtc).toLocaleString('en-GB')}
      </small>
    )}
    {eligible && (
      <div style={{ display: 'flex', gap: '6px', marginTop: '6px', flexWrap: 'wrap' }}>
        <button
          type="button"
          className="pd-bookings-view-btn"
          style={{ background: '#166534', color: '#ffffff', borderColor: '#166534', padding: '4px 10px', fontSize: '12px' }}
          disabled={saving}
          onClick={() => mark('Attended')}
        >
          {saving ? 'Saving…' : 'Verify Attended'}
        </button>
        <button
          type="button"
          className="pd-bookings-view-btn"
          style={{ background: '#ffffff', color: '#991b1b', borderColor: '#fca5a5', padding: '4px 10px', fontSize: '12px' }}
          disabled={saving}
          onClick={() => mark('NoShow')}
        >
          {saving ? 'Saving…' : 'Mark No-Show'}
        </button>
      </div>
    )}
    {error && <p role="alert" style={{ color: '#b91c1c', fontSize: '12px', margin: '4px 0 0' }}>{error}</p>}
  </div>
}
