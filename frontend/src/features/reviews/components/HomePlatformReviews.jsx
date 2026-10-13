import { useEffect, useState } from 'react'
import { getPlatformReviews } from '../../../api/platformReviewApi'
import PlatformReviewCard from './PlatformReviewCard'

export default function HomePlatformReviews({ onViewReviews }) {
  const [reviews, setReviews] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    const controller = new AbortController()

    getPlatformReviews({
      pageSize: 3,
      signal: controller.signal,
    })
      .then((result) => setReviews(result.items))
      .catch((requestError) => {
        if (requestError.name !== 'AbortError') {
          setError(requestError.message)
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false)
        }
      })

    return () => controller.abort()
  }, [])

  const handleViewAll = (e) => {
    if (onViewReviews) {
      e.preventDefault()
      onViewReviews()
    }
  }

  return (
    <section className="home-platform-reviews-section" aria-label="Platform reviews">
      <div className="home-platform-reviews-inner">
        <div className="platform-reviews-heading">
          <div>
            <p className="platform-reviews-eyebrow">Community Voices</p>
            <h2 className="platform-reviews-title">What our users say</h2>
          </div>

          <a
            className="platform-reviews-view-all"
            href="/?page=platform-reviews"
            onClick={handleViewAll}
          >
            Explore all reviews &rarr;
          </a>
        </div>

        {loading && <p role="status" className="platform-reviews-loading">Loading community reviews…</p>}
        {error && <p role="alert" className="platform-reviews-error-msg">{error}</p>}

        {!loading && !error && reviews.length === 0 && (
          <p className="platform-reviews-empty">No platform reviews yet. Be the first to share your experience.</p>
        )}

        <div className="platform-review-grid">
          {reviews.map((review) => (
            <PlatformReviewCard key={review.id} review={review} />
          ))}
        </div>
      </div>
    </section>
  )
}