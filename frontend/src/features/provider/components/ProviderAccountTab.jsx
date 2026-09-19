import React, { useState, useEffect } from 'react'
import {
  PermIdentityIcon,
  EmailIcon,
  LocalPhoneIcon,
  PhotoCameraIcon,
  CheckCircleIcon,
  CreateIcon
} from '../../../components/Icons'
import { apiUrl } from '../../../api/client'
import LoadingSpinner from '../../../components/common/LoadingSpinner'

function initials(first, last) {
  return `${(first || '').charAt(0)}${(last || '').charAt(0)}`.toUpperCase() || 'P'
}

function formatAvatarUrl(url) {
  if (!url) return null
  if (url.startsWith('/uploads/avatars/')) {
    const fileName = url.split('/').pop()
    return apiUrl(`/api/users/avatar/${fileName}`)
  }
  return url
}

export default function ProviderAccountTab({ token, onLogout, showToast, onProfileUpdate }) {
  const [profile, setProfile] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  
  // Password change form
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [pwLoading, setPwLoading] = useState(false)
  const [pwError, setPwError] = useState(null)
  const [pwSuccess, setPwSuccess] = useState(false)

  useEffect(() => {
    const loadProfile = async () => {
      if (!token) return
      setLoading(true)
      try {
        const resp = await fetch(apiUrl('/api/users/me'), {
          headers: { Authorization: `Bearer ${token}` }
        })
        if (resp.ok) {
          const data = await resp.json()
          setProfile(data)
          onProfileUpdate && onProfileUpdate(data)
        }
      } catch {
        setError('Failed to load profile data.')
      } finally {
        setLoading(false)
      }
    }
    loadProfile()
  }, [token, onProfileUpdate])

  const handleChangePassword = async (e) => {
    e.preventDefault()
    setPwError(null)
    setPwSuccess(false)

    if (newPassword !== confirmPassword) {
      setPwError('New passwords do not match.')
      return
    }

    setPwLoading(true)
    try {
      const resp = await fetch(apiUrl('/api/users/change-password'), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({ currentPassword, newPassword })
      })

      if (resp.ok) {
        setPwSuccess(true)
        setCurrentPassword('')
        setNewPassword('')
        setConfirmPassword('')
        showToast('Password changed successfully.')
      } else {
        const err = await resp.json().catch(() => ({}))
        setPwError(err.message || 'Failed to change password.')
      }
    } catch {
      setPwError('Network error. Unable to change password.')
    } finally {
      setPwLoading(false)
    }
  }

  if (loading) return <LoadingSpinner label="Loading account details…" fullPage />

  return (
    <div className="pd-account-view">
      <div className="pd-page-header">
        <div className="pd-page-header__left">
          <h1>Account & Security</h1>
          <p>Manage your login credentials and personal profile.</p>
        </div>
      </div>

      <div className="pd-account-grid">
        <div className="pd-card">
          <div className="pd-card__header">
            <h3>Personal Information</h3>
          </div>
          <div className="pd-card__body">
            {profile && (
              <div className="pd-account-profile-summary">
                <div className="pd-avatar-large">
                  {profile.profilePictureUrl ? (
                    <img src={formatAvatarUrl(profile.profilePictureUrl)} alt="" className="pd-avatar-img" />
                  ) : (
                    initials(profile.firstName, profile.lastName)
                  )}
                </div>
                <div className="pd-account-info-list">
                  <div>
                    <span className="pd-info-label">Full Name</span>
                    <span className="pd-info-val">{profile.firstName} {profile.lastName}</span>
                  </div>
                  <div>
                    <span className="pd-info-label">Email</span>
                    <span className="pd-info-val">{profile.email}</span>
                  </div>
                  <div>
                    <span className="pd-info-label">Phone</span>
                    <span className="pd-info-val">{profile.phoneNumber || '—'}</span>
                  </div>
                  <div>
                    <span className="pd-info-label">Role</span>
                    <span className="pd-info-val">Service Provider</span>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="pd-card">
          <div className="pd-card__header">
            <h3>Change Password</h3>
          </div>
          <div className="pd-card__body">
            {pwSuccess && <div className="pd-alert pd-alert--success">Password updated successfully!</div>}
            {pwError && <div className="pd-alert pd-alert--danger">{pwError}</div>}

            <form onSubmit={handleChangePassword} className="pd-form">
              <div className="pd-form-group">
                <label className="pd-form-label">Current Password</label>
                <input
                  type="password"
                  className="pd-form-input"
                  value={currentPassword}
                  onChange={(e) => setCurrentPassword(e.target.value)}
                  required
                />
              </div>
              <div className="pd-form-group">
                <label className="pd-form-label">New Password</label>
                <input
                  type="password"
                  className="pd-form-input"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  required
                />
              </div>
              <div className="pd-form-group">
                <label className="pd-form-label">Confirm New Password</label>
                <input
                  type="password"
                  className="pd-form-input"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  required
                />
              </div>
              <button
                type="submit"
                className="pd-btn-primary"
                disabled={pwLoading}
              >
                {pwLoading ? 'Updating…' : 'Update Password'}
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  )
}
