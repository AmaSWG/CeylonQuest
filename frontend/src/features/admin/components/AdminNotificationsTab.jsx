import React from 'react'
import {
  NotificationsActiveIcon,
  CheckCircleIcon,
  DeleteSweepIcon
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


export default function NotificationsTab({ notifications, onMarkAllRead, onToggleRead, onClearAll }) {
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
               Mark All Read
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
          <DocumentScannerIcon size={14} className="ad-icon-spacing" /> Applications ({notifications.filter(n => n.category === 'applications').length})
        </button>
        <button className={`ad-filter-pill ${filter === 'users' ? 'active' : ''}`} onClick={() => setFilter('users')}>
          <GroupIcon size={14} className="ad-icon-spacing" /> Users ({notifications.filter(n => n.category === 'users').length})
        </button>
        <button className={`ad-filter-pill ${filter === 'system' ? 'active' : ''}`} onClick={() => setFilter('system')}>
          <NotificationsActiveIcon size={14} className="ad-icon-spacing" /> System ({notifications.filter(n => n.category === 'system').length})
        </button>
      </div>

      {filtered.length === 0 ? (
        <div className="ad-card">
          <div className="ad-card__body">
            <div className="ad-empty">
              <div className="ad-empty__icon"><NotificationsActiveIcon size={32} /></div>
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
              className="ad-cursor-pointer"
              onClick={() => onToggleRead && onToggleRead(n.id)}
            >
              <div
                className="ad-notif-icon"
                style={{
                  background: n.category === 'applications' ? 'rgba(214, 168, 95, 0.2)' : n.category === 'users' ? 'rgba(22, 138, 173, 0.15)' : 'rgba(18, 59, 93, 0.12)',
                  color: n.category === 'applications' ? '#b8860b' : n.category === 'users' ? '#168aad' : '#123b5d'
                }}
              >
                {n.category === 'applications' ? (
                  <DocumentScannerIcon size={20} />
                ) : n.category === 'users' ? (
                  <GroupIcon size={20} />
                ) : (
                  <NotificationsActiveIcon size={20} />
                )}
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
