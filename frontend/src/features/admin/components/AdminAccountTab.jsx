import React, { useState, useEffect, useRef } from 'react'
import {
  PermIdentityIcon,
  EmailIcon,
  LocalPhoneIcon,
  MyLocationIcon,
  PublicIcon,
  PhotoCameraIcon,
  DeleteSweepIcon,
  BadgeIcon,
  CreateIcon,
  CheckCircleIcon
} from '../../../components/Icons'
import ConfirmModal from '../../../components/ConfirmModal'
import { apiUrl } from '../../../api/client'

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


export default function AdminAccountTab({ token, onLogout, showToast, onProfileUpdate }) {
  const [profile, setProfile] = useState(null)
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState(null)
  const [editing, setEditing] = useState(false)
  const [formData, setFormData] = useState({})
  const [saveLoading, setSaveLoading] = useState(false)
  const [saveError, setSaveError] = useState(null)
  const [avatarUploading, setAvatarUploading] = useState(false)
  const [avatarError, setAvatarError] = useState(null)
  const [showRemoveConfirm, setShowRemoveConfirm] = useState(false)

  const onLogoutRef = useRef(onLogout)
  useEffect(() => { onLogoutRef.current = onLogout }, [onLogout])
  const onProfileUpdateRef = useRef(onProfileUpdate)
  useEffect(() => { onProfileUpdateRef.current = onProfileUpdate }, [onProfileUpdate])

  const fetchProfile = useCallback(async () => {
    const activeToken = token || localStorage.getItem('authToken')
    if (!activeToken) {
      setLoadError('Session expired. Please log in again.')
      setLoading(false)
      onLogoutRef.current && onLogoutRef.current()
      return
    }

    setLoading(true)
    setLoadError(null)
    try {
      const resp = await fetch(apiUrl('/api/users/me'), {
        headers: { Authorization: `Bearer ${activeToken}` }
      })
      if (resp.ok) {
        const data = await resp.json()
        setProfile(data)
        onProfileUpdateRef.current && onProfileUpdateRef.current(data)
        setFormData({
          firstName: data.firstName || '',
          lastName: data.lastName || '',
          phoneNumber: data.phoneNumber || '',
          nationality: data.nationality || ''
        })
      } else if (resp.status === 401) {
        setLoadError('Session expired or unauthorized. Please log in again.')
        onLogoutRef.current && onLogoutRef.current()
      } else {
        setLoadError('Failed to load admin profile. Please try again.')
      }
    } catch {
      setLoadError('Network error. Please check your connection.')
    } finally {
      setLoading(false)
    }
  }, [token])

  useEffect(() => {
    fetchProfile()
  }, [fetchProfile])

  const handleEdit = () => {
    setSaveError(null)
    setEditing(true)
  }

  const handleCancel = () => {
    setFormData({
      firstName: profile?.firstName || '',
      lastName: profile?.lastName || '',
      phoneNumber: profile?.phoneNumber || '',
      nationality: profile?.nationality || ''
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

    const activeToken = token || localStorage.getItem('authToken')
    try {
      const resp = await fetch(apiUrl('/api/users/me'), {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${activeToken}`
        },
        body: JSON.stringify(formData)
      })

      if (resp.ok) {
        const body = await resp.json()
        const updated = body.profile ?? body
        setProfile(updated)
        onProfileUpdateRef.current && onProfileUpdateRef.current(updated)
        setFormData({
          firstName: updated.firstName,
          lastName: updated.lastName,
          phoneNumber: updated.phoneNumber,
          nationality: updated.nationality
        })
        setEditing(false)
        showToast('Admin profile updated successfully.')
      } else if (resp.status === 401) {
        onLogoutRef.current && onLogoutRef.current()
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

    const activeToken = token || localStorage.getItem('authToken')
    try {
      const data = new FormData()
      data.append('file', file)

      const resp = await fetch(apiUrl('/api/users/me/profile-picture'), {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${activeToken}`
        },
        body: data
      })

      if (resp.ok) {
        const result = await resp.json()
        const updated = result.profile ?? { ...profile, profilePictureUrl: result.profilePictureUrl }
        setProfile(updated)
        onProfileUpdateRef.current && onProfileUpdateRef.current(updated)
        showToast('Admin profile picture updated successfully.')
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

    const activeToken = token || localStorage.getItem('authToken')
    try {
      const resp = await fetch(apiUrl('/api/users/me/profile-picture'), {
        method: 'DELETE',
        headers: {
          Authorization: `Bearer ${activeToken}`
        }
      })

      if (resp.ok) {
        const result = await resp.json()
        const updated = result.profile ?? { ...profile, profilePictureUrl: null }
        setProfile(updated)
        onProfileUpdateRef.current && onProfileUpdateRef.current(updated)
        setShowRemoveConfirm(false)
        showToast('Admin profile picture removed.')
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
    <div className="ad-account-tab">
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
      <div className="ad-page-header">
        <div className="ad-page-header__left">
          <h1>Admin Account Settings</h1>
          <p>View and manage administrative profile credentials.</p>
        </div>
      </div>

      <div className="ad-card">
        <div className="ad-card__body">
          {loading && <LoadingState label="Loading profile information…" />}
          {loadError && !loading && (
            <div className="ad-form-error" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <span>{loadError}</span>
              <button
                type="button"
                onClick={fetchProfile}
                style={{
                  background: '#123b5d',
                  color: '#ffffff',
                  border: 'none',
                  padding: '5px 12px',
                  borderRadius: '6px',
                  cursor: 'pointer',
                  fontSize: '12px',
                  fontWeight: '600'
                }}
              >
                Retry
              </button>
            </div>
          )}

          {!loading && profile && !editing && (
            <>
              <div className="ad-identity">
                <div className="ad-avatar-wrapper">
                  <div className="ad-avatar">
                    {profile.profilePictureUrl ? (
                      <img src={formatAvatarUrl(profile.profilePictureUrl)} alt="" className="ad-avatar__img" />
                    ) : (
                      initials(profile.firstName, profile.lastName)
                    )}
                  </div>
                  <label className="ad-avatar-upload-btn" title="Upload / Change admin photo">
                    <PhotoCameraIcon size={14} />
                    <input
                      type="file"
                      accept="image/png, image/jpeg, image/webp"
                      onChange={handleAvatarChange}
                      disabled={avatarUploading}
                      className="ad-hidden-input"
                    />
                  </label>
                </div>

                <div className="ad-identity__info">
                  <h2 className="ad-identity__name">{profile.firstName} {profile.lastName}</h2>
                  <p className="ad-identity__email">{profile.email}</p>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginTop: '6px' }}>
                    <span className="ad-identity__badge"><BadgeIcon size={13} className="ad-icon-spacing-4" /> System Administrator</span>
                    {profile.profilePictureUrl && (
                      <button
                        type="button"
                        className="ad-avatar-remove-text-btn"
                        onClick={() => setShowRemoveConfirm(true)}
                        disabled={avatarUploading}
                      >
                        <DeleteSweepIcon size={13} className="ad-icon-spacing-4" /> Remove Photo
                      </button>
                    )}
                  </div>
                  {avatarUploading && <div className="ad-avatar-status">Uploading photo…</div>}
                  {avatarError && <div className="ad-avatar-error">{avatarError}</div>}
                </div>
                <button className="ad-edit-btn" onClick={handleEdit} id="edit-admin-profile-btn">
                  <CreateIcon size={14} className="ad-icon-spacing" /> Edit Profile
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
                <div className="ad-avatar-wrapper">
                  <div className="ad-avatar">
                    {profile.profilePictureUrl ? (
                      <img src={formatAvatarUrl(profile.profilePictureUrl)} alt="" className="ad-avatar__img" />
                    ) : (
                      initials(formData.firstName, formData.lastName)
                    )}
                  </div>
                  <label className="ad-avatar-upload-btn" title="Upload / Change admin photo">
                    <PhotoCameraIcon size={14} />
                    <input
                      type="file"
                      accept="image/png, image/jpeg, image/webp"
                      onChange={handleAvatarChange}
                      disabled={avatarUploading}
                      className="ad-hidden-input"
                    />
                  </label>
                </div>
                <div className="ad-identity__info">
                  <h2 className="ad-identity__name">{formData.firstName} {formData.lastName}</h2>
                  <p className="ad-identity__email">{profile.email}</p>
                  {avatarUploading && <div className="ad-avatar-status">Uploading photo…</div>}
                  {avatarError && <div className="ad-avatar-error">{avatarError}</div>}
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

// ── 8. Reports Tab ───────────────────────────────────────────────────────────
