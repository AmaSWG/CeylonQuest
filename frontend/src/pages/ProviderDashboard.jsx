import { useState, useEffect, useCallback, useRef } from 'react'
import '../styles/ProviderDashboard.css'
import {
  DashboardIcon,
  StorefrontIcon,
  KitesurfingIcon,
  CalendarMonthIcon,
  NotificationsActiveIcon,
  PermIdentityIcon,
  LogoutIcon,
  AddIcon,
  CreateIcon,
  DeleteSweepIcon,
  MonetizationOnIcon,
  AlarmIcon,
  GroupIcon,
  ManageSearchIcon,
  VerifiedUserIcon,
  CheckCircleIcon,
  CancelIcon,
  SettingsIcon,
  EmailIcon,
  LocalPhoneIcon,
  MyLocationIcon,
  PhotoCameraIcon,
  BadgeIcon
} from '../components/Icons'
import ConfirmModal from '../components/ConfirmModal'
import { apiUrl, catalogUrl } from '../api/client'

// ── Helpers & Formatting ──────────────────────────────────────────────────────

function initials(first, last) {
  return `${(first || '').charAt(0)}${(last || '').charAt(0)}`.toUpperCase() || '?'
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

function formatCurrency(amount) {
  return new Intl.NumberFormat('en-LK', { style: 'currency', currency: 'LKR', maximumFractionDigits: 0 }).format(amount || 0)
}

// ── Shared UI Components ──────────────────────────────────────────────────────

function Toast({ message, title = 'Success', onClose }) {
  useEffect(() => {
    const t = setTimeout(onClose, 4000)
    return () => clearTimeout(t)
  }, [onClose])

  return (
    <div className="pd-toast" role="alert" aria-live="polite">
      <div className="pd-toast__icon"></div>
      <div className="pd-toast__body">
        <p className="pd-toast__title">{title}</p>
        <p className="pd-toast__msg">{message}</p>
      </div>
      <button className="pd-toast__close" onClick={onClose} aria-label="Close"></button>
    </div>
  )
}

function LoadingState({ label = 'Loading data…' }) {
  return (
    <div className="pd-loading">
      <div className="pd-spinner" />
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
    <div className="pd-modal-overlay" onClick={(e) => { if (e.target === e.currentTarget) onClose() }}>
      <div className={`pd-modal ${wide ? 'pd-modal--wide' : ''}`} role="dialog" aria-modal="true">
        <div className="pd-modal__header">
          <h2 className="pd-modal__title">{title}</h2>
          <button className="pd-modal__close" onClick={onClose} aria-label="Close modal"></button>
        </div>
        <div className="pd-modal__body">{children}</div>
      </div>
    </div>
  )
}

// ── 1. Dashboard Overview Tab ─────────────────────────────────────────────────

function OverviewTab({ providerInfo, services, bookings, notifications, onNavigate }) {
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
            Welcome back, <span style={{ fontWeight: 'bold' }}>{providerInfo?.businessName || providerInfo?.firstName || 'Provider'}</span>!
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
        <div className="pd-metric-card" style={{ cursor: 'pointer' }} onClick={() => onNavigate('services')}>
          <div className="pd-metric-icon pd-metric-icon--teal"><KitesurfingIcon size={24} /></div>
          <div className="pd-metric-info">
            <div className="pd-metric-title">Listings</div>
            <div className="pd-metric-value">{activeServices.length} Active</div>
            <div className="pd-metric-sub">{services.length} Total Registered</div>
          </div>
        </div>

        <div className="pd-metric-card" style={{ cursor: 'pointer' }} onClick={() => onNavigate('bookings')}>
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
            <div className="pd-metric-value" style={{ fontSize: '18px' }}>{formatCurrency(totalRevenue)}</div>
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

      {/* Two Column Section */}
      <div className="pd-overview-cols">
        {/* Left Column: Business & Services Summary */}
        <div className="pd-card">
          <div className="pd-card__body">
            <div className="pd-section-header">
              <h2>Business Profile Summary</h2>
              <button className="pd-row-btn pd-row-btn--edit" onClick={() => onNavigate('business')}>Edit</button>
            </div>
            <div className="pd-fields">
              <div className="pd-field">
                <span className="pd-field__label">Business Name</span>
                <span className="pd-field__value">{providerInfo?.businessName || '—'}</span>
              </div>
              <div className="pd-field">
                <span className="pd-field__label">Service Type</span>
                <span className="pd-field__value">{providerInfo?.serviceType || '—'}</span>
              </div>
              <div className="pd-field">
                <span className="pd-field__label">Business Contact</span>
                <span className="pd-field__value">
                  <LocalPhoneIcon size={15} style={{ marginRight: 6 }} /> {providerInfo?.phoneNumber || '—'}
                </span>
              </div>
              <div className="pd-field">
                <span className="pd-field__label">Operating Location</span>
                <span className="pd-field__value">
                  <MyLocationIcon size={15} style={{ marginRight: 6 }} /> {providerInfo?.location || 'Sri Lanka'}
                </span>
              </div>
            </div>

            <div className="pd-section-header" style={{ marginTop: '28px' }}>
              <h3>Active Offerings ({activeServices.length})</h3>
              <button className="pd-row-btn pd-row-btn--view" onClick={() => onNavigate('services')}>View All</button>
            </div>

            {activeServices.length === 0 ? (
              <p style={{ color: '#888', fontSize: '13px' }}>No active services listed yet. Click &quot;Add Activity&quot; to begin.</p>
            ) : (
              <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
                {activeServices.slice(0, 3).map(s => (
                  <li key={s.id} style={{ display: 'flex', justifyContent: 'space-between', padding: '9px 0', borderBottom: '1px solid #f3eee4', fontSize: '13.5px' }}>
                    <span style={{ fontWeight: 600, color: '#123b5d' }}>{s.title}</span>
                    <span style={{ fontWeight: 700, color: '#168aad' }}>{formatCurrency(s.price)} <small style={{ color: '#888', fontWeight: 400 }}>/{s.unit}</small></span>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>

        {/* Right Column: Recent Bookings & Notifications Preview */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
          {/* Recent Bookings */}
          <div className="pd-card" style={{ marginBottom: 0 }}>
            <div className="pd-card__body">
              <div className="pd-section-header">
                <h2>Recent Bookings</h2>
                <button className="pd-row-btn pd-row-btn--view" onClick={() => onNavigate('bookings')}>View All</button>
              </div>

              {recentBookings.length === 0 ? (
                <p style={{ color: '#888', fontSize: '13px' }}>No bookings received yet.</p>
              ) : (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                  {recentBookings.map(b => (
                    <div key={b.id} style={{ padding: '12px 14px', borderRadius: '10px', background: '#faf8f3', border: '1px solid #ede8dc', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <div>
                        <div style={{ fontWeight: 700, color: '#123b5d', fontSize: '13.5px' }}>{b.visitorName}</div>
                        <div style={{ fontSize: '12px', color: '#666' }}>{b.activityName} • {formatDate(b.date)}</div>
                      </div>
                      <div style={{ textAlign: 'right' }}>
                        <span className={`pd-badge pd-badge--${b.status.toLowerCase()}`}>{b.status}</span>
                        <div style={{ fontSize: '12.5px', fontWeight: 700, color: '#123b5d', marginTop: '3px' }}>{formatCurrency(b.totalAmount)}</div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Recent Notifications */}
          <div className="pd-card" style={{ marginBottom: 0 }}>
            <div className="pd-card__body">
              <div className="pd-section-header">
                <h2>Notifications</h2>
                <button className="pd-row-btn pd-row-btn--view" onClick={() => onNavigate('notifications')}>View All</button>
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                {recentNotifs.map(n => (
                  <div key={n.id} style={{ display: 'flex', gap: '12px', alignItems: 'flex-start', padding: '10px 0', borderBottom: '1px solid #f3eee4' }}>
                    <span style={{ fontSize: '16px', display: 'flex', alignItems: 'center', color: '#168aad' }}>
                      {n.category === 'booking' ? <CalendarMonthIcon size={16} /> : n.category === 'verification' ? <VerifiedUserIcon size={16} /> : <NotificationsActiveIcon size={16} />}
                    </span>
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div style={{ fontWeight: 600, fontSize: '13px', color: '#123b5d' }}>{n.title}</div>
                      <div style={{ fontSize: '12px', color: '#777' }}>{n.desc}</div>
                    </div>
                    <span style={{ fontSize: '11px', color: '#999', whiteSpace: 'nowrap' }}>{n.time}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

// ── 2. Business Profile Tab ───────────────────────────────────────────────────

function BusinessProfileTab({ token, onLogout, providerInfo, onUpdateSuccess, showToast }) {
  const [editing, setEditing] = useState(false)
  const [formData, setFormData] = useState({
    businessName: providerInfo?.businessName || '',
    serviceType: providerInfo?.serviceType || '',
    location: providerInfo?.location || '',
    description: providerInfo?.description || ''
  })
  const [saveLoading, setSaveLoading] = useState(false)
  const [saveError, setSaveError] = useState(null)

  useEffect(() => {
    if (providerInfo) {
      setFormData({
        businessName: providerInfo.businessName || '',
        serviceType: providerInfo.serviceType || '',
        location: providerInfo.location || '',
        description: providerInfo.description || ''
      })
    }
  }, [providerInfo])

  const handleEdit = () => {
    setSaveError(null)
    setEditing(true)
  }

  const handleCancel = () => {
    setFormData({
      businessName: providerInfo?.businessName || '',
      serviceType: providerInfo?.serviceType || '',
      location: providerInfo?.location || '',
      description: providerInfo?.description || ''
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
      const resp = await fetch(apiUrl('/api/provider/info'), {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify(formData)
      })

      if (resp.ok) {
        const updated = await resp.json()
        onUpdateSuccess && onUpdateSuccess(updated)
        setEditing(false)
        showToast('Business information updated successfully.')
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
    <div className="pd-business-tab">
      <div className="pd-page-header">
        <div className="pd-page-header__left">
          <h1>Business Profile</h1>
          <p>View and manage your registered tourism business details and verification status.</p>
        </div>
      </div>

      {/* Verification Status Card */}
      <div className="pd-card">
        <div className="pd-card__body">
          <div className="pd-section-header">
            <h2>Verification Status</h2>
            <span className="pd-badge pd-badge--active"> Verified & Approved</span>
          </div>

          <div style={{ background: '#faf8f3', border: '1px solid #ede8dc', borderRadius: '12px', padding: '18px 20px', marginTop: '14px', textAlign: 'center' }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '12px', marginBottom: '10px' }}>
              <div>
                <strong style={{ color: '#123b5d', fontSize: '18px' }}>Officially Verified By</strong>
                <p style={{ margin: 0, color: '#666', fontSize: '12.5px' }}>CeylonQuest Admin Team</p>
              </div>
            </div>
            <div className="pd-fields" style={{ marginTop: '14px', paddingTop: '14px', borderTop: '1px solid #ede8dc' }}>
              <div className="pd-field">
                <span className="pd-field__label">Verification Tier</span>
                <span className="pd-field__value">Certified Tourism Provider</span>
              </div>
              <div className="pd-field">
                <span className="pd-field__label">Booking Eligibility</span>
                <span className="pd-field__value" style={{ color: '#4f8a45', fontWeight: 600 }}>Active for Direct Visitor Bookings</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Business Information Card */}
      <div className="pd-card">
        <div className="pd-card__body">
          <div className="pd-section-header">
            <h2>Business Information</h2>
            {!editing && (
              <button className="pd-edit-btn" onClick={handleEdit} id="edit-business-btn">
                 Edit Business Info
              </button>
            )}
          </div>

          {!editing ? (
            <div className="pd-fields" style={{ marginTop: '16px' }}>
              <div className="pd-field">
                <span className="pd-field__label">Business / Enterprise Name</span>
                <span className="pd-field__value">{providerInfo?.businessName || '—'}</span>
              </div>
              <div className="pd-field">
                <span className="pd-field__label">Primary Service Category</span>
                <span className="pd-field__value">{providerInfo?.serviceType || '—'}</span>
              </div>
              <div className="pd-field pd-field--full">
                <span className="pd-field__label">Operating Location / Base</span>
                <span className="pd-field__value"> {providerInfo?.location || '—'}</span>
              </div>
              <div className="pd-field pd-field--full">
                <span className="pd-field__label">Business & Service Description</span>
                <span className="pd-field__value" style={{ lineHeight: 1.6 }}>{providerInfo?.description || '—'}</span>
              </div>
              <div className="pd-field">
                <span className="pd-field__label">Contact Phone</span>
                <span className="pd-field__value"> {providerInfo?.phoneNumber || '—'}</span>
              </div>
              <div className="pd-field">
                <span className="pd-field__label">Official Contact Email</span>
                <span className="pd-field__value"> {providerInfo?.email || '—'}</span>
              </div>
            </div>
          ) : (
            <form onSubmit={handleSave} className="pd-edit-form" style={{ marginTop: '16px' }}>
              {saveError && <div className="pd-form-error">{saveError}</div>}

              <div className="pd-form-grid">
                <div className="pd-form-group">
                  <label htmlFor="biz-name">Business Name *</label>
                  <input
                    id="biz-name"
                    name="businessName"
                    type="text"
                    value={formData.businessName}
                    onChange={handleChange}
                    placeholder="e.g. Ceylon Safari Adventures"
                    required
                  />
                </div>

                <div className="pd-form-group">
                  <label htmlFor="biz-serviceType">Primary Service Category *</label>
                  <input
                    id="biz-serviceType"
                    name="serviceType"
                    type="text"
                    value={formData.serviceType}
                    onChange={handleChange}
                    placeholder="e.g. Wildlife Safari & Trekking"
                    required
                  />
                </div>

                <div className="pd-form-group pd-form-group--full">
                  <label htmlFor="biz-location">Operating Location *</label>
                  <input
                    id="biz-location"
                    name="location"
                    type="text"
                    value={formData.location}
                    onChange={handleChange}
                    placeholder="e.g. Yala & Tissamaharama, Southern Province"
                    required
                  />
                </div>

                <div className="pd-form-group pd-form-group--full">
                  <label htmlFor="biz-desc">Business Description</label>
                  <textarea
                    id="biz-desc"
                    name="description"
                    rows="4"
                    value={formData.description}
                    onChange={handleChange}
                    placeholder="Describe your tourism offerings, experience, safety standards, and specialties..."
                  />
                </div>
              </div>

              <div className="pd-form-actions">
                <button type="submit" className="pd-save-btn" disabled={saveLoading}>
                  {saveLoading ? 'Saving…' : 'Save Changes'}
                </button>
                <button type="button" className="pd-cancel-btn" onClick={handleCancel} disabled={saveLoading}>
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

// ── 3. Activity / Service Management Tab ──────────────────────────────────────

const toDisplayTime = (hhmm) => {
  if (!hhmm) return ''
  const [hStr, mStr] = hhmm.split(':')
  let h = parseInt(hStr, 10)
  const m = mStr || '00'
  const modifier = h >= 12 ? 'PM' : 'AM'
  if (h === 0) h = 12
  else if (h > 12) h -= 12
  return `${String(h).padStart(2, '0')}:${m} ${modifier}`
}

const toInputTime = (display) => {
  if (!display) return '09:00'
  const parts = display.trim().split(' ')
  if (parts.length < 2) return display
  const [timePart, modifier] = parts
  let [hStr, mStr] = timePart.split(':')
  let h = parseInt(hStr, 10)
  if (modifier === 'PM' && h !== 12) h += 12
  if (modifier === 'AM' && h === 12) h = 0
  return `${String(h).padStart(2, '0')}:${mStr || '00'}`
}

// Helper to add duration to a "HH:MM" string and return "HH:MM"
const addDurationToTime = (startTimeStr, durationStr) => {
  if (!startTimeStr) return '10:00'
  const [hStr, mStr] = startTimeStr.split(':')
  let totalMinutes = parseInt(hStr || 0, 10) * 60 + parseInt(mStr || 0, 10)

  const durLower = (durationStr || '').toLowerCase()
  const numVal = parseFloat(durLower) || 0

  if (durLower.includes('hour')) {
    totalMinutes += Math.round(numVal * 60)
  } else if (durLower.includes('min')) {
    totalMinutes += Math.round(numVal)
  } else {
    totalMinutes += 120 // default 2 hours
  }

  const endH = Math.floor(totalMinutes / 60) % 24
  const endM = totalMinutes % 60
  return `${String(endH).padStart(2, '0')}:${String(endM).padStart(2, '0')}`
}

function ActivitiesTab({ token, onLogout, services = [], onRefreshServices, showToast }) {
  const [search, setSearch] = useState('')
  const [filterStatus, setFilterStatus] = useState('all')
  const [modal, setModal] = useState(null)
  const [editTarget, setEditTarget] = useState(null)

  const [slotsList, setSlotsList] = useState([{ startTime: '', endTime: '' }])

  const [form, setForm] = useState({
    title: '',
    description: '',
    price: '',
    unit: 'Per Person',
    location: '',
    maxParticipants: 10,
    duration: '',
    availableDays: '',
    timeSlots: '',
    validFrom: '',
    validUntil: '',
    isActive: true
  })
  const [formError, setFormError] = useState(null)
  const [formLoading, setFormLoading] = useState(false)

  const [serviceToDelete, setServiceToDelete] = useState(null)
  const [deleteLoading, setDeleteLoading] = useState(false)

  // Re-chains slots sequentially: slot[i].end becomes slot[i+1].start
  const recalculateAllSlots = (currentSlots, duration) => {
    let nextStart = currentSlots[0]?.startTime || '08:00'
    return currentSlots.map((slot) => {
      const start = nextStart
      const end = addDurationToTime(start, duration)
      nextStart = end // Chain next slot's start to current slot's end
      return { startTime: start, endTime: end }
    })
  }

  const handleSlotCountChange = (count) => {
    const num = parseInt(count, 10) || 1
    setSlotsList(prev => {
      const next = [...prev]
      while (next.length < num) {
        const lastSlot = next[next.length - 1] || { startTime: '09:00', endTime: '11:00' }
        const newStart = lastSlot.endTime
        const newEnd = addDurationToTime(newStart, form.duration)
        next.push({ startTime: newStart, endTime: newEnd })
      }
      return recalculateAllSlots(next.slice(0, num), form.duration)
    })
  }

  const updateSlotStartTime = (index, newStartVal) => {
    setSlotsList(prev => {
      const next = [...prev]
      next[index] = {
        startTime: newStartVal,
        endTime: addDurationToTime(newStartVal, form.duration)
      }
      // Cascade downstream slots so they cleanly chain from the updated end time
      for (let i = index + 1; i < next.length; i++) {
        const prevEnd = next[i - 1].endTime
        next[i] = {
          startTime: prevEnd,
          endTime: addDurationToTime(prevEnd, form.duration)
        }
      }
      return next
    })
  }

  const updateSlotEndTime = (index, newEndVal) => {
    setSlotsList(prev => {
      const next = [...prev]
      next[index] = {
        ...next[index],
        endTime: newEndVal
      }
      // Cascade downstream slots so they start where this slot ends
      for (let i = index + 1; i < next.length; i++) {
        const prevEnd = next[i - 1].endTime
        next[i] = {
          startTime: prevEnd,
          endTime: addDurationToTime(prevEnd, form.duration)
        }
      }
      return next
    })
  }

  const handleDurationChange = (e) => {
    const newDur = e.target.value
    setForm(prev => ({ ...prev, duration: newDur }))
    setSlotsList(prev => recalculateAllSlots(prev, newDur))
  }

  const openAdd = () => {
    setEditTarget(null)
    const initialDuration = ''
    setSlotsList([{ startTime: '08:00', endTime: addDurationToTime('08:00', initialDuration) }])
    setForm({
      title: '',
      description: '',
      price: '',
      unit: 'Per Person',
      location: '',
      maxParticipants: 10,
      duration: initialDuration,
      availableDays: '',
      timeSlots: '',
      validFrom: '',
      validUntil: '',
      isActive: true
    })
    setFormError(null)
    setModal('add')
  }

  const openEdit = (service) => {
    const currentDuration = service.duration || '2 Hours'
    const parsedSlots = (service.timeSlots || '')
      .split(',')
      .map(s => s.trim())
      .filter(Boolean)
      .map(s => {
        const parts = s.split(' - ')
        if (parts.length === 2) {
          return {
            startTime: toInputTime(parts[0]),
            endTime: toInputTime(parts[1].split(' ')[0])
          }
        }
        const singleTime = toInputTime(s)
        return {
          startTime: singleTime,
          endTime: addDurationToTime(singleTime, currentDuration)
        }
      })
    if (parsedSlots.length === 0) {
      parsedSlots.push({ startTime: '08:00', endTime: addDurationToTime('08:00', currentDuration) })
    }

    setEditTarget(service)
    setSlotsList(parsedSlots)
    setForm({
      title: service.title || '',
      description: service.description || '',
      price: service.price ?? '',
      unit: service.unit || 'Per Person',
      location: service.location || '',
      maxParticipants: service.maxParticipants || 10,
      duration: currentDuration,
      availableDays: service.availableDays || '',
      timeSlots: service.timeSlots || '',
      validFrom: service.validFrom ? service.validFrom.slice(0, 10) : '',
      validUntil: service.validUntil ? service.validUntil.slice(0, 10) : '',
      isActive: service.isActive !== false
    })
    setFormError(null)
    setModal('edit')
  }

  const closeModal = () => {
    setModal(null)
    setEditTarget(null)
    setFormError(null)
    setSlotsList([{ startTime: '08:00', endTime: '10:00' }])
  }

  const handleFormChange = (e) => {
    const { name, value, type, checked } = e.target
    setForm(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }))
    if (formError) setFormError(null)
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setFormError(null)

    if (!form.title.trim()) {
      setFormError('Experience title is required.')
      return
    }
    if (!form.location.trim()) {
      setFormError('Operating location is required.')
      return
    }
    const numPrice = parseFloat(form.price)
    if (isNaN(numPrice) || numPrice <= 0) {
      setFormError('Price must be a valid positive amount.')
      return
    }

    const validSlots = slotsList.filter(s => s.startTime && s.endTime)
    if (validSlots.length === 0) {
      setFormError('At least one complete time slot is required.')
      return
    }

    setFormLoading(true)

    const serializedTimeSlots = validSlots
      .map(s => `${toDisplayTime(s.startTime)} - ${toDisplayTime(s.endTime)}`)
      .join(', ')

    const payload = {
      title: form.title.trim(),
      description: form.description.trim(),
      price: numPrice,
      unit: form.unit.trim(),
      location: form.location.trim(),
      maxParticipants: parseInt(form.maxParticipants, 10) || 1,
      duration: form.duration.trim(),
      availableDays: form.availableDays.trim(),
      timeSlots: serializedTimeSlots,
      validFrom: form.validFrom || null,
      validUntil: form.validUntil || null,
      isActive: form.isActive
    }

    try {
      const url = modal === 'edit'
        ? catalogUrl(`/api/catalog/activity-listings/${editTarget.id}`)
        : catalogUrl('/api/catalog/activity-listings')
      const method = modal === 'edit' ? 'PUT' : 'POST'

      const resp = await fetch(url, {
        method,
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify(payload)
      })

      if (resp.ok || resp.status === 201) {
        closeModal()
        showToast(modal === 'edit' ? 'Experience listing updated.' : 'New experience listing created.')
        onRefreshServices && onRefreshServices()
      } else if (resp.status === 403) {
        const body = await resp.json().catch(() => ({}))
        setFormError(body.message || 'Only approved providers can manage listings.')
      } else if (resp.status === 401) {
        onLogout && onLogout()
      } else {
        const body = await resp.json().catch(() => ({}))
        setFormError(body.message || 'Error saving listing. Check your input.')
      }
    } catch {
      setFormError('Network error. Please check your connection.')
    } finally {
      setFormLoading(false)
    }
  }

  const handleToggleStatus = async (service) => {
    const newStatus = !service.isActive
    try {
      const resp = await fetch(catalogUrl(`/api/catalog/activity-listings/${service.id}`), {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({
          title: service.title,
          description: service.description,
          price: service.price,
          unit: service.unit,
          location: service.location,
          maxParticipants: service.maxParticipants,
          duration: service.duration,
          availableDays: service.availableDays,
          timeSlots: service.timeSlots,
          validFrom: service.validFrom,
          validUntil: service.validUntil,
          isActive: newStatus
        })
      })
      if (resp.ok) {
        showToast(`Listing ${newStatus ? 'activated' : 'deactivated'}.`)
        onRefreshServices && onRefreshServices()
      } else {
        showToast('Failed to update status.')
      }
    } catch {
      showToast('Network error. Please check connection.')
    }
  }

  const executeDeleteService = async () => {
    if (!serviceToDelete) return
    setDeleteLoading(true)
    try {
      const resp = await fetch(catalogUrl(`/api/catalog/activity-listings/${serviceToDelete}`), {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.status === 204 || resp.ok) {
        showToast('Listing deleted successfully.')
        setServiceToDelete(null)
        onRefreshServices && onRefreshServices()
      } else if (resp.status === 401) {
        onLogout && onLogout()
      } else {
        const body = await resp.json().catch(() => ({}))
        showToast(body.message || 'Failed to delete listing.')
      }
    } catch {
      showToast('Network error. Please check connection.')
    } finally {
      setDeleteLoading(false)
    }
  }

  const filtered = services.filter(s => {
    const q = search.toLowerCase().trim()
    const matchSearch = !q ||
      (s.title || '').toLowerCase().includes(q) ||
      (s.description || '').toLowerCase().includes(q) ||
      (s.location || '').toLowerCase().includes(q)
    if (!matchSearch) return false
    if (filterStatus === 'active') return s.isActive !== false
    if (filterStatus === 'inactive') return s.isActive === false
    return true
  })

  const selectedDates = form.availableDays
    ? form.availableDays.split(',').map(d => d.trim()).filter(Boolean)
    : []

  const addSelectedDate = (chosen) => {
    if (!chosen) return
    if (form.validFrom && chosen < form.validFrom) {
      alert('Selected date is before the Valid From date.')
      return
    }
    if (form.validUntil && chosen > form.validUntil) {
      alert('Selected date is after the Valid Until date.')
      return
    }
    if (!selectedDates.includes(chosen)) {
      const updated = [...selectedDates, chosen].sort().join(', ')
      setForm(prev => ({ ...prev, availableDays: updated }))
    }
  }

  const removeSelectedDate = (dateToRemove) => {
    const remaining = selectedDates.filter(x => x !== dateToRemove).join(', ')
    setForm(prev => ({ ...prev, availableDays: remaining }))
  }

  return (
    <div className="pd-activities-tab">
      <ConfirmModal
        isOpen={Boolean(serviceToDelete)}
        title="Delete Experience Listing"
        message="Are you sure you want to delete this listing? It will no longer be discoverable by visitors."
        confirmText="Delete Listing"
        cancelText="Cancel"
        confirmVariant="danger"
        onConfirm={executeDeleteService}
        onCancel={() => setServiceToDelete(null)}
        loading={deleteLoading}
      />

      <div className="pd-page-header">
        <div className="pd-page-header__left">
          <h1>Experience Listings</h1>
          <p>Create, edit, activate, or remove tourism experiences you offer to visitors.</p>
        </div>
        <button className="pd-quick-btn pd-quick-btn--primary" onClick={openAdd} id="add-activity-btn">
          <AddIcon size={16} /> Create New Experience
        </button>
      </div>

      {modal && (
        <Modal title={modal === 'edit' ? 'Edit Experience Listing' : 'Create New Experience'} onClose={closeModal} wide>
          <form onSubmit={handleSubmit} className="pd-modal__form" noValidate>
            {formError && <div className="pd-form-error">{formError}</div>}

            <div className="pd-form-group">
              <label htmlFor="exp-title">Experience Title *</label>
              <input
                id="exp-title"
                name="title"
                type="text"
                value={form.title}
                onChange={handleFormChange}
                placeholder="e.g. Guided Snorkeling at Pigeon Island"
                required
              />
            </div>

            <div className="pd-form-group">
              <label htmlFor="exp-location">Operating Location *</label>
              <input
                id="exp-location"
                name="location"
                type="text"
                value={form.location}
                onChange={handleFormChange}
                placeholder="e.g. Nilaveli, Trincomalee"
                required
              />
            </div>

            <div className="pd-form-group">
              <label htmlFor="exp-desc">Description & Inclusions *</label>
              <textarea
                id="exp-desc"
                name="description"
                rows="3"
                value={form.description}
                onChange={handleFormChange}
                placeholder="Describe the experience, itinerary, gear provided, and meeting point..."
                required
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '12px' }}>
              <div className="pd-form-group">
                <label htmlFor="exp-price">Price (LKR) *</label>
                <input
                  id="exp-price"
                  name="price"
                  type="number"
                  min="0.01"
                  step="100"
                  value={form.price}
                  onChange={handleFormChange}
                  placeholder="e.g. 8500"
                  required
                />
              </div>

              <div className="pd-form-group">
                <label htmlFor="exp-unit">Pricing Unit *</label>
                <select id="exp-unit" name="unit" value={form.unit} onChange={handleFormChange}>
                  <option value="Per Person">Per Person</option>
                  <option value="Per Pair">Per Two Persons</option>
                  <option value="Per Group">Per Group</option>
                  <option value="Per Hour">Per Hour</option>
                  <option value="Per Day">Per Day</option>
                </select>
              </div>

              <div style={{ maxWidth: 90 }} className="pd-form-group">
                <label htmlFor="exp-max">Max Guests</label>
                <input
                  id="exp-max"
                  name="maxParticipants"
                  type="number"
                  min="1"
                  value={form.maxParticipants}
                  onChange={handleFormChange}
                  required
                />
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="pd-form-group">
                <label htmlFor="exp-valid-from">Valid From</label>
                <input
                  id="exp-valid-from"
                  name="validFrom"
                  type="date"
                  value={form.validFrom}
                  onChange={handleFormChange}
                />
              </div>

              <div className="pd-form-group">
                <label htmlFor="exp-valid-until">Valid Until</label>
                <input
                  id="exp-valid-until"
                  name="validUntil"
                  type="date"
                  value={form.validUntil}
                  onChange={handleFormChange}
                />
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', marginTop: '4px' }}>
              <div className="pd-form-group">
                <label htmlFor="exp-duration">Duration *</label>
                <input
                  id="exp-duration"
                  name="duration"
                  type="text"
                  value={form.duration}
                  onChange={handleDurationChange}
                  placeholder="e.g. 2 Hours or 45 Mins"
                  required
                />
              </div>

              <div className="pd-form-group">
                <label htmlFor="exp-days">Available Days *</label>
                <input
                  id="exp-days"
                  name="availableDays"
                  type="text"
                  value={form.availableDays}
                  onChange={handleFormChange}
                  placeholder="e.g. Daily / Mon–Fri"
                  required
                />
              </div>
            </div>

            {/* Dynamic Time Slots Section with sequential continuity logic */}
            <div style={{ marginTop: '16px', background: '#faf8f3', padding: '16px', borderRadius: '10px', border: '1px solid #ede8dc' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
                <label style={{ fontWeight: 600, fontSize: '13.5px', color: '#123b5d' }}>Daily Time Slots Configuration</label>
                <select
                  value={slotsList.length}
                  onChange={(e) => handleSlotCountChange(e.target.value)}
                  style={{ padding: '4px 8px', borderRadius: '6px', border: '1px solid #cbd5e1', fontSize: '12.5px' }}
                >
                  {[1, 2, 3, 4, 5, 6].map(n => (
                    <option key={n} value={n}>{n} {n === 1 ? 'Slot' : 'Slots'}</option>
                  ))}
                </select>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(210px, 1fr))', gap: '10px' }}>
                {slotsList.map((slot, idx) => (
                  <div key={idx} style={{ background: '#ffffff', borderRadius: '8px', border: '1px solid #e2e8f0', padding: '10px 12px', boxShadow: '0 1px 2px rgba(0,0,0,0.03)' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                      <span style={{ fontSize: '12px', fontWeight: 700, color: '#168aad' }}>Slot {idx + 1}</span>
                      <span style={{ fontSize: '11px', color: '#64748b', background: '#f1f5f9', padding: '2px 6px', borderRadius: '4px' }}>{form.duration || 'Duration not set'}</span>
                    </div>

                    <div style={{ display: 'flex', gap: '6px', alignItems: 'center' }}>
                      <div style={{ flex: 1 }}>
                        <label style={{ fontSize: '10px', color: '#888', display: 'block', marginBottom: '2px' }}>Start</label>
                        <input
                          type="time"
                          value={slot.startTime}
                          onChange={(e) => updateSlotStartTime(idx, e.target.value)}
                          style={{ width: '100%', padding: '4px 6px', borderRadius: '4px', border: '1px solid #cbd5e1', fontSize: '12px' }}
                          required
                        />
                      </div>
                      <span style={{ color: '#888', fontSize: '12px', paddingBottom: '6px', marginLeft: '12px', marginTop: '30px'}}>to</span>
                      <div style={{ flex: 1 }}>
                        <label style={{ fontSize: '10px', color: '#888', display: 'block', marginBottom: '2px' }}>End</label>
                        <input
                          type="time"
                          value={slot.endTime}
                          onChange={(e) => updateSlotEndTime(idx, e.target.value)}
                          style={{ width: '100%', padding: '4px 6px', borderRadius: '4px', border: '1px solid #cbd5e1', fontSize: '12px' }}
                          required
                        />
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            
            <div className="pd-checkbox-group" style={{ marginTop: '8px' }}>
              <label className="pd-checkbox-label">
                <input
                  type="checkbox"
                  name="isActive"
                  checked={form.isActive}
                  onChange={handleFormChange}
                />
                <span>Active and bookable by visitors immediately</span>
              </label>
            </div>

            <div className="pd-modal__actions">
              <button type="button" className="pd-cancel-btn" onClick={closeModal} disabled={formLoading}>
                Cancel
              </button>
              <button type="submit" className="pd-quick-btn pd-quick-btn--primary" disabled={formLoading}>
                {formLoading ? 'Saving…' : (modal === 'edit' ? 'Save Changes' : 'Publish Experience')}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Filter and Search Bar */}
      <div className="pd-filter-bar">
        <div className="pd-search-wrap">
          <span className="pd-search-icon"><ManageSearchIcon size={18} /></span>
          <input
            type="text"
            className="pd-search-input"
            placeholder="Search your listings..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <div className="pd-filter-pills">
          <button className={`pd-filter-pill ${filterStatus === 'all' ? 'active' : ''}`} onClick={() => setFilterStatus('all')}>
            All ({services.length})
          </button>
          <button className={`pd-filter-pill ${filterStatus === 'active' ? 'active' : ''}`} onClick={() => setFilterStatus('active')}>
            Active ({services.filter(s => s.isActive !== false).length})
          </button>
          <button className={`pd-filter-pill ${filterStatus === 'inactive' ? 'active' : ''}`} onClick={() => setFilterStatus('inactive')}>
            Inactive ({services.filter(s => s.isActive === false).length})
          </button>
        </div>
      </div>

      {/* Listings Table */}
      <div className="pd-card">
        <div className="pd-card__body" style={{ padding: 0 }}>
          {filtered.length === 0 ? (
            <div className="pd-empty">
              <div className="pd-empty__icon"><KitesurfingIcon size={32} /></div>
              <p className="pd-empty__title">No experience listings found</p>
              <p className="pd-empty__msg">Create your first tourism experience listing to start receiving visitor bookings.</p>
              <button className="pd-quick-btn pd-quick-btn--primary" onClick={openAdd} style={{ marginTop: '12px' }}>
                <AddIcon size={16} /> Add Your First Listing
              </button>
            </div>
          ) : (
            <div className="pd-table-wrap" style={{ border: 'none', borderRadius: 0 }}>
              <table className="pd-table">
                <thead>
                  <tr>
                    <th>Experience Title</th>
                    <th>Location</th>
                    <th>Price</th>
                    <th>Time Slots</th>
                    <th>Max Guests</th>
                    <th>Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(s => (
                    <tr key={s.id}>
                      <td style={{ fontWeight: 600, color: '#123b5d' }}>
                        <div>{s.title}</div>
                        <div style={{ fontSize: '11.5px', color: '#64748b', fontWeight: 400 }}>{s.description?.slice(0, 60)}{s.description?.length > 60 ? '…' : ''}</div>
                      </td>
                      <td>{s.location}</td>
                      <td style={{ fontWeight: 700, color: '#4f8a45' }}>
                        LKR {Number(s.price).toLocaleString()} <span style={{ fontSize: '11px', color: '#777', fontWeight: 400 }}>/ {s.unit}</span>
                      </td>
                      <td style={{ fontSize: '12px', color: '#475569', maxWidth: '180px' }}>
                        {s.timeSlots ? (
                          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '4px' }}>
                            {s.timeSlots.split(',').map((slot, i) => (
                              <span key={i} style={{ background: '#f1f5f9', padding: '2px 6px', borderRadius: '4px', border: '1px solid #e2e8f0', whiteSpace: 'nowrap' }}>
                                {slot.trim()}
                              </span>
                            ))}
                          </div>
                        ) : '—'}
                      </td>
                      <td>{s.maxParticipants} guests</td>
                      <td>
                        <button
                          type="button"
                          className={`pd-badge pd-badge--${s.isActive ? 'active' : 'inactive'}`}
                          style={{ cursor: 'pointer', border: 'none' }}
                          onClick={() => handleToggleStatus(s)}
                          title="Click to toggle status"
                        >
                          {s.isActive ? 'Active' : 'Inactive'}
                        </button>
                      </td>
                      <td>
                        <div className="pd-row-actions">
                          <button className="pd-row-btn" onClick={() => openEdit(s)}>Edit</button>
                          <button className="pd-row-btn pd-row-btn--delete" onClick={() => setServiceToDelete(s.id)}>Delete</button>
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

// ── Root Provider Dashboard Component ─────────────────────────────────────────

function ProviderDashboard({ onLogout }) {
  const [activeTab, setActiveTab] = useState('overview')
  const [toast, setToast] = useState(null)

  const [providerInfo, setProviderInfo] = useState(null)
  const [services, setServices] = useState([])
  const [bookings, setBookings] = useState(() => {
    try {
      const saved = localStorage.getItem('ceylonquest_provider_bookings')
      return saved ? JSON.parse(saved) : []
    } catch {
      return []
    }
  })
  const [notifications, setNotifications] = useState(() => {
    try {
      const saved = localStorage.getItem('ceylonquest_provider_notifications')
      return saved ? JSON.parse(saved) : []
    } catch {
      return []
    }
  })

  const token = localStorage.getItem('authToken')
  const role = localStorage.getItem('userRole')

  useEffect(() => {
    if (!token || role !== 'Provider') {
      onLogout && onLogout()
    }
  }, [token, role, onLogout])

  useEffect(() => {
    try {
      localStorage.setItem('ceylonquest_provider_bookings', JSON.stringify(bookings))
    } catch {}
  }, [bookings])

  useEffect(() => {
    try {
      localStorage.setItem('ceylonquest_provider_notifications', JSON.stringify(notifications))
    } catch {}
  }, [notifications])

  const showToast = useCallback((msg) => setToast(msg), [])

  const fetchProviderInfo = useCallback(async () => {
    if (!token) return
    try {
      const resp = await fetch(catalogUrl('/api/catalog/provider/profile'), {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        setProviderInfo(await resp.json())
      }
    } catch {}
  }, [token])

  const fetchServices = useCallback(async () => {
    if (!token) return
    try {
      const resp = await fetch(catalogUrl('/api/catalog/activity-listings'), {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        const data = await resp.json()
        setServices(data)
      }
    } catch (err) {
      console.error('Failed to load listings', err)
    }
  }, [token])

  useEffect(() => {
    fetchProviderInfo()
    fetchServices()
  }, [fetchProviderInfo, fetchServices])

  const handleUpdateBookingStatus = (bookingId, newStatus) => {
    setBookings(prev => prev.map(b => b.id === bookingId ? { ...b, status: newStatus } : b))
    showToast(`Booking ${bookingId} marked as ${newStatus}.`)
  }

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

  const handleLogout = useCallback(() => {
    localStorage.removeItem('authToken')
    localStorage.removeItem('userRole')
    onLogout && onLogout()
  }, [onLogout])

  const [userProfile, setUserProfile] = useState(null)

  const fetchUserProfile = useCallback(async () => {
    if (!token) return
    try {
      const resp = await fetch(apiUrl('/api/users/me'), {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        setUserProfile(await resp.json())
      }
    } catch {}
  }, [token])

  useEffect(() => {
    fetchUserProfile()
  }, [fetchUserProfile])

  const unreadNotifCount = notifications.filter(n => !n.read).length

  const navItems = [
    { key: 'overview',      icon: <DashboardIcon size={18} />,         label: 'Overview' },
    { key: 'business',      icon: <StorefrontIcon size={18} />,        label: 'Business Profile' },
    { key: 'services',      icon: <KitesurfingIcon size={18} />,       label: 'Activities & Services' },
    { key: 'bookings',      icon: <CalendarMonthIcon size={18} />,      label: 'Bookings' },
    { key: 'notifications', icon: <NotificationsActiveIcon size={18} />, label: 'Notifications', badge: unreadNotifCount > 0 ? unreadNotifCount : null },
    { key: 'account',       icon: <PermIdentityIcon size={18} />,        label: 'Account' }
  ]

  return (
    <div className="pd-page">
      {toast && <Toast message={toast} onClose={() => setToast(null)} />}

      <aside className="pd-sidebar">
        <div className="pd-sidebar__brand">
          <img src="/dashboard-logo.png" alt="CeylonQuest" className="pd-sidebar__logo-img" />
          <span className="pd-sidebar__role">Provider Portal</span>
        </div>

        <ul className="pd-sidebar__nav">
          {navItems.map(item => (
            <li key={item.key}>
              <button
                className={activeTab === item.key ? 'active' : ''}
                onClick={() => setActiveTab(item.key)}
                id={`pd-nav-${item.key}`}
              >
                <span className="pd-nav-icon">{item.icon}</span>
                <span>{item.label}</span>
                {item.badge && <span className="pd-nav-badge">{item.badge}</span>}
              </button>
            </li>
          ))}
        </ul>

        <div className="pd-sidebar__footer">
          {userProfile && (
            <div className="pd-sidebar-user">
              <div className="pd-sidebar-avatar">
                {userProfile.profilePictureUrl ? (
                  <img src={formatAvatarUrl(userProfile.profilePictureUrl)} alt="" className="pd-sidebar-avatar__img" />
                ) : (
                  initials(userProfile.firstName, userProfile.lastName)
                )}
              </div>
              <div className="pd-sidebar-user__info">
                <div className="pd-sidebar-user__name">{userProfile.firstName} {userProfile.lastName}</div>
                <div className="pd-sidebar-user__email">{userProfile.email}</div>
              </div>
            </div>
          )}
          <button className="pd-logout-btn" onClick5={handleLogout} id="pd-logout-btn" onClick={handleLogout}>
            <span className="pd-nav-icon"><LogoutIcon size={18} /></span> Log Out
          </button>
        </div>
      </aside>

      <main className="pd-main">
        {activeTab === 'overview' && (
          <OverviewTab
            providerInfo={providerInfo}
            services={services}
            bookings={bookings}
            notifications={notifications}
            onNavigate={(tab) => setActiveTab(tab)}
          />
        )}

        {activeTab === 'business' && (
          <BusinessProfileTab
            token={token}
            onLogout={handleLogout}
            providerInfo={providerInfo}
            onUpdateSuccess={(updated) => setProviderInfo(updated)}
            showToast={showToast}
          />
        )}

        {activeTab === 'services' && (
          <ActivitiesTab
            token={token}
            onLogout={handleLogout}
            services={services}
            onRefreshServices={fetchServices}
            showToast={showToast}
          />
        )}

        {activeTab === 'bookings' && (
          <BookingsTab
            bookings={bookings}
            onUpdateBookingStatus={handleUpdateBookingStatus}
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
          <AccountTab
            token={token}
            onLogout={handleLogout}
            showToast={showToast}
            onProfileUpdate={setUserProfile}
          />
        )}
      </main>
    </div>
  )
}

export default ProviderDashboard