import React, { useState } from 'react'
import {
  CalendarMonthIcon,
  ManageSearchIcon
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
          <button className="ad-modal__close" onClick={onClose} aria-label="Close modal"></button>
        </div>
        <div className="ad-modal__body">{children}</div>
      </div>
    </div>
  )
}


export default function BookingsOverviewTab({ bookings }) {
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
                <span className="ad-field__value"> {selectedBooking.visitorName}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Email</span>
                <span className="ad-field__value"> {selectedBooking.visitorEmail}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Scheduled Date</span>
                <span className="ad-field__value"> {formatDate(selectedBooking.date)}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Party Size</span>
                <span className="ad-field__value"> {selectedBooking.guests} Guests</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Payment</span>
                <span className="ad-field__value"> {selectedBooking.paymentStatus}</span>
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
          <span className="ad-search-icon"></span>
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
              <div className="ad-empty__icon"></div>
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
                      <td className="ad-text-teal">{b.id}</td>
                      <td>
                        <div className="ad-title-primary">{b.visitorName}</div>
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
