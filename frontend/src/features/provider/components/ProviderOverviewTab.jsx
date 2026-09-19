import React from 'react'
import {
  StorefrontIcon,
  KitesurfingIcon,
  CalendarMonthIcon,
  MonetizationOnIcon,
  ManageSearchIcon,
  VerifiedUserIcon,
  LocalPhoneIcon,
  MyLocationIcon,
  AddIcon,
  CreateIcon,
  SettingsIcon
} from '../../../components/Icons'

function formatCurrency(amount) {
  return new Intl.NumberFormat('en-LK', { style: 'currency', currency: 'LKR', maximumFractionDigits: 0 }).format(amount || 0)
}

function formatDate(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
}

export default function OverviewTab({ providerInfo, services = [], bookings = [], notifications = [], onNavigate }) {
  const activeServices = services.filter(s => s.isActive !== false)
  const pendingBookings = bookings.filter(b => b.status === 'Pending')
  const confirmedBookings = bookings.filter(b => b.status === 'Confirmed')
  const completedBookings = bookings.filter(b => b.status === 'Completed')

  const totalRevenue = bookings
    .filter(b => b.status === 'Completed' || b.status === 'Confirmed')
    .reduce((sum, b) => sum + (b.totalAmount || 0), 0)

  const recentBookings = bookings.slice(0, 3)
  const recentNotifs = notifications.slice(0, 3)

  return (
    <div className="pd-overview">
      <div className="pd-page-header">
        <div className="pd-page-header__left">
          <h1>Dashboard Overview</h1>
          <p>
            Welcome back, <span className="pd-bold-text">{providerInfo?.businessName || providerInfo?.firstName || 'Provider'}</span>!
          </p>
        </div>
      </div>

      {/* Verification Status Banner */}
      <div className="pd-verification-banner">
        <div className="pd-verification-banner__left">
          <div className="pd-verification-badge-icon"><VerifiedUserIcon size={24} /></div>
          <div>
            <h2 className="pd-verification-banner__title">Verified and Approved Partner</h2>
            <p className="pd-verification-banner__desc">
              Your business is officially certified to accept visitor bookings and list tourism services across Sri Lanka.
            </p>
          </div>
        </div>
      </div>

      {/* Metric Cards */}
      <div className="pd-metrics-grid">
        <div className="pd-metric-card pd-cursor-pointer" onClick={() => onNavigate('services')}>
          <div className="pd-metric-icon pd-metric-icon--teal"><KitesurfingIcon size={24} /></div>
          <div className="pd-metric-info">
            <div className="pd-metric-title">Listings</div>
            <div className="pd-metric-value">{activeServices.length} Active</div>
            <div className="pd-metric-sub">{services.length} Total Registered</div>
          </div>
        </div>

        <div className="pd-metric-card pd-cursor-pointer" onClick={() => onNavigate('bookings')}>
          <div className="pd-metric-icon pd-metric-icon--blue"><CalendarMonthIcon size={24} /></div>
          <div className="pd-metric-info">
            <div className="pd-metric-title">Total Bookings</div>
            <div className="pd-metric-value">{bookings.length}</div>
            <div className="pd-metric-sub">{pendingBookings.length} Pending, {confirmedBookings.length} Confirmed</div>
          </div>
        </div>

        <div className="pd-metric-card">
          <div className="pd-metric-icon pd-metric-icon--green"><MonetizationOnIcon size={24} /></div>
          <div className="pd-metric-info">
            <div className="pd-metric-title">Estimated Earnings</div>
            <div className="pd-metric-value pd-metric-value--large">{formatCurrency(totalRevenue)}</div>
            <div className="pd-metric-sub">{completedBookings.length} Completed Trips</div>
          </div>
        </div>
      </div>

      {/* Quick Actions Bar */}
      <div className="pd-quick-actions">
        <button className="pd-quick-btn pd-quick-btn--primary" onClick={() => onNavigate('services')}>
          <AddIcon size={16} /> Add New Listing
        </button>
        <button className="pd-quick-btn pd-quick-btn--secondary" onClick={() => onNavigate('business')}>
          <CreateIcon size={16} /> Edit Business Profile
        </button>
        <button className="pd-quick-btn pd-quick-btn--secondary" onClick={() => onNavigate('bookings')}>
          <CalendarMonthIcon size={16} /> Manage Bookings ({pendingBookings.length} action required)
        </button>
        <button className="pd-quick-btn pd-quick-btn--secondary" onClick={() => onNavigate('account')}>
          <SettingsIcon size={16} /> Account Settings
        </button>
      </div>

      {/* Top Full-Width Card: Business & Services Summary */}
      <div className="pd-card pd-overview-top-card">
        <div className="pd-card__body">
          <div className="pd-section-header">
            <h2>Business Profile Summary</h2>
            <button className="pd-row-btn pd-row-btn--edit" onClick={() => onNavigate('business')}>Edit</button>
          </div>
          <div className="pd-fields pd-fields--4cols">
            <div className="pd-field">
              <span className="pd-field__label">Business Name</span>
              <span className="pd-field__value pd-title-primary">
                {providerInfo?.businessName || '—'}
              </span>
            </div>
            <div className="pd-field">
              <span className="pd-field__label">Service Category</span>
              <span className="pd-field__value">
                {providerInfo?.serviceType || 'Tourism Provider'}
              </span>
            </div>
            <div className="pd-field">
              <span className="pd-field__label">Business Phone</span>
              <span className="pd-field__value">
                <LocalPhoneIcon size={15} className="pd-icon-spacing" /> {providerInfo?.phoneNumber || '—'}
              </span>
            </div>
            <div className="pd-field">
              <span className="pd-field__label">Primary Operation Region</span>
              <span className="pd-field__value">
                <MyLocationIcon size={15} className="pd-icon-spacing" /> {providerInfo?.location || 'Sri Lanka'}
              </span>
            </div>
          </div>

          <div className="pd-section-header pd-mt-28">
            <h2>Active Listings ({activeServices.length})</h2>
            <button className="pd-row-btn pd-row-btn--edit" onClick={() => onNavigate('services')}>View All</button>
          </div>
          {activeServices.length === 0 ? (
            <p className="pd-muted-13">No active services listed yet. Click &quot;Add New Listing&quot; to begin.</p>
          ) : (
            <ul className="pd-clean-list">
              {activeServices.slice(0, 4).map(s => (
                <li key={s.id} className="pd-list-item-between">
                  <span className="pd-title-primary">{s.title || s.name || s.roomType}</span>
                  <span className="pd-price-teal">
                    {formatCurrency(s.price || s.pricePerPerson || s.pricePerNight)}
                    <small className="pd-unit-muted">/{s.unit || (s.pricePerPerson ? 'person' : 'night')}</small>
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      {/* Bottom Row: Recent Bookings & Notifications in 1 row */}
      <div className="pd-overview-bottom-grid">
        {/* Recent Bookings Card */}
        <div className="pd-card pd-mb-0">
          <div className="pd-card__body">
            <div className="pd-section-header">
              <h2>Recent Bookings</h2>
              <button className="pd-row-btn pd-row-btn--edit" onClick={() => onNavigate('bookings')}>All Bookings</button>
            </div>
            {recentBookings.length === 0 ? (
              <p className="pd-muted-13">No bookings received yet.</p>
            ) : (
              <div className="pd-column-gap-10">
                {recentBookings.map(b => (
                  <div key={b.id} className="pd-recent-booking-card">
                    <div>
                      <div className="pd-bold-13">{b.visitorName || b.customerName}</div>
                      <div className="pd-muted-12">{b.activityName || b.serviceTitle} • {formatDate(b.date || b.bookingDate)}</div>
                    </div>
                    <div className="pd-text-right">
                      <span className={`pd-status-pill pd-status-pill--${b.status?.toLowerCase()}`}>{b.status}</span>
                      <div className="pd-amount-text">{formatCurrency(b.totalAmount || b.amount)}</div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Recent Notifications Card */}
        <div className="pd-card pd-mb-0">
          <div className="pd-card__body">
            <div className="pd-section-header">
              <h2>Recent Notifications</h2>
              <button className="pd-row-btn pd-row-btn--edit" onClick={() => onNavigate('notifications')}>View All</button>
            </div>
            {recentNotifs.length === 0 ? (
              <p className="pd-muted-13">No new notifications.</p>
            ) : (
              <div className="pd-column-gap-10">
                {recentNotifs.map(n => (
                  <div key={n.id} className="pd-notif-preview-item">
                    <span className="pd-notif-icon-wrap"><ManageSearchIcon size={18} /></span>
                    <div className="pd-flex-1">
                      <div className="pd-notif-title">{n.title}</div>
                      <div className="pd-notif-desc">{n.message || n.desc}</div>
                    </div>
                    <span className="pd-notif-time">{n.time || formatDate(n.createdAt)}</span>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}