import React, { useState, useEffect, useCallback } from 'react'
import {
  BarChartIcon,
  CalendarMonthIcon,
  ManageSearchIcon
} from '../../../components/Icons'
import InventoryReportView from '../../../components/InventoryReportView'
import { apiUrl } from '../../../api/client'

function initials(first, last) {
  return `${(first || '').charAt(0)}${(last || '').charAt(0)}`.toUpperCase() || 'A'
}

function formatAvatarUrl(url) {
  if (!url) return null
  if (url.startsWith('/uploads/avatars/')) {
    const fileName = url.split('/').pop()
    return apiUrl(`/api/users/avatar/${fileName}`)
  }
  return url
}

function formatDate(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
}

function formatDateTime(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString('en-GB', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function LoadingState({ label = 'Loading…' }) {
  return (
    <div className="ad-loading">
      <div className="ad-spinner" />
      <p>{label}</p>
    </div>
  )
}

function RegistrationReportsSection({ token, onLogout }) {
  const emptyFilters = { dateFrom: '', dateTo: '', role: '', applicationStatus: '' }
  const [filters, setFilters] = useState(emptyFilters)
  const [appliedFilters, setAppliedFilters] = useState({})
  const [report, setReport] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  const buildQuery = (f) => {
    const params = new URLSearchParams()
    if (f.dateFrom)          params.append('dateFrom', f.dateFrom)
    if (f.dateTo)            params.append('dateTo',   f.dateTo)
    if (f.role)              params.append('role',     f.role)
    if (f.applicationStatus) params.append('applicationStatus', f.applicationStatus)
    return params.toString()
  }

  const fetchReport = useCallback(async (activeFilters) => {
    if (!token) return
    setLoading(true)
    setError(null)
    try {
      const qs = buildQuery(activeFilters)
      const resp = await fetch(apiUrl(`/api/admin/reports${qs ? '?' + qs : ''}`), {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        setReport(await resp.json())
      } else if (resp.status === 401) {
        onLogout && onLogout()
      } else {
        const body = await resp.json().catch(() => ({}))
        setError(body.message || 'Failed to load report.')
      }
    } catch {
      setError('Network error — could not load report.')
    } finally {
      setLoading(false)
    }
  }, [token, onLogout])

  // Load report on first render with empty filters
  useEffect(() => { fetchReport({}) }, [fetchReport])

  const handleApply = (e) => {
    e.preventDefault()
    setAppliedFilters(filters)
    fetchReport(filters)
  }

  const handleClear = () => {
    setFilters(emptyFilters)
    setAppliedFilters({})
    fetchReport({})
  }

  const r = report?.registrations
  const a = report?.applications
  const af = report?.appliedFilters ?? {}

  return (
    <div className="ad-report">
      <div className="ad-page-header">
        <div className="ad-page-header__left">
          <h1>Registration and Verification Report</h1>
          <p>
            Dynamic report aggregated from live database data.
            {report && <span className="ad-report__generated"> Generated at {formatDateTime(report.generatedAt)}</span>}
          </p>
        </div>
      </div>

      {/* ── Filter Bar ── */}
      <form className="ad-report__filters" onSubmit={handleApply} id="ad-report-filter-form">
        <div className="ad-report__filter-row">
          <label className="ad-report__filter-label" htmlFor="ad-report-dateFrom">Date From</label>
          <input
            id="ad-report-dateFrom"
            type="date"
            className="ad-report__filter-input"
            value={filters.dateFrom}
            onChange={e => setFilters(f => ({ ...f, dateFrom: e.target.value }))}
          />
        </div>
        <div className="ad-report__filter-row">
          <label className="ad-report__filter-label" htmlFor="ad-report-dateTo">Date To</label>
          <input
            id="ad-report-dateTo"
            type="date"
            className="ad-report__filter-input"
            value={filters.dateTo}
            onChange={e => setFilters(f => ({ ...f, dateTo: e.target.value }))}
          />
        </div>
        <div className="ad-report__filter-row">
          <label className="ad-report__filter-label" htmlFor="ad-report-role">User Role</label>
          <select
            id="ad-report-role"
            className="ad-report__filter-select"
            value={filters.role}
            onChange={e => setFilters(f => ({ ...f, role: e.target.value }))}
          >
            <option value="">All Roles</option>
            <option value="Visitor">Visitor</option>
            <option value="Provider">Provider</option>
            <option value="Admin">Admin</option>
          </select>
        </div>
        <div className="ad-report__filter-row">
          <label className="ad-report__filter-label" htmlFor="ad-report-status">App Status</label>
          <select
            id="ad-report-status"
            className="ad-report__filter-select"
            value={filters.applicationStatus}
            onChange={e => setFilters(f => ({ ...f, applicationStatus: e.target.value }))}
          >
            <option value="">All Statuses</option>
            <option value="Pending">Pending</option>
            <option value="Approved">Approved</option>
            <option value="Rejected">Rejected</option>
          </select>
        </div>
        <div className="ad-report__filter-actions">
          <button type="submit" className="ad-report__apply-btn" id="ad-report-apply-btn">Apply Filters</button>
          <button type="button" className="ad-report__clear-btn" id="ad-report-clear-btn" onClick={handleClear}>Clear</button>
        </div>
      </form>

      {/* ── Active filter badges ── */}
      {(af.dateFrom || af.dateTo || af.role || af.applicationStatus) && (
        <div className="ad-report__active-filters">
          <span className="ad-report__filter-badge-label">Active filters:</span>
          {af.dateFrom && <span className="ad-report__filter-badge">From: {af.dateFrom}</span>}
          {af.dateTo   && <span className="ad-report__filter-badge">To: {af.dateTo}</span>}
          {af.role     && <span className="ad-report__filter-badge">Role: {af.role}</span>}
          {af.applicationStatus && <span className="ad-report__filter-badge">Status: {af.applicationStatus}</span>}
        </div>
      )}

      {loading && <LoadingState label="Generating report…" />}
      {error   && <div className="ad-report__error">{error}</div>}

      {!loading && !error && report && (
        <>
          {r && (
            <section className="ad-report__section">
              <h2 className="ad-report__section-title">
                 User Registrations {af.role ? `(${af.role}s)` : ''}
              </h2>
              <div className="ad-report__cards">
                {af.role === 'Visitor' && (
                  <>
                    <div className="ad-report__card ad-report__card--visitor">
                      <span className="ad-report__card-value">{r.totalVisitors ?? r.totalUsers ?? 0}</span>
                      <span className="ad-report__card-label">Total Visitors</span>
                    </div>
                    <div className="ad-report__card ad-report__card--active">
                      <span className="ad-report__card-value">{r.activeUsers ?? 0}</span>
                      <span className="ad-report__card-label">Active Visitors</span>
                    </div>
                    <div className="ad-report__card ad-report__card--inactive">
                      <span className="ad-report__card-value">{r.inactiveUsers ?? 0}</span>
                      <span className="ad-report__card-label">Inactive Visitors</span>
                    </div>
                  </>
                )}

                {af.role === 'Provider' && (
                  <>
                    <div className="ad-report__card ad-report__card--provider">
                      <span className="ad-report__card-value">{r.totalProviders ?? r.totalUsers ?? 0}</span>
                      <span className="ad-report__card-label">Total Providers</span>
                    </div>
                    <div className="ad-report__card ad-report__card--active">
                      <span className="ad-report__card-value">{r.activeUsers ?? 0}</span>
                      <span className="ad-report__card-label">Active Providers</span>
                    </div>
                    <div className="ad-report__card ad-report__card--inactive">
                      <span className="ad-report__card-value">{r.inactiveUsers ?? 0}</span>
                      <span className="ad-report__card-label">Inactive Providers</span>
                    </div>
                  </>
                )}

                {af.role === 'Admin' && (
                  <>
                    <div className="ad-report__card ad-report__card--admin">
                      <span className="ad-report__card-value">{r.totalAdmins ?? r.totalUsers ?? 0}</span>
                      <span className="ad-report__card-label">Total Admins</span>
                    </div>
                    <div className="ad-report__card ad-report__card--active">
                      <span className="ad-report__card-value">{r.activeUsers ?? 0}</span>
                      <span className="ad-report__card-label">Active Admins</span>
                    </div>
                    <div className="ad-report__card ad-report__card--inactive">
                      <span className="ad-report__card-value">{r.inactiveUsers ?? 0}</span>
                      <span className="ad-report__card-label">Inactive Admins</span>
                    </div>
                  </>
                )}

                {!af.role && (
                  <>
                    <div className="ad-report__card ad-report__card--total">
                      <span className="ad-report__card-value">{r.totalUsers ?? 0}</span>
                      <span className="ad-report__card-label">Total Users</span>
                    </div>
                    <div className="ad-report__card ad-report__card--visitor">
                      <span className="ad-report__card-value">{r.totalVisitors ?? 0}</span>
                      <span className="ad-report__card-label">Visitors</span>
                    </div>
                    <div className="ad-report__card ad-report__card--provider">
                      <span className="ad-report__card-value">{r.totalProviders ?? 0}</span>
                      <span className="ad-report__card-label">Providers</span>
                    </div>
                    <div className="ad-report__card ad-report__card--admin">
                      <span className="ad-report__card-value">{r.totalAdmins ?? 0}</span>
                      <span className="ad-report__card-label">Admins</span>
                    </div>
                    <div className="ad-report__card ad-report__card--active">
                      <span className="ad-report__card-value">{r.activeUsers ?? 0}</span>
                      <span className="ad-report__card-label">Active</span>
                    </div>
                    <div className="ad-report__card ad-report__card--inactive">
                      <span className="ad-report__card-value">{r.inactiveUsers ?? 0}</span>
                      <span className="ad-report__card-label">Inactive</span>
                    </div>
                  </>
                )}
              </div>
            </section>
          )}

          {a && (
            <section className="ad-report__section">
              <h2 className="ad-report__section-title">
                 Provider Applications {af.applicationStatus ? `(${af.applicationStatus})` : ''}
              </h2>
              <div className="ad-report__cards">
                {af.applicationStatus === 'Pending' && (
                  <div className="ad-report__card ad-report__card--pending">
                    <span className="ad-report__card-value">{a.pendingApplications ?? a.totalApplications ?? 0}</span>
                    <span className="ad-report__card-label">Pending Applications</span>
                  </div>
                )}

                {af.applicationStatus === 'Approved' && (
                  <div className="ad-report__card ad-report__card--approved">
                    <span className="ad-report__card-value">{a.approvedApplications ?? a.totalApplications ?? 0}</span>
                    <span className="ad-report__card-label">Approved Applications</span>
                  </div>
                )}

                {af.applicationStatus === 'Rejected' && (
                  <div className="ad-report__card ad-report__card--rejected">
                    <span className="ad-report__card-value">{a.rejectedApplications ?? a.totalApplications ?? 0}</span>
                    <span className="ad-report__card-label">Rejected Applications</span>
                  </div>
                )}

                {!af.applicationStatus && (
                  <>
                    <div className="ad-report__card ad-report__card--total">
                      <span className="ad-report__card-value">{a.totalApplications ?? 0}</span>
                      <span className="ad-report__card-label">Total Applications</span>
                    </div>
                    <div className="ad-report__card ad-report__card--pending">
                      <span className="ad-report__card-value">{a.pendingApplications ?? 0}</span>
                      <span className="ad-report__card-label">Pending</span>
                    </div>
                    <div className="ad-report__card ad-report__card--approved">
                      <span className="ad-report__card-value">{a.approvedApplications ?? 0}</span>
                      <span className="ad-report__card-label">Approved</span>
                    </div>
                    <div className="ad-report__card ad-report__card--rejected">
                      <span className="ad-report__card-value">{a.rejectedApplications ?? 0}</span>
                      <span className="ad-report__card-label">Rejected</span>
                    </div>
                  </>
                )}
              </div>

              {a.byServiceType?.length > 0 && (
                <div className="ad-report__breakdown">
                  <h3 className="ad-report__breakdown-title">
                    Applications by Service Type {af.applicationStatus ? `(${af.applicationStatus})` : ''}
                  </h3>
                  <table className="ad-report__table" id="ad-report-service-type-table">
                    <thead>
                      <tr>
                        <th>Service Type</th>
                        <th>Applications</th>
                        <th>Share</th>
                      </tr>
                    </thead>
                    <tbody>
                      {a.byServiceType.map(row => {
                        const total = a.totalApplications || 1
                        const pct = Math.round((row.count / total) * 100)
                        return (
                          <tr key={row.serviceType}>
                            <td>{row.serviceType}</td>
                            <td>{row.count}</td>
                            <td>
                              <div className="ad-report__share-bar">
                                <div
                                  className="ad-report__share-fill"
                                  style={{ width: `${pct}%` }}
                                />
                                <span>{pct}%</span>
                              </div>
                            </td>
                          </tr>
                        )
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </section>
          )}

          {!r && !a && (
            <div className="ad-report__cards" style={{ padding: '24px', textAlign: 'center', color: '#64748b' }}>
              No report metrics found matching the selected filter criteria.
            </div>
          )}
        </>
      )}
    </div>
  )
}

export default function ReportsTab({ token, onLogout }) {
  const [reportSubTab, setReportSubTab] = useState('inventory')

  return (
    <div className="ad-report">
      {/* ── Sub Tab Selector ── */}
      <div className="cq-report-subtabs">
        <button
          type="button"
          className={`cq-report-subtab-btn ${reportSubTab === 'inventory' ? 'active' : ''}`}
          onClick={() => setReportSubTab('inventory')}
        >
          <BarChartIcon size={16} /> Catalog & Inventory Availability Report
        </button>
        <button
          type="button"
          className={`cq-report-subtab-btn ${reportSubTab === 'registrations' ? 'active' : ''}`}
          onClick={() => setReportSubTab('registrations')}
        >
          <ManageSearchIcon size={16} /> Identity & Registrations Report
        </button>
      </div>

      {reportSubTab === 'inventory' ? (
        <InventoryReportView token={token} onLogout={onLogout} isAdmin={true} />
      ) : (
        <RegistrationReportsSection token={token} onLogout={onLogout} />
      )}
    </div>
  )
}

// ── Root Admin Dashboard Component ────────────────────────────────────────────