import { useEffect, useState } from 'react'
import { getReviews, verifyReviewAttendance, verifyAttendance } from '../../../api/reviewApi'
import StarRating from '../../../components/StarRating'
import ProviderReviewResponse from '../../../components/ProviderReviewResponse'
import '../../../components/ListingReviews.css'
import './ProviderReviewsTab.css'

export default function ProviderReviewsTab({ services = [], bookingType }) {
  const [selectedId, setSelectedId] = useState('')
  const [page, setPage] = useState(1)
  const [refresh, setRefresh] = useState(0)
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [verifyingReviewId, setVerifyingReviewId] = useState(null)
  const [attendanceFeedback, setAttendanceFeedback] = useState({})

  const listingId = services.some(item => item.id === selectedId)
    ? selectedId
    : services[0]?.id

  useEffect(() => {
    if (!listingId) return

    const controller = new AbortController()

    setLoading(true)
    setError('')
    setData(null)

    getReviews(listingId, bookingType, {
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
  }, [listingId, bookingType, page, refresh])

  const [readReviewIds, setReadReviewIds] = useState([])

  function toggleReviewRead(reviewId) {
    setReadReviewIds(current =>
      current.includes(reviewId)
        ? current.filter(id => id !== reviewId)
        : [...current, reviewId]
    )
  }

  async function handleConfirmAttendance(review) {
    setVerifyingReviewId(review.id)
    setAttendanceFeedback(prev => ({ ...prev, [review.id]: null }))
    try {
      try {
        await verifyReviewAttendance(review.id)
      } catch (err) {
        if (review.bookingId) {
          await verifyAttendance({ id: review.bookingId, bookingType: review.bookingType || bookingType }, 'Attended')
        } else {
          throw err
        }
      }

      setData(current => {
        if (!current) return current
        return {
          ...current,
          items: current.items.map(item =>
            item.id === review.id ? { ...item, attendanceVerified: true } : item
          )
        }
      })
      setAttendanceFeedback(prev => ({
        ...prev,
        [review.id]: { text: 'Attendance confirmed successfully!', isError: false }
      }))
    } catch (err) {
      const message = err.status === 404
        ? 'Verification endpoint not found (404). Please restart booking-service to apply changes.'
        : (err.message || 'Failed to confirm attendance.')
      setAttendanceFeedback(prev => ({
        ...prev,
        [review.id]: { text: message, isError: true }
      }))
    } finally {
      setVerifyingReviewId(null)
    }
  }

  return (
    <div className="pd-notifications-view pd-reviews-view">
      <div className="pd-page-header">
        <div className="pd-page-header__left">
          <h1>Reviews &amp; Ratings</h1>
          <p>View customer feedback and respond to reviews.</p>
        </div>
      </div>

      <section className="pd-reviews-card" aria-labelledby="provider-reviews-title">
        <h2 id="provider-reviews-title" className="pd-reviews-title">
          Customer Reviews &amp; Ratings
        </h2>

        {services.length === 0 ? (
          <p>No listings available.</p>
        ) : (
          <>
            <div className="pd-reviews-selector-group">
              <label htmlFor="provider-review-listing" className="pd-reviews-label">
                Select listing
              </label>

              <select
                id="provider-review-listing"
                className="pd-reviews-select"
                value={listingId || ''}
                onChange={event => {
                  setSelectedId(event.target.value)
                  setPage(1)
                }}
              >
                {services.map(listing => (
                  <option key={listing.id} value={listing.id}>
                    {listing.title ||
                      listing.name ||
                      listing.roomType ||
                      'Listing'}
                  </option>
                ))}
              </select>
            </div>

            {loading && <p role="status">Loading reviews…</p>}

            {error && (
              <div role="alert">
                <p style={{ color: '#b42318' }}>{error}</p>
                <button
                  type="button"
                  className="pd-btn-read-toggle"
                  onClick={() => setRefresh(value => value + 1)}
                >
                  Retry
                </button>
              </div>
            )}

            {!loading && !error && data && (
              <>
                <div className="pd-reviews-summary-bar">
                  <span className="pd-reviews-summary-score">
                    {Number(data.averageRating).toFixed(2)} / 5
                  </span>
                  <span>·</span>
                  <span>
                    {data.reviewCount} review{data.reviewCount === 1 ? '' : 's'}
                  </span>
                </div>

                {data.items.length === 0 ? (
                  <p>No customer reviews for this listing yet.</p>
                ) : (
                  <div className="pd-reviews-grid">
                    {data.items.map(review => {
                      const isRead = readReviewIds.includes(review.id)
                      const feedback = attendanceFeedback[review.id]

                      return (
                        <article
                          key={review.id}
                          className={`pd-review-item ${isRead ? 'pd-review-item--read' : 'pd-review-item--unread'}`}
                        >
                          <div className="pd-review-item__header">
                            <strong className="pd-review-reviewer">
                              {review.reviewerDisplayName || 'Customer'}
                            </strong>

                            <div className="pd-review-item__meta">
                              <span className={`pd-read-status-badge ${isRead ? 'pd-read-status-badge--read' : 'pd-read-status-badge--unread'}`}>
                                {isRead ? 'Read' : 'Unread'}
                              </span>
                              <span>·</span>
                              <time dateTime={review.createdAtUtc}>
                                {new Date(review.createdAtUtc).toLocaleDateString(
                                  'en-GB',
                                  {
                                    day: 'numeric',
                                    month: 'short',
                                    year: 'numeric',
                                    timeZone: 'Asia/Colombo'
                                  }
                                )}
                              </time>
                            </div>
                          </div>

                          <div className="pd-review-rating-row">
                            <StarRating value={review.rating} />
                            <span className="pd-review-rating-score">{review.rating} / 5</span>
                          </div>

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

                          <p className="pd-review-comment">
                            {review.comment}
                          </p>

                          <div className="pd-review-actions-bar">
                            {!review.attendanceVerified ? (
                              <button
                                type="button"
                                className="pd-btn-confirm-attendance"
                                disabled={verifyingReviewId === review.id}
                                onClick={() => handleConfirmAttendance(review)}
                                title="Confirm this customer attended the scheduled booking"
                              >
                                {verifyingReviewId === review.id ? 'Confirming...' : '✓ Confirm Attendance'}
                              </button>
                            ) : (
                              <span className="pd-attendance-confirmed-tag">
                                ✓ Attendance Confirmed
                              </span>
                            )}

                            <button
                              type="button"
                              className="pd-btn-read-toggle"
                              aria-label={`Mark review by ${review.reviewerDisplayName || 'Customer'} as ${isRead ? 'unread' : 'read'}`}
                              onClick={() => toggleReviewRead(review.id)}
                            >
                              {isRead ? 'Mark as unread' : 'Mark as read'}
                            </button>

                            {feedback && (
                              <span className={`pd-attendance-feedback ${feedback.isError ? 'pd-attendance-feedback--error' : 'pd-attendance-feedback--success'}`}>
                                {feedback.text}
                              </span>
                            )}
                          </div>

                          <ProviderReviewResponse review={review} onSaved={() => setRefresh(v => v + 1)} />
                        </article>
                      )
                    })}
                  </div>
                )}

                {data.totalPages > 1 && (
                  <div
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      flexWrap: 'wrap',
                      gap: 12,
                      marginTop: 24
                    }}
                  >
                    <button
                      type="button"
                      className="pd-btn-read-toggle"
                      disabled={page === 1}
                      onClick={() => setPage(value => value - 1)}
                    >
                      Previous
                    </button>

                    <span style={{ fontSize: 13, color: '#64748b', fontWeight: 500 }}>
                      Page {page} of {data.totalPages}
                    </span>

                    <button
                      type="button"
                      className="pd-btn-read-toggle"
                      disabled={page >= data.totalPages}
                      onClick={() => setPage(value => value + 1)}
                    >
                      Next
                    </button>
                  </div>
                )}
              </>
            )}
          </>
        )}
      </section>
    </div>
  )
}