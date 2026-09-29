import { useEffect, useState } from 'react'
import { bookingUrl } from '../../../api/client'
import { BarChartIcon } from '../../../components/Icons'
import './ProviderBookingsRevenueReportTab.css'

const EMPTY_FILTERS = { startDate: '', endDate: '', status: '', bookingType: '' }
const STATUSES = ['PendingPayment', 'Confirmed', 'Cancelled', 'Completed']
const TYPES = ['Experience', 'Restaurant', 'Accommodation']
const currency = new Intl.NumberFormat('en-LK', { style: 'currency', currency: 'LKR', minimumFractionDigits: 2 })
const money = (value) => currency.format(Number(value) || 0)
const statusLabel = (value) => value === 'PendingPayment' ? 'Pending Payment' : value

function formatDate(value) {
  if (!value) return '—'
  const date = new Date(`${value.slice(0, 10)}T00:00:00Z`)
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString('en-GB', {
    day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC'
  })
}

function Breakdown({ title, counts, labels }) {
  return (
    <section className="pbr-panel">
      <h2>{title}</h2>
      <dl className="pbr-breakdown">
        {labels.map(label => (
          <div key={label}><dt>{statusLabel(label)}</dt><dd>{counts?.[label] ?? 0}</dd></div>
        ))}
      </dl>
    </section>
  )
}

export default function ProviderBookingsRevenueReportTab({ token, onLogout }) {
  const [filters, setFilters] = useState({ ...EMPTY_FILTERS })
  const [request, setRequest] = useState({ filters: { ...EMPTY_FILTERS } })
  const [result, setResult] = useState(null)
  const [validationError, setValidationError] = useState('')
  const loading = result?.request !== request
  const report = loading ? null : result?.report
  const error = loading ? null : result?.error

  useEffect(() => {
    const controller = new AbortController()
    if (!token) {
      onLogout?.()
      return () => controller.abort()
    }

    async function loadReport() {
      const params = new URLSearchParams()
      Object.entries(request.filters).forEach(([key, value]) => {
        if (value) params.set(key, value)
      })
      const query = params.toString()
      try {
        const response = await fetch(bookingUrl(`/api/provider-bookings/reports/bookings-revenue${query ? `?${query}` : ''}`), {
          headers: { Authorization: `Bearer ${token}`, Accept: 'application/json' },
          signal: controller.signal
        })
        if (controller.signal.aborted) return
        if (response.status === 401) {
          onLogout?.()
          return
        }
        const data = await response.json().catch(() => null)
        if (controller.signal.aborted) return
        if (!response.ok) {
          let message = 'Unable to load the report. Please try again.'
          if (response.status === 400) {
            const fields = Object.keys(data?.errors || {})
            message = fields.length
              ? 'Please check the dates, booking status and booking type, then apply the filters again.'
              : (data?.message || 'The selected filters are invalid. Please check them and try again.')
          } else if (response.status === 403) {
            message = 'You are not authorized to access provider reports. Please sign in with a provider account.'
          } else if (response.status === 503) {
            message = 'We could not verify your services right now. Please try again shortly.'
          }
          setResult({ request, error: message, forbidden: response.status === 403 })
          return
        }
        if (!data || !Array.isArray(data.records) || typeof data.hasData !== 'boolean') {
          setResult({ request, error: 'Unable to load the report. Please try again.' })
          return
        }
        setResult({ request, report: data })
      } catch (err) {
        if (err.name !== 'AbortError' && !controller.signal.aborted) {
          setResult({ request, error: 'Unable to reach the reporting service. Check your connection and try again.' })
        }
      }
    }
    loadReport()
    const timer = setInterval(loadReport, 5000)
    window.addEventListener('focus', loadReport)
    return () => {
      controller.abort()
      clearInterval(timer)
      window.removeEventListener('focus', loadReport)
    }
  }, [request, token, onLogout])

  function applyFilters(event) {
    event.preventDefault()
    if (filters.startDate && filters.endDate && filters.startDate > filters.endDate) {
      setValidationError('End date cannot be earlier than start date.')
      return
    }
    setValidationError('')
    setRequest({ filters: { ...filters } })
  }

  function resetFilters() {
    setFilters({ ...EMPTY_FILTERS })
    setValidationError('')
    setRequest({ filters: { ...EMPTY_FILTERS } })
  }

  const changeFilter = (event) => {
    const { name, value } = event.target
    const next = { ...filters, [name]: value }
    setFilters(next)
    setValidationError(
      next.startDate && next.endDate && next.endDate < next.startDate
        ? 'End date cannot be earlier than start date.'
        : ''
    )
  }

  return (
    <div className="pbr-report">
      <div className="pd-page-header">
        <div className="pd-page-header__left">
          <h1>Bookings &amp; Revenue Report</h1>
          <p>Review bookings and revenue for your services.</p>
        </div>
      </div>

      <form className="pbr-panel pbr-filters" onSubmit={applyFilters} aria-label="Report filters">
        <div className="pbr-field">
          <label htmlFor="pbr-start">Start Date</label>
          <input id="pbr-start" type="date" name="startDate" value={filters.startDate} onChange={changeFilter} />
        </div>
        <div className="pbr-field">
          <label htmlFor="pbr-end">End Date</label>
          <input id="pbr-end" type="date" name="endDate" min={filters.startDate || undefined} value={filters.endDate} onChange={changeFilter} />
        </div>
        <div className="pbr-field">
          <label htmlFor="pbr-status">Booking Status</label>
          <select id="pbr-status" name="status" value={filters.status} onChange={changeFilter}>
            <option value="">All</option>
            {STATUSES.map(status => <option key={status} value={status}>{statusLabel(status)}</option>)}
          </select>
        </div>
        <div className="pbr-field">
          <label htmlFor="pbr-type">Booking Type</label>
          <select id="pbr-type" name="bookingType" value={filters.bookingType} onChange={changeFilter}>
            <option value="">All</option>
            {TYPES.map(type => <option key={type} value={type}>{type}</option>)}
          </select>
        </div>
        <div className="pbr-actions">
          <button className="pbr-button pbr-button--primary" type="submit" disabled={loading}>Apply Filters</button>
          <button className="pbr-button" type="button" onClick={resetFilters} disabled={loading}>Reset Filters</button>
        </div>
        {validationError && <p className="pbr-validation" role="alert">{validationError}</p>}
      </form>

      <div aria-busy={loading}>
        {loading && <div className="pbr-panel pbr-state" role="status">Loading bookings and revenue report…</div>}
        {error && (
          <div className="pbr-panel pbr-state pbr-error" role="alert">
            <p>{error}</p>
            {result.forbidden
              ? <button type="button" className="pbr-button" onClick={onLogout}>Sign in again</button>
              : <button type="button" className="pbr-button" onClick={() => setRequest({ filters: { ...request.filters } })}>Retry</button>}
          </div>
        )}
        {report && (
          <>
            <div className="pbr-summary">
              <section className="pbr-panel"><h2>Total Bookings</h2><p className="pbr-total">{report.totalBookings.toLocaleString('en-LK')}</p></section>
              <section className="pbr-panel"><h2>Total Revenue</h2><p className="pbr-total">{money(report.totalRevenue)}</p></section>
            </div>
            <p className="pbr-basis">{report.revenueBasis}</p>
            <div className="pbr-summary">
              <Breakdown title="Booking Status Breakdown" counts={report.statusCounts} labels={STATUSES} />
              <Breakdown title="Booking Type Breakdown" counts={report.bookingTypeCounts} labels={TYPES} />
            </div>
            {!report.hasData ? (
              <div className="pbr-panel pbr-state" role="status">
                <BarChartIcon size={32} />
                <p>{report.message || 'No data available for the selected criteria.'}</p>
              </div>
            ) : (
              <section className="pbr-panel pbr-records">
                <h2>Booking Records</h2>
                <div className="pd-table-wrap" tabIndex={0} role="region" aria-label="Booking report records, scroll horizontally to see all columns">
                  <table className="pd-table">
                    <thead><tr>
                      <th scope="col">Booking Type</th><th scope="col">Service</th><th scope="col">Date</th><th scope="col">Status</th>
                      <th scope="col">Booking Value</th><th scope="col">Refund Amount</th><th scope="col">Revenue</th>
                    </tr></thead>
                    <tbody>{report.records.map((record, index) => (
                      <tr key={`${record.bookingType}-${record.date}-${index}`}>
                        <td>{record.bookingType}</td><td className="pbr-service">{record.serviceName}</td><td>{formatDate(record.date)}</td>
                        <td><span className={`pbr-status pbr-status--${record.status.toLowerCase()}`}>{statusLabel(record.status)}</span></td>
                        <td className="pbr-money">{money(record.bookingValue)}</td><td className="pbr-money">{money(record.refundAmount)}</td><td className="pbr-money">{money(record.revenue)}</td>
                      </tr>
                    ))}</tbody>
                  </table>
                </div>
              </section>
            )}
          </>
        )}
      </div>
    </div>
  )
}
