import { useState, useEffect, useCallback } from 'react'
import '../styles/VisitorDashboard.css'

function SuccessToast({ message, onClose }) {
  useEffect(() => {
    const t = setTimeout(onClose, 4000)
    return () => clearTimeout(t)
  }, [onClose])

  return (
    <div className="vd-toast" role="alert" aria-live="polite">
      <div className="vd-toast__icon">✓</div>
      <div className="vd-toast__body">
        <p className="vd-toast__title">Profile Updated</p>
        <p className="vd-toast__msg">{message}</p>
      </div>
      <button className="vd-toast__close" onClick={onClose} aria-label="Close">✕</button>
    </div>
  )
}

function initials(first, last) {
  return `${(first || '').charAt(0)}${(last || '').charAt(0)}`.toUpperCase() || '?'
}

function formatDate(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })
}

function VisitorDashboard({ onLogout }) {
  const [activePage, setActivePage]   = useState('profile')
  const [profile,    setProfile]      = useState(null)
  const [loadError,  setLoadError]    = useState(null)
  const [loading,    setLoading]      = useState(true)

  const [editing,     setEditing]     = useState(false)
  const [formData,    setFormData]    = useState({})
  const [saveLoading, setSaveLoading] = useState(false)
  const [saveError,   setSaveError]   = useState(null)
  const [toast,       setToast]       = useState(null)

  const token = localStorage.getItem('authToken')

  const fetchProfile = useCallback(async () => {
    const currentToken = localStorage.getItem('authToken')
    if (!currentToken) {
      onLogout && onLogout()
      return
    }
    setLoading(true)
    setLoadError(null)
    try {
      const resp = await fetch('/api/users/me', {
        headers: { Authorization: `Bearer ${currentToken}` }
      })
      if (resp.ok) {
        const data = await resp.json()
        setProfile(data)
        setFormData({
          firstName:   data.firstName,
          lastName:    data.lastName,
          phoneNumber: data.phoneNumber,
          nationality: data.nationality
        })
      } else if (resp.status === 401) {
        setLoadError('Session expired or unauthorized. Please log in again.')
        setTimeout(() => { onLogout && onLogout() }, 2000)
      } else {
        setLoadError('Failed to load profile. Please try again.')
      }
    } catch {
      setLoadError('Network error. Please check your connection.')
    } finally {
      setLoading(false)
    }
  }, [onLogout])

  useEffect(() => { fetchProfile() }, [fetchProfile])

  const handleEdit = () => {
    setSaveError(null)
    setEditing(true)
  }

  const handleCancel = () => {
    setFormData({
      firstName:   profile.firstName,
      lastName:    profile.lastName,
      phoneNumber: profile.phoneNumber,
      nationality: profile.nationality
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
        method:  'PUT',
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
          firstName:   updated.firstName,
          lastName:    updated.lastName,
          phoneNumber: updated.phoneNumber,
          nationality: updated.nationality
        })
        setEditing(false)
        setToast('Your profile has been updated successfully.')
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

  const handleLogout = () => {
    localStorage.removeItem('authToken')
    localStorage.removeItem('userRole')
    onLogout && onLogout()
  }

  return (
    <div className="vd-page">
      {toast && <SuccessToast message={toast} onClose={() => setToast(null)} />}

      {/* ── Sidebar ── */}
      <aside className="vd-sidebar">
        <div className="vd-sidebar__brand">
          <span className="vd-sidebar__logo">CeylonQuest</span>
          <span className="vd-sidebar__role">Visitor</span>
        </div>

        <ul className="vd-sidebar__nav">
          <li>
            <button
              className={activePage === 'profile' ? 'active' : ''}
              onClick={() => setActivePage('profile')}
              id="nav-profile"
            >
              <span className="vd-nav-icon">👤</span> My Profile
            </button>
          </li>
          <li>
            <button disabled title="Coming soon" id="nav-bookings">
              <span className="vd-nav-icon">🗓️</span> My Bookings
              <span className="vd-nav-soon">Soon</span>
            </button>
          </li>
          <li>
            <button disabled title="Coming soon" id="nav-settings">
              <span className="vd-nav-icon">⚙️</span> Settings
              <span className="vd-nav-soon">Soon</span>
            </button>
          </li>
        </ul>

        <div className="vd-sidebar__footer">
          <button className="vd-logout-btn" onClick={handleLogout} id="logout-btn">
            <span className="vd-nav-icon">🚪</span> Log Out
          </button>
        </div>
      </aside>

      {/* ── Main ── */}
      <main className="vd-main">
        <div className="vd-page-header">
          <h1>My Profile</h1>
          <p>View and manage your personal information.</p>
        </div>

        <div className="vd-profile-card">
          <div className="vd-profile-card__accent" />
          <div className="vd-profile-card__body">

            {loading && (
              <div className="vd-loading">
                <div className="vd-spinner" />
                <span>Loading your profile…</span>
              </div>
            )}

            {loadError && !loading && (
              <div className="vd-form-error">{loadError}</div>
            )}

            {!loading && profile && !editing && (
              <>
                {/* Identity row */}
                <div className="vd-identity">
                  <div className="vd-avatar" aria-hidden="true">
                    {initials(profile.firstName, profile.lastName)}
                  </div>
                  <div className="vd-identity__info">
                    <h2 className="vd-identity__name">{profile.firstName} {profile.lastName}</h2>
                    <p className="vd-identity__email">{profile.email}</p>
                    <span className="vd-identity__badge">✈ Visitor</span>
                  </div>
                  <button className="vd-edit-btn" onClick={handleEdit} id="edit-profile-btn">
                    ✏️ Edit Profile
                  </button>
                </div>

                {/* Fields */}
                <div className="vd-fields">
                  <div className="vd-field">
                    <span className="vd-field__label">First Name</span>
                    <span className="vd-field__value">{profile.firstName}</span>
                  </div>
                  <div className="vd-field">
                    <span className="vd-field__label">Last Name</span>
                    <span className="vd-field__value">{profile.lastName}</span>
                  </div>
                  <div className="vd-field">
                    <span className="vd-field__label">Email Address</span>
                    <span className="vd-field__value">{profile.email}</span>
                  </div>
                  <div className="vd-field">
                    <span className="vd-field__label">Phone Number</span>
                    <span className="vd-field__value">{profile.phoneNumber || '—'}</span>
                  </div>
                  <div className="vd-field">
                    <span className="vd-field__label">Nationality</span>
                    <span className="vd-field__value">{profile.nationality || '—'}</span>
                  </div>
                </div>

                <div className="vd-member-since">
                  Member since {formatDate(profile.createdAt)}
                </div>
              </>
            )}

            {!loading && profile && editing && (
              <form onSubmit={handleSave} className="vd-edit-form" noValidate>
                {/* Identity row (read-only header stays visible) */}
                <div className="vd-identity" style={{ marginBottom: 24 }}>
                  <div className="vd-avatar" aria-hidden="true">
                    {initials(formData.firstName, formData.lastName)}
                  </div>
                  <div className="vd-identity__info">
                    <h2 className="vd-identity__name">{formData.firstName} {formData.lastName}</h2>
                    <p className="vd-identity__email">{profile.email}</p>
                  </div>
                </div>

                {saveError && <div className="vd-form-error">{saveError}</div>}

                <div className="vd-form-grid">
                  <div className="vd-form-group">
                    <label htmlFor="edit-firstName">First Name *</label>
                    <input
                      id="edit-firstName"
                      name="firstName"
                      type="text"
                      value={formData.firstName}
                      onChange={handleChange}
                      placeholder="First name"
                      required
                    />
                  </div>

                  <div className="vd-form-group">
                    <label htmlFor="edit-lastName">Last Name *</label>
                    <input
                      id="edit-lastName"
                      name="lastName"
                      type="text"
                      value={formData.lastName}
                      onChange={handleChange}
                      placeholder="Last name"
                      required
                    />
                  </div>

                  <div className="vd-form-group">
                    <label htmlFor="edit-email">Email Address</label>
                    <input
                      id="edit-email"
                      type="email"
                      value={profile.email}
                      disabled
                      aria-readonly="true"
                    />
                    <p className="vd-field-note">Email cannot be changed.</p>
                  </div>

                  <div className="vd-form-group">
                    <label htmlFor="edit-phone">Phone Number *</label>
                    <input
                      id="edit-phone"
                      name="phoneNumber"
                      type="tel"
                      value={formData.phoneNumber}
                      onChange={handleChange}
                      placeholder="Phone number"
                      required
                    />
                  </div>

                  <div className="vd-form-group vd-form-group--full">
                    <label htmlFor="edit-nationality">Nationality *</label>
                    <input
                      id="edit-nationality"
                      name="nationality"
                      type="text"
                      value={formData.nationality}
                      onChange={handleChange}
                      placeholder="Your nationality"
                      required
                    />
                  </div>
                </div>

                <div className="vd-form-actions">
                  <button
                    type="submit"
                    className="vd-save-btn"
                    id="save-profile-btn"
                    disabled={saveLoading}
                  >
                    {saveLoading ? 'Saving…' : 'Save Changes'}
                  </button>
                  <button
                    type="button"
                    className="vd-cancel-btn"
                    id="cancel-edit-btn"
                    onClick={handleCancel}
                    disabled={saveLoading}
                  >
                    Cancel
                  </button>
                </div>
              </form>
            )}

          </div>
        </div>
      </main>
    </div>
  )
}

export default VisitorDashboard
