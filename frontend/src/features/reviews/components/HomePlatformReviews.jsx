import { useEffect, useState } from 'react'
import { getPlatformReviews } from '../../../api/platformReviewApi'
import PlatformReviewCard from './PlatformReviewCard'

export default function HomePlatformReviews() {
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

  return (
    <section className="platform-reviews" aria-label="Platform reviews">
      <div className="platform-reviews-heading">
        <h2>What our users say</h2>

        <a className="platform-reviews-link" href="/?page=platform-reviews">
          View More
        </a>
      </div>

      {loading && <p role="status">Loading reviews…</p>}
      {error && <p role="alert">{error}</p>}

      {!loading && !error && reviews.length === 0 && (
        <p>No platform reviews yet. Be the first to share your experience.</p>
      )}

      <div className="platform-review-grid">
        {reviews.map((review) => (
          <PlatformReviewCard key={review.id} review={review} />
        ))}
      </div>
    </section>
  )
}