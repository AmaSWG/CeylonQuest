import { useEffect, useState } from 'react'
import { bookingUrl } from '../../../api/client'
import './AdminProviderPerformanceReportTab.css'

const DEFAULT_FILTERS = {
  startDate: '',
  endDate: '',
  category: '',
  minimumRating: '',
}

const STAR_FIELDS = [
  'oneStar',
  'twoStars',
  'threeStars',
  'fourStars',
  'fiveStars',
]

const currency = new Intl.NumberFormat('en-LK', {
  style: 'currency',
  currency: 'LKR',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

function StarDistribution({ breakdown, reviewCount }) {
  return (
    <div className="ppr-distribution">
      {STAR_FIELDS.map((field, index) => {
        const count = breakdown?.[field] ?? 0
        const percentage = reviewCount > 0
          ? (count / reviewCount) * 100
          : 0

        return (
          <div className="ppr-star-row" key={field}>
            <span>{index + 1} ★</span>

            <div className="ppr-star-track" aria-hidden="true">
              <div
                className="ppr-star-fill"
                style={{ width: `${percentage}%` }}
              />
            </div>

            <span className="ppr-star-count">{count}</span>
          </div>
        )
      })}
    </div>
  )
}

export default function AdminProviderPerformanceReportTab({
  token,
  onLogout,
}) {
  const [filters, setFilters] = useState({ ...DEFAULT_FILTERS })
  const [appliedFilters, setAppliedFilters] = useState({
    ...DEFAULT_FILTERS,
  })
  const [sortBy, setSortBy] = useState('Revenue')
  const [sortDirection, setSortDirection] = useState('Desc')
  const [reloadKey, setReloadKey] = useState(0)

  const [providers, setProviders] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [validationError, setValidationError] = useState('')

  useEffect(() => {
    const controller = new AbortController()

    async function loadReport() {
      setLoading(true)
      setError('')
      setProviders([])

      try {
        if (!token) {
          throw new Error('Please sign in as an administrator.')
        }

        const params = new URLSearchParams({
          SortBy: sortBy,
          SortDirection: sortDirection,
        })

        const queryFields = {
          StartDate: appliedFilters.startDate,
          EndDate: appliedFilters.endDate,
          Category: appliedFilters.category,
          MinimumRating: appliedFilters.minimumRating,
        }

        Object.entries(queryFields).forEach(([key, value]) => {
          if (value !== '') {
            params.set(key, value)
          }
        })

        const response = await fetch(
          bookingUrl(
            `/api/admin/reports/provider-performance?${params}`
          ),
          {
            headers: {
              Authorization: `Bearer ${token}`,
              Accept: 'application/json',
            },
            signal: controller.signal,
          }
        )

        if (controller.signal.aborted) return

        if (response.status === 401) {
          onLogout?.()
          throw new Error('Your session has expired. Please sign in again.')
        }

        if (response.status === 403) {
          throw new Error(
            'You do not have permission to view this report.'
          )
        }

        const data = await response.json().catch(() => null)

        if (controller.signal.aborted) return

        if (!response.ok) {
          const validationMessages = data?.errors
            ? Object.values(data.errors).flat().join(' ')
            : ''

          throw new Error(
            data?.message ||
            validationMessages ||
            data?.title ||
            'Unable to load the provider performance report.'
          )
        }

        if (!Array.isArray(data?.providers)) {
          throw new Error('The report API returned an invalid response.')
        }

        setProviders(data.providers)
      } catch (requestError) {
        if (!controller.signal.aborted) {
          setError(
            requestError.message ||
            'Unable to load the provider performance report.'
          )
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false)
        }
      }
    }

    loadReport()

    return () => controller.abort()
  }, [
    token,
    onLogout,
    appliedFilters,
    sortBy,
    sortDirection,
    reloadKey,
  ])

  function changeFilter(event) {
    const { name, value } = event.target

    setFilters(current => ({
      ...current,
      [name]: value,
    }))

    setValidationError('')
  }

  function applyFilters(event) {
    event.preventDefault()

    if (
      filters.startDate &&
      filters.endDate &&
      filters.startDate > filters.endDate
    ) {
      setValidationError(
        'End date must be on or after the start date.'
      )
      return
    }

    if (
      filters.minimumRating !== '' &&
      (
        !Number.isFinite(Number(filters.minimumRating)) ||
        Number(filters.minimumRating) < 0 ||
        Number(filters.minimumRating) > 5
      )
    ) {
      setValidationError('Minimum rating must be between 0 and 5.')
      return
    }

    setValidationError('')
    setAppliedFilters({ ...filters })
  }

  function resetFilters() {
    setFilters({ ...DEFAULT_FILTERS })
    setAppliedFilters({ ...DEFAULT_FILTERS })
    setSortBy('Revenue')
    setSortDirection('Desc')
    setValidationError('')
  }

  return (
    <section className="ppr-report">
      <div className="ad-page-header">
        <div className="ad-page-header__left">
          <h1>Provider Performance &amp; Reviews</h1>
          <p>
            Monitor provider revenue, customer ratings, and review activity.
          </p>
        </div>
      </div>

      <form
        className="ppr-filters"
        onSubmit={applyFilters}
        aria-label="Provider report filters"
      >
        <label className="ppr-field" htmlFor="ppr-start-date">
          <span>Start date</span>
          <input
            id="ppr-start-date"
            name="startDate"
            type="date"
            value={filters.startDate}
            onChange={changeFilter}
          />
        </label>

        <label className="ppr-field" htmlFor="ppr-end-date">
          <span>End date</span>
          <input
            id="ppr-end-date"
            name="endDate"
            type="date"
            min={filters.startDate || undefined}
            value={filters.endDate}
            onChange={changeFilter}
          />
        </label>

        <label className="ppr-field" htmlFor="ppr-category">
          <span>Category</span>
          <select
            id="ppr-category"
            name="category"
            value={filters.category}
            onChange={changeFilter}
          >
            <option value="">All categories</option>
            <option value="Experience">Experience</option>
            <option value="Restaurant">Restaurant</option>
            <option value="Accommodation">Accommodation</option>
          </select>
        </label>

        <label className="ppr-field" htmlFor="ppr-minimum-rating">
          <span>Minimum rating</span>
          <input
            id="ppr-minimum-rating"
            name="minimumRating"
            type="number"
            min="0"
            max="5"
            step="0.1"
            placeholder="Any rating"
            value={filters.minimumRating}
            onChange={changeFilter}
          />
        </label>

        <div className="ppr-actions">
          <button type="submit" className="ppr-button ppr-button--primary">
            Apply filters
          </button>

          <button
            type="button"
            className="ppr-button"
            onClick={resetFilters}
          >
            Reset
          </button>
        </div>

        {validationError && (
          <p className="ppr-validation" role="alert">
            {validationError}
          </p>
        )}

        <p className="ppr-filter-note">
          Leave dates blank for all time. Revenue uses booking,
          reservation, or check-in dates; reviews use submission
          dates in UTC. A minimum rating excludes providers with no reviews.
        </p>
      </form>

      <div className="ppr-toolbar">
        <p role="status" aria-live="polite">
          {loading
            ? 'Loading report…'
            : error
              ? 'Report unavailable'
              : `${providers.length} ${
                  providers.length === 1 ? 'provider' : 'providers'
                } found`}
        </p>

        <div className="ppr-sort-controls">
          <label className="ppr-field" htmlFor="ppr-sort-by">
            <span>Sort by</span>
            <select
              id="ppr-sort-by"
              value={sortBy}
              onChange={event => setSortBy(event.target.value)}
            >
              <option value="Revenue">Revenue</option>
              <option value="AverageRating">Average rating</option>
            </select>
          </label>

          <label className="ppr-field" htmlFor="ppr-sort-direction">
            <span>Order</span>
            <select
              id="ppr-sort-direction"
              value={sortDirection}
              onChange={event => setSortDirection(event.target.value)}
            >
              <option value="Desc">Highest first</option>
              <option value="Asc">Lowest first</option>
            </select>
          </label>
        </div>
      </div>

      <div aria-busy={loading}>
        {loading && (
          <div className="ppr-state">
            <div className="ad-spinner" aria-hidden="true" />
            <p>Loading provider performance data…</p>
          </div>
        )}

        {!loading && error && (
          <div className="ppr-state ppr-state--error" role="alert">
            <p>{error}</p>
            <button
              type="button"
              className="ppr-button"
              onClick={() => setReloadKey(current => current + 1)}
            >
              Try again
            </button>
          </div>
        )}

        {!loading && !error && providers.length === 0 && (
          <div className="ppr-state">
            <h2>No providers match the selected filters</h2>
            <p>Try a wider date range or reset the filters.</p>
            <button
              type="button"
              className="ppr-button"
              onClick={resetFilters}
            >
              Reset filters
            </button>
          </div>
        )}

        {!loading && !error && providers.length > 0 && (
          <div
            className="ppr-table-wrapper"
            role="region"
            aria-label="Provider performance results"
            tabIndex={0}
          >
            <table className="cq-report-table ppr-table">
              <caption className="ppr-visually-hidden">
                Provider revenue, review counts, average ratings,
                and one to five star review distribution
              </caption>

              <thead>
                <tr>
                  <th scope="col">Provider</th>
                  <th scope="col">Revenue</th>
                  <th scope="col">Reviews</th>
                  <th scope="col">Average rating</th>
                  <th scope="col">Rating status</th>
                  <th scope="col">1–5 star distribution</th>
                </tr>
              </thead>

              <tbody>
                {providers.map(provider => {
                  const noReviews = provider.totalReviewCount === 0
                  const lowRating =
                    !noReviews &&
                    provider.averageRating != null &&
                    provider.averageRating < 3

                  return (
                    <tr
                      key={provider.providerId}
                      className={lowRating ? 'ppr-row--low' : ''}
                    >
                      <th scope="row">
                        <span className="ppr-provider-name">
                          {provider.providerName || 'Unnamed provider'}
                        </span>
                        <span className="ppr-provider-categories">
                          {(provider.categories || []).join(', ') ||
                            'No category available'}
                        </span>
                      </th>

                      <td className="ppr-number">
                        {currency.format(provider.totalRevenue)}
                      </td>

                      <td className="ppr-number">
                        {provider.totalReviewCount}
                      </td>

                      <td className="ppr-number">
                        {noReviews || provider.averageRating == null
                          ? '—'
                          : `${Number(
                              provider.averageRating
                            ).toFixed(2)} / 5`}
                      </td>

                      <td>
                        <span
                          className={`ppr-badge ${
                            noReviews
                              ? 'ppr-badge--unreviewed'
                              : lowRating
                                ? 'ppr-badge--low'
                                : 'ppr-badge--rated'
                          }`}
                        >
                          {noReviews
                            ? 'No Reviews'
                            : lowRating
                              ? 'Low Rating'
                              : 'Rated'}
                        </span>
                      </td>

                      <td>
                        <StarDistribution
                          breakdown={provider.starRatingBreakdown}
                          reviewCount={provider.totalReviewCount}
                        />
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </section>
  )
}