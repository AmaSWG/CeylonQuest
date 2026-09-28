import { useEffect, useState } from 'react'
import { bookingUrl } from '../../../api/client'

// ── Constants ────────────────────────────────────────────────────────────────

const EMPTY = {
  startDate: '',
  endDate: '',
  status: '',
  bookingType: '',
  providerId: '',
}

const TYPES = ['Experience', 'Restaurant', 'Accommodation']

const STATUSES = [
  'PendingPayment',
  'Confirmed',
  'Cancelled',
  'Completed',
]

// ── Formatters ───────────────────────────────────────────────────────────────

const money = value =>
  new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    maximumFractionDigits: 0,
  }).format(Number(value) || 0)

const pct = value => `${Number(value || 0).toFixed(2)}%`

const statusLabel = value =>
  value === 'PendingPayment' ? 'Pending Payment' : value

// ── Sub-components ──────────────────────────────────────────────────────────

function KpiCard({ label, value, iconColor = 'blue' }) {
  return (
    <div className="cq-kpicard">
      <div
        className={`cq-kpicard__icon cq-kpicard__icon--${iconColor}`}
        aria-hidden="true"
      >
        <span className="brr-kpi-icon">📊</span>
      </div>

      <div className="cq-kpicard__info">
        <span className="cq-kpicard__label">{label}</span>
        <span className="cq-kpicard__val brr-kpi-val" title={String(value)}>
          {value}
        </span>
      </div>
    </div>
  )
}

function HighlightCard({ label, value }) {
  return (
    <div className="cq-report-box brr-highlight-card">
      <p className="cq-kpicard__label">{label}</p>
      <p className="brr-highlight-val" title={value || '—'}>
        {value || '—'}
      </p>
    </div>
  )
}

function Distribution({ title, items }) {
  if (!items || items.length === 0) {
    return (
      <div className="cq-report-box">
        <h3 className="cq-report-box__title">{title}</h3>
        <div className="cq-report-empty">
          No data available.
        </div>
      </div>
    )
  }

  const max = Math.max(
    ...items.map(item => Number(item.count) || 0),
    1
  )

  return (
    <div className="cq-report-box">
      <h3 className="cq-report-box__title">{title}</h3>

      <div className="brr-dist">
        {items.map(item => {
          const count = Number(item.count) || 0
          const percentage = Number(item.percentage) || 0

          return (
            <div className="brr-dist__row" key={item.name}>
              <div className="brr-dist__meta">
                <span className="brr-dist__name">
                  {statusLabel(item.name)}
                </span>

                <span className="brr-dist__nums">
                  <strong>{count}</strong>
                  <small>{pct(percentage)}</small>
                </span>
              </div>

              <div
                className="brr-dist__track"
                title={`${count} (${pct(percentage)})`}
              >
                <div
                  className="brr-dist__fill"
                  style={{
                    width: `${(count / max) * 100}%`,
                  }}
                />
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}

function RevenueByType({ items }) {
  if (!items || items.length === 0) {
    return (
      <div className="cq-report-empty">
        No revenue data available.
      </div>
    )
  }

  const max = Math.max(
    ...items.map(item => Number(item.revenue) || 0),
    1
  )

  return (
    <div className="brr-dist">
      {items.map(item => {
        const revenue = Number(item.revenue) || 0
        const bookingCount = Number(item.bookingCount) || 0

        return (
          <div
            className="brr-dist__row"
            key={item.bookingType}
          >
            <div className="brr-dist__meta">
              <span className="brr-dist__name">
                {item.bookingType}
              </span>

              <span className="brr-dist__nums">
                <strong>{money(revenue)}</strong>
                <small>
                  {bookingCount}{' '}
                  {bookingCount === 1 ? 'booking' : 'bookings'}
                </small>
              </span>
            </div>

            <div
              className="brr-dist__track"
              title={`${item.bookingType}: ${money(revenue)}`}
            >
              <div
                className="brr-dist__fill brr-dist__fill--gold"
                style={{
                  width: `${(revenue / max) * 100}%`,
                }}
              />
            </div>
          </div>
        )
      })}
    </div>
  )
}

// ── Main Component ──────────────────────────────────────────────────────────

export default function AdminBookingsRevenueReportTab({
  token,
  onLogout,
}) {
  const [filters, setFilters] = useState({ ...EMPTY })
  const [request, setRequest] = useState({ ...EMPTY })

  const [report, setReport] = useState(null)

  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [validationError, setValidationError] = useState('')

  // ── Load Report ───────────────────────────────────────────────────────────

  useEffect(() => {
    const controller = new AbortController()

    async function load() {
      if (!token) {
        onLogout?.()
        return
      }

      setLoading(true)
      setError('')

      const params = new URLSearchParams()

      Object.entries(request).forEach(([key, value]) => {
        if (value) {
          params.set(key, value)
        }
      })

      const query = params.toString()

      try {
        const response = await fetch(
          bookingUrl(
            `/api/admin/reports/bookings-revenue${query ? `?${query}` : ''
            }`
          ),
          {
            headers: {
              Authorization: `Bearer ${token}`,
              Accept: 'application/json',
            },
            signal: controller.signal,
          }
        )

        if (response.status === 401) {
          onLogout?.()
          return
        }

        const data = await response
          .json()
          .catch(() => null)

        if (!response.ok) {
          if (response.status === 400) {
            setError(
              data?.message ||
              'The selected filters are invalid.'
            )
          } else if (response.status === 403) {
            setError(
              'You are not authorized to access admin reports.'
            )
          } else if (response.status === 503) {
            setError(
              'The booking report is temporarily unavailable.'
            )
          } else {
            setError('Unable to load the report.')
          }

          return
        }

        setReport(data)
      } catch (err) {
        if (err.name !== 'AbortError') {
          setError(
            'Unable to reach the reporting service. Check your connection and try again.'
          )
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false)
        }
      }
    }

    load()

    return () => controller.abort()
  }, [request, token, onLogout])

  // ── Filter Handlers ───────────────────────────────────────────────────────

  const change = event => {
    const { name, value } = event.target

    const next = {
      ...filters,
      [name]: value,
    }
    setFilters(next)
    setValidationError(
      next.startDate && next.endDate && next.endDate < next.startDate
        ? 'End date cannot be earlier than start date.'
        : ''
    )
  }

  const apply = event => {
    event.preventDefault()

    if (
      filters.startDate &&
      filters.endDate &&
      filters.startDate > filters.endDate
    ) {
      setValidationError(
        'End date cannot be earlier than start date.'
      )
      return
    }

    setRequest({ ...filters })
    setValidationError('')
  }

  const reset = () => {
    setFilters({ ...EMPTY })
    setRequest({ ...EMPTY })
    setValidationError('')
  }

  // ── Render ────────────────────────────────────────────────────────────────

  return (
    <div className="cq-inv-report">

      {/* ── Page Header ─────────────────────────────────────────────────── */}

      <div className="pd-page-header pd-mb-16">
        <div className="pd-page-header__left">
          <h1 className="cq-report-header-title">
            Bookings &amp; Revenue Report
          </h1>


        </div>
      </div>

      {/* ── Filter Card ─────────────────────────────────────────────────── */}

      <div className="cq-report-filtercard">
        <form
          onSubmit={apply}
          className="cq-report-filterform"
          aria-label="Booking report filters"
        >

          {/* Start Date */}

          <div className="cq-report-field">
            <label
              className="cq-report-label"
              htmlFor="brr-start"
            >
              Start Date
            </label>

            <input
              id="brr-start"
              className="cq-report-input"
              type="date"
              name="startDate"
              value={filters.startDate}
              onChange={change}
            />
          </div>

          {/* End Date */}

          <div className="cq-report-field">
            <label
              className="cq-report-label"
              htmlFor="brr-end"
            >
              End Date
            </label>

            <input
              id="brr-end"
              className="cq-report-input"
              type="date"
              name="endDate"
              min={filters.startDate || undefined}
              value={filters.endDate}
              onChange={change}
            />
          </div>

          {/* Booking Type */}

          <div className="cq-report-field">
            <label
              className="cq-report-label"
              htmlFor="brr-type"
            >
              Booking Type
            </label>

            <select
              id="brr-type"
              className="cq-report-select"
              name="bookingType"
              value={filters.bookingType}
              onChange={change}
            >
              <option value="">All</option>

              {TYPES.map(type => (
                <option
                  key={type}
                  value={type}
                >
                  {type}
                </option>
              ))}
            </select>
          </div>

          {/* Booking Status */}

          <div className="cq-report-field">
            <label
              className="cq-report-label"
              htmlFor="brr-status"
            >
              Booking Status
            </label>

            <select
              id="brr-status"
              className="cq-report-select"
              name="status"
              value={filters.status}
              onChange={change}
            >
              <option value="">All</option>

              {STATUSES.map(status => (
                <option
                  key={status}
                  value={status}
                >
                  {statusLabel(status)}
                </option>
              ))}
            </select>
          </div>

          {/* Provider */}

          <div className="cq-report-field">
            <label
              className="cq-report-label"
              htmlFor="brr-provider"
            >
              Provider
            </label>

            <select
              id="brr-provider"
              className="cq-report-select"
              name="providerId"
              value={filters.providerId}
              onChange={change}
            >
              <option value="">All</option>

              {(report?.providers || []).map(provider => (
                <option
                  key={provider.providerId}
                  value={provider.providerId}
                >
                  {provider.providerName}
                </option>
              ))}
            </select>
          </div>

          {/* Buttons */}

          <div className="cq-report-actions">
            <button
              type="submit"
              className="cq-report-btn cq-report-btn--primary"
              disabled={loading}
            >
              Apply Filters
            </button>

            <button
              type="button"
              className="cq-report-btn cq-report-btn--secondary"
              onClick={reset}
              disabled={loading}
            >
              Reset
            </button>
          </div>

          {/* Validation Error */}

          {validationError && (
            <p
              className="brr-validation"
              role="alert"
            >
              {validationError}
            </p>
          )}
        </form>
      </div>

      {/* ── Loading ─────────────────────────────────────────────────────── */}

      {loading && (
        <div className="ad-loading">
          <div className="ad-spinner" />

          <p>
            Generating booking report…
          </p>
        </div>
      )}

      {/* ── Error ───────────────────────────────────────────────────────── */}

      {error && (
        <div
          className="cq-report-alert cq-report-alert--danger"
          role="alert"
        >
          {error}
        </div>
      )}

      {/* ── Report ──────────────────────────────────────────────────────── */}

      {!loading && !error && report && (
        <>

          {/* ── KPI Cards ─────────────────────────────────────────────── */}

          <div className="cq-report-kpigrid brr-kpigrid-4">
            <KpiCard
              label="Total Bookings"
              value={Number(
                report.totalBookings || 0
              ).toLocaleString('en-LK')}
              iconColor="blue"
            />

            <KpiCard
              label="Total Revenue"
              value={money(report.totalRevenue)}
              iconColor="green"
            />


          </div>

          {/* ── Highlight Cards ───────────────────────────────────────── */}

          <div className="brr-highlights">
            <HighlightCard
              label="Most Booked Service"
              value={
                report.mostBookedService?.serviceName
              }
            />

            <HighlightCard
              label="Top Provider"
              value={
                report.topProviderByBookings
                  ?.providerName
              }
            />
          </div>

          {/* ── No Data ───────────────────────────────────────────────── */}

          {!report.hasData && (
            <div
              className="brr-empty-state"
              role="status"
            >
              {report.message ||
                'No data available for the selected criteria.'}
            </div>
          )}

          {/* ── Report Details ────────────────────────────────────────── */}

          {report.hasData && (
            <>

              {/* Booking Type + Status Distribution */}

              <div className="brr-grid-2">
                <Distribution
                  title="Booking Type Distribution"
                  items={
                    report.bookingTypeDistribution
                  }
                />

                <Distribution
                  title="Status Distribution"
                  items={report.statusDistribution}
                />
              </div>

              {/* Revenue by Booking Type + Top Services */}

              <div className="brr-grid-2">

                {/* Revenue by Booking Type */}

                <div className="cq-report-box">
                  <h3 className="cq-report-box__title">
                    Revenue by Booking Type
                  </h3>

                  <RevenueByType
                    items={
                      report.revenueByBookingType
                    }
                  />
                </div>

                {/* Top 5 Services */}

                <div className="cq-report-box">
                  <h3 className="cq-report-box__title">
                    Top 5 Services
                  </h3>

                  {report.topServices?.length > 0 ? (
                    <div className="cq-table-wrapper">
                      <table className="cq-report-table">
                        <thead>
                          <tr>
                            <th>Service</th>
                            <th>Type</th>
                            <th>Bookings</th>
                            <th>Revenue</th>
                          </tr>
                        </thead>

                        <tbody>
                          {report.topServices.map(
                            (service, index) => (
                              <tr
                                key={
                                  service.serviceId ||
                                  `${service.serviceName}-${index}`
                                }
                              >
                                <td
                                  className="brr-svc-name"
                                  title={
                                    service.serviceName
                                  }
                                >
                                  {
                                    service.serviceName
                                  }
                                </td>

                                <td>
                                  <span className="cq-tag-cat">
                                    {
                                      service.bookingType
                                    }
                                  </span>
                                </td>

                                <td>
                                  {Number(
                                    service.totalBookings ||
                                    0
                                  ).toLocaleString(
                                    'en-LK'
                                  )}
                                </td>

                                <td>
                                  {money(
                                    service.totalRevenue
                                  )}
                                </td>
                              </tr>
                            )
                          )}
                        </tbody>
                      </table>
                    </div>
                  ) : (
                    <div className="cq-report-empty">
                      No service data available.
                    </div>
                  )}
                </div>
              </div>

              {/* ── Top Providers ─────────────────────────────────────── */}

              <div className="cq-report-section">
                <div className="cq-report-section__header">
                  <h2>
                    Top Providers
                  </h2>

                  <span className="cq-report-section__hint">
                    Up to 5 providers by booking
                    volume
                  </span>
                </div>

                {report.topProviders?.length > 0 ? (
                  <div className="cq-table-wrapper">
                    <table className="cq-report-table">
                      <thead>
                        <tr>
                          <th>Provider</th>
                          <th>Total Bookings</th>
                          <th>Revenue</th>
                          <th>
                            Cancellation Rate
                          </th>
                        </tr>
                      </thead>

                      <tbody>
                        {report.topProviders.map(
                          (provider, index) => (
                            <tr
                              key={
                                provider.providerId ||
                                index
                              }
                            >
                              <td>
                                {
                                  provider.providerName
                                }
                              </td>

                              <td>
                                {Number(
                                  provider.totalBookings ||
                                  0
                                ).toLocaleString(
                                  'en-LK'
                                )}
                              </td>

                              <td>
                                {money(
                                  provider.totalRevenue
                                )}
                              </td>

                              <td>
                                {pct(
                                  provider.cancellationRate
                                )}
                              </td>
                            </tr>
                          )
                        )}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <div className="cq-report-empty">
                    No provider data available.
                  </div>
                )}
              </div>
            </>
          )}
        </>
      )}
    </div>
  )
}
