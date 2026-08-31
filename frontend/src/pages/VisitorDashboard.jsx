import { useState, useEffect, useCallback } from 'react'
import '../styles/VisitorDashboard.css'
import {
  PermIdentityIcon,
  CalendarMonthIcon,
  SettingsIcon,
  LogoutIcon,
  PublicIcon,
  EmailIcon,
  LocalPhoneIcon,
  CreateIcon,
  BadgeIcon,
  PhotoCameraIcon,
  DeleteSweepIcon
} from '../components/Icons'
import ConfirmModal from '../components/ConfirmModal'
import { apiUrl } from '../api/client'

function SuccessToast({ message, onClose }) {
  useEffect(() => {
    const t = setTimeout(onClose, 4000)
    return () => clearTimeout(t)
  }, [onClose])

  return (
    <div className="vd-toast" role="alert" aria-live="polite">
      <div className="vd-toast__icon"></div>
      <div className="vd-toast__body">
        <p className="vd-toast__title">Profile Updated</p>
        <p className="vd-toast__msg">{message}</p>
      </div>
      <button className="vd-toast__close" onClick={onClose} aria-label="Close"></button>
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

function formatAvatarUrl(url) {
  if (!url) return null
  if (url.startsWith('/uploads/avatars/')) {
    const fileName = url.split('/').pop()
    return apiUrl(`/api/users/avatar/${fileName}`)
  }
  return url
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
      const resp = await fetch(apiUrl('/api/users/me'), {
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
      const resp = await fetch(apiUrl('/api/users/me'), {
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

  const [avatarUploading, setAvatarUploading] = useState(false)
  const [avatarError, setAvatarError] = useState(null)
  const [showRemoveConfirm, setShowRemoveConfirm] = useState(false)

  const handleAvatarChange = async (e) => {
    const file = e.target.files?.[0]
    if (!file) return

    if (!file.type.startsWith('image/')) {
      setAvatarError('Please select a valid image file (JPG, PNG, WebP).')
      return
    }

    if (file.size > 5 * 1024 * 1024) {
      setAvatarError('Image must be smaller than 5 MB.')
      return
    }

    setAvatarError(null)
    setAvatarUploading(true)

    try {
      const data = new FormData()
      data.append('file', file)

      const resp = await fetch(apiUrl('/api/users/me/profile-picture'), {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`
        },
        body: data
      })

      if (resp.ok) {
        const result = await resp.json()
        const updated = result.profile ?? { ...profile, profilePictureUrl: result.profilePictureUrl }
        setProfile(updated)
        setToast('Profile picture updated successfully.')
      } else {
        const errBody = await resp.json().catch(() => ({}))
        setAvatarError(errBody.message || 'Failed to upload profile picture.')
      }
    } catch {
      setAvatarError('Network error while uploading photo.')
    } finally {
      setAvatarUploading(false)
      e.target.value = ''
    }
  }

  const handleRemoveAvatar = async () => {
    setAvatarError(null)
    setAvatarUploading(true)

    try {
      const resp = await fetch(apiUrl('/api/users/me/profile-picture'), {
        method: 'DELETE',
        headers: {
          Authorization: `Bearer ${token}`
        }
      })

      if (resp.ok) {
        const result = await resp.json()
        const updated = result.profile ?? { ...profile, profilePictureUrl: null }
        setProfile(updated)
        setShowRemoveConfirm(false)
        setToast('Profile picture removed.')
      } else {
        const errBody = await resp.json().catch(() => ({}))
        setAvatarError(errBody.message || 'Failed to remove profile picture.')
      }
    } catch {
      setAvatarError('Network error while removing photo.')
    } finally {
      setAvatarUploading(false)
    }
  }

  return (
    <div className="vd-page">
      {toast && <SuccessToast message={toast} onClose={() => setToast(null)} />}

      <ConfirmModal
        isOpen={showRemoveConfirm}
        title="Remove Profile Picture"
        message="Are you sure you want to remove your profile picture?"
        confirmText="Remove Photo"
        cancelText="Cancel"
        confirmVariant="danger"
        onConfirm={handleRemoveAvatar}
        onCancel={() => setShowRemoveConfirm(false)}
        loading={avatarUploading}
      />

      {/* ── Sidebar ── */}
      <aside className="vd-sidebar">
        <div className="vd-sidebar__brand">
          <img src="/dashboard-logo.png" alt="CeylonQuest" className="vd-sidebar__logo-img" />
          <span className="vd-sidebar__role">Visitor</span>
        </div>

        <ul className="vd-sidebar__nav">
          <li>
            <button
              className={activePage === 'profile' ? 'active' : ''}
              onClick={() => setActivePage('profile')}
              id="nav-profile"
            >
              <span className="vd-nav-icon"><PermIdentityIcon size={18} /></span> My Profile
            </button>
          </li>
          <li>
            <button disabled title="Coming soon" id="nav-bookings">
              <span className="vd-nav-icon"><CalendarMonthIcon size={18} /></span> My Bookings
              <span className="vd-nav-soon">Soon</span>
            </button>
          </li>
          <li>
            <button disabled title="Coming soon" id="nav-settings">
              <span className="vd-nav-icon"><SettingsIcon size={18} /></span> Settings
              <span className="vd-nav-soon">Soon</span>
            </button>
          </li>
        </ul>

        <div className="vd-sidebar__footer">
          {profile && (
            <div className="vd-sidebar-user">
              <div className="vd-sidebar-avatar">
                {profile.profilePictureUrl ? (
                  <img src={formatAvatarUrl(profile.profilePictureUrl)} alt="" className="vd-sidebar-avatar__img" />
                ) : (
                  initials(profile.firstName, profile.lastName)
                )}
              </div>
              <div className="vd-sidebar-user__info">
                <div className="vd-sidebar-user__name">{profile.firstName} {profile.lastName}</div>
                <div className="vd-sidebar-user__email">{profile.email}</div>
              </div>
            </div>
          )}
          <button className="vd-logout-btn" onClick={handleLogout} id="logout-btn">
            <span className="vd-nav-icon"><LogoutIcon size={18} /></span> Log Out
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
                  <div className="vd-avatar-wrapper">
                    <div className="vd-avatar">
                      {profile.profilePictureUrl ? (
                        <img src={formatAvatarUrl(profile.profilePictureUrl)} alt="" className="vd-avatar__img" />
                      ) : (
                        initials(profile.firstName, profile.lastName)
                      )}
                    </div>
                    <label className="vd-avatar-upload-btn" title="Upload / Change profile photo">
                      <PhotoCameraIcon size={14} />
                      <input
                        type="file"
                        accept="image/png, image/jpeg, image/webp"
                        onChange={handleAvatarChange}
                        disabled={avatarUploading}
                        style={{ display: 'none' }}
                      />
                    </label>
                  </div>

                  <div className="vd-identity__info">
                    <h2 className="vd-identity__name">{profile.firstName} {profile.lastName}</h2>
                    <p className="vd-identity__email">{profile.email}</p>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginTop: '6px' }}>
                      <span className="vd-identity__badge"><BadgeIcon size={13} style={{ marginRight: 4 }} /> Visitor</span>
                      {profile.profilePictureUrl && (
                        <button
                          type="button"
                          className="vd-avatar-remove-text-btn"
                          onClick={() => setShowRemoveConfirm(true)}
                          disabled={avatarUploading}
                        >
                          <DeleteSweepIcon size={13} style={{ marginRight: 4 }} /> Remove Photo
                        </button>
                      )}
                    </div>
                    {avatarUploading && <div className="vd-avatar-status">Uploading photo…</div>}
                    {avatarError && <div className="vd-avatar-error">{avatarError}</div>}
                  </div>
                  <button className="vd-edit-btn" onClick={handleEdit} id="edit-profile-btn">
                    <CreateIcon size={14} style={{ marginRight: 6 }} /> Edit Profile
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
                    <span className="vd-field__value"><EmailIcon size={14} style={{ marginRight: 6 }} /> {profile.email}</span>
                  </div>
                  <div className="vd-field">
                    <span className="vd-field__label">Phone Number</span>
                    <span className="vd-field__value"><LocalPhoneIcon size={14} style={{ marginRight: 6 }} /> {profile.phoneNumber || '—'}</span>
                  </div>
                  <div className="vd-field">
                    <span className="vd-field__label">Nationality</span>
                    <span className="vd-field__value"><PublicIcon size={14} style={{ marginRight: 6 }} /> {profile.nationality || '—'}</span>
                  </div>
                </div>

                <div className="vd-member-since">
                  <CalendarMonthIcon size={14} style={{ marginRight: 6 }} /> Member since {formatDate(profile.createdAt)}
                </div>
              </>
            )}

            {!loading && profile && editing && (
              <form onSubmit={handleSave} className="vd-edit-form" noValidate>
                {/* Identity row (read-only header stays visible) */}
                <div className="vd-identity" style={{ marginBottom: 24 }}>
                  <div className="vd-avatar-wrapper">
                    <div className="vd-avatar">
                      {profile.profilePictureUrl ? (
                        <img src={formatAvatarUrl(profile.profilePictureUrl)} alt="" className="vd-avatar__img" />
                      ) : (
                        initials(formData.firstName, formData.lastName)
                      )}
                    </div>
                    <label className="vd-avatar-upload-btn" title="Upload / Change profile photo">
                      <PhotoCameraIcon size={14} />
                      <input
                        type="file"
                        accept="image/png, image/jpeg, image/webp"
                        onChange={handleAvatarChange}
                        disabled={avatarUploading}
                        style={{ display: 'none' }}
                      />
                    </label>
                  </div>
                  <div className="vd-identity__info">
                    <h2 className="vd-identity__name">{formData.firstName} {formData.lastName}</h2>
                    <p className="vd-identity__email">{profile.email}</p>
                    {avatarUploading && <div className="vd-avatar-status">Uploading photo…</div>}
                    {avatarError && <div className="vd-avatar-error">{avatarError}</div>}
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
