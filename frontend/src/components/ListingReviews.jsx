import { useEffect, useState } from 'react'
import {
  getReviewEligibility,
  getReviews
} from '../api/reviewApi'
import ReviewForm from './ReviewForm'
import StarRating from './StarRating'
import './ListingReviews.css'
import ProviderReviewResponse from './ProviderReviewResponse'

const dateFormatter = new Intl.DateTimeFormat('en-LK', {
  dateStyle: 'medium',
  timeZone: 'Asia/Colombo'
})

export default function ListingReviews({
  listingId,
  bookingType,
  preferredBookingId,
  onSummaryChange,
  allowSubmission = false,
  allowResponse = false,
  onSubmitted,
  submitOnly = false
}) {
  const [page, setPage] = useState(1)
  const [rating, setRating] = useState('')
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
    if (submitOnly) {
      setLoading(false)
      return
    }

    const controller = new AbortController()

    queueMicrotask(() => {
      if (!controller.signal.aborted) {
        setLoading(true)
        setError('')
      }
    })

    getReviews(listingId, bookingType, {
      page,
      rating,
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
  }, [listingId, bookingType, page, rating, refresh, submitOnly])

  useEffect(() => {
    const controller = new AbortController()

    queueMicrotask(() => {
      if (!controller.signal.aborted) {
        setEligibility(null)
        setEligibilityError('')
        setEligibilityLoading(Boolean(authToken && allowSubmission))
      }
    })
    if (!authToken || !allowSubmission) return () => controller.abort()

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
  }, [listingId, bookingType, authToken, refresh, allowSubmission])

  function submitted() {
    setSubmissionError('')
    setSuccess('Thank you! Your review has been submitted.')
    setEligibility(null)
    setEligibilityLoading(true)
    setPage(1)
    setRefresh(value => value + 1)
    onSubmitted?.()
  }

  useEffect(() => {
    if (data) {
      onSummaryChange?.({
        averageRating: data.averageRating,
        reviewCount: data.reviewCount
      })
    }
  }, [data, onSummaryChange])

  const rawEligible = eligibility?.eligibleBookings ?? []
  const filteredEligible = rawEligible.filter(b => !preferredBookingId || b.bookingId === preferredBookingId)
  const eligibleBookings = filteredEligible.length > 0
    ? filteredEligible
    : (preferredBookingId && rawEligible.length > 0)
      ? rawEligible
      : (preferredBookingId && allowSubmission)
        ? [{ bookingId: preferredBookingId, bookingDate: 'Completed booking' }]
        : []

  if (submitOnly) {
    return (
      <section className="cq-reviews cq-reviews--submit-only" aria-label="Submit review">
        {success && <p role="status" className="cq-review-success">{success}</p>}
        {submissionError && <p role="alert" className="cq-review-error">{submissionError}</p>}

        {!authToken ? (
          <p>Sign in as a visitor to submit a review.</p>
        ) : eligibilityLoading ? (
          <p role="status">Preparing review form…</p>
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

  return (
    <section className="cq-reviews" aria-label="Visitor reviews">
      <h2>Customer Reviews &amp; Ratings</h2>

      <div className="cq-review-filter-bar">
        <label htmlFor="cq-rating-filter" className="cq-review-filter-label">
          Filter reviews by rating:
        </label>
        <select
          id="cq-rating-filter"
          className="cq-review-filter-select"
          value={rating}
          onChange={event => {
            setRating(event.target.value)
            setPage(1)
            setLoading(true)
            setError('')
          }}
        >
          <option value="">All ratings</option>
          {[5, 4, 3, 2, 1].map(stars => (
            <option key={stars} value={stars}>
              {stars} {stars === 1 ? 'star' : 'stars'}
            </option>
          ))}
        </select>
      </div>

      {data && (
        <p className="cq-review-summary">
          {rating !== '' ? (
            <>
              <strong>{rating} / 5</strong>
              {' . '}
              {!loading && ! error && (
                <>
                  {data.totalCount} matching review
                  {data.totalCount === 1 ? '' : 's'}
                </>
              )}
            </>
          ) : (
            <>
              {data.reviewCount > 0 && (
                <>
                  <strong>{Number(data.averageRating)} / 5</strong>
                  {' . '}
                </>
              )}
              {data.reviewCount} review
              {data.reviewCount === 1 ? '' : 's'}
            </>
          )}
        </p>
      )}

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
              {rating !== ''
                ? 'No reviews match this star rating.'
                : 'No reviews yet. Be the first to share your experience after completing a booking.'}
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
                  <div className="cq-review-badges">
                    <span className="cq-badge cq-badge--verified-customer" title="Verified Customer with a valid booking">
                      ✓ Verified Customer
                    </span>
                    {review.attendanceVerified && (
                      <span className="cq-badge cq-badge--attendance-verified" title="Attendance verified by provider">
                        ✓ Attendance Verified
                      </span>
                    )}
                  </div>
                  <p className="cq-review-comment">{review.comment}</p>
                  {review.providerResponse && <blockquote className="cq-provider-response"><strong>Provider response</strong><p>{review.providerResponse}</p>{review.providerRespondedAtUtc && <time dateTime={review.providerRespondedAtUtc}>{dateFormatter.format(new Date(review.providerRespondedAtUtc))}</time>}</blockquote>}
                  {allowResponse && <ProviderReviewResponse review={review} onSaved={() => setRefresh(v => v + 1)} />}
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

      {allowSubmission && (!authToken ? (
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
      ))}
    </section>
  )
}
