import { apiUrl } from '../../../api/client'
import '../PlatformReviews.css'

export default function PlatformReviewCard({ review }) {
  const avatar = review.profilePictureUrl

  const avatarUrl = avatar?.startsWith('/uploads/')
    ? apiUrl(avatar)
    : avatar

  const date = new Intl.DateTimeFormat('en-LK', {
    dateStyle: 'medium',
    timeZone: 'Asia/Colombo',
  }).format(new Date(review.createdAtUtc))

  const initial = (review.reviewerName || 'U').trim().charAt(0).toUpperCase()

  return (
    <article className="platform-review-card">
      <div className="platform-review-author">
        {avatarUrl ? (
          <img
            className="platform-review-avatar"
            src={avatarUrl}
            alt={review.reviewerName || 'Reviewer'}
          />
        ) : (
          <div className="platform-review-avatar-placeholder" aria-hidden="true">
            {initial}
          </div>
        )}

        <div className="platform-review-author-info">
          <strong>{review.reviewerName || 'Anonymous Traveler'}</strong>
          <span className="platform-review-author-verified">Verified Community Member</span>
        </div>
      </div>

      <div
        className="platform-review-stars"
        aria-label={`${review.rating} out of 5 stars`}
      >
        <span aria-hidden="true">
          {'★'.repeat(review.rating)}
          <span className="platform-review-stars-empty">{'★'.repeat(5 - review.rating)}</span>
        </span>
      </div>

      <p className="platform-review-content">{review.content}</p>

      <div className="platform-review-card-footer">
        <time dateTime={review.createdAtUtc}>{date}</time>
      </div>
    </article>
  )
}