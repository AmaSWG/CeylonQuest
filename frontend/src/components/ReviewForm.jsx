import { useId, useRef, useState } from 'react'
import { submitReview } from '../api/reviewApi'
import StarRating from './StarRating'

export default function ReviewForm({
  bookingType,
  bookings,
  preferredBookingId,
  onSubmitted,
  onConflict
}) {
  const id = useId()
  const submitting = useRef(false)
  const initial = bookings.find(b => b.bookingId === preferredBookingId)
    ?? bookings[0]

  const [bookingId, setBookingId] = useState(initial?.bookingId ?? '')
  const [rating, setRating] = useState(0)
  const [comment, setComment] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  async function handleSubmit(event) {
    event.preventDefault()
    if (submitting.current) return

    if (
      !bookings.some(b => b.bookingId === bookingId) ||
      rating < 1 || rating > 5 ||
      !comment.trim() || comment.length > 2000
    ) {
      setError('Select a booking, a rating and written feedback.')
      return
    }

    submitting.current = true
    setSaving(true)
    setError('')

    try {
      const review = await submitReview({
        bookingId,
        bookingType,
        rating,
        comment: comment.trim()
      })

      setRating(0)
      setComment('')
      onSubmitted(review)
    } catch (err) {
      setError(err.message)
      if (err.status === 409 || err.status === 401 || err.status === 403) {
        onConflict?.(err.message)
      }
    } finally {
      submitting.current = false
      setSaving(false)
    }
  }

  return (
    <form className="cq-review-form" onSubmit={handleSubmit}>
      <h3>Share your experience</h3>

      {bookings.length > 1 && (
        <>
          <label htmlFor={`${id}-booking`}>Completed booking</label>
          <select
            id={`${id}-booking`}
            value={bookingId}
            onChange={event => setBookingId(event.target.value)}
            disabled={saving}
          >
            {bookings.map(booking => (
              <option key={booking.bookingId} value={booking.bookingId}>
                {booking.date} — {booking.bookingId.slice(0, 8)}
              </option>
            ))}
          </select>
        </>
      )}

      <StarRating value={rating} onChange={setRating} disabled={saving} />

      <label htmlFor={`${id}-comment`}>Written feedback *</label>
      <textarea
        id={`${id}-comment`}
        value={comment}
        onChange={event => setComment(event.target.value)}
        maxLength={2000}
        rows={4}
        required
        disabled={saving}
        aria-describedby={`${id}-length`}
      />
      <small id={`${id}-length`}>{comment.length}/2000 characters</small>

      {error && <p role="alert" className="cq-review-error">{error}</p>}

      <button
        type="submit"
        disabled={saving || !rating || !comment.trim()}
      >
        {saving ? 'Submitting…' : 'Submit review'}
      </button>
    </form>
  )
}
