import { useRef, useState } from 'react'
import { respondToReview } from '../api/reviewApi'

export default function ProviderReviewResponse({ review, onSaved }) {
  const [comment, setComment] = useState(review.providerResponse || '')
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)
  const busy = useRef(false)
  async function submit(event) {
    event.preventDefault()
    if (busy.current || !comment.trim()) return
    busy.current = true; setSaving(true); setError('')
    try { await respondToReview(review.id, comment.trim()); onSaved?.() }
    catch (err) { setError(err.message) }
    finally { busy.current = false; setSaving(false) }
  }
  return <form className="cq-review-form" onSubmit={submit}>
    <label>Provider response<textarea required maxLength={2000} value={comment} onChange={e => setComment(e.target.value)} /></label>
    {error && <p role="alert">{error}</p>}
    <button type="submit" disabled={saving || !comment.trim()}>{saving ? 'Saving…' : review.providerResponse ? 'Update response' : 'Respond to review'}</button>
  </form>
}
