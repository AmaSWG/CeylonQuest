import { useState, useEffect, useCallback } from 'react'
import '../styles/ProviderDashboard.css'

// ── Helpers & Formatting ──────────────────────────────────────────────────────

function initials(first, last) {
  return `${(first || '').charAt(0)}${(last || '').charAt(0)}`.toUpperCase() || '?'
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
      <div className="pd-toast__icon">✓</div>
      <div className="pd-toast__body">
        <p className="pd-toast__title">{title}</p>
        <p className="pd-toast__msg">{message}</p>
      </div>
      <button className="pd-toast__close" onClick={onClose} aria-label="Close">✕</button>
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
          <button className="pd-modal__close" onClick={onClose} aria-label="Close modal">✕</button>
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
          <p>Welcome back, {providerInfo?.businessName || providerInfo?.firstName || 'Provider'}! Here is your service performance summary.</p>
        </div>
      </div>

      {/* Verification Status Banner */}
      <div className="pd-verification-banner">
        <div className="pd-verification-banner__left">
          <div className="pd-verification-badge-icon">🛡️</div>
          <div>
            <h2 className="pd-verification-banner__title">Verification Status: Verified & Approved Partner</h2>
            <p className="pd-verification-banner__desc">
              Your business is officially certified to accept visitor bookings and list tourism services across Sri Lanka.
            </p>
          </div>
        </div>
        <button className="pd-quick-btn pd-quick-btn--secondary" onClick={() => onNavigate('business')}>
          View Business Profile
        </button>
      </div>

      {/* Metric Cards */}
      <div className="pd-metrics-grid">
        <div className="pd-metric-card">
          <div className="pd-metric-icon pd-metric-icon--gold">🛡️</div>
          <div className="pd-metric-info">
            <div className="pd-metric-title">Verification</div>
            <div className="pd-metric-value" style={{ fontSize: '18px', color: '#4f8a45' }}>Verified</div>
            <div className="pd-metric-sub">Active Partner</div>
          </div>
        </div>

        <div className="pd-metric-card" style={{ cursor: 'pointer' }} onClick={() => onNavigate('services')}>
          <div className="pd-metric-icon pd-metric-icon--teal">🏄</div>
          <div className="pd-metric-info">
            <div className="pd-metric-title">Activities & Services</div>
            <div className="pd-metric-value">{activeServices.length} Active</div>
            <div className="pd-metric-sub">{services.length} Total Registered</div>
          </div>
        </div>

        <div className="pd-metric-card" style={{ cursor: 'pointer' }} onClick={() => onNavigate('bookings')}>
          <div className="pd-metric-icon pd-metric-icon--blue">📅</div>
          <div className="pd-metric-info">
            <div className="pd-metric-title">Total Bookings</div>
            <div className="pd-metric-value">{bookings.length}</div>
            <div className="pd-metric-sub">{pendingBookings.length} Pending, {confirmedBookings.length} Confirmed</div>
          </div>
        </div>

        <div className="pd-metric-card">
          <div className="pd-metric-icon pd-metric-icon--green">💰</div>
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
          ➕ Add New Activity / Service
        </button>
        <button className="pd-quick-btn pd-quick-btn--secondary" onClick={() => onNavigate('business')}>
          🏢 Edit Business Profile
        </button>
        <button className="pd-quick-btn pd-quick-btn--secondary" onClick={() => onNavigate('bookings')}>
          📅 Manage Bookings ({pendingBookings.length} action required)
        </button>
        <button className="pd-quick-btn pd-quick-btn--secondary" onClick={() => onNavigate('account')}>
          👤 Account Settings
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
              <div className="pd-field pd-field--full">
                <span className="pd-field__label">Operating Location</span>
                <span className="pd-field__value">📍 {providerInfo?.location || 'Sri Lanka'}</span>
              </div>
              <div className="pd-field pd-field--full">
                <span className="pd-field__label">Business Contact</span>
                <span className="pd-field__value">📞 {providerInfo?.phoneNumber || '—'}</span>
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
                    <span style={{ fontWeight: 600, color: '#123b5d' }}>{s.serviceName}</span>
                    <span style={{ fontWeight: 700, color: '#168aad' }}>{formatCurrency(s.pricePerUnit)} <small style={{ color: '#888', fontWeight: 400 }}>/{s.unit}</small></span>
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
                    <span style={{ fontSize: '16px' }}>{n.category === 'booking' ? '📅' : n.category === 'verification' ? '🛡️' : '📢'}</span>
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
      const resp = await fetch('/api/provider/info', {
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
            <span className="pd-badge pd-badge--active">🛡️ Verified & Approved</span>
          </div>

          <div style={{ background: '#faf8f3', border: '1px solid #ede8dc', borderRadius: '12px', padding: '18px 20px', marginTop: '14px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '10px' }}>
              <span style={{ fontSize: '20px' }}>✓</span>
              <div>
                <strong style={{ color: '#123b5d', fontSize: '14px' }}>Official Partner Accreditation</strong>
                <p style={{ margin: 0, color: '#666', fontSize: '12.5px' }}>Verified by CeylonQuest Quality & Safety Assurance Team.</p>
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
                ✏️ Edit Business Info
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
                <span className="pd-field__value">📍 {providerInfo?.location || '—'}</span>
              </div>
              <div className="pd-field pd-field--full">
                <span className="pd-field__label">Business & Service Description</span>
                <span className="pd-field__value" style={{ lineHeight: 1.6 }}>{providerInfo?.description || '—'}</span>
              </div>
              <div className="pd-field">
                <span className="pd-field__label">Contact Phone</span>
                <span className="pd-field__value">📞 {providerInfo?.phoneNumber || '—'}</span>
              </div>
              <div className="pd-field">
                <span className="pd-field__label">Official Contact Email</span>
                <span className="pd-field__value">✉️ {providerInfo?.email || '—'}</span>
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

const EMPTY_SERVICE_FORM = {
  serviceName: '',
  description: '',
  pricePerUnit: '',
  unit: 'per person',
  isActive: true
}

function ActivitiesTab({ token, onLogout, services, onRefreshServices, showToast }) {
  const [modal, setModal] = useState(null) // null | 'add' | 'edit'
  const [editTarget, setEditTarget] = useState(null)
  const [form, setForm] = useState(EMPTY_SERVICE_FORM)
  const [formError, setFormError] = useState(null)
  const [formLoading, setFormLoading] = useState(false)
  const [search, setSearch] = useState('')
  const [filterStatus, setFilterStatus] = useState('all') // all | active | inactive

  const openAdd = () => {
    setForm(EMPTY_SERVICE_FORM)
    setFormError(null)
    setModal('add')
  }

  const openEdit = (service) => {
    setEditTarget(service)
    setForm({
      serviceName: service.serviceName,
      description: service.description || '',
      pricePerUnit: String(service.pricePerUnit),
      unit: service.unit || 'per person',
      isActive: service.isActive !== false
    })
    setFormError(null)
    setModal('edit')
  }

  const closeModal = () => {
    setModal(null)
    setEditTarget(null)
  }

  const handleFormChange = (e) => {
    const { name, value, type, checked } = e.target
    setForm(prev => ({ ...prev, [name]: type === 'checkbox' ? checked : value }))
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setFormError(null)
    setFormLoading(true)

    const payload = {
      serviceName: form.serviceName.trim(),
      description: form.description.trim(),
      pricePerUnit: parseFloat(form.pricePerUnit),
      unit: form.unit.trim(),
      isActive: Boolean(form.isActive)
    }

    if (isNaN(payload.pricePerUnit) || payload.pricePerUnit <= 0) {
      setFormError('Price must be a valid positive number.')
      setFormLoading(false)
      return
    }

    try {
      const url = modal === 'edit' ? `/api/provider/prices/${editTarget.id}` : '/api/provider/prices'
      const method = modal === 'edit' ? 'PUT' : 'POST'
      const resp = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify(payload)
      })

      if (resp.ok || resp.status === 201) {
        closeModal()
        showToast(modal === 'edit' ? 'Activity/Service updated successfully.' : 'New Activity/Service published.')
        onRefreshServices && onRefreshServices()
      } else if (resp.status === 400 || resp.status === 422) {
        const body = await resp.json().catch(() => ({}))
        const first = body.errors && Object.values(body.errors).flat()[0]
        setFormError(first || body.message || 'Validation error. Check your input.')
      } else if (resp.status === 401) {
        onLogout && onLogout()
      } else {
        setFormError('Server error. Please try again.')
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
      const resp = await fetch(`/api/provider/prices/${service.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify({
          serviceName: service.serviceName,
          description: service.description || '',
          pricePerUnit: service.pricePerUnit,
          unit: service.unit,
          isActive: newStatus
        })
      })
      if (resp.ok) {
        showToast(`Activity ${newStatus ? 'activated' : 'deactivated'}.`)
        onRefreshServices && onRefreshServices()
      } else {
        showToast('Failed to update status.')
      }
    } catch {
      showToast('Network error. Please check connection.')
    }
  }

  const handleDelete = async (serviceId) => {
    if (!window.confirm('Are you sure you want to delete this activity/service?')) return
    try {
      const resp = await fetch(`/api/provider/prices/${serviceId}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.status === 204) {
        showToast('Activity/Service deleted.')
        onRefreshServices && onRefreshServices()
      } else if (resp.status === 401) {
        onLogout && onLogout()
      } else {
        showToast('Failed to delete item.')
      }
    } catch {
      showToast('Network error. Please check connection.')
    }
  }

  // Filtered List
  const filtered = services.filter(s => {
    const matchSearch = s.serviceName.toLowerCase().includes(search.toLowerCase()) ||
      (s.description && s.description.toLowerCase().includes(search.toLowerCase()))
    if (!matchSearch) return false
    if (filterStatus === 'active') return s.isActive !== false
    if (filterStatus === 'inactive') return s.isActive === false
    return true
  })

  return (
    <div className="pd-activities-tab">
      <div className="pd-page-header">
        <div className="pd-page-header__left">
          <h1>Activity & Service Management</h1>
          <p>Add, edit, deactivate, or remove tourism activities and services you offer to visitors.</p>
        </div>
        <button className="pd-quick-btn pd-quick-btn--primary" onClick={openAdd} id="add-activity-btn">
          ➕ Add New Activity / Service
        </button>
      </div>

      {modal && (
        <Modal title={modal === 'edit' ? 'Edit Activity / Service' : 'Add New Activity / Service'} onClose={closeModal}>
          <form onSubmit={handleSubmit} className="pd-modal__form" noValidate>
            {formError && <div className="pd-form-error">{formError}</div>}

            <div className="pd-form-group">
              <label htmlFor="srv-name">Activity / Service Name *</label>
              <input
                id="srv-name"
                name="serviceName"
                type="text"
                value={form.serviceName}
                onChange={handleFormChange}
                placeholder="e.g. Half-Day Yala Safari Tour"
                required
              />
            </div>

            <div className="pd-form-group">
              <label htmlFor="srv-desc">Description & Inclusions</label>
              <textarea
                id="srv-desc"
                name="description"
                rows="3"
                value={form.description}
                onChange={handleFormChange}
                placeholder="Provide details about duration, inclusions, difficulty, and meeting points..."
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px' }}>
              <div className="pd-form-group">
                <label htmlFor="srv-price">Price (LKR) *</label>
                <input
                  id="srv-price"
                  name="pricePerUnit"
                  type="number"
                  min="0.01"
                  step="100"
                  value={form.pricePerUnit}
                  onChange={handleFormChange}
                  placeholder="e.g. 7500"
                  required
                />
              </div>

              <div className="pd-form-group">
                <label htmlFor="srv-unit">Pricing Unit *</label>
                <input
                  id="srv-unit"
                  name="unit"
                  type="text"
                  value={form.unit}
                  onChange={handleFormChange}
                  placeholder="per person, per group, per hour"
                  required
                />
              </div>
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', padding: '6px 0' }}>
              <input
                id="srv-active"
                name="isActive"
                type="checkbox"
                checked={form.isActive}
                onChange={handleFormChange}
                style={{ width: '18px', height: '18px', accentColor: '#168aad' }}
              />
              <label htmlFor="srv-active" style={{ fontSize: '13.5px', fontWeight: 600, color: '#123b5d', cursor: 'pointer' }}>
                Active & visible for bookings
              </label>
            </div>

            <div className="pd-modal__actions">
              <button type="button" className="pd-cancel-btn" onClick={closeModal} disabled={formLoading}>Cancel</button>
              <button type="submit" className="pd-save-btn" disabled={formLoading}>
                {formLoading ? 'Saving…' : modal === 'edit' ? 'Update Activity' : 'Publish Activity'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Filter and Search Bar */}
      <div className="pd-filter-bar">
        <div className="pd-search-wrap">
          <span className="pd-search-icon">🔍</span>
          <input
            type="text"
            className="pd-search-input"
            placeholder="Search activities or descriptions..."
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

      {/* Table of Services */}
      <div className="pd-card">
        <div className="pd-card__body" style={{ padding: 0 }}>
          {filtered.length === 0 ? (
            <div className="pd-empty">
              <div className="pd-empty__icon">🏄</div>
              <p className="pd-empty__title">No activities found</p>
              <p className="pd-empty__msg">
                {search || filterStatus !== 'all' ? 'Try adjusting your search query or filter.' : 'Click "Add New Activity" to publish your first service offering.'}
              </p>
            </div>
          ) : (
            <div className="pd-table-wrap" style={{ border: 'none', borderRadius: 0 }}>
              <table className="pd-table">
                <thead>
                  <tr>
                    <th>Activity / Service</th>
                    <th>Description & Details</th>
                    <th>Price (LKR)</th>
                    <th>Unit</th>
                    <th>Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(s => (
                    <tr key={s.id}>
                      <td style={{ fontWeight: 700, color: '#123b5d' }}>{s.serviceName}</td>
                      <td style={{ color: '#666666', maxWidth: '280px' }}>{s.description || '—'}</td>
                      <td style={{ fontWeight: 700, color: '#168aad' }}>{formatCurrency(s.pricePerUnit)}</td>
                      <td>{s.unit}</td>
                      <td>
                        <span className={`pd-badge pd-badge--${s.isActive !== false ? 'active' : 'inactive'}`}>
                          {s.isActive !== false ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td>
                        <div className="pd-row-actions">
                          <button
                            className="pd-row-btn pd-row-btn--toggle"
                            title={s.isActive !== false ? 'Deactivate this activity' : 'Activate this activity'}
                            onClick={() => handleToggleStatus(s)}
                          >
                            {s.isActive !== false ? 'Deactivate' : 'Activate'}
                          </button>
                          <button className="pd-row-btn pd-row-btn--edit" onClick={() => openEdit(s)}>
                            Edit
                          </button>
                          <button className="pd-row-btn pd-row-btn--delete" onClick={() => handleDelete(s.id)}>
                            Delete
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

// ── 4. Booking Management Tab ─────────────────────────────────────────────────

function BookingsTab({ bookings, onUpdateBookingStatus }) {
  const [search, setSearch] = useState('')
  const [filterStatus, setFilterStatus] = useState('all') // all | pending | confirmed | completed | cancelled
  const [selectedBooking, setSelectedBooking] = useState(null)

  const handleStatusChange = (bookingId, newStatus) => {
    onUpdateBookingStatus && onUpdateBookingStatus(bookingId, newStatus)
    if (selectedBooking && selectedBooking.id === bookingId) {
      setSelectedBooking(prev => ({ ...prev, status: newStatus }))
    }
  }

  const filtered = bookings.filter(b => {
    const matchSearch = b.visitorName.toLowerCase().includes(search.toLowerCase()) ||
      b.id.toLowerCase().includes(search.toLowerCase()) ||
      b.activityName.toLowerCase().includes(search.toLowerCase())
    if (!matchSearch) return false
    if (filterStatus !== 'all' && b.status.toLowerCase() !== filterStatus.toLowerCase()) return false
    return true
  })

  return (
    <div className="pd-bookings-tab">
      <div className="pd-page-header">
        <div className="pd-page-header__left">
          <h1>Booking Management</h1>
          <p>Review visitor reservations, view complete booking details, and manage reservation statuses.</p>
        </div>
      </div>

      {/* Booking Details Modal */}
      {selectedBooking && (
        <Modal title={`Booking Details (${selectedBooking.id})`} onClose={() => setSelectedBooking(null)} wide>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', paddingBottom: '16px', borderBottom: '1px solid #f0ece3' }}>
              <div>
                <h3 style={{ margin: '0 0 4px', color: '#123b5d' }}>{selectedBooking.activityName}</h3>
                <span style={{ fontSize: '13px', color: '#777' }}>Booking Ref: <strong>{selectedBooking.id}</strong></span>
              </div>
              <span className={`pd-badge pd-badge--${selectedBooking.status.toLowerCase()}`} style={{ fontSize: '12px', padding: '5px 12px' }}>
                {selectedBooking.status}
              </span>
            </div>

            <div className="pd-fields">
              <div className="pd-field">
                <span className="pd-field__label">Visitor Name</span>
                <span className="pd-field__value">👤 {selectedBooking.visitorName}</span>
              </div>
              <div className="pd-field">
                <span className="pd-field__label">Email Address</span>
                <span className="pd-field__value">✉️ {selectedBooking.visitorEmail}</span>
              </div>
              <div className="pd-field">
                <span className="pd-field__label">Contact Phone</span>
                <span className="pd-field__value">📞 {selectedBooking.visitorPhone}</span>
              </div>
              <div className="pd-field">
                <span className="pd-field__label">Scheduled Date</span>
                <span className="pd-field__value">📅 {formatDate(selectedBooking.date)}</span>
              </div>
              <div className="pd-field">
                <span className="pd-field__label">Time Slot</span>
                <span className="pd-field__value">🕐 {selectedBooking.timeSlot}</span>
              </div>
              <div className="pd-field">
                <span className="pd-field__label">Party Size / Guests</span>
                <span className="pd-field__value">👥 {selectedBooking.guests} People</span>
              </div>
              <div className="pd-field">
                <span className="pd-field__label">Payment Status</span>
                <span className="pd-field__value" style={{ color: '#4f8a45', fontWeight: 600 }}>💳 {selectedBooking.paymentStatus}</span>
              </div>
              <div className="pd-field">
                <span className="pd-field__label">Total Amount</span>
                <span className="pd-field__value" style={{ color: '#168aad', fontWeight: 800, fontSize: '16px' }}>{formatCurrency(selectedBooking.totalAmount)}</span>
              </div>
              <div className="pd-field pd-field--full">
                <span className="pd-field__label">Special Requests / Notes</span>
                <span className="pd-field__value" style={{ background: '#faf8f3', padding: '10px 12px', borderRadius: '8px', border: '1px solid #ede8dc' }}>
                  {selectedBooking.specialRequests || 'No special requests provided.'}
                </span>
              </div>
            </div>

            <div style={{ paddingTop: '16px', borderTop: '1px solid #f0ece3', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '10px' }}>
              <span style={{ fontSize: '13px', fontWeight: 600, color: '#123b5d' }}>Manage Status:</span>
              <div style={{ display: 'flex', gap: '8px' }}>
                {selectedBooking.status === 'Pending' && (
                  <button className="pd-quick-btn pd-quick-btn--primary" onClick={() => handleStatusChange(selectedBooking.id, 'Confirmed')}>
                    ✓ Confirm Booking
                  </button>
                )}
                {selectedBooking.status === 'Confirmed' && (
                  <button className="pd-quick-btn pd-quick-btn--primary" onClick={() => handleStatusChange(selectedBooking.id, 'Completed')}>
                    ★ Mark Completed
                  </button>
                )}
                {selectedBooking.status !== 'Cancelled' && (
                  <button className="pd-quick-btn pd-quick-btn--secondary" style={{ color: '#e74c3c', borderColor: '#e74c3c' }} onClick={() => handleStatusChange(selectedBooking.id, 'Cancelled')}>
                    ✕ Cancel Reservation
                  </button>
                )}
              </div>
            </div>
          </div>
        </Modal>
      )}

      {/* Filter and Search Bar */}
      <div className="pd-filter-bar">
        <div className="pd-search-wrap">
          <span className="pd-search-icon">🔍</span>
          <input
            type="text"
            className="pd-search-input"
            placeholder="Search by visitor, ID, or activity..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <div className="pd-filter-pills">
          <button className={`pd-filter-pill ${filterStatus === 'all' ? 'active' : ''}`} onClick={() => setFilterStatus('all')}>
            All ({bookings.length})
          </button>
          <button className={`pd-filter-pill ${filterStatus === 'pending' ? 'active' : ''}`} onClick={() => setFilterStatus('pending')}>
            Pending ({bookings.filter(b => b.status === 'Pending').length})
          </button>
          <button className={`pd-filter-pill ${filterStatus === 'confirmed' ? 'active' : ''}`} onClick={() => setFilterStatus('confirmed')}>
            Confirmed ({bookings.filter(b => b.status === 'Confirmed').length})
          </button>
          <button className={`pd-filter-pill ${filterStatus === 'completed' ? 'active' : ''}`} onClick={() => setFilterStatus('completed')}>
            Completed ({bookings.filter(b => b.status === 'Completed').length})
          </button>
        </div>
      </div>

      {/* Bookings Table */}
      <div className="pd-card">
        <div className="pd-card__body" style={{ padding: 0 }}>
          {filtered.length === 0 ? (
            <div className="pd-empty">
              <div className="pd-empty__icon">📅</div>
              <p className="pd-empty__title">No bookings found</p>
              <p className="pd-empty__msg">No reservation records match the active search and status filter.</p>
            </div>
          ) : (
            <div className="pd-table-wrap" style={{ border: 'none', borderRadius: 0 }}>
              <table className="pd-table">
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
                      <td style={{ color: '#333' }}>{b.activityName}</td>
                      <td>{formatDate(b.date)}</td>
                      <td>{b.guests}</td>
                      <td style={{ fontWeight: 700, color: '#123b5d' }}>{formatCurrency(b.totalAmount)}</td>
                      <td>
                        <span className={`pd-badge pd-badge--${b.status.toLowerCase()}`}>{b.status}</span>
                      </td>
                      <td>
                        <div className="pd-row-actions">
                          <button className="pd-row-btn pd-row-btn--view" onClick={() => setSelectedBooking(b)}>
                            Details
                          </button>
                          {b.status === 'Pending' && (
                            <button className="pd-row-btn pd-row-btn--confirm" onClick={() => handleStatusChange(b.id, 'Confirmed')}>
                              Confirm
                            </button>
                          )}
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

// ── 5. Notifications Tab ──────────────────────────────────────────────────────

function NotificationsTab({ notifications, onMarkAllRead, onToggleRead, onClearAll }) {
  const [filter, setFilter] = useState('all') // all | booking | verification | provider

  const filtered = notifications.filter(n => {
    if (filter === 'all') return true
    return n.category === filter
  })

  const unreadCount = notifications.filter(n => !n.read).length

  return (
    <div className="pd-notifications-tab">
      <div className="pd-page-header">
        <div className="pd-page-header__left">
          <h1>Notifications</h1>
          <p>Stay updated on new visitor reservations, accreditation verification, and system updates.</p>
        </div>
        <div style={{ display: 'flex', gap: '8px' }}>
          {unreadCount > 0 && (
            <button className="pd-quick-btn pd-quick-btn--secondary" onClick={onMarkAllRead}>
              ✓ Mark All Read
            </button>
          )}
          {notifications.length > 0 && (
            <button className="pd-quick-btn pd-quick-btn--secondary" onClick={onClearAll}>
              Clear All
            </button>
          )}
        </div>
      </div>

      <div className="pd-filter-pills" style={{ marginBottom: '20px' }}>
        <button className={`pd-filter-pill ${filter === 'all' ? 'active' : ''}`} onClick={() => setFilter('all')}>
          All ({notifications.length})
        </button>
        <button className={`pd-filter-pill ${filter === 'booking' ? 'active' : ''}`} onClick={() => setFilter('booking')}>
          📅 Bookings ({notifications.filter(n => n.category === 'booking').length})
        </button>
        <button className={`pd-filter-pill ${filter === 'verification' ? 'active' : ''}`} onClick={() => setFilter('verification')}>
          🛡️ Verification ({notifications.filter(n => n.category === 'verification').length})
        </button>
        <button className={`pd-filter-pill ${filter === 'provider' ? 'active' : ''}`} onClick={() => setFilter('provider')}>
          📢 Updates ({notifications.filter(n => n.category === 'provider').length})
        </button>
      </div>

      {filtered.length === 0 ? (
        <div className="pd-card">
          <div className="pd-card__body">
            <div className="pd-empty">
              <div className="pd-empty__icon">🔔</div>
              <p className="pd-empty__title">No notifications</p>
              <p className="pd-empty__msg">You have caught up with all notifications in this category.</p>
            </div>
          </div>
        </div>
      ) : (
        <div className="pd-notif-list">
          {filtered.map(n => (
            <div
              key={n.id}
              className={`pd-notif-item ${!n.read ? 'pd-notif-item--unread' : ''}`}
              style={{ cursor: 'pointer' }}
              onClick={() => onToggleRead && onToggleRead(n.id)}
            >
              <div
                className="pd-notif-icon"
                style={{
                  background: n.category === 'booking' ? 'rgba(22, 138, 173, 0.15)' : n.category === 'verification' ? 'rgba(79, 138, 69, 0.15)' : 'rgba(214, 168, 95, 0.2)'
                }}
              >
                {n.category === 'booking' ? '📅' : n.category === 'verification' ? '🛡️' : '📢'}
              </div>
              <div className="pd-notif-content">
                <h3 className="pd-notif-title">{n.title}</h3>
                <p className="pd-notif-desc">{n.desc}</p>
                <span className="pd-notif-time">{n.time}</span>
              </div>
              {!n.read && <div className="pd-notif-dot" title="Unread" />}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

// ── 6. Account Profile Tab ────────────────────────────────────────────────────

function AccountTab({ token, onLogout, showToast }) {
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
        setLoadError('Failed to load profile.')
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
        showToast('Personal account profile updated successfully.')
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
    <div className="pd-account-tab">
      <div className="pd-page-header">
        <div className="pd-page-header__left">
          <h1>Account Settings</h1>
          <p>Manage your personal profile and account credentials.</p>
        </div>
      </div>

      <div className="pd-card">
        <div className="pd-card__body">
          {loading && <LoadingState label="Loading profile information…" />}
          {loadError && !loading && <div className="pd-form-error">{loadError}</div>}

          {!loading && profile && !editing && (
            <>
              <div className="pd-identity">
                <div className="pd-avatar" aria-hidden="true">{initials(profile.firstName, profile.lastName)}</div>
                <div className="pd-identity__info">
                  <h2 className="pd-identity__name">{profile.firstName} {profile.lastName}</h2>
                  <p className="pd-identity__email">{profile.email}</p>
                  <span className="pd-identity__badge">🏔 Provider Account</span>
                </div>
                <button className="pd-edit-btn" onClick={handleEdit} id="edit-account-profile-btn">
                  ✏️ Edit Profile
                </button>
              </div>

              <div className="pd-fields">
                <div className="pd-field">
                  <span className="pd-field__label">First Name</span>
                  <span className="pd-field__value">{profile.firstName}</span>
                </div>
                <div className="pd-field">
                  <span className="pd-field__label">Last Name</span>
                  <span className="pd-field__value">{profile.lastName}</span>
                </div>
                <div className="pd-field">
                  <span className="pd-field__label">Email Address</span>
                  <span className="pd-field__value">{profile.email}</span>
                </div>
                <div className="pd-field">
                  <span className="pd-field__label">Contact Phone</span>
                  <span className="pd-field__value">{profile.phoneNumber || '—'}</span>
                </div>
                <div className="pd-field">
                  <span className="pd-field__label">Nationality</span>
                  <span className="pd-field__value">{profile.nationality || '—'}</span>
                </div>
                <div className="pd-field">
                  <span className="pd-field__label">Member Since</span>
                  <span className="pd-field__value">{formatDate(profile.createdAt)}</span>
                </div>
              </div>
            </>
          )}

          {!loading && profile && editing && (
            <form onSubmit={handleSave} className="pd-edit-form" noValidate>
              <div className="pd-identity" style={{ marginBottom: '24px' }}>
                <div className="pd-avatar" aria-hidden="true">{initials(formData.firstName, formData.lastName)}</div>
                <div className="pd-identity__info">
                  <h2 className="pd-identity__name">{formData.firstName} {formData.lastName}</h2>
                  <p className="pd-identity__email">{profile.email}</p>
                </div>
              </div>

              {saveError && <div className="pd-form-error">{saveError}</div>}

              <div className="pd-form-grid">
                <div className="pd-form-group">
                  <label htmlFor="acc-firstName">First Name *</label>
                  <input
                    id="acc-firstName"
                    name="firstName"
                    type="text"
                    value={formData.firstName}
                    onChange={handleChange}
                    required
                  />
                </div>
                <div className="pd-form-group">
                  <label htmlFor="acc-lastName">Last Name *</label>
                  <input
                    id="acc-lastName"
                    name="lastName"
                    type="text"
                    value={formData.lastName}
                    onChange={handleChange}
                    required
                  />
                </div>
                <div className="pd-form-group">
                  <label htmlFor="acc-email">Email Address</label>
                  <input id="acc-email" type="email" value={profile.email} disabled aria-readonly="true" />
                  <p className="pd-field-note">Email address cannot be changed.</p>
                </div>
                <div className="pd-form-group">
                  <label htmlFor="acc-phone">Phone Number *</label>
                  <input
                    id="acc-phone"
                    name="phoneNumber"
                    type="tel"
                    value={formData.phoneNumber}
                    onChange={handleChange}
                    required
                  />
                </div>
                <div className="pd-form-group pd-form-group--full">
                  <label htmlFor="acc-nationality">Nationality *</label>
                  <input
                    id="acc-nationality"
                    name="nationality"
                    type="text"
                    value={formData.nationality}
                    onChange={handleChange}
                    required
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

  // Auth Guard
  useEffect(() => {
    if (!token || role !== 'Provider') {
      onLogout && onLogout()
    }
  }, [token, role, onLogout])

  // Sync Bookings to LocalStorage
  useEffect(() => {
    try {
      localStorage.setItem('ceylonquest_provider_bookings', JSON.stringify(bookings))
    } catch {
      // ignore
    }
  }, [bookings])

  // Sync Notifications to LocalStorage
  useEffect(() => {
    try {
      localStorage.setItem('ceylonquest_provider_notifications', JSON.stringify(notifications))
    } catch {
      // ignore
    }
  }, [notifications])

  const showToast = useCallback((msg) => setToast(msg), [])

  // Fetch Provider Info
  const fetchProviderInfo = useCallback(async () => {
    if (!token) return
    try {
      const resp = await fetch('/api/provider/info', {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        setProviderInfo(await resp.json())
      }
    } catch {
      // ignore
    }
  }, [token])

  // Fetch Services & Prices
  const fetchServices = useCallback(async () => {
    if (!token) return
    try {
      const resp = await fetch('/api/provider/prices', {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        setServices(await resp.json())
      }
    } catch {
      // ignore
    }
  }, [token])

  useEffect(() => {
    fetchProviderInfo()
    fetchServices()
  }, [fetchProviderInfo, fetchServices])

  // Handlers for Bookings & Notifications
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

  const handleLogout = () => {
    localStorage.removeItem('authToken')
    localStorage.removeItem('userRole')
    onLogout && onLogout()
  }

  const unreadNotifCount = notifications.filter(n => !n.read).length

  const navItems = [
    { key: 'overview', icon: '📊', label: 'Overview' },
    { key: 'business', icon: '🏢', label: 'Business Profile' },
    { key: 'services', icon: '🏄', label: 'Activities & Services' },
    { key: 'bookings', icon: '📅', label: 'Bookings' },
    { key: 'notifications', icon: '🔔', label: 'Notifications', badge: unreadNotifCount > 0 ? unreadNotifCount : null },
    { key: 'account', icon: '👤', label: 'Account' }
  ]

  return (
    <div className="pd-page">
      {toast && <Toast message={toast} onClose={() => setToast(null)} />}

      {/* ── Sidebar ── */}
      <aside className="pd-sidebar">
        <div className="pd-sidebar__brand">
          <span className="pd-sidebar__logo">CeylonQuest</span>
          <span className="pd-sidebar__role">🏔 Provider Hub</span>
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
          <button className="pd-logout-btn" onClick={handleLogout} id="pd-logout-btn">
            <span className="pd-nav-icon">🚪</span> Log Out
          </button>
        </div>
      </aside>

      {/* ── Main Content Body ── */}
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
          />
        )}
      </main>
    </div>
  )
}

export default ProviderDashboard
