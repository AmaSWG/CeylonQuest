import { useState, useEffect, useCallback } from 'react'
import { catalogUrl } from '../api/client'
import {
  BarChartIcon,
  CalendarMonthIcon,
  CheckCircleIcon,
  AlarmIcon,
  LocationOnIcon,
  CheckIcon,
  DangerIcon,
  CelebrationIcon,
  CloseIcon
} from './Icons'

function formatDate(iso) {
  if (!iso) return '—'
  const parts = iso.split('-')
  if (parts.length === 3) {
    const d = new Date(parseInt(parts[0]), parseInt(parts[1]) - 1, parseInt(parts[2]))
    return d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
  }
  return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
}

function getTodayString() {
  const d = new Date()
  return d.toISOString().split('T')[0]
}

function getFutureDateString(daysAhead) {
  const d = new Date()
  d.setDate(d.getDate() + daysAhead)
  return d.toISOString().split('T')[0]
}

const CATEGORY_NAMES = {
  Experience: 'Experiences',
  Restaurant: 'Dining',
  Accommodation: 'Stays'
}

export default function InventoryReportView({ token, onLogout, isAdmin = false }) {
  const [preset, setPreset] = useState('30d')
  const [startDate, setStartDate] = useState(getTodayString())
  const [endDate, setEndDate] = useState(getFutureDateString(30))
  const [category, setCategory] = useState('')
  const [location, setLocation] = useState('')

  const [report, setReport] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  const handlePresetChange = (newPreset) => {
    setPreset(newPreset)
    const today = getTodayString()
    let s = today
    let e = today

    if (newPreset === '7d') {
      e = getFutureDateString(7)
    } else if (newPreset === '30d') {
      e = getFutureDateString(30)
    }

    setStartDate(s)
    setEndDate(e)
    if (newPreset !== 'custom') {
      fetchReport(s, e, category, location)
    }
  }

  const fetchReport = useCallback(async (sDate, eDate, cat, loc) => {
    if (!token) return
    setLoading(true)
    setError(null)

    try {
      const params = new URLSearchParams()
      if (sDate) params.append('startDate', sDate)
      if (eDate) params.append('endDate', eDate)
      if (isAdmin && cat) params.append('category', cat)
      if (isAdmin && loc && loc.trim()) params.append('location', loc.trim())

      const resp = await fetch(catalogUrl(`/api/catalog/reports/inventory-summary?${params.toString()}`), {
        headers: {
          Authorization: `Bearer ${token}`,
          Accept: 'application/json'
        }
      })

      if (resp.ok) {
        const data = await resp.json()
        setReport(data)
      } else if (resp.status === 401) {
        onLogout && onLogout()
      } else {
        const errData = await resp.json().catch(() => ({}))
        setError(errData.message || 'Failed to load inventory report.')
      }
    } catch {
      setError('Network error — could not reach the catalog reporting service.')
    } finally {
      setLoading(false)
    }
  }, [token, onLogout, isAdmin])

  useEffect(() => {
    fetchReport(startDate, endDate, category, location)
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const handleApplyFilters = (e) => {
    e?.preventDefault()
    fetchReport(startDate, endDate, category, location)
  }

  const handleResetFilters = () => {
    const today = getTodayString()
    const next30 = getFutureDateString(30)
    setPreset('30d')
    setStartDate(today)
    setEndDate(next30)
    setCategory('')
    setLocation('')
    fetchReport(today, next30, '', '')
  }

  const s = report?.summary || {
    totalListings: 0,
    activeListings: 0,
    totalCapacity: 0,
    bookedCapacity: 0,
    remainingCapacity: 0,
    occupancyRate: 0,
    lowAvailabilityCount: 0,
    soldOutCount: 0
  }

  return (
    <div className="cq-inv-report">
      {/* ── Page Header ── */}
      <div className="pd-page-header" style={{ marginBottom: '16px' }}>
        <div className="pd-page-header__left">
          <h1 style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <BarChartIcon size={26} />
            {isAdmin ? 'Island-wide Listings & Inventory Report' : 'Live Availability & Capacity Report'}
          </h1>
          <p>
            {isAdmin
              ? 'Aggregated real-time inventory, slot capacity, regional supply matrix, and platform occupancy.'
              : 'Real-time overview of your bookable slots, low-capacity alerts, and occupancy rates.'}
            {report?.generatedAt && (
              <span className="cq-report-badge-time">
                {' '}• Generated at {new Date(report.generatedAt).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })}
              </span>
            )}
          </p>
        </div>
      </div>

      {/* ── Filter Bar ── */}
      <div className="cq-report-filtercard">
        <form onSubmit={handleApplyFilters} className="cq-report-filterform">
          <div className="cq-report-fieldgroup">
            <label className="cq-report-label">Date Window</label>
            <div className="cq-preset-btngroup">
              <button
                type="button"
                className={`cq-preset-btn ${preset === 'today' ? 'active' : ''}`}
                onClick={() => handlePresetChange('today')}
              >
                Today
              </button>
              <button
                type="button"
                className={`cq-preset-btn ${preset === '7d' ? 'active' : ''}`}
                onClick={() => handlePresetChange('7d')}
              >
                Next 7 Days
              </button>
              <button
                type="button"
                className={`cq-preset-btn ${preset === '30d' ? 'active' : ''}`}
                onClick={() => handlePresetChange('30d')}
              >
                Next 30 Days
              </button>
              <button
                type="button"
                className={`cq-preset-btn ${preset === 'custom' ? 'active' : ''}`}
                onClick={() => setPreset('custom')}
              >
                Custom Range
              </button>
            </div>
          </div>

          {preset === 'custom' && (
            <div className="cq-report-daterow">
              <div className="cq-report-field">
                <label className="cq-report-label" htmlFor="inv-start-date">From</label>
                <input
                  id="inv-start-date"
                  type="date"
                  className="cq-report-input"
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                  required
                />
              </div>
              <div className="cq-report-field">
                <label className="cq-report-label" htmlFor="inv-end-date">To</label>
                <input
                  id="inv-end-date"
                  type="date"
                  className="cq-report-input"
                  value={endDate}
                  onChange={(e) => setEndDate(e.target.value)}
                  required
                />
              </div>
            </div>
          )}

          {/* Admin-only Filters: Category & Location */}
          {isAdmin && (
            <>
              <div className="cq-report-field">
                <label className="cq-report-label" htmlFor="inv-category">Category</label>
                <select
                  id="inv-category"
                  className="cq-report-select"
                  value={category}
                  onChange={(e) => setCategory(e.target.value)}
                >
                  <option value="">All Categories</option>
                  <option value="Experience">Experiences & Tours</option>
                  <option value="Restaurant">Dining & Restaurants</option>
                  <option value="Accommodation">Stays & Accommodations</option>
                </select>
              </div>

              <div className="cq-report-field">
                <label className="cq-report-label" htmlFor="inv-location">Location</label>
                <input
                  id="inv-location"
                  type="text"
                  className="cq-report-input"
                  placeholder="e.g. Ella, Kandy, Galle…"
                  value={location}
                  onChange={(e) => setLocation(e.target.value)}
                />
              </div>
            </>
          )}

          <div className="cq-report-actions">
            <button type="submit" className="cq-report-btn cq-report-btn--primary">
              Apply Filters
            </button>
            <button type="button" className="cq-report-btn cq-report-btn--secondary" onClick={handleResetFilters}>
              Reset
            </button>
          </div>
        </form>
      </div>

      {/* ── Loading / Error States ── */}
      {loading && (
        <div className="pd-loading" style={{ margin: '30px 0' }}>
          <div className="pd-spinner" />
          <span>Aggregating real-time availability metrics…</span>
        </div>
      )}

      {error && (
        <div className="cq-report-alert cq-report-alert--danger" style={{ marginBottom: '20px' }}>
          {error}
        </div>
      )}

      {!loading && !error && report && (
        <>
          {/* ── KPI Metric Cards ── */}
          <div className="cq-report-kpigrid">
            <div className="cq-kpicard">
              <div className="cq-kpicard__icon cq-kpicard__icon--blue">
                <BarChartIcon size={22} />
              </div>
              <div className="cq-kpicard__info">
                <span className="cq-kpicard__label">Active Listings</span>
                <span className="cq-kpicard__val">{s.activeListings}</span>
                <span className="cq-kpicard__sub">out of {s.totalListings} total</span>
              </div>
            </div>

            <div className="cq-kpicard">
              <div className="cq-kpicard__icon cq-kpicard__icon--teal">
                <CalendarMonthIcon size={22} />
              </div>
              <div className="cq-kpicard__info">
                <span className="cq-kpicard__label">Window Capacity</span>
                <span className="cq-kpicard__val">{s.totalCapacity} spots</span>
                <span className="cq-kpicard__sub">{s.remainingCapacity} available</span>
              </div>
            </div>

            <div className="cq-kpicard">
              <div className="cq-kpicard__icon cq-kpicard__icon--green">
                <CheckCircleIcon size={22} />
              </div>
              <div className="cq-kpicard__info">
                <span className="cq-kpicard__label">Booked Spots</span>
                <span className="cq-kpicard__val">{s.bookedCapacity}</span>
                <span className="cq-kpicard__sub">Confirmed reservations</span>
              </div>
            </div>

            <div className="cq-kpicard">
              <div className="cq-kpicard__icon cq-kpicard__icon--gold">
                <AlarmIcon size={22} />
              </div>
              <div className="cq-kpicard__info">
                <span className="cq-kpicard__label">Occupancy Rate</span>
                <span className="cq-kpicard__val">{s.occupancyRate}%</span>
                <div className="cq-kpicard__progressbar">
                  <div
                    className="cq-kpicard__progressfill"
                    style={{ width: `${Math.min(s.occupancyRate, 100)}%` }}
                  />
                </div>
              </div>
            </div>
          </div>

          {/* ── ADMIN-ONLY SECTIONS: Category Distribution & Regional Supply Matrix ── */}
          {isAdmin && (
            <>
              {/* Category Distribution */}
              <div className="cq-report-box">
                <h3 className="cq-report-box__title">Category Distribution & Inventory</h3>
                {(!report.byCategory || report.byCategory.length === 0) ? (
                  <div className="cq-report-empty">No category data matching query.</div>
                ) : (
                  <div className="cq-table-wrapper">
                    <table className="cq-report-table">
                      <thead>
                        <tr>
                          <th>Category</th>
                          <th>Active Listings</th>
                          <th>Window Capacity</th>
                          <th>Booked Spots</th>
                          <th>Available Spots</th>
                          <th>Occupancy</th>
                        </tr>
                      </thead>
                      <tbody>
                        {report.byCategory.map((cat, idx) => {
                          const occ = cat.totalCapacity > 0 ? Math.round((cat.bookedCapacity / cat.totalCapacity) * 100) : 0
                          return (
                            <tr key={idx}>
                              <td style={{ fontWeight: '600', color: '#123b5d' }}>
                                {cat.displayName || cat.category}
                              </td>
                              <td>{cat.listingCount}</td>
                              <td>{cat.totalCapacity} spots</td>
                              <td>{cat.bookedCapacity}</td>
                              <td><strong style={{ color: '#16a34a' }}>{cat.remainingCapacity}</strong></td>
                              <td>
                                <div className="cq-table-occ">
                                  <div className="cq-table-occ__bar">
                                    <div className="cq-table-occ__fill" style={{ width: `${Math.min(occ, 100)}%` }} />
                                  </div>
                                  <span>{occ}%</span>
                                </div>
                              </td>
                            </tr>
                          )
                        })}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>

              {/* Regional Supply & Coverage Matrix (Combined with Gaps) */}
              <div className="cq-report-box">
                <div className="cq-report-box__header-flex">
                  <div>
                    <h3 className="cq-report-box__title" style={{ margin: 0 }}>Regional Supply & Coverage Matrix</h3>
                    <span className="cq-report-section__hint">
                      Overview of tourism coverage across regions and identified supply deficits.
                    </span>
                  </div>
                </div>

                {(!report.byLocation || report.byLocation.length === 0) ? (
                  <div className="cq-report-empty">No locations matching current filters.</div>
                ) : (
                  <div className="cq-table-wrapper" style={{ marginTop: '12px' }}>
                    <table className="cq-report-table">
                      <thead>
                        <tr>
                          <th>Region / Location</th>
                          <th>Listings</th>
                          <th>Capacity</th>
                          <th>Categories Present</th>
                          <th>Coverage & Supply Gaps</th>
                        </tr>
                      </thead>
                      <tbody>
                        {report.byLocation.map((loc, idx) => {
                          const hasGaps = loc.missingCategories && loc.missingCategories.length > 0
                          return (
                            <tr key={idx}>
                              <td style={{ fontWeight: '700', color: '#123b5d' }}>
                                <><LocationOnIcon/> {loc.location}</>
                              </td>
                              <td>{loc.listingCount}</td>
                              <td>{loc.totalCapacity} spots</td>
                              <td>
                                <div className="cq-badge-cluster">
                                  {loc.presentCategories?.map((c, cIdx) => (
                                    <span key={cIdx} className="cq-cat-pill cq-cat-pill--present">
                                      <><CheckIcon/> {CATEGORY_NAMES[c] || c}</>
                                    </span>
                                  ))}
                                </div>
                              </td>
                              <td>
                                {hasGaps ? (
                                  <div className="cq-badge-cluster">
                                    {loc.missingCategories.map((m, mIdx) => (
                                      <span key={mIdx} className="cq-cat-pill cq-cat-pill--missing">
                                        <DangerIcon/> Missing {CATEGORY_NAMES[m] || m}
                                      </span>
                                    ))}
                                  </div>
                                ) : (
                                  <span className="cq-cat-pill cq-cat-pill--complete">
                                    <CheckIcon/> Full Coverage
                                  </span>
                                )}
                              </td>
                            </tr>
                          )
                        })}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </>
          )}

          {/* ── PROVIDER-ONLY SECTION: Capacity & Low-Availability Alerts Table ── */}
          {!isAdmin && (
            <div className="cq-report-section">
              <div className="cq-report-section__header">
                <h2><DangerIcon/> Capacity & Low-Availability Alerts</h2>
                <span className="cq-report-section__hint">
                  Upcoming slots with ≤3 spots remaining or 100% booked in the selected window.
                </span>
              </div>

              {(!report.lowAvailabilityAlerts || report.lowAvailabilityAlerts.length === 0) ? (
                <div className="cq-report-empty">
                  <CelebrationIcon/> All your inventory slots have healthy remaining availability for the selected dates.
                </div>
              ) : (
                <div className="cq-table-wrapper">
                  <table className="cq-report-table">
                    <thead>
                      <tr>
                        <th>Status</th>
                        <th>Listing Title</th>
                        <th>Category</th>
                        <th>Location</th>
                        <th>Date</th>
                        <th>Time Slot</th>
                        <th>Remaining</th>
                        <th>Capacity</th>
                      </tr>
                    </thead>
                    <tbody>
                      {report.lowAvailabilityAlerts.map((item, idx) => {
                        const isSoldOut = item.status === 'Sold Out' || item.remainingCapacity === 0
                        return (
                          <tr key={idx} className={isSoldOut ? 'cq-row--soldout' : 'cq-row--low'}>
                            <td>
                              <span className={`cq-statusbadge ${isSoldOut ? 'cq-statusbadge--soldout' : 'cq-statusbadge--low'}`}>
                                {isSoldOut ? 'Sold Out' : 'Low Stock'}
                              </span>
                            </td>
                            <td style={{ fontWeight: '600', color: '#123b5d' }}>{item.title}</td>
                            <td>
                              <span className="cq-tag-cat">{item.category}</span>
                            </td>
                            <td>{item.location}</td>
                            <td>{formatDate(item.date)}</td>
                            <td><code>{item.timeSlot}</code></td>
                            <td>
                              <strong style={{ color: isSoldOut ? '#dc2626' : '#d97706' }}>
                                {item.remainingCapacity} spots
                              </strong>
                            </td>
                            <td>{item.totalCapacity} total</td>
                          </tr>
                        )
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}
        </>
      )}
    </div>
  )
}