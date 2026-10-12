import { useEffect, useState } from 'react'
import { getPlatformReviews } from '../../../api/platformReviewApi'
import PlatformReviewCard from '../components/PlatformReviewCard'
import PlatformReviewForm from '../components/PlatformReviewForm'
import '../PlatformReviews.css'

const PAGE_SIZE = 3


export default function PlatformReviewsPage({ embedded = false }) {
  const [rating, setRating] = useState('')
  const [page, setPage] = useState(1)
  const [refresh, setRefresh] = useState(0)
  const [result, setResult] = useState({ items: [], totalCount: 0 })
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const token = localStorage.getItem('authToken')
  const role = localStorage.getItem('userRole')
  const canSubmit = Boolean(token) && ['Visitor', 'Provider'].includes(role)

  useEffect(() => {
    const controller = new AbortController()

    getPlatformReviews({
      rating,
      page,
      pageSize: PAGE_SIZE,
      signal: controller.signal,
    })
      .then((data) => {
        if (!controller.signal.aborted) {
          setResult(data)
        }
      })
      .catch((requestError) => {
        if (!controller.signal.aborted) {
          setError(requestError.message)
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false)
        }
      })

    return () => controller.abort()
  }, [rating, page, refresh])

  function prepareReload() {
    setLoading(true)
    setError('')
    setResult({ items: [], totalCount: 0 })
  }

  function changeRating(event) {
    prepareReload()
    setRating(event.target.value)
    setPage(1)
  }

  function changePage(nextPage) {
    prepareReload()
    setPage(nextPage)
  }

  function handleSubmitted() {
    prepareReload()
    setRating('')
    setPage(1)
    setRefresh((value) => value + 1)
  }

  const totalPages = Math.max(
    1,
    Math.ceil(result.totalCount / PAGE_SIZE),
  )

  return (
    <main className="platform-reviews">
      {!embedded && <a href="/">Back to CeylonQuest</a>}

      <header className="platform-reviews-page-header">
        <h1>Platform Reviews</h1>
        <p>Experiences shared by CeylonQuest visitors and providers.</p>
      </header>

      {canSubmit ? (
        <PlatformReviewForm onSubmitted={handleSubmitted} />
      ) : !token ? (
        <p>
          <a href="/?page=login">Log in</a> as a Visitor or Provider to
          submit a review.
        </p>
      ) : (
        <p>Platform review submission is available to Visitors and Providers.</p>
      )}

      <label htmlFor="platform-review-filter">Filter by star rating: </label>
      <select
        id="platform-review-filter"
        value={rating}
        onChange={changeRating}
      >
        <option value="">All ratings</option>

        {[1, 2, 3, 4, 5].map((value) => (
          <option key={value} value={value}>
            {value} {value === 1 ? 'star' : 'stars'}
          </option>
        ))}
      </select>

      {loading && <p role="status">Loading reviews…</p>}

      {error && (
        <div role="alert">
          <p>{error}</p>
          <button
            type="button"
            onClick={() => {
              prepareReload()
              setRefresh((value) => value + 1)
            }}
          >
            Retry
          </button>
        </div>
      )}

      {!loading && !error && (
        <>
          {result.items.length === 0 && (
            <p>No reviews match this selection.</p>
          )}

          <div className="platform-review-grid">
            {result.items.map((review) => (
              <PlatformReviewCard key={review.id} review={review} />
            ))}
          </div>

          <nav
            className="platform-review-pagination"
            aria-label="Review pages"
          >
            <button
              type="button"
              disabled={page === 1}
              onClick={() => changePage(page - 1)}
            >
              Previous
            </button>

            <span>Page {page} of {totalPages}</span>

            <button
              type="button"
              disabled={page >= totalPages}
              onClick={() => changePage(page + 1)}
            >
              Next
            </button>
          </nav>
        </>
      )}
    </main>
  )
}
