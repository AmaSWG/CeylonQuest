import { useState } from 'react'
import { submitPlatformReview } from '../../../api/platformReviewApi'

export default function PlatformReviewForm({ onSubmitted }) {
  const [rating, setRating] = useState('')
  const [content, setContent] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  async function handleSubmit(event) {
    event.preventDefault()
    setError('')
    setSuccess('')

    const trimmedContent = content.trim()

    if (!rating || !trimmedContent || trimmedContent.length > 2000) {
      setError('Select a rating and enter 1–2,000 characters of feedback.')
      return
    }

    setBusy(true)

    try {
      await submitPlatformReview({
        rating: Number(rating),
        content: trimmedContent,
      })

      setRating('')
      setContent('')
      setSuccess('Your review has been submitted.')
      onSubmitted()
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="platform-review-form" onSubmit={handleSubmit}>
      <h2>Share your platform experience</h2>

      <label htmlFor="platform-review-rating">Star rating</label>
      <select
        id="platform-review-rating"
        value={rating}
        onChange={(event) => setRating(event.target.value)}
        required
        disabled={busy}
      >
        <option value="">Select a rating</option>

        {[1, 2, 3, 4, 5].map((value) => (
          <option key={value} value={value}>
            {value} {value === 1 ? 'star' : 'stars'}
          </option>
        ))}
      </select>

      <label htmlFor="platform-review-content">Your review</label>
      <textarea
        id="platform-review-content"
        value={content}
        onChange={(event) => setContent(event.target.value)}
        rows={5}
        maxLength={2000}
        required
        disabled={busy}
      />

      <small>{content.length}/2,000 characters</small>

      {error && (
        <p className="platform-review-error" role="alert">
          {error}
        </p>
      )}

      {success && <p className="platform-review-success" role="status">{success}</p>}

      <button type="submit" disabled={busy}>
        {busy ? 'Submitting…' : 'Submit review'}
      </button>
    </form>
  )
}