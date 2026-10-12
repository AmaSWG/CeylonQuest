import { useEffect, useState } from 'react'
import { getReviews } from '../../../api/reviewApi'
import StarRating from '../../../components/StarRating'
import ProviderReviewResponse from '../../../components/ProviderReviewResponse'

export default function ProviderReviewsTab({ services = [], bookingType }) {
  const [selectedId, setSelectedId] = useState('')
  const [page, setPage] = useState(1)
  const [refresh, setRefresh] = useState(0)
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

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

  return (
    <div className="pd-notifications-view">
      <div className="pd-page-header">
        <div className="pd-page-header__left">
          <h1>Reviews &amp; Ratings</h1>
          <p>View customer feedback and respond to reviews.</p>
        </div>
      </div>

      <section
        aria-labelledby="provider-reviews-title"
        style={{
          background: '#fff',
          border: '1px solid #e0e8e3',
          borderRadius: 14,
          padding: 24,
          textAlign: 'left'
        }}
      >
        <h2
          id="provider-reviews-title"
          style={{ margin: '0 0 20px', color: '#123b5d' }}
        >
          Customer Reviews &amp; Ratings
        </h2>

        {services.length === 0 ? (
          <p>No listings available.</p>
        ) : (
          <>
            <label
              htmlFor="provider-review-listing"
              style={{ display: 'block', marginBottom: 8 }}
            >
              Select listing
            </label>

            <select
              id="provider-review-listing"
              value={listingId || ''}
              onChange={event => {
                setSelectedId(event.target.value)
                setPage(1)
              }}
              style={{
                width: '100%',
                maxWidth: 440,
                padding: '10px 12px',
                border: '1px solid #c9dcd3',
                borderRadius: 8,
                marginBottom: 20,
                font: 'inherit'
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

            {loading && <p role="status">Loading reviews…</p>}

            {error && (
              <div role="alert">
                <p style={{ color: '#b42318' }}>{error}</p>
                <button
                  type="button"
                  className="pd-btn-secondary"
                  onClick={() => setRefresh(value => value + 1)}
                >
                  Retry
                </button>
              </div>
            )}

            {!loading && !error && data && (
              <>
                <p style={{ color: '#527063', marginBottom: 20 }}>
                  <strong>
                    {Number(data.averageRating).toFixed(2)} / 5
                  </strong>
                  {' · '}
                  {data.reviewCount} review
                  {data.reviewCount === 1 ? '' : 's'}
                </p>

                {data.items.length === 0 ? (
                  <p>No customer reviews for this listing yet.</p>
                ) : (
                  <div style={{ display: 'grid', gap: 14 }}>
                    {data.items.map(review => {
                      const isRead = readReviewIds.includes(review.id)

                      return (
                        <article
                          key={review.id}
                          style={{
                            background: isRead ? '#fff' : '#f0faf4',
                            border: '1px solid #e0e8e3',
                            borderLeft: `4px solid ${isRead ? '#e0e8e3' : '#23825d'}`,
                            borderRadius: 12,
                            padding: 20,
                            textAlign: 'left',
                            overflowWrap: 'anywhere'
                          }}
                        >
                          <div
                            style={{
                              display: 'flex',
                              justifyContent: 'space-between',
                              alignItems: 'center',
                              flexWrap: 'wrap',
                              gap: 12
                            }}
                          >
                            <strong style={{ color: '#123b5d', fontSize: 17 }}>
                              {review.reviewerDisplayName || 'Customer'}
                            </strong>

                            <span style={{ color: '#527063', fontSize: 13 }}>
                              {isRead ? 'Read' : 'Unread'}
                              {' · '}
                              {new Date(review.createdAtUtc).toLocaleDateString(
                                'en-GB',
                                {
                                  day: 'numeric',
                                  month: 'short',
                                  year: 'numeric',
                                  timeZone: 'Asia/Colombo'
                                }
                              )}
                            </span>
                          </div>

                          <div
                            style={{
                              display: 'flex',
                              alignItems: 'center',
                              gap: 10,
                              margin: '12px 0'
                            }}
                          >
                            <StarRating value={review.rating} />
                            <strong>{review.rating} / 5</strong>
                          </div>

                          <p
                            style={{
                              margin: '0 0 16px',
                              lineHeight: 1.6,
                              color: '#23443a',
                              whiteSpace: 'pre-wrap'
                            }}
                          >
                            {review.comment}
                          </p>

                          <button
                            type="button"
                            className="pd-btn-secondary"
                            aria-label={`Mark review by ${review.reviewerDisplayName || 'Customer'
                              } as ${isRead ? 'unread' : 'read'}`}
                            onClick={() => toggleReviewRead(review.id)}
                          >
                            {isRead ? 'Mark as unread' : 'Mark as read'}
                          </button>
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
                      marginTop: 20
                    }}
                  >
                    <button
                      type="button"
                      className="pd-btn-secondary"
                      disabled={page === 1}
                      onClick={() => setPage(value => value - 1)}
                    >
                      Previous
                    </button>

                    <span>
                      Page {page} of {data.totalPages}
                    </span>

                    <button
                      type="button"
                      className="pd-btn-secondary"
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