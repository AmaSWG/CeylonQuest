import { useState, useEffect, useCallback } from 'react'
import '../styles/AdminDashboard.css'

// ── Helpers ──────────────────────────────────────────────────────────────────

function initials(first, last) {
  return `${(first || '').charAt(0)}${(last || '').charAt(0)}`.toUpperCase() || 'A'
}

function formatDate(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
}

function formatDateTime(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString('en-GB', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

// ── Shared UI Components ──────────────────────────────────────────────────────

function Toast({ message, title = 'Success', onClose }) {
  useEffect(() => {
    const t = setTimeout(onClose, 4000)
    return () => clearTimeout(t)
  }, [onClose])

  return (
    <div className="ad-toast" role="alert" aria-live="polite">
      <div className="ad-toast__icon">✓</div>
      <div className="ad-toast__body">
        <p className="ad-toast__title">{title}</p>
        <p className="ad-toast__msg">{message}</p>
      </div>
      <button className="ad-toast__close" onClick={onClose} aria-label="Close notification">✕</button>
    </div>
  )
}

function LoadingState({ label = 'Loading data…' }) {
  return (
    <div className="ad-loading">
      <div className="ad-spinner" />
      <span>{label}</span>
    </div>
  )
}

function Modal({ title, onClose, wide = false, children }) {
  useEffect(() => {
    const handler = (e) => { if (e.key === 'Escape') onClose() }
    document.addEventListener('keydown', handler)
    return () => document.removeEventListener('keydown', handler)
  }, [onClose])

  return (
    <div className="ad-modal-overlay" onClick={(e) => { if (e.target === e.currentTarget) onClose() }}>
      <div className={`ad-modal ${wide ? 'ad-modal--wide' : ''}`} role="dialog" aria-modal="true">
        <div className="ad-modal__header">
          <h2 className="ad-modal__title">{title}</h2>
          <button className="ad-modal__close" onClick={onClose} aria-label="Close modal">✕</button>
        </div>
        <div className="ad-modal__body">{children}</div>
      </div>
    </div>
  )
}

// ── 1. Overview Tab ───────────────────────────────────────────────────────────

function OverviewTab({ stats, users = [], applications = [], bookings = [], onNavigate }) {
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
        <div className="ad-metric-card" style={{ cursor: 'pointer' }} onClick={() => onNavigate('users')}>
          <div className="ad-metric-icon ad-metric-icon--blue">👥</div>
          <div className="ad-metric-info">
            <div className="ad-metric-title">Total Registered Users</div>
            <div className="ad-metric-value">{stats?.totalUsers ?? users.length}</div>
            <div className="ad-metric-sub">{activeUsers.length} Active Accounts</div>
          </div>
        </div>

        <div className="ad-metric-card" style={{ cursor: 'pointer' }} onClick={() => onNavigate('providers')}>
          <div className="ad-metric-icon ad-metric-icon--teal">🏔️</div>
          <div className="ad-metric-info">
            <div className="ad-metric-title">Service Providers</div>
            <div className="ad-metric-value">{stats?.totalProviders ?? approvedProviders.length}</div>
            <div className="ad-metric-sub">Certified Partners</div>
          </div>
        </div>

        <div className="ad-metric-card" style={{ cursor: 'pointer' }} onClick={() => onNavigate('applications')}>
          <div className="ad-metric-icon ad-metric-icon--gold">📋</div>
          <div className="ad-metric-info">
            <div className="ad-metric-title">Provider Applications</div>
            <div className="ad-metric-value">{applications.length}</div>
            <div className="ad-metric-sub">{pendingApps.length} Pending Review</div>
          </div>
        </div>

        <div className="ad-metric-card" style={{ cursor: 'pointer' }} onClick={() => onNavigate('bookings')}>
          <div className="ad-metric-icon ad-metric-icon--green">📅</div>
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
          📋 View Provider Applications ({applications.length} total, {pendingApps.length} pending)
        </button>
        <button className="ad-quick-btn ad-quick-btn--secondary" onClick={() => onNavigate('users')}>
          👥 Manage Users
        </button>
        <button className="ad-quick-btn ad-quick-btn--secondary" onClick={() => onNavigate('providers')}>
          🏔️ View Approved Providers
        </button>
        <button className="ad-quick-btn ad-quick-btn--secondary" onClick={() => onNavigate('bookings')}>
          📅 View Bookings Overview
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
                <div className="ad-empty__icon">📋</div>
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
                  icon: '📋',
                  iconBg: 'rgba(22, 138, 173, 0.15)',
                  iconColor: '#168aad',
                  title: 'Provider Application Submitted',
                  desc: `Application received from ${a.businessName || `${a.firstName} ${a.lastName}`.trim() || 'Service Provider'} (${a.serviceType || 'Tourism'}).`,
                  time: formatDate(a.createdAt)
                })),
                ...users.slice(0, 2).map(u => ({
                  id: `usr-${u.id}`,
                  icon: '👤',
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
                    <div className="ad-empty__icon">⚡</div>
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

// ── 2. Provider Applications Review Tab ───────────────────────────────────────

function ProviderApplicationsTab({ token, applications = [], onRefresh, showToast }) {
  const [search, setSearch] = useState('')
  const [filterStatus, setFilterStatus] = useState('all') // all | pending | approved | rejected
  const [selectedApp, setSelectedApp] = useState(null)
  const [downloadingId, setDownloadingId] = useState(null)
  const [refreshing, setRefreshing] = useState(false)

  const handleManualRefresh = async () => {
    if (!onRefresh) return
    setRefreshing(true)
    try {
      await onRefresh()
      showToast && showToast('Applications list refreshed.')
    } finally {
      setRefreshing(false)
    }
  }

  const handleDownloadDocument = async (app) => {
    if (!app) return
    setDownloadingId(app.id)
    try {
      const resp = await fetch(`/api/admin/provider-applications/${app.id}/document`, {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        const blob = await resp.blob()
        const url = window.URL.createObjectURL(blob)
        const a = document.createElement('a')
        a.href = url
        let fileName = app.legalDocumentFileName || `${(app.businessName || 'application').replace(/\s+/g, '_')}_document.txt`
        const disposition = resp.headers.get('Content-Disposition')
        if (disposition && disposition.includes('filename=')) {
          const match = disposition.match(/filename="?([^";]+)"?/)
          if (match && match[1]) fileName = match[1]
        }
        a.download = fileName
        document.body.appendChild(a)
        a.click()
        a.remove()
        window.URL.revokeObjectURL(url)
        showToast && showToast(`Downloaded: ${fileName}`)
      } else {
        showToast && showToast('Unable to download document file.')
      }
    } catch {
      showToast && showToast('Download failed. Please check network connection.')
    } finally {
      setDownloadingId(null)
    }
  }

  const list = Array.isArray(applications) ? applications : []
  const filtered = list.filter(a => {
    if (!a) return false
    const q = (search || '').toLowerCase().trim()
    const matchSearch =
      !q ||
      (a.businessName || '').toLowerCase().includes(q) ||
      (a.firstName || '').toLowerCase().includes(q) ||
      (a.lastName || '').toLowerCase().includes(q) ||
      (a.email || '').toLowerCase().includes(q) ||
      (a.serviceType || '').toLowerCase().includes(q) ||
      (a.location || '').toLowerCase().includes(q)
    if (!matchSearch) return false
    if (filterStatus !== 'all' && (a.status || '').toLowerCase() !== filterStatus.toLowerCase()) return false
    return true
  })

  return (
    <div className="ad-applications-tab">
      <div className="ad-page-header">
        <div className="ad-page-header__left">
          <h1>Provider Applications</h1>
          <p>Review submitted tourism provider applications, business information, and download legal documents.</p>
        </div>
      </div>

      {/* Review Details Modal */}
      {selectedApp && (
        <Modal title={`Application Details: ${selectedApp.businessName}`} onClose={() => setSelectedApp(null)} wide>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', paddingBottom: '16px', borderBottom: '1px solid #f0ece3' }}>
              <div>
                <h3 style={{ margin: '0 0 4px', color: '#123b5d', fontSize: '18px' }}>{selectedApp.businessName}</h3>
                <span style={{ fontSize: '13px', color: '#777' }}>Submitted: {formatDateTime(selectedApp.createdAt)}</span>
              </div>
              <span className={`ad-badge ad-badge--${selectedApp.status.toLowerCase()}`} style={{ fontSize: '12px', padding: '5px 12px' }}>
                {selectedApp.status}
              </span>
            </div>

            <div className="ad-fields">
              <div className="ad-field">
                <span className="ad-field__label">Business / Contact</span>
                <span className="ad-field__value">🏢 {selectedApp.businessName || 'Business Application'}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Contact Email</span>
                <span className="ad-field__value">✉️ {selectedApp.email}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Applicant Personal Details</span>
                <span className="ad-field__value">
                  👤 {(selectedApp.firstName || selectedApp.lastName) 
                    ? `${selectedApp.firstName || ''} ${selectedApp.lastName || ''}`.trim() 
                    : 'To be completed by Provider via OTP activation'}
                </span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Contact Phone</span>
                <span className="ad-field__value">
                  📞 {selectedApp.phoneNumber || 'Completed upon OTP activation'}
                </span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Service Category</span>
                <span className="ad-field__value">🏷️ {selectedApp.serviceType}</span>
              </div>
              <div className="ad-field pd-field--full">
                <span className="ad-field__label">Operating Location</span>
                <span className="ad-field__value">📍 {selectedApp.location}</span>
              </div>
              <div className="ad-field pd-field--full">
                <span className="ad-field__label">Business Description</span>
                <span className="ad-field__value" style={{ background: '#faf8f3', padding: '12px 14px', borderRadius: '8px', border: '1px solid #ede8dc', lineHeight: 1.6 }}>
                  {selectedApp.description || 'No description provided.'}
                </span>
              </div>
              <div className="ad-field pd-field--full">
                <span className="ad-field__label">Legal & Registration Document</span>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '12px', background: '#faf8f3', padding: '12px 14px', borderRadius: '8px', border: '1px solid #ede8dc', flexWrap: 'wrap' }}>
                  <span style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13.5px' }}>
                    📄 <strong style={{ color: '#123b5d' }}>{selectedApp.legalDocumentFileName || 'Standard Registration Record'}</strong>
                  </span>
                  <button
                    type="button"
                    className="ad-quick-btn ad-quick-btn--primary"
                    style={{ padding: '7px 14px', fontSize: '12.5px' }}
                    onClick={() => handleDownloadDocument(selectedApp)}
                    disabled={downloadingId === selectedApp.id}
                  >
                    {downloadingId === selectedApp.id ? 'Downloading…' : '⬇️ Download Document'}
                  </button>
                </div>
              </div>
            </div>

            <div style={{ paddingTop: '16px', borderTop: '1px solid #f0ece3', display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
              <button
                type="button"
                className="ad-quick-btn ad-quick-btn--primary"
                onClick={() => handleDownloadDocument(selectedApp)}
                disabled={downloadingId === selectedApp.id}
              >
                {downloadingId === selectedApp.id ? 'Downloading…' : '⬇️ Download Legal Document'}
              </button>
              <button className="ad-cancel-btn" onClick={() => setSelectedApp(null)}>
                Close Review
              </button>
            </div>
          </div>
        </Modal>
      )}

      {/* Filter and Search Bar */}
      <div className="ad-filter-bar">
        <div className="ad-search-wrap">
          <span className="ad-search-icon">🔍</span>
          <input
            type="text"
            className="ad-search-input"
            placeholder="Search by business, applicant, location..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <div className="ad-filter-pills">
          <button className={`ad-filter-pill ${filterStatus === 'all' ? 'active' : ''}`} onClick={() => setFilterStatus('all')}>
            All ({list.length})
          </button>
          <button className={`ad-filter-pill ${filterStatus === 'pending' ? 'active' : ''}`} onClick={() => setFilterStatus('pending')}>
            Pending ({list.filter(a => (a.status || '').toLowerCase() === 'pending').length})
          </button>
          <button className={`ad-filter-pill ${filterStatus === 'approved' ? 'active' : ''}`} onClick={() => setFilterStatus('approved')}>
            Approved ({list.filter(a => (a.status || '').toLowerCase() === 'approved').length})
          </button>
          <button className={`ad-filter-pill ${filterStatus === 'rejected' ? 'active' : ''}`} onClick={() => setFilterStatus('rejected')}>
            Rejected ({list.filter(a => (a.status || '').toLowerCase() === 'rejected').length})
          </button>
        </div>
      </div>

      {/* Applications Table */}
      <div className="ad-card">
        <div className="ad-card__body" style={{ padding: 0 }}>
          {filtered.length === 0 ? (
            <div className="ad-empty">
              <div className="ad-empty__icon">📋</div>
              <p className="ad-empty__title">No applications found</p>
              <p className="ad-empty__msg">No application records match your filter criteria.</p>
            </div>
          ) : (
            <div className="ad-table-wrap" style={{ border: 'none', borderRadius: 0 }}>
              <table className="ad-table">
                <thead>
                  <tr>
                    <th>Business Name</th>
                    <th>Applicant</th>
                    <th>Service Category</th>
                    <th>Location</th>
                    <th>Submitted</th>
                    <th>Legal Document</th>
                    <th>Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(app => (
                    <tr key={app.id}>
                      <td style={{ fontWeight: 700, color: '#123b5d' }}>{app.businessName}</td>
                      <td>
                        <div style={{ fontWeight: 600, color: '#334155' }}>
                          {(app.firstName || app.lastName) ? `${app.firstName || ''} ${app.lastName || ''}`.trim() : 'Business Contact'}
                        </div>
                        <div style={{ fontSize: '11.5px', color: '#64748b' }}>{app.email}</div>
                      </td>
                      <td>{app.serviceType}</td>
                      <td>{app.location}</td>
                      <td>{formatDate(app.createdAt)}</td>
                      <td>
                        <button
                          type="button"
                          className="ad-row-btn ad-row-btn--view"
                          style={{ fontSize: '11.5px', padding: '4px 8px', color: '#168aad', borderColor: '#168aad' }}
                          onClick={() => handleDownloadDocument(app)}
                          disabled={downloadingId === app.id}
                          title="Download Document File"
                        >
                          {downloadingId === app.id ? '…' : `⬇️ ${app.legalDocumentFileName || 'Download Record'}`}
                        </button>
                      </td>
                      <td>
                        <span className={`ad-badge ad-badge--${app.status.toLowerCase()}`}>
                          {app.status}
                        </span>
                      </td>
                      <td>
                        <button className="ad-row-btn ad-row-btn--view" onClick={() => setSelectedApp(app)}>
                          Review Details
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

// ── 3. User Management Tab ────────────────────────────────────────────────────

function UserManagementTab({ token, onLogout, users, onRefresh, showToast }) {
  const [search, setSearch] = useState('')
  const [filterRole, setFilterRole] = useState('all') // all | visitor | provider | admin
  const [filterStatus, setFilterStatus] = useState('all') // all | active | inactive
  const [selectedUser, setSelectedUser] = useState(null)
  const [actionLoading, setActionLoading] = useState(false)

  const handleToggleUserStatus = async (user) => {
    const newStatus = !user.isActive
    if (!window.confirm(`Are you sure you want to ${newStatus ? 'activate' : 'deactivate'} user ${user.email}?`)) return
    setActionLoading(true)
    try {
      const resp = await fetch(`/api/admin/users/${user.id}/status`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify({ isActive: newStatus })
      })
      if (resp.ok) {
        showToast(`User ${user.firstName} ${user.lastName} is now ${newStatus ? 'Active' : 'Inactive'}.`)
        if (selectedUser && selectedUser.id === user.id) {
          setSelectedUser(prev => ({ ...prev, isActive: newStatus }))
        }
        onRefresh && onRefresh()
      } else if (resp.status === 401) {
        onLogout && onLogout()
      } else {
        showToast('Failed to update user status.')
      }
    } catch {
      showToast('Network error. Please check connection.')
    } finally {
      setActionLoading(false)
    }
  }

  const userList = Array.isArray(users) ? users : []
  const filtered = userList.filter(u => {
    if (!u) return false
    const q = (search || '').toLowerCase().trim()
    const matchSearch =
      !q ||
      (u.firstName || '').toLowerCase().includes(q) ||
      (u.lastName || '').toLowerCase().includes(q) ||
      (u.email || '').toLowerCase().includes(q) ||
      (u.phoneNumber && u.phoneNumber.includes(q)) ||
      (u.nationality && u.nationality.toLowerCase().includes(q))
    if (!matchSearch) return false
    if (filterRole !== 'all' && (u.role || '').toLowerCase() !== filterRole.toLowerCase()) return false
    if (filterStatus === 'active' && !u.isActive) return false
    if (filterStatus === 'inactive' && u.isActive) return false
    return true
  })

  return (
    <div className="ad-users-tab">
      <div className="ad-page-header">
        <div className="ad-page-header__left">
          <h1>User Management</h1>
          <p>View all registered platform accounts, assigned roles, and manage user statuses.</p>
        </div>
      </div>

      {/* User Details Modal */}
      {selectedUser && (
        <Modal title={`User Details: ${selectedUser.firstName} ${selectedUser.lastName}`} onClose={() => setSelectedUser(null)}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
            <div className="ad-identity" style={{ marginBottom: 0, paddingBottom: '16px' }}>
              <div className="ad-avatar">{initials(selectedUser.firstName, selectedUser.lastName)}</div>
              <div className="ad-identity__info">
                <h3 className="ad-identity__name">{selectedUser.firstName} {selectedUser.lastName}</h3>
                <p className="ad-identity__email">{selectedUser.email}</p>
                <div style={{ display: 'flex', gap: '8px', marginTop: '6px' }}>
                  <span className={`ad-badge ad-badge--${selectedUser.role.toLowerCase()}`}>{selectedUser.role}</span>
                  <span className={`ad-badge ad-badge--${selectedUser.isActive ? 'active' : 'inactive'}`}>
                    {selectedUser.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>
              </div>
            </div>

            <div className="ad-fields">
              <div className="ad-field">
                <span className="ad-field__label">User ID</span>
                <span className="ad-field__value" style={{ fontSize: '12px', fontFamily: 'monospace' }}>{selectedUser.id}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Assigned Role</span>
                <span className="ad-field__value">{selectedUser.role}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Phone Number</span>
                <span className="ad-field__value">{selectedUser.phoneNumber || '—'}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Nationality</span>
                <span className="ad-field__value">{selectedUser.nationality || '—'}</span>
              </div>
              <div className="ad-field pd-field--full">
                <span className="ad-field__label">Registered On</span>
                <span className="ad-field__value">{formatDateTime(selectedUser.createdAt)}</span>
              </div>
            </div>

            <div style={{ paddingTop: '14px', borderTop: '1px solid #f0ece3', display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
              <button
                className={`ad-quick-btn ${selectedUser.isActive ? 'ad-quick-btn--danger' : 'ad-quick-btn--success'}`}
                onClick={() => handleToggleUserStatus(selectedUser)}
                disabled={actionLoading}
              >
                {selectedUser.isActive ? 'Deactivate User Account' : 'Activate User Account'}
              </button>
            </div>
          </div>
        </Modal>
      )}

      {/* Filter and Search Bar */}
      <div className="ad-filter-bar">
        <div className="ad-search-wrap">
          <span className="ad-search-icon">🔍</span>
          <input
            type="text"
            className="ad-search-input"
            placeholder="Search by name, email, phone, country..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <div className="ad-filter-pills">
          <button className={`ad-filter-pill ${filterRole === 'all' ? 'active' : ''}`} onClick={() => setFilterRole('all')}>
            All Roles ({users.length})
          </button>
          <button className={`ad-filter-pill ${filterRole === 'visitor' ? 'active' : ''}`} onClick={() => setFilterRole('visitor')}>
            Visitors ({users.filter(u => u.role === 'Visitor').length})
          </button>
          <button className={`ad-filter-pill ${filterRole === 'provider' ? 'active' : ''}`} onClick={() => setFilterRole('provider')}>
            Providers ({users.filter(u => u.role === 'Provider').length})
          </button>
          <button className={`ad-filter-pill ${filterRole === 'admin' ? 'active' : ''}`} onClick={() => setFilterRole('admin')}>
            Admins ({users.filter(u => u.role === 'Admin').length})
          </button>
        </div>
      </div>

      {/* Users Table */}
      <div className="ad-card">
        <div className="ad-card__body" style={{ padding: 0 }}>
          {filtered.length === 0 ? (
            <div className="ad-empty">
              <div className="ad-empty__icon">👥</div>
              <p className="ad-empty__title">No users found</p>
              <p className="ad-empty__msg">No registered user accounts match your search filters.</p>
            </div>
          ) : (
            <div className="ad-table-wrap" style={{ border: 'none', borderRadius: 0 }}>
              <table className="ad-table">
                <thead>
                  <tr>
                    <th>User</th>
                    <th>Contact Phone</th>
                    <th>Nationality</th>
                    <th>Role</th>
                    <th>Status</th>
                    <th>Registered</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(user => (
                    <tr key={user.id}>
                      <td>
                        <div style={{ fontWeight: 700, color: '#123b5d' }}>{user.firstName} {user.lastName}</div>
                        <div style={{ fontSize: '11.5px', color: '#888' }}>{user.email}</div>
                      </td>
                      <td>{user.phoneNumber || '—'}</td>
                      <td>{user.nationality || '—'}</td>
                      <td>
                        <span className={`ad-badge ad-badge--${user.role.toLowerCase()}`}>
                          {user.role}
                        </span>
                      </td>
                      <td>
                        <span className={`ad-badge ad-badge--${user.isActive ? 'active' : 'inactive'}`}>
                          {user.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td>{formatDate(user.createdAt)}</td>
                      <td>
                        <div className="ad-row-actions">
                          <button className="ad-row-btn ad-row-btn--view" onClick={() => setSelectedUser(user)}>
                            View
                          </button>
                          <button
                            className="ad-row-btn ad-row-btn--toggle"
                            onClick={() => handleToggleUserStatus(user)}
                          >
                            {user.isActive ? 'Deactivate' : 'Activate'}
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

// ── 4. Provider Management Tab ────────────────────────────────────────────────

function ProviderManagementTab({ token, onLogout, users = [], applications = [], onRefresh, showToast }) {
  const [search, setSearch] = useState('')
  const [selectedProvider, setSelectedProvider] = useState(null)
  const [actionLoading, setActionLoading] = useState(false)

  const appList = Array.isArray(applications) ? applications : []
  const userList = Array.isArray(users) ? users : []

  // Match provider users with their application details if available
  const providers = userList
    .filter(u => (u.role || '').toLowerCase() === 'provider')
    .map(p => {
      const app = appList.find(a => (a.email || '').toLowerCase() === (p.email || '').toLowerCase())
      const name = `${p.firstName || ''} ${p.lastName || ''}`.trim()
      return {
        ...p,
        businessName: app?.businessName || (name ? `${name} Services` : 'Tourism Service Provider'),
        serviceType: app?.serviceType || 'Tourism Services',
        location: app?.location || p.nationality || 'Sri Lanka',
        description: app?.description || 'Verified Tourism Service Provider'
      }
    })

  const handleToggleStatus = async (provider) => {
    const newStatus = !provider.isActive
    if (!window.confirm(`Are you sure you want to ${newStatus ? 're-activate' : 'suspend'} provider ${provider.businessName}?`)) return
    setActionLoading(true)
    try {
      const resp = await fetch(`/api/admin/users/${provider.id}/status`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify({ isActive: newStatus })
      })
      if (resp.ok) {
        showToast(`Provider ${provider.businessName} is now ${newStatus ? 'Active' : 'Suspended'}.`)
        if (selectedProvider && selectedProvider.id === provider.id) {
          setSelectedProvider(prev => ({ ...prev, isActive: newStatus }))
        }
        onRefresh && onRefresh()
      } else if (resp.status === 401) {
        onLogout && onLogout()
      } else {
        showToast('Failed to update provider status.')
      }
    } catch {
      showToast('Network error. Please check connection.')
    } finally {
      setActionLoading(false)
    }
  }

  const filtered = providers.filter(p => {
    const q = (search || '').toLowerCase().trim()
    if (!q) return true
    return (
      (p.businessName || '').toLowerCase().includes(q) ||
      (p.firstName || '').toLowerCase().includes(q) ||
      (p.lastName || '').toLowerCase().includes(q) ||
      (p.email || '').toLowerCase().includes(q) ||
      (p.location || '').toLowerCase().includes(q) ||
      (p.serviceType || '').toLowerCase().includes(q)
    )
  })

  return (
    <div className="ad-providers-tab">
      <div className="ad-page-header">
        <div className="ad-page-header__left">
          <h1>Provider Management</h1>
          <p>Monitor certified tourism operators, inspect service profiles, and manage account statuses.</p>
        </div>
      </div>

      {/* Provider Details Modal */}
      {selectedProvider && (
        <Modal title={`Provider Profile: ${selectedProvider.businessName}`} onClose={() => setSelectedProvider(null)} wide>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
            <div className="ad-identity" style={{ marginBottom: 0, paddingBottom: '16px' }}>
              <div className="ad-avatar">{initials(selectedProvider.firstName, selectedProvider.lastName)}</div>
              <div className="ad-identity__info">
                <h3 className="ad-identity__name">{selectedProvider.businessName}</h3>
                <p className="ad-identity__email">{selectedProvider.email}</p>
                <div style={{ display: 'flex', gap: '8px', marginTop: '6px' }}>
                  <span className="ad-badge ad-badge--provider">Certified Partner</span>
                  <span className={`ad-badge ad-badge--${selectedProvider.isActive ? 'active' : 'inactive'}`}>
                    {selectedProvider.isActive ? 'Active' : 'Suspended'}
                  </span>
                </div>
              </div>
            </div>

            <div className="ad-fields">
              <div className="ad-field">
                <span className="ad-field__label">Contact Person</span>
                <span className="ad-field__value">{selectedProvider.firstName} {selectedProvider.lastName}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Service Category</span>
                <span className="ad-field__value">{selectedProvider.serviceType}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Operating Location</span>
                <span className="ad-field__value">📍 {selectedProvider.location}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Phone Number</span>
                <span className="ad-field__value">📞 {selectedProvider.phoneNumber || '—'}</span>
              </div>
              <div className="ad-field pd-field--full">
                <span className="ad-field__label">Business Description</span>
                <span className="ad-field__value" style={{ background: '#faf8f3', padding: '12px 14px', borderRadius: '8px', border: '1px solid #ede8dc' }}>
                  {selectedProvider.description || 'No description provided.'}
                </span>
              </div>
            </div>

            <div style={{ paddingTop: '14px', borderTop: '1px solid #f0ece3', display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
              <button
                className={`ad-quick-btn ${selectedProvider.isActive ? 'ad-quick-btn--danger' : 'ad-quick-btn--success'}`}
                onClick={() => handleToggleStatus(selectedProvider)}
                disabled={actionLoading}
              >
                {selectedProvider.isActive ? 'Suspend Provider Account' : 'Reactivate Provider Account'}
              </button>
            </div>
          </div>
        </Modal>
      )}

      {/* Filter and Search Bar */}
      <div className="ad-filter-bar">
        <div className="ad-search-wrap">
          <span className="ad-search-icon">🔍</span>
          <input
            type="text"
            className="ad-search-input"
            placeholder="Search by business, contact, location..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
      </div>

      {/* Providers Table */}
      <div className="ad-card">
        <div className="ad-card__body" style={{ padding: 0 }}>
          {filtered.length === 0 ? (
            <div className="ad-empty">
              <div className="ad-empty__icon">🏔️</div>
              <p className="ad-empty__title">No approved providers</p>
              <p className="ad-empty__msg">Approved provider accounts will appear here.</p>
            </div>
          ) : (
            <div className="ad-table-wrap" style={{ border: 'none', borderRadius: 0 }}>
              <table className="ad-table">
                <thead>
                  <tr>
                    <th>Business Name</th>
                    <th>Contact Person</th>
                    <th>Category</th>
                    <th>Location</th>
                    <th>Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(provider => (
                    <tr key={provider.id}>
                      <td style={{ fontWeight: 700, color: '#123b5d' }}>{provider.businessName}</td>
                      <td>
                        <div>{provider.firstName} {provider.lastName}</div>
                        <div style={{ fontSize: '11.5px', color: '#888' }}>{provider.email}</div>
                      </td>
                      <td>{provider.serviceType}</td>
                      <td>{provider.location}</td>
                      <td>
                        <span className={`ad-badge ad-badge--${provider.isActive ? 'active' : 'inactive'}`}>
                          {provider.isActive ? 'Active' : 'Suspended'}
                        </span>
                      </td>
                      <td>
                        <div className="ad-row-actions">
                          <button className="ad-row-btn ad-row-btn--view" onClick={() => setSelectedProvider(provider)}>
                            Details
                          </button>
                          <button
                            className="ad-row-btn ad-row-btn--toggle"
                            onClick={() => handleToggleStatus(provider)}
                          >
                            {provider.isActive ? 'Suspend' : 'Activate'}
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

// ── 5. Bookings Overview Tab ──────────────────────────────────────────────────

function BookingsOverviewTab({ bookings }) {
  const [search, setSearch] = useState('')
  const [filterStatus, setFilterStatus] = useState('all') // all | pending | confirmed | completed | cancelled
  const [selectedBooking, setSelectedBooking] = useState(null)

  const filtered = bookings.filter(b => {
    const matchSearch =
      b.visitorName?.toLowerCase().includes(search.toLowerCase()) ||
      b.id?.toLowerCase().includes(search.toLowerCase()) ||
      b.activityName?.toLowerCase().includes(search.toLowerCase())
    if (!matchSearch) return false
    if (filterStatus !== 'all' && b.status.toLowerCase() !== filterStatus.toLowerCase()) return false
    return true
  })

  return (
    <div className="ad-bookings-tab">
      <div className="ad-page-header">
        <div className="ad-page-header__left">
          <h1>Platform Bookings Overview</h1>
          <p>Monitor all visitor reservation activity across Sri Lanka tourism services.</p>
        </div>
      </div>

      {/* Booking Details Modal */}
      {selectedBooking && (
        <Modal title={`Booking Details: ${selectedBooking.id}`} onClose={() => setSelectedBooking(null)} wide>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', paddingBottom: '16px', borderBottom: '1px solid #f0ece3' }}>
              <div>
                <h3 style={{ margin: '0 0 4px', color: '#123b5d' }}>{selectedBooking.activityName}</h3>
                <span style={{ fontSize: '13px', color: '#777' }}>Ref: <strong>{selectedBooking.id}</strong></span>
              </div>
              <span className={`ad-badge ad-badge--${selectedBooking.status.toLowerCase()}`}>
                {selectedBooking.status}
              </span>
            </div>

            <div className="ad-fields">
              <div className="ad-field">
                <span className="ad-field__label">Visitor</span>
                <span className="ad-field__value">👤 {selectedBooking.visitorName}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Email</span>
                <span className="ad-field__value">✉️ {selectedBooking.visitorEmail}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Scheduled Date</span>
                <span className="ad-field__value">📅 {formatDate(selectedBooking.date)}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Party Size</span>
                <span className="ad-field__value">👥 {selectedBooking.guests} Guests</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Payment</span>
                <span className="ad-field__value">💳 {selectedBooking.paymentStatus}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Total Amount</span>
                <span className="ad-field__value" style={{ fontWeight: 800, color: '#168aad' }}>
                  LKR {selectedBooking.totalAmount?.toLocaleString()}
                </span>
              </div>
            </div>
          </div>
        </Modal>
      )}

      {/* Filter and Search Bar */}
      <div className="ad-filter-bar">
        <div className="ad-search-wrap">
          <span className="ad-search-icon">🔍</span>
          <input
            type="text"
            className="ad-search-input"
            placeholder="Search bookings by visitor, activity, ID..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <div className="ad-filter-pills">
          <button className={`ad-filter-pill ${filterStatus === 'all' ? 'active' : ''}`} onClick={() => setFilterStatus('all')}>
            All ({bookings.length})
          </button>
          <button className={`ad-filter-pill ${filterStatus === 'pending' ? 'active' : ''}`} onClick={() => setFilterStatus('pending')}>
            Pending ({bookings.filter(b => b.status === 'Pending').length})
          </button>
          <button className={`ad-filter-pill ${filterStatus === 'confirmed' ? 'active' : ''}`} onClick={() => setFilterStatus('confirmed')}>
            Confirmed ({bookings.filter(b => b.status === 'Confirmed').length})
          </button>
          <button className={`ad-filter-pill ${filterStatus === 'completed' ? 'active' : ''}`} onClick={() => setFilterStatus('completed')}>
            Completed ({bookings.filter(b => b.status === 'Completed').length})
          </button>
        </div>
      </div>

      {/* Bookings Table */}
      <div className="ad-card">
        <div className="ad-card__body" style={{ padding: 0 }}>
          {filtered.length === 0 ? (
            <div className="ad-empty">
              <div className="ad-empty__icon">📅</div>
              <p className="ad-empty__title">No bookings recorded</p>
              <p className="ad-empty__msg">Platform booking activities and visitor reservations will appear here.</p>
            </div>
          ) : (
            <div className="ad-table-wrap" style={{ border: 'none', borderRadius: 0 }}>
              <table className="ad-table">
                <thead>
                  <tr>
                    <th>Ref #</th>
                    <th>Visitor</th>
                    <th>Activity / Service</th>
                    <th>Scheduled Date</th>
                    <th>Guests</th>
                    <th>Total</th>
                    <th>Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(b => (
                    <tr key={b.id}>
                      <td style={{ fontWeight: 700, color: '#168aad' }}>{b.id}</td>
                      <td>
                        <div style={{ fontWeight: 600, color: '#123b5d' }}>{b.visitorName}</div>
                        <div style={{ fontSize: '11.5px', color: '#888' }}>{b.visitorEmail}</div>
                      </td>
                      <td>{b.activityName}</td>
                      <td>{formatDate(b.date)}</td>
                      <td>{b.guests}</td>
                      <td style={{ fontWeight: 700, color: '#123b5d' }}>LKR {b.totalAmount?.toLocaleString()}</td>
                      <td>
                        <span className={`ad-badge ad-badge--${b.status.toLowerCase()}`}>
                          {b.status}
                        </span>
                      </td>
                      <td>
                        <button className="ad-row-btn ad-row-btn--view" onClick={() => setSelectedBooking(b)}>
                          Inspect
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

// ── 6. Notifications Tab ──────────────────────────────────────────────────────

function NotificationsTab({ notifications, onMarkAllRead, onToggleRead, onClearAll }) {
  const [filter, setFilter] = useState('all') // all | applications | users | system

  const filtered = notifications.filter(n => {
    if (filter === 'all') return true
    return n.category === filter
  })

  const unreadCount = notifications.filter(n => !n.read).length

  return (
    <div className="ad-notifications-tab">
      <div className="ad-page-header">
        <div className="ad-page-header__left">
          <h1>System & Application Notifications</h1>
          <p>Important administrative alerts, provider application submissions, and platform events.</p>
        </div>
        <div style={{ display: 'flex', gap: '8px' }}>
          {unreadCount > 0 && (
            <button className="ad-quick-btn ad-quick-btn--secondary" onClick={onMarkAllRead}>
              ✓ Mark All Read
            </button>
          )}
          {notifications.length > 0 && (
            <button className="ad-quick-btn ad-quick-btn--secondary" onClick={onClearAll}>
              Clear All
            </button>
          )}
        </div>
      </div>

      <div className="ad-filter-pills" style={{ marginBottom: '20px' }}>
        <button className={`ad-filter-pill ${filter === 'all' ? 'active' : ''}`} onClick={() => setFilter('all')}>
          All ({notifications.length})
        </button>
        <button className={`ad-filter-pill ${filter === 'applications' ? 'active' : ''}`} onClick={() => setFilter('applications')}>
          📋 Applications ({notifications.filter(n => n.category === 'applications').length})
        </button>
        <button className={`ad-filter-pill ${filter === 'users' ? 'active' : ''}`} onClick={() => setFilter('users')}>
          👥 Users ({notifications.filter(n => n.category === 'users').length})
        </button>
        <button className={`ad-filter-pill ${filter === 'system' ? 'active' : ''}`} onClick={() => setFilter('system')}>
          📢 System ({notifications.filter(n => n.category === 'system').length})
        </button>
      </div>

      {filtered.length === 0 ? (
        <div className="ad-card">
          <div className="ad-card__body">
            <div className="ad-empty">
              <div className="ad-empty__icon">🔔</div>
              <p className="ad-empty__title">No notifications</p>
              <p className="ad-empty__msg">You are caught up with all administrative notifications in this category.</p>
            </div>
          </div>
        </div>
      ) : (
        <div className="ad-notif-list">
          {filtered.map(n => (
            <div
              key={n.id}
              className={`ad-notif-item ${!n.read ? 'ad-notif-item--unread' : ''}`}
              style={{ cursor: 'pointer' }}
              onClick={() => onToggleRead && onToggleRead(n.id)}
            >
              <div
                className="ad-notif-icon"
                style={{
                  background: n.category === 'applications' ? 'rgba(214, 168, 95, 0.2)' : n.category === 'users' ? 'rgba(22, 138, 173, 0.15)' : 'rgba(18, 59, 93, 0.12)'
                }}
              >
                {n.category === 'applications' ? '📋' : n.category === 'users' ? '👥' : '📢'}
              </div>
              <div className="ad-notif-content">
                <h3 className="ad-notif-title">{n.title}</h3>
                <p className="ad-notif-desc">{n.desc}</p>
                <span className="ad-notif-time">{n.time}</span>
              </div>
              {!n.read && <div className="ad-notif-dot" title="Unread" />}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

// ── 7. Admin Account Tab ──────────────────────────────────────────────────────

function AdminAccountTab({ token, onLogout, showToast }) {
  const [profile, setProfile] = useState(null)
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState(null)
  const [editing, setEditing] = useState(false)
  const [formData, setFormData] = useState({})
  const [saveLoading, setSaveLoading] = useState(false)
  const [saveError, setSaveError] = useState(null)

  const fetchProfile = useCallback(async () => {
    setLoading(true)
    setLoadError(null)
    try {
      const resp = await fetch('/api/users/me', {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        const data = await resp.json()
        setProfile(data)
        setFormData({
          firstName: data.firstName || '',
          lastName: data.lastName || '',
          phoneNumber: data.phoneNumber || '',
          nationality: data.nationality || ''
        })
      } else if (resp.status === 401) {
        onLogout && onLogout()
      } else {
        setLoadError('Failed to load admin profile.')
      }
    } catch {
      setLoadError('Network error. Please check your connection.')
    } finally {
      setLoading(false)
    }
  }, [token, onLogout])

  useEffect(() => { fetchProfile() }, [fetchProfile])

  const handleEdit = () => {
    setSaveError(null)
    setEditing(true)
  }

  const handleCancel = () => {
    setFormData({
      firstName: profile.firstName || '',
      lastName: profile.lastName || '',
      phoneNumber: profile.phoneNumber || '',
      nationality: profile.nationality || ''
    })
    setSaveError(null)
    setEditing(false)
  }

  const handleChange = (e) => {
    setFormData(prev => ({ ...prev, [e.target.name]: e.target.value }))
  }

  const handleSave = async (e) => {
    e.preventDefault()
    setSaveError(null)
    setSaveLoading(true)

    try {
      const resp = await fetch('/api/users/me', {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify(formData)
      })

      if (resp.ok) {
        const body = await resp.json()
        const updated = body.profile ?? body
        setProfile(updated)
        setFormData({
          firstName: updated.firstName,
          lastName: updated.lastName,
          phoneNumber: updated.phoneNumber,
          nationality: updated.nationality
        })
        setEditing(false)
        showToast('Admin profile updated successfully.')
      } else if (resp.status === 401) {
        onLogout && onLogout()
      } else if (resp.status === 400 || resp.status === 422) {
        const body = await resp.json().catch(() => ({}))
        const first = body.errors && Object.values(body.errors).flat()[0]
        setSaveError(first || body.message || 'Validation error. Check your input.')
      } else {
        setSaveError('Server error. Please try again.')
      }
    } catch {
      setSaveError('Network error. Please check your connection.')
    } finally {
      setSaveLoading(false)
    }
  }

  return (
    <div className="ad-account-tab">
      <div className="ad-page-header">
        <div className="ad-page-header__left">
          <h1>Admin Account Settings</h1>
          <p>View and manage administrative profile credentials.</p>
        </div>
      </div>

      <div className="ad-card">
        <div className="ad-card__body">
          {loading && <LoadingState label="Loading profile information…" />}
          {loadError && !loading && <div className="ad-form-error">{loadError}</div>}

          {!loading && profile && !editing && (
            <>
              <div className="ad-identity">
                <div className="ad-avatar">{initials(profile.firstName, profile.lastName)}</div>
                <div className="ad-identity__info">
                  <h2 className="ad-identity__name">{profile.firstName} {profile.lastName}</h2>
                  <p className="ad-identity__email">{profile.email}</p>
                  <span className="ad-identity__badge">👑 System Administrator</span>
                </div>
                <button className="ad-edit-btn" onClick={handleEdit} id="edit-admin-profile-btn">
                  ✏️ Edit Profile
                </button>
              </div>

              <div className="ad-fields">
                <div className="ad-field">
                  <span className="ad-field__label">First Name</span>
                  <span className="ad-field__value">{profile.firstName}</span>
                </div>
                <div className="ad-field">
                  <span className="ad-field__label">Last Name</span>
                  <span className="ad-field__value">{profile.lastName}</span>
                </div>
                <div className="ad-field">
                  <span className="ad-field__label">Official Email</span>
                  <span className="ad-field__value">{profile.email}</span>
                </div>
                <div className="ad-field">
                  <span className="ad-field__label">Phone Number</span>
                  <span className="ad-field__value">{profile.phoneNumber || '—'}</span>
                </div>
                <div className="ad-field">
                  <span className="ad-field__label">Nationality</span>
                  <span className="ad-field__value">{profile.nationality || '—'}</span>
                </div>
                <div className="ad-field">
                  <span className="ad-field__label">Member Since</span>
                  <span className="ad-field__value">{formatDate(profile.createdAt)}</span>
                </div>
              </div>
            </>
          )}

          {!loading && profile && editing && (
            <form onSubmit={handleSave} className="ad-edit-form" noValidate>
              <div className="ad-identity" style={{ marginBottom: '24px' }}>
                <div className="ad-avatar">{initials(formData.firstName, formData.lastName)}</div>
                <div className="ad-identity__info">
                  <h2 className="ad-identity__name">{formData.firstName} {formData.lastName}</h2>
                  <p className="ad-identity__email">{profile.email}</p>
                </div>
              </div>

              {saveError && <div className="ad-form-error">{saveError}</div>}

              <div className="ad-form-grid">
                <div className="ad-form-group">
                  <label htmlFor="adm-firstName">First Name *</label>
                  <input
                    id="adm-firstName"
                    name="firstName"
                    type="text"
                    value={formData.firstName}
                    onChange={handleChange}
                    required
                  />
                </div>
                <div className="ad-form-group">
                  <label htmlFor="adm-lastName">Last Name *</label>
                  <input
                    id="adm-lastName"
                    name="lastName"
                    type="text"
                    value={formData.lastName}
                    onChange={handleChange}
                    required
                  />
                </div>
                <div className="ad-form-group">
                  <label htmlFor="adm-email">Email Address</label>
                  <input id="adm-email" type="email" value={profile.email} disabled aria-readonly="true" />
                  <p className="ad-field-note">Admin email address cannot be changed directly.</p>
                </div>
                <div className="ad-form-group">
                  <label htmlFor="adm-phone">Phone Number *</label>
                  <input
                    id="adm-phone"
                    name="phoneNumber"
                    type="tel"
                    value={formData.phoneNumber}
                    onChange={handleChange}
                    required
                  />
                </div>
                <div className="ad-form-group ad-form-group--full">
                  <label htmlFor="adm-nationality">Nationality *</label>
                  <input
                    id="adm-nationality"
                    name="nationality"
                    type="text"
                    value={formData.nationality}
                    onChange={handleChange}
                    required
                  />
                </div>
              </div>

              <div className="ad-form-actions">
                <button type="submit" className="ad-save-btn" disabled={saveLoading}>
                  {saveLoading ? 'Saving…' : 'Save Changes'}
                </button>
                <button type="button" className="ad-cancel-btn" onClick={handleCancel} disabled={saveLoading}>
                  Cancel
                </button>
              </div>
            </form>
          )}
        </div>
      </div>
    </div>
  )
}

// ── 7. Reports Tab ───────────────────────────────────────────────────────────

function ReportsTab({ token, onLogout }) {
  const emptyFilters = { dateFrom: '', dateTo: '', role: '', applicationStatus: '' }
  const [filters, setFilters] = useState(emptyFilters)
  const [appliedFilters, setAppliedFilters] = useState({})
  const [report, setReport] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  const buildQuery = (f) => {
    const params = new URLSearchParams()
    if (f.dateFrom)           params.append('dateFrom', f.dateFrom)
    if (f.dateTo)             params.append('dateTo',   f.dateTo)
    if (f.role)               params.append('role',     f.role)
    if (f.applicationStatus)  params.append('applicationStatus', f.applicationStatus)
    return params.toString()
  }

  const fetchReport = useCallback(async (activeFilters) => {
    if (!token) return
    setLoading(true)
    setError(null)
    try {
      const qs = buildQuery(activeFilters)
      const resp = await fetch(`/api/admin/reports${qs ? '?' + qs : ''}`, {
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
      <div className="ad-section-header">
        <div>
          <h1 className="ad-section-title">📈 Registration &amp; Verification Report</h1>
          <p className="ad-section-sub">
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
          {/* ── Registration Summary ── */}
          <section className="ad-report__section">
            <h2 className="ad-report__section-title">👥 User Registrations</h2>
            <div className="ad-report__cards">
              <div className="ad-report__card ad-report__card--total">
                <span className="ad-report__card-value">{r?.totalUsers ?? 0}</span>
                <span className="ad-report__card-label">Total Users</span>
              </div>
              <div className="ad-report__card ad-report__card--visitor">
                <span className="ad-report__card-value">{r?.totalVisitors ?? 0}</span>
                <span className="ad-report__card-label">Visitors</span>
              </div>
              <div className="ad-report__card ad-report__card--provider">
                <span className="ad-report__card-value">{r?.totalProviders ?? 0}</span>
                <span className="ad-report__card-label">Providers</span>
              </div>
              <div className="ad-report__card ad-report__card--admin">
                <span className="ad-report__card-value">{r?.totalAdmins ?? 0}</span>
                <span className="ad-report__card-label">Admins</span>
              </div>
              <div className="ad-report__card ad-report__card--active">
                <span className="ad-report__card-value">{r?.activeUsers ?? 0}</span>
                <span className="ad-report__card-label">Active</span>
              </div>
              <div className="ad-report__card ad-report__card--inactive">
                <span className="ad-report__card-value">{r?.inactiveUsers ?? 0}</span>
                <span className="ad-report__card-label">Inactive</span>
              </div>
            </div>
          </section>

          {/* ── Application Summary ── */}
          <section className="ad-report__section">
            <h2 className="ad-report__section-title">📋 Provider Applications</h2>
            <div className="ad-report__cards">
              <div className="ad-report__card ad-report__card--total">
                <span className="ad-report__card-value">{a?.totalApplications ?? 0}</span>
                <span className="ad-report__card-label">Total</span>
              </div>
              <div className="ad-report__card ad-report__card--pending">
                <span className="ad-report__card-value">{a?.pendingApplications ?? 0}</span>
                <span className="ad-report__card-label">Pending</span>
              </div>
              <div className="ad-report__card ad-report__card--approved">
                <span className="ad-report__card-value">{a?.approvedApplications ?? 0}</span>
                <span className="ad-report__card-label">Approved</span>
              </div>
              <div className="ad-report__card ad-report__card--rejected">
                <span className="ad-report__card-value">{a?.rejectedApplications ?? 0}</span>
                <span className="ad-report__card-label">Rejected</span>
              </div>
            </div>

            {/* ── Service Type Breakdown ── */}
            {a?.byServiceType?.length > 0 && (
              <div className="ad-report__breakdown">
                <h3 className="ad-report__breakdown-title">Applications by Service Type</h3>
                <table className="ad-report__table" id="ad-report-service-type-table">
                  <thead>
                    <tr>
                      <th>Service Type</th>
                      <th>Applications</th>
                      <th>Share</th>
                    </tr>
                  </thead>
                  <tbody>
                    {a.byServiceType.map(row => (
                      <tr key={row.serviceType}>
                        <td>{row.serviceType}</td>
                        <td>{row.count}</td>
                        <td>
                          <div className="ad-report__share-bar">
                            <div
                              className="ad-report__share-fill"
                              style={{ width: `${Math.round((row.count / a.totalApplications) * 100)}%` }}
                            />
                            <span>{Math.round((row.count / a.totalApplications) * 100)}%</span>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </div>
  )
}

// ── Root Admin Dashboard Component ────────────────────────────────────────────

function AdminDashboard({ onLogout }) {
  const [activeTab, setActiveTab] = useState('overview')
  const [toast, setToast] = useState(null)

  const [stats, setStats] = useState(null)
  const [users, setUsers] = useState([])
  const [applications, setApplications] = useState([])
  const [bookings, setBookings] = useState(() => {
    try {
      const saved = localStorage.getItem('ceylonquest_provider_bookings')
      return saved ? JSON.parse(saved) : []
    } catch {
      return []
    }
  })
  const [notifications, setNotifications] = useState([])

  const token = localStorage.getItem('authToken')
  const role = localStorage.getItem('userRole')

  // Auth Guard
  useEffect(() => {
    if (!token || role !== 'Admin') {
      onLogout && onLogout()
    }
  }, [token, role, onLogout])

  const showToast = useCallback((msg) => setToast(msg), [])

  // Fetch Stats
  const fetchStats = useCallback(async () => {
    if (!token) return
    try {
      const resp = await fetch('/api/admin/stats', {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        setStats(await resp.json())
      } else if (resp.status === 401) {
        localStorage.removeItem('authToken')
        localStorage.removeItem('userRole')
        onLogout && onLogout()
      }
    } catch {
      // ignore
    }
  }, [token, onLogout])

  // Fetch Users
  const fetchUsers = useCallback(async () => {
    if (!token) return
    try {
      const resp = await fetch('/api/admin/users', {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        const data = await resp.json()
        setUsers(Array.isArray(data) ? data : (data ? [data] : []))
      } else if (resp.status === 401) {
        localStorage.removeItem('authToken')
        localStorage.removeItem('userRole')
        onLogout && onLogout()
      }
    } catch (err) {
      console.error('Failed to fetch users:', err)
    }
  }, [token, onLogout])

  // Fetch Provider Applications
  const fetchApplications = useCallback(async () => {
    if (!token) return
    try {
      const resp = await fetch('/api/admin/provider-applications', {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        const data = await resp.json()
        setApplications(Array.isArray(data) ? data : (data ? [data] : []))
      } else if (resp.status === 401) {
        localStorage.removeItem('authToken')
        localStorage.removeItem('userRole')
        onLogout && onLogout()
      }
    } catch (err) {
      console.error('Failed to fetch provider applications:', err)
    }
  }, [token, onLogout])

  const handleRefreshAll = useCallback(() => {
    fetchStats()
    fetchUsers()
    fetchApplications()
  }, [fetchStats, fetchUsers, fetchApplications])

  useEffect(() => {
    handleRefreshAll()
  }, [handleRefreshAll, activeTab])

  const handleMarkAllNotificationsRead = () => {
    setNotifications(prev => prev.map(n => ({ ...n, read: true })))
    showToast('All notifications marked as read.')
  }

  const handleToggleNotificationRead = (notifId) => {
    setNotifications(prev => prev.map(n => n.id === notifId ? { ...n, read: !n.read } : n))
  }

  const handleClearNotifications = () => {
    setNotifications([])
    showToast('Notifications cleared.')
  }

  const handleLogout = () => {
    localStorage.removeItem('authToken')
    localStorage.removeItem('userRole')
    onLogout && onLogout()
  }

  const appList = Array.isArray(applications) ? applications : (applications ? [applications] : [])
  const pendingAppsCount = appList.filter(a => (a.status || '').toLowerCase() === 'pending').length
  const unreadNotifCount = notifications.filter(n => !n.read).length

  const navItems = [
    { key: 'overview',      icon: '📊', label: 'Dashboard Overview' },
    { key: 'applications',  icon: '📋', label: 'Provider Applications', badge: pendingAppsCount > 0 ? pendingAppsCount : null },
    { key: 'users',         icon: '👥', label: 'User Management' },
    { key: 'providers',     icon: '🏔️', label: 'Provider Management' },
    { key: 'bookings',      icon: '📅', label: 'Bookings Overview' },
    { key: 'reports',       icon: '📈', label: 'Reports' },
    { key: 'notifications', icon: '🔔', label: 'Notifications', badge: unreadNotifCount > 0 ? unreadNotifCount : null },
    { key: 'account',       icon: '👤', label: 'Admin Account' }
  ]

  return (
    <div className="ad-page">
      {toast && <Toast message={toast} onClose={() => setToast(null)} />}

      {/* ── Sidebar ── */}
      <aside className="ad-sidebar">
        <div className="ad-sidebar__brand">
          <span className="ad-sidebar__logo">CeylonQuest</span>
          <span className="ad-sidebar__role">👑 Admin Portal</span>
        </div>

        <ul className="ad-sidebar__nav">
          {navItems.map(item => (
            <li key={item.key}>
              <button
                className={activeTab === item.key ? 'active' : ''}
                onClick={() => setActiveTab(item.key)}
                id={`ad-nav-${item.key}`}
              >
                <span className="ad-nav-icon">{item.icon}</span>
                <span>{item.label}</span>
                {item.badge && <span className="ad-nav-badge">{item.badge}</span>}
              </button>
            </li>
          ))}
        </ul>

        <div className="ad-sidebar__footer">
          <button className="ad-logout-btn" onClick={handleLogout} id="ad-logout-btn">
            <span className="ad-nav-icon">🚪</span> Log Out
          </button>
        </div>
      </aside>

      {/* ── Main Content Body ── */}
      <main className="ad-main">
        {activeTab === 'overview' && (
          <OverviewTab
            stats={stats}
            users={users}
            applications={applications}
            bookings={bookings}
            onNavigate={(tab) => setActiveTab(tab)}
          />
        )}

        {activeTab === 'applications' && (
          <ProviderApplicationsTab
            token={token}
            applications={applications}
            onRefresh={fetchApplications}
            showToast={showToast}
          />
        )}

        {activeTab === 'users' && (
          <UserManagementTab
            token={token}
            onLogout={handleLogout}
            users={users}
            onRefresh={handleRefreshAll}
            showToast={showToast}
          />
        )}

        {activeTab === 'providers' && (
          <ProviderManagementTab
            token={token}
            onLogout={handleLogout}
            users={users}
            applications={applications}
            onRefresh={handleRefreshAll}
            showToast={showToast}
          />
        )}

        {activeTab === 'bookings' && (
          <BookingsOverviewTab
            bookings={bookings}
          />
        )}

        {activeTab === 'reports' && (
          <ReportsTab
            token={token}
            onLogout={handleLogout}
          />
        )}

        {activeTab === 'notifications' && (
          <NotificationsTab
            notifications={notifications}
            onMarkAllRead={handleMarkAllNotificationsRead}
            onToggleRead={handleToggleNotificationRead}
            onClearAll={handleClearNotifications}
          />
        )}

        {activeTab === 'account' && (
          <AdminAccountTab
            token={token}
            onLogout={handleLogout}
            showToast={showToast}
          />
        )}
      </main>
    </div>
  )
}

export default AdminDashboard
