import React, { useState } from 'react'
import './VisitorProfileTab.css'
import {
  PermIdentityIcon,
  CalendarMonthIcon,
  PublicIcon,
  EmailIcon,
  LocalPhoneIcon,
  CreateIcon,
  BadgeIcon,
  PhotoCameraIcon,
  DeleteSweepIcon
} from '../../../components/Icons'
import ConfirmModal from '../../../components/ConfirmModal'
import LoadingSpinner from '../../../components/common/LoadingSpinner'
import { apiUrl } from '../../../api/client'

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

export default function VisitorProfileTab({
  profile,
  loading,
  loadError,
  token,
  onProfileUpdated,
  showToast
}) {
  const [editing, setEditing] = useState(false)
  const [formData, setFormData] = useState({
    firstName: profile?.firstName || '',
    lastName: profile?.lastName || '',
    phoneNumber: profile?.phoneNumber || '',
    nationality: profile?.nationality || ''
  })
  const [saveLoading, setSaveLoading] = useState(false)
  const [saveError, setSaveError] = useState(null)

  const [avatarUploading, setAvatarUploading] = useState(false)
  const [avatarError, setAvatarError] = useState(null)
  const [showRemoveConfirm, setShowRemoveConfirm] = useState(false)

  const handleStartEdit = () => {
    setFormData({
      firstName: profile?.firstName || '',
      lastName: profile?.lastName || '',
      phoneNumber: profile?.phoneNumber || '',
      nationality: profile?.nationality || ''
    })
    setSaveError(null)
    setEditing(true)
  }

  const handleFormChange = (e) => {
    const { name, value } = e.target
    setFormData(prev => ({ ...prev, [name]: value }))
  }

  const handleSaveProfile = async (e) => {
    e.preventDefault()
    setSaveLoading(true)
    setSaveError(null)

    try {
      const resp = await fetch(apiUrl('/api/users/me'), {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify(formData)
      })

      if (resp.ok) {
        const updated = await resp.json()
        onProfileUpdated(updated)
        setEditing(false)
        showToast('Profile updated successfully.')
      } else {
        const err = await resp.json().catch(() => ({}))
        setSaveError(err.message || 'Failed to update profile.')
      }
    } catch {
      setSaveError('Network error. Unable to save profile.')
    } finally {
      setSaveLoading(false)
    }
  }

  const handleAvatarUpload = async (e) => {
    const file = e.target.files?.[0]
    if (!file) return

    setAvatarError(null)
    setAvatarUploading(true)

    const fd = new FormData()
    fd.append('file', file)

    try {
      const resp = await fetch(apiUrl('/api/users/me/profile-picture'), {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`
        },
        body: fd
      })

      if (resp.ok) {
        const result = await resp.json()
        const updated = result.profile ?? { ...profile, profilePictureUrl: result.profilePictureUrl }
        onProfileUpdated(updated)
        showToast('Profile picture updated.')
      } else {
        const errBody = await resp.json().catch(() => ({}))
        setAvatarError(errBody.message || 'Failed to upload image.')
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
        onProfileUpdated(updated)
        setShowRemoveConfirm(false)
        showToast('Profile picture removed.')
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
    <div className="vd-profile-view">
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

      <div className="vd-page-header">
        <h1>My Profile</h1>
        <p>View and manage your personal information.</p>
      </div>

      <div className="vd-profile-card">
        <div className="vd-profile-card__accent" />
        <div className="vd-profile-card__body">

          {loading && <LoadingSpinner label="Loading your profile…" fullPage />}

          {loadError && !loading && (
            <div className="vd-form-error">{loadError}</div>
          )}

          {!loading && profile && !editing && (
            <>
              <div className="vd-identity">
                <div className="vd-avatar-wrapper">
                  <div className="vd-avatar">
                    {profile.profilePictureUrl ? (
                      <img src={formatAvatarUrl(profile.profilePictureUrl)} alt="" className="vd-avatar__img" />
                    ) : (
                      initials(profile.firstName, profile.lastName)
                    )}
                  </div>

                  <label className="vd-avatar-action vd-avatar-action--upload" title="Upload Photo">
                    <PhotoCameraIcon size={18} />
                    <input
                      type="file"
                      accept="image/jpeg,image/png,image/webp,image/gif"
                      onChange={handleAvatarUpload}
                      disabled={avatarUploading}
                      className="vd-hidden-input"
                    />
                  </label>
                </div>

                <div className="vd-identity__info">
                  <h2 className="vd-identity__name">
                    {profile.firstName} {profile.lastName}
                  </h2>
                  <div className="vd-identity__role-row">
                    <span className="vd-identity__badge">
                      <BadgeIcon size={13} className="vd-icon-spacing" /> Visitor
                    </span>
                    {profile.profilePictureUrl && (
                      <button
                        className="vd-avatar-remove-btn"
                        onClick={() => setShowRemoveConfirm(true)}
                        disabled={avatarUploading}
                      >
                        <DeleteSweepIcon size={13} className="vd-icon-spacing" /> Remove Photo
                      </button>
                    )}
                  </div>
                  {avatarError && <div className="vd-avatar-error">{avatarError}</div>}
                </div>

                <button className="vd-btn-edit " onClick={handleStartEdit}>
                  <CreateIcon size={14} className="vd-icon-spacing-right" /> Edit Profile
                </button>
              </div>

              <div className="vd-fields-grid">
                <div className="vd-field">
                  <span className="vd-field__label">First Name</span>
                  <span className="vd-field__value">{profile.firstName || '—'}</span>
                </div>
                <div className="vd-field">
                  <span className="vd-field__label">Last Name</span>
                  <span className="vd-field__value">{profile.lastName || '—'}</span>
                </div>
                <div className="vd-field">
                  <span className="vd-field__label">Email Address</span>
                  <span className="vd-field__value">
                    <EmailIcon size={14} className="vd-icon-spacing-right" /> {profile.email}
                  </span>
                </div>
                <div className="vd-field">
                  <span className="vd-field__label">Phone Number</span>
                  <span className="vd-field__value">
                    <LocalPhoneIcon size={14} className="vd-icon-spacing-right" /> {profile.phoneNumber || '—'}
                  </span>
                </div>
                <div className="vd-field">
                  <span className="vd-field__label">Nationality</span>
                  <span className="vd-field__value">
                    <PublicIcon size={14} className="vd-icon-spacing-right" /> {profile.nationality || '—'}
                  </span>
                </div>
                <div className="vd-field">
                  <span className="vd-field__label">Account Created</span>
                  <span className="vd-field__value">
                    <CalendarMonthIcon size={14} className="vd-icon-spacing-right" /> Member since {formatDate(profile.createdAt)}
                  </span>
                </div>
              </div>
            </>
          )}

          {!loading && profile && editing && (
            <form onSubmit={handleSaveProfile} className="vd-edit-form">
              {saveError && <div className="vd-form-error">{saveError}</div>}

              <div className="vd-edit-form__grid">
                <div className="vd-form-group">
                  <label className="vd-form-label">First Name</label>
                  <input
                    type="text"
                    name="firstName"
                    value={formData.firstName}
                    onChange={handleFormChange}
                    className="vd-form-input"
                    required
                  />
                </div>
                <div className="vd-form-group">
                  <label className="vd-form-label">Last Name</label>
                  <input
                    type="text"
                    name="lastName"
                    value={formData.lastName}
                    onChange={handleFormChange}
                    className="vd-form-input"
                    required
                  />
                </div>
                <div className="vd-form-group">
                  <label className="vd-form-label">Phone Number</label>
                  <input
                    type="text"
                    name="phoneNumber"
                    value={formData.phoneNumber}
                    onChange={handleFormChange}
                    className="vd-form-input"
                    placeholder="+94 77 123 4567"
                  />
                </div>
                <div className="vd-form-group">
                  <label className="vd-form-label">Nationality</label>
                  <input
                    type="text"
                    name="nationality"
                    value={formData.nationality}
                    onChange={handleFormChange}
                    className="vd-form-input"
                    placeholder="Sri Lankan, British, etc."
                  />
                </div>
              </div>

              <div className="vd-edit-actions">
                <button
                  type="button"
                  className="vd-btn-cancel"
                  onClick={() => setEditing(false)}
                  disabled={saveLoading}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="vd-btn-save"
                  disabled={saveLoading}
                >
                  {saveLoading ? 'Saving…' : 'Save Changes'}
                </button>
              </div>
            </form>
          )}
        </div>
      </div>
    </div>
  )
}
