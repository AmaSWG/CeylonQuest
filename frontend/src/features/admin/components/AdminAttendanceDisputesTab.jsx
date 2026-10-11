import { useEffect, useState } from 'react'
import { getAttendanceDisputes, resolveAttendanceDispute } from '../../../api/reviewApi'
import '../../../components/ListingReviews.css'

function Dispute({ item, onSaved }) {
  const [status, setStatus] = useState('Attended')
  const [resolution, setResolution] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  async function submit(e) {
    e.preventDefault()
    if (saving || !resolution.trim()) return
    setSaving(true); setError('')
    try { await resolveAttendanceDispute(item.id, status, resolution.trim()); onSaved() }
    catch (err) { setError(err.message) }
    finally { setSaving(false) }
  }
  return <article className="cq-reviews"><h3>{item.bookingType} booking {item.bookingId}</h3>
    <p>Visitor: {item.visitorId}</p><p>Recorded attendance: {['Pending', 'Attended', 'NoShow'][item.originalAttendanceStatus] || item.originalAttendanceStatus}</p><p>{item.reason}</p>
    {item.resolvedAtUtc ? <p>Resolved: {item.resolution}</p> : <form className="cq-review-form" onSubmit={submit}>
      <label>Verified outcome<select value={status} onChange={e => setStatus(e.target.value)}><option>Attended</option><option>NoShow</option></select></label>
      <label>Resolution and evidence<textarea required maxLength={2000} value={resolution} onChange={e => setResolution(e.target.value)} /></label>
      <button type="submit" disabled={saving || !resolution.trim()}>{saving ? 'Saving…' : 'Resolve dispute'}</button>
    </form>}{error && <p role="alert">{error}</p>}
  </article>
}

export default function AdminAttendanceDisputesTab() {
  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [refresh, setRefresh] = useState(0)
  useEffect(() => {
    let active = true
    getAttendanceDisputes().then(data => { if (active) { setItems(data); setError('') } })
      .catch(err => { if (active) setError(err.message) }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [refresh])
  return <section><h2>Attendance disputes</h2>{loading && <p role="status">Loading…</p>}
    {error && <p role="alert">{error}</p>}<button type="button" onClick={() => setRefresh(v => v + 1)}>Refresh</button>
    {!loading && !error && items.length === 0 && <p>No attendance disputes.</p>}
    {items.map(item => <Dispute key={item.id} item={item} onSaved={() => setRefresh(v => v + 1)} />)}
  </section>
}
