import { useEffect, useState } from 'react'
import {
  getReviewEligibility,
  getReviews
} from '../api/reviewApi'
import ReviewForm from './ReviewForm'
import StarRating from './StarRating'
import './ListingReviews.css'

const dateFormatter = new Intl.DateTimeFormat('en-LK', {
  dateStyle: 'medium',
  timeZone: 'Asia/Colombo'
})

export default function ListingReviews({
  listingId,
  bookingType,
  preferredBookingId,
  onSummaryChange
}) {
  const [rating, setRating] = useState('')
  const [page, setPage] = useState(1)
  const [refresh, setRefresh] = useState(0)
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [eligibility, setEligibility] = useState(null)
  const [eligibilityLoading, setEligibilityLoading] = useState(true)
  const [eligibilityError, setEligibilityError] = useState('')
  const [success, setSuccess] = useState('')
  const [submissionError, setSubmissionError] = useState('')

  const authToken = localStorage.getItem('authToken')

  useEffect(() => {
    const controller = new AbortController()

    queueMicrotask(() => {
      if (!controller.signal.aborted) {
        setLoading(true)
        setError('')
      }
    })

    getReviews(listingId, bookingType, {
      rating,
      page,
      signal: controller.signal
    })
      .then(result => {
        if (!controller.signal.aborted) setData(result)
      })
      .catch(err => {
        if (!controller.signal.aborted) setError(err.message)
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false)
      })

    return () => controller.abort()
  }, [listingId, bookingType, rating, page, refresh])

  useEffect(() => {
    const controller = new AbortController()

    queueMicrotask(() => {
      if (!controller.signal.aborted) {
        setEligibility(null)
        setEligibilityError('')
        setEligibilityLoading(Boolean(authToken))
      }
    })
    if (!authToken) return () => controller.abort()

    getReviewEligibility(listingId, bookingType, controller.signal)
      .then(result => {
        if (!controller.signal.aborted) setEligibility(result)
      })
      .catch(err => {
        if (!controller.signal.aborted) {
          setEligibilityError(err.message)
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setEligibilityLoading(false)
      })

    return () => controller.abort()
  }, [listingId, bookingType, authToken, refresh])

  function submitted() {
    setSubmissionError('')
    setSuccess('Thank you! Your review has been submitted.')
    setEligibility(null)
    setEligibilityLoading(true)
    setRating('')
    setPage(1)
    setRefresh(value => value + 1)
  }

  useEffect(() => {
    if (data) {
      onSummaryChange?.({
        averageRating: data.averageRating,
        reviewCount: data.reviewCount
      })
    }
  }, [data, onSummaryChange])

  const eligibleBookings = eligibility?.eligibleBookings ?? []

  return (
    <section className="cq-reviews" aria-label="Visitor reviews">
      <h2>Visitor reviews</h2>

      {data && (
        <p className="cq-review-summary">
          <strong>{Number(data.averageRating).toFixed(2)} / 5</strong>
          {' · '}
          {data.reviewCount} review{data.reviewCount === 1 ? '' : 's'}
        </p>
      )}

      <div className="cq-review-filters" aria-label="Filter by star rating">
        {['', 5, 4, 3, 2, 1].map(value => (
          <button
            key={value === '' ? 'all' : value}
            type="button"
            aria-pressed={rating === value}
            onClick={() => {
              setRating(value)
              setPage(1)
            }}
          >
            {value === '' ? 'All' : `${value} ★`}
          </button>
        ))}
      </div>

      {success && <p role="status">{success}</p>}
      {submissionError && <p role="alert" className="cq-review-error">{submissionError}</p>}
      {loading && <p role="status">Loading reviews…</p>}

      {error && (
        <div role="alert" className="cq-review-error">
          <p>{error}</p>
          <button type="button" onClick={() => setRefresh(v => v + 1)}>
            Retry
          </button>
        </div>
      )}

      {!loading && !error && data && (
        <>
          {data.items.length === 0 ? (
            <p>
              {rating === ''
                ? 'No reviews yet.'
                : `No ${rating}-star reviews yet.`}
            </p>
          ) : (
            <ul className="cq-review-list">
              {data.items.map(review => (
                <li key={review.id}>
                  <div className="cq-review-heading">
                    <strong>{review.reviewerDisplayName || 'Visitor'}</strong>
                    <time dateTime={review.createdAtUtc}>
                      {dateFormatter.format(new Date(review.createdAtUtc))}
                    </time>
                  </div>
                  <StarRating value={review.rating} />
                  <p className="cq-review-comment">{review.comment}</p>
                </li>
              ))}
            </ul>
          )}

          {data.totalPages > 1 && (
            <div className="cq-review-pagination">
              <button
                type="button"
                disabled={page === 1}
                onClick={() => setPage(value => value - 1)}
              >
                Previous
              </button>
              <span>Page {page} of {data.totalPages}</span>
              <button
                type="button"
                disabled={page >= data.totalPages}
                onClick={() => setPage(value => value + 1)}
              >
                Next
              </button>
            </div>
          )}
        </>
      )}

      {!authToken ? (
        <p>Sign in as a visitor to submit a review.</p>
      ) : eligibilityLoading ? (
        <p role="status">Checking review eligibility…</p>
      ) : eligibilityError ? (
        <div role="alert">
          <p>{eligibilityError}</p>
          <button type="button" onClick={() => setRefresh(v => v + 1)}>
            Check again
          </button>
        </div>
      ) : eligibleBookings.length > 0 ? (
        <ReviewForm
          key={`${listingId}-${refresh}`}
          bookingType={bookingType}
          bookings={eligibleBookings}
          preferredBookingId={preferredBookingId}
          onSubmitted={submitted}
          onConflict={(message) => {
            setSuccess('')
            setSubmissionError(message)
            setEligibility(null)
            setEligibilityLoading(true)
            setRefresh(value => value + 1)
          }}
        />
      ) : (
        <p>{eligibility?.message || 'You are not eligible to review yet.'}</p>
      )}
    </section>
  )
}
