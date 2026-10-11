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

  return (
    <article className="platform-review-card">
      <div className="platform-review-author">
        {avatarUrl && (
          <img
            className="platform-review-avatar"
            src={avatarUrl}
            alt=""
          />
        )}

        <strong>{review.reviewerName}</strong>
      </div>

      <div
        className="platform-review-stars"
        aria-label={`${review.rating} out of 5 stars`}
      >
        <span aria-hidden="true">
          {'★'.repeat(review.rating)}
          {'☆'.repeat(5 - review.rating)}
        </span>
      </div>

      <p className="platform-review-content">{review.content}</p>

      <time dateTime={review.createdAtUtc}>{date}</time>
    </article>
  )
}