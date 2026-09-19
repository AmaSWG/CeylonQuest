import React from 'react'
import {
  DocumentScannerIcon,
  GroupIcon,
  WorkIcon,
  CalendarMonthIcon,
  ManageSearchIcon,
  NotificationsActiveIcon
} from '../../../components/Icons'

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


export default function OverviewTab({ stats, users = [], applications = [], bookings = [], onNavigate }) {
  const appList = Array.isArray(applications) ? applications : (applications ? [applications] : [])
  const userList = Array.isArray(users) ? users : (users ? [users] : [])

  const pendingApps = appList.filter(a => (a.status || '').toLowerCase() === 'pending')
  const approvedProviders = userList.filter(u => u.role === 'Provider')
  const activeUsers = userList.filter(u => u.isActive)

  const recentApps = appList.slice(0, 4)

  return (
    <div className="ad-overview">
      <div className="ad-page-header">
        <div className="ad-page-header__left">
          <h1>Admin Overview</h1>
          <p>Real-time platform metrics, provider applications review, and user management monitoring.</p>
        </div>
      </div>

      {/* Metrics Cards */}
      <div className="ad-metrics-grid">
        <div className="ad-metric-card" className="ad-cursor-pointer" onClick={() => onNavigate('users')}>
          <div className="ad-metric-icon ad-metric-icon--blue"><GroupIcon size={24} /></div>
          <div className="ad-metric-info">
            <div className="ad-metric-title">Total Registered Users</div>
            <div className="ad-metric-value">{stats?.totalUsers ?? users.length}</div>
            <div className="ad-metric-sub">{activeUsers.length} Active Accounts</div>
          </div>
        </div>

        <div className="ad-metric-card" className="ad-cursor-pointer" onClick={() => onNavigate('providers')}>
          <div className="ad-metric-icon ad-metric-icon--teal"><WorkIcon size={24} /></div>
          <div className="ad-metric-info">
            <div className="ad-metric-title">Service Providers</div>
            <div className="ad-metric-value">{stats?.totalProviders ?? approvedProviders.length}</div>
            <div className="ad-metric-sub">Certified Partners</div>
          </div>
        </div>

        <div className="ad-metric-card" className="ad-cursor-pointer" onClick={() => onNavigate('applications')}>
          <div className="ad-metric-icon ad-metric-icon--gold"><DocumentScannerIcon size={24} /></div>
          <div className="ad-metric-info">
            <div className="ad-metric-title">Provider Applications</div>
            <div className="ad-metric-value">{applications.length}</div>
            <div className="ad-metric-sub">{pendingApps.length} Pending Review</div>
          </div>
        </div>

        <div className="ad-metric-card" className="ad-cursor-pointer" onClick={() => onNavigate('bookings')}>
          <div className="ad-metric-icon ad-metric-icon--green"><CalendarMonthIcon size={24} /></div>
          <div className="ad-metric-info">
            <div className="ad-metric-title">Platform Bookings</div>
            <div className="ad-metric-value">{bookings.length}</div>
            <div className="ad-metric-sub">Visitor Reservations</div>
          </div>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="ad-quick-actions">
        <button className="ad-quick-btn ad-quick-btn--primary" onClick={() => onNavigate('applications')}>
          <DocumentScannerIcon size={16} /> View Provider Applications ({applications.length} total, {pendingApps.length} pending)
        </button>
        <button className="ad-quick-btn ad-quick-btn--secondary" onClick={() => onNavigate('users')}>
          <GroupIcon size={16} /> Manage Users
        </button>
        <button className="ad-quick-btn ad-quick-btn--secondary" onClick={() => onNavigate('providers')}>
          <WorkIcon size={16} /> View Approved Providers
        </button>
        <button className="ad-quick-btn ad-quick-btn--secondary" onClick={() => onNavigate('bookings')}>
          <CalendarMonthIcon size={16} /> View Bookings Overview
        </button>
      </div>

      {/* Two Column Layout */}
      <div className="ad-overview-cols">
        {/* Left Column: Recent Provider Applications */}
        <div className="ad-card">
          <div className="ad-card__body">
            <div className="ad-section-header">
              <h2>Recent Provider Applications</h2>
              <button className="ad-row-btn ad-row-btn--view" onClick={() => onNavigate('applications')}>
                View All ({applications.length})
              </button>
            </div>

            {recentApps.length === 0 ? (
              <div className="ad-empty">
                <div className="ad-empty__icon"></div>
                <p className="ad-empty__title">No applications received yet</p>
                <p className="ad-empty__msg">Submitted provider registration applications will appear here for review.</p>
              </div>
            ) : (
              <div className="ad-table-wrap" style={{ border: 'none' }}>
                <table className="ad-table">
                  <thead>
                    <tr>
                      <th>Business</th>
                      <th>Applicant</th>
                      <th>Category</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {recentApps.map(app => (
                      <tr key={app.id}>
                        <td style={{ fontWeight: 700, color: '#123b5d' }}>{app.businessName}</td>
                        <td>{app.firstName} {app.lastName}</td>
                        <td>{app.serviceType}</td>
                        <td>
                          <span className={`ad-badge ad-badge--${app.status.toLowerCase()}`}>
                            {app.status}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>

        {/* Right Column: Recent System Activity Feed */}
        <div className="ad-card">
          <div className="ad-card__body">
            <div className="ad-section-header">
              <h2>Recent System Activity</h2>
              <span style={{ fontSize: '12px', color: '#888' }}>Live Feed</span>
            </div>

            {(() => {
              const events = [
                ...applications.slice(0, 2).map(a => ({
                  id: `app-${a.id}`,
                  icon: <DocumentScannerIcon size={16} />,
                  iconBg: 'rgba(22, 138, 173, 0.15)',
                  iconColor: '#168aad',
                  title: 'Provider Application Submitted',
                  desc: `Application received from ${a.businessName || `${a.firstName} ${a.lastName}`.trim() || 'Service Provider'} (${a.serviceType || 'Tourism'}).`,
                  time: formatDate(a.createdAt)
                })),
                ...users.slice(0, 2).map(u => ({
                  id: `usr-${u.id}`,
                  icon: <GroupIcon size={16} />,
                  iconBg: 'rgba(79, 138, 69, 0.15)',
                  iconColor: '#3a6b30',
                  title: 'New User Registration',
                  desc: `User registered: ${`${u.firstName} ${u.lastName}`.trim() || u.email} (${u.role || 'Visitor'}).`,
                  time: formatDate(u.createdAt)
                }))
              ]

              if (events.length === 0) {
                return (
                  <div className="ad-empty" style={{ padding: '24px 16px' }}>
                    <div className="ad-empty__icon"><NotificationsActiveIcon size={28} /></div>
                    <p className="ad-empty__title">No recent activity</p>
                    <p className="ad-empty__msg">Platform registrations and provider application events will appear here.</p>
                  </div>
                )
              }

              return (
                <div className="ad-timeline">
                  {events.map(evt => (
                    <div key={evt.id} className="ad-timeline-item">
                      <div className="ad-timeline-icon" style={{ background: evt.iconBg, color: evt.iconColor }}>{evt.icon}</div>
                      <div>
                        <div className="ad-timeline-title">{evt.title}</div>
                        <div className="ad-timeline-desc">{evt.desc}</div>
                      </div>
                      <span className="ad-timeline-time">{evt.time}</span>
                    </div>
                  ))}
                </div>
              )
            })()}
          </div>
        </div>
      </div>
    </div>
  )
}

// ── 2. Provider Applications Tab ───────────────

