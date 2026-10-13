import { useEffect, useState } from 'react'
import { getPlatformReviews } from '../../../api/platformReviewApi'
import PlatformReviewCard from '../components/PlatformReviewCard'
import PlatformReviewForm from '../components/PlatformReviewForm'
import HomeFooter from '../../home/components/HomeFooter'
import '../PlatformReviews.css'

const PAGE_SIZE = 6

export default function PlatformReviewsPage({
  embedded = false,
  onHome,
  onLogin,
  onRegister,
}) {
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
    if (embedded) {
      setLoading(false)
      return
    }

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
  }, [rating, page, refresh, embedded])

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

  const handleBackHome = (e) => {
    if (onHome) {
      e.preventDefault()
      onHome()
    }
  }

  const handleLoginClick = (e) => {
    if (onLogin) {
      e.preventDefault()
      onLogin()
    }
  }

  const handleRegisterClick = (e) => {
    if (onRegister) {
      e.preventDefault()
      onRegister()
    }
  }

  const totalPages = Math.max(
    1,
    Math.ceil(result.totalCount / PAGE_SIZE),
  )

  const content = (
    <>
      <header className="platform-reviews-page-header">
        {!embedded && <p className="platform-reviews-eyebrow">Community Voices</p>}
        <h1>{embedded ? 'Platform Review' : 'Platform Reviews'}</h1>
        <p className="platform-reviews-subtitle">
          {embedded
            ? 'Share your feedback and rating about your experience with CeylonQuest.'
            : 'Real experiences and ratings shared by CeylonQuest travelers and verified providers.'}
        </p>
      </header>

      {/* Review Submission Form or Auth Callout */}
      {canSubmit ? (
        <PlatformReviewForm onSubmitted={handleSubmitted} />
      ) : !token ? (
        <div className="platform-reviews-auth-card">
          <div className="platform-reviews-auth-info">
            <span className="platform-reviews-auth-badge">Share Your Feedback</span>
            <h3>Traveled or hosted with CeylonQuest?</h3>
            <p>Sign in to your account to submit a review and share your experience with the community.</p>
          </div>
          <div className="platform-reviews-auth-actions">
            <button
              type="button"
              className="platform-reviews-auth-login-btn"
              onClick={handleLoginClick}
            >
              Log In to Review
            </button>
            <button
              type="button"
              className="platform-reviews-auth-register-btn"
              onClick={handleRegisterClick}
            >
              Get Started
            </button>
          </div>
        </div>
      ) : (
        <div className="platform-reviews-role-banner">
          <p>Platform review submission is currently open to active Visitors and Providers.</p>
        </div>
      )}

      {/* Public reviews feed - only on standalone public page */}
      {!embedded && (
        <section className="platform-reviews-feed-section" aria-label="Customer Reviews">
          <div className="platform-review-filter-wrap">
            <label htmlFor="platform-review-filter" className="platform-review-filter-label">
              Filter by rating:
            </label>
            <select
              id="platform-review-filter"
              className="platform-review-filter-select"
              value={rating}
              onChange={changeRating}
            >
              <option value="">All ratings (All reviews)</option>
              {[5, 4, 3, 2, 1].map((value) => (
                <option key={value} value={value}>
                  {'★'.repeat(value)} {value} {value === 1 ? 'star' : 'stars'}
                </option>
              ))}
            </select>
          </div>

          {loading && (
            <div className="platform-reviews-loading-state" role="status">
              <p>Loading community reviews…</p>
            </div>
          )}

          {error && (
            <div className="platform-reviews-error-alert" role="alert">
              <p>{error}</p>
              <button
                type="button"
                className="platform-review-retry-btn"
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
              {result.items.length === 0 ? (
                <div className="platform-reviews-empty-state">
                  <p>No reviews match this selection yet.</p>
                </div>
              ) : (
                <div className="platform-review-grid">
                  {result.items.map((review) => (
                    <PlatformReviewCard key={review.id} review={review} />
                  ))}
                </div>
              )}

              {totalPages > 1 && (
                <nav
                  className="platform-review-pagination"
                  aria-label="Review pages"
                >
                  <button
                    type="button"
                    disabled={page === 1}
                    onClick={() => changePage(page - 1)}
                  >
                    &larr; Previous
                  </button>

                  <span className="platform-review-page-indicator">
                    Page {page} of {totalPages}
                  </span>

                  <button
                    type="button"
                    disabled={page >= totalPages}
                    onClick={() => changePage(page + 1)}
                  >
                    Next &rarr;
                  </button>
                </nav>
              )}
            </>
          )}
        </section>
      )}
    </>
  )

  if (embedded) {
    return <div className="platform-reviews platform-reviews--embedded">{content}</div>
  }

  return (
    <div className="platform-reviews-page">
      {/* Top Navbar */}
      <nav className="platform-reviews-nav" role="navigation" aria-label="Page navigation">
        <div className="platform-reviews-nav__inner">
          <a
            href="/"
            onClick={handleBackHome}
            className="platform-reviews-nav__brand"
            aria-label="Back to CeylonQuest Home"
          >
            <img src="/logo.png" alt="CeylonQuest" />
          </a>

          <div className="platform-reviews-nav__actions">
            <a
              href="/"
              onClick={handleBackHome}
              className="platform-reviews-nav__back"
            >
              &larr; Back to Home
            </a>

            {!token ? (
              <>
                <button
                  type="button"
                  className="platform-reviews-nav__login"
                  onClick={handleLoginClick}
                >
                  Login
                </button>
                <button
                  type="button"
                  className="platform-reviews-nav__cta"
                  onClick={handleRegisterClick}
                >
                  Get Started
                </button>
              </>
            ) : (
              <span className="platform-reviews-nav__role-badge">
                Signed in as {role}
              </span>
            )}
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="platform-reviews-main">
        {content}
      </main>

      {/* Page Footer */}
      <HomeFooter onLogin={handleLoginClick} onRegister={handleRegisterClick} />
    </div>
  )
}
