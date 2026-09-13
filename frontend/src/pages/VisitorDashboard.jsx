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
  DeleteSweepIcon,
  CloseIcon,
  SearchIcon,
  GroupIcon,
  KitesurfingIcon,
  RestaurantIcon,
  HotelIcon,
  HourglassTopIcon,
  AccessTimeFilledIcon,
  MyLocationIcon,
  LocationOnIcon,
  CheckCircleIcon,
  MoneyIcon,
  RangeIcon,
  HouseIcon,
  DiningIcon,
  CheckIcon,
  DangerIcon
} from '../components/Icons'
import ConfirmModal from '../components/ConfirmModal'
import { apiUrl, catalogUrl } from '../api/client'

function SuccessToast({ message, onClose }) {
  useEffect(() => {
    const t = setTimeout(onClose, 4000)
    return () => clearTimeout(t)
  }, [onClose])

  return (
    <div className="vd-toast" role="alert" aria-live="polite">
      <div className="vd-toast__icon"><CheckCircleIcon/></div>
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

// ── 1. Service Detail Modal ───────────────────────────────────────
function ServiceDetailModal({ item, onClose, onOpenBooking }) {
  if (!item) return null

  const icons = {
    experience:    [HourglassTopIcon, GroupIcon],
    restaurant:    [RestaurantIcon, DiningIcon, GroupIcon],
    accommodation: [HouseIcon, GroupIcon, HotelIcon],
  }[item.type?.toLowerCase()] || []

  return (
    <div className="vd-modal-overlay" onClick={onClose}>
      <div className="vd-detail-modal" onClick={(e) => e.stopPropagation()}>
        <div className="vd-detail-modal__header">
          <button className="vd-detail-modal__close" onClick={onClose} aria-label="Close">
            <CloseIcon size={16} />
          </button>
          <span className={`vd-type-badge vd-type-badge--${item.type.toLowerCase()}`}>
            {item.type}
          </span>
          <h2 className="vd-detail-modal__title">{item.title}</h2>
          <p className="vd-detail-modal__provider">By {item.providerBusinessName}</p>
        </div>

        <div className="vd-detail-modal__body">
          <p className="vd-detail-modal__desc">{item.description}</p>

          <div className="vd-detail-modal__grid">
            {item.keyDetail && item.keyDetail.split('•').map((detail, idx) => {
              const Icon = icons[idx] || GroupIcon
              return (
                <div key={idx} className="vd-detail-modal__item">
                  <Icon size={16} />
                  <div className="vd-detail-modal__item-text">
                    <span className="vd-detail-modal__item-value">{detail.trim()}</span>
                  </div>
                </div>
              )
            })}
            {item.scheduleInfo && (
              <div className="vd-detail-modal__item">
                <AccessTimeFilledIcon size={16} />
                <div className="vd-detail-modal__item-text">
                  <span className="vd-detail-modal__item-label">Schedule</span>
                  <span className="vd-detail-modal__item-value">{item.scheduleInfo}</span>
                </div>
              </div>
            )}
            {item.location && (
              <div className="vd-detail-modal__item">
                <LocationOnIcon size={16} />
                <div className="vd-detail-modal__item-text">
                  <span className="vd-detail-modal__item-label">Location</span>
                  <span className="vd-detail-modal__item-value">{item.location}</span>
                </div>
              </div>
            )}
          </div>

          <div className="vd-detail-modal__price-row">
            <span className="vd-detail-modal__price-label">Price</span>
            <span className="vd-detail-modal__price-value">{item.priceFormatted}</span>
          </div>
        </div>

        <div className="vd-detail-modal__footer">
          <button type="button" className="vd-cancel-btn" onClick={onClose}>
            Close
          </button>
          <button
            type="button"
            className="vd-save-btn"
            onClick={() => {
              onClose()
              onOpenBooking(item)
            }}
          >
            Check Availability and Book
          </button>
        </div>
      </div>
    </div>
  )
}

// ── 2. Availability & Booking Slot Picker Modal (Story 6.1) ────────
function BookingAvailabilityModal({ item, onClose }) {
  const [selectedDate, setSelectedDate] = useState('')
  const [selectedSlot, setSelectedSlot] = useState('')
  const [availability, setAvailability] = useState(null)
  const [loadingSlots, setLoadingSlots] = useState(false)

  // Initialize date to today
  useEffect(() => {
    const today = new Date().toISOString().split('T')[0]
    setSelectedDate(today)
  }, [])

  // Fetch real-time slot capacity whenever date changes
  useEffect(() => {
    if (!item?.id || !selectedDate) return

    let isMounted = true
    const fetchSlots = async () => {
      setLoadingSlots(true)
      try {
        const resp = await fetch(catalogUrl(`/api/catalog/availability/${item.id}?date=${selectedDate}`))
        if (resp.ok && isMounted) {
          const data = await resp.json()
          setAvailability(data)
          if (data.slots?.length > 0) {
            setSelectedSlot(data.slots[0].timeSlot)
          }
        }
      } catch (err) {
        console.error('Failed to fetch availability:', err)
      } finally {
        if (isMounted) setLoadingSlots(false)
      }
    }

    fetchSlots()
    return () => { isMounted = false }
  }, [item?.id, selectedDate])

  const currentSlotObj = availability?.slots?.find(s => s.timeSlot === selectedSlot)

  return (
    <div className="vd-modal-overlay" onClick={onClose}>
      <div className="vd-detail-modal" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 580 }}>
        <div className="vd-detail-modal__header">
          <button className="vd-detail-modal__close" onClick={onClose} aria-label="Close">
            <CloseIcon size={16} />
          </button>
          <span className={`vd-type-badge vd-type-badge--${item.type.toLowerCase()}`}>
            {item.type}
          </span>
          <h2 className="vd-detail-modal__title">Book: {item.title}</h2>
          <p className="vd-detail-modal__provider">By {item.providerBusinessName}</p>
        </div>

        <div className="vd-detail-modal__body" style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          {/* Price & Location Header */}
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: '#f8fafc', padding: '10px 14px', borderRadius: 8, border: '1px solid #e2e8f0' }}>
            <span style={{ fontSize: 13, color: '#475569', display: 'flex', alignItems: 'center', gap: 4 }}>
              <LocationOnIcon size={14} /> {item.location}
            </span>
            <span style={{ fontSize: 16, fontWeight: 800, color: '#0d9488' }}>
              {item.priceFormatted}
            </span>
          </div>

          {/* ── Schedule & Validity Metadata Banner ── */}
          <div style={{ background: '#f8fafc', border: '1px solid #e2e8f0', borderRadius: 8, padding: '12px 14px', display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
            <div>
              <span style={{ fontSize: 10.5, fontWeight: 700, color: '#64748b', textTransform: 'uppercase', letterSpacing: '0.5px', display: 'block' }}>
                Validity Window
              </span>
              <span style={{ fontSize: 13, fontWeight: 600, color: '#1e293b' }}>
                {availability?.validFrom && availability?.validUntil
                  ? `${formatDate(availability.validFrom)} – ${formatDate(availability.validUntil)}`
                  : 'Ongoing / Open Season'}
              </span>
            </div>

            <div>
              <span style={{ fontSize: 10.5, fontWeight: 700, color: '#64748b', textTransform: 'uppercase', letterSpacing: '0.5px', display: 'block' }}>
                Available Operating Days
              </span>
              <span style={{ fontSize: 13, fontWeight: 600, color: '#1e293b' }}>
                {availability?.availableDays || 'Daily'}
              </span>
            </div>

            <div style={{ gridColumn: 'span 2', borderTop: '1px dashed #e2e8f0', paddingTop: 8 }}>
              <span style={{ fontSize: 10.5, fontWeight: 700, color: '#64748b', textTransform: 'uppercase', letterSpacing: '0.5px', display: 'block' }}>
                Scheduled Time Slots
              </span>
              <span style={{ fontSize: 13, fontWeight: 600, color: '#0f172a' }}>
                {availability?.timeSlots || 'All Day / Operating Hours'}
              </span>
            </div>
          </div>

          {/* ── Date & Slot Selectors ── */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <div className="vd-form-group">
              <label style={{ fontSize: 12, fontWeight: 700, color: '#334155', marginBottom: 4, display: 'block' }}>
                Select Date *
              </label>
              <input
                type="date"
                min={availability?.validFrom || new Date().toISOString().split('T')[0]}
                max={availability?.validUntil || undefined}
                value={selectedDate}
                onChange={(e) => setSelectedDate(e.target.value)}
                style={{ width: '100%', padding: '8px 10px', borderRadius: 6, border: '1px solid #cbd5e1' }}
              />
            </div>

            <div className="vd-form-group">
              <label style={{ fontSize: 12, fontWeight: 700, color: '#334155', marginBottom: 4, display: 'block' }}>
                Select Time Slot *
              </label>
              <select
                value={selectedSlot}
                onChange={(e) => setSelectedSlot(e.target.value)}
                disabled={loadingSlots || !availability?.slots?.length}
                style={{ width: '100%', padding: '8px 10px', borderRadius: 6, border: '1px solid #cbd5e1' }}
              >
                {availability?.slots?.map((s, idx) => (
                  <option key={idx} value={s.timeSlot}>
                    {s.timeSlot} ({s.remainingCapacity} left)
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* ── Real-time Slot Availability Display ── */}
          <div style={{ background: '#f0fdf4', border: '1px solid #bbf7d0', borderRadius: 8, padding: 12 }}>
            {loadingSlots ? (
              <span style={{ fontSize: 13, color: '#64748b' }}>Checking real-time capacity…</span>
            ) : !availability?.isOperatingDay ? (
              <div style={{ color: '#dc2626', fontWeight: 600, fontSize: 13 }}>
                <DangerIcon/> The provider does not operate on this selected day of the week.
              </div>
            ) : currentSlotObj?.isFullyBooked ? (
              <div style={{ color: '#dc2626', fontWeight: 700, fontSize: 13 }}>
                ● Fully Booked (0 spots left for this time slot)
              </div>
            ) : (
              <div style={{ color: '#166534', fontWeight: 600, fontSize: 13 }}>
                <><CheckIcon/> <strong>{currentSlotObj?.remainingCapacity}</strong> of {currentSlotObj?.totalCapacity} spots available</>
              </div>
            )}
          </div>
        </div>

        <div className="vd-detail-modal__footer">
          <button type="button" className="vd-cancel-btn" onClick={onClose}>
            Cancel
          </button>
          <button
            type="button"
            className="vd-save-btn"
            disabled={!availability?.isOperatingDay || currentSlotObj?.isFullyBooked}
            style={(!availability?.isOperatingDay || currentSlotObj?.isFullyBooked) ? { opacity: 0.5, cursor: 'not-allowed' } : {}}
            onClick={() => alert(`Selection confirmed for ${item.title} on ${selectedDate} (${selectedSlot}). Booking service will process this in next sprint!`)}
          >
            {currentSlotObj?.isFullyBooked ? 'Fully Booked' : 'Confirm Selection'}
          </button>
        </div>
      </div>
    </div>
  )
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
            <button
              className={activePage === 'explore' ? 'active' : ''}
              onClick={() => setActivePage('explore')}
              id="nav-explore"
            >
              <span className="vd-nav-icon"><PublicIcon size={18} /></span> Explore and Search
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

      {/* ── Main Content ── */}
      <main className="vd-main">
        {activePage === 'explore' ? (
          <ExploreTab />
        ) : (
          <>
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
          </>
        )}
      </main>
    </div>
  )
}

// ── 3. Explore & Search Tab ───────────────────────────────────────
function ExploreTab() {
  const [keywordInput, setKeywordInput] = useState('')
  const [locationInput, setLocationInput] = useState('')
  const [minPriceInput, setMinPriceInput] = useState('')
  const [maxPriceInput, setMaxPriceInput] = useState('')
  const [sortInput, setSortInput] = useState('') 

  const [detailItem, setDetailItem] = useState(null)
  const [bookingItem, setBookingItem] = useState(null)
  
  const [appliedFilters, setAppliedFilters] = useState({
    q: '',
    type: 'all',
    location: '',
    minPrice: '',
    maxPrice: '',
    sort: '',
    page: 1
  })
  
  const [data, setData] = useState({ items: [], totalCount: 0, totalPages: 1, page: 1, hasPreviousPage: false, hasNextPage: false })
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const timer = setTimeout(() => {
      setAppliedFilters(prev => ({
        ...prev,
        q: keywordInput.trim(),
        location: locationInput,
        minPrice: minPriceInput,
        maxPrice: maxPriceInput,
        sort: sortInput,
        page: 1
      }))
    }, 350)

    return () => clearTimeout(timer)
  }, [keywordInput, locationInput, minPriceInput, maxPriceInput, sortInput])

  useEffect(() => {
    let isMounted = true
    const fetchResults = async () => {
      setLoading(true)
      try {
        const params = new URLSearchParams()

        if (appliedFilters.q) params.append('q', appliedFilters.q)
        if (appliedFilters.type && appliedFilters.type !== 'all') params.append('type', appliedFilters.type)
        if (appliedFilters.location) params.append('location', appliedFilters.location)
        if (appliedFilters.minPrice) params.append('minPrice', appliedFilters.minPrice)
        if (appliedFilters.maxPrice) params.append('maxPrice', appliedFilters.maxPrice)
        if (appliedFilters.sort) params.append('sort', appliedFilters.sort)

        params.append('page', appliedFilters.page)
        params.append('pageSize', 9)
        const resp = await fetch(catalogUrl(`/api/catalog/search?${params.toString()}`))
        if (resp.ok && isMounted) {
          const result = await resp.json()
          setData(result)
        }
      } catch (err) {
        console.error('Failed to search listings:', err)
      } finally {
        if (isMounted) setLoading(false)
      }
    }
    fetchResults()
    return () => { isMounted = false }
  }, [appliedFilters])

  const handleTypeChange = (type) => {
    setAppliedFilters(prev => ({ ...prev, type, page: 1 }))
  }
  const handlePricePreset = (min, max) => {
    setMinPriceInput(min ? String(min) : '')
    setMaxPriceInput(max ? String(max) : '')
  }
  const handleLocationPreset = (loc) => {
    setLocationInput(loc)
  }

  const clearAllFilters = () => {
    setKeywordInput('')
    setLocationInput('')
    setMinPriceInput('')
    setMaxPriceInput('')
    setSortInput('')
    setAppliedFilters({
      q: '',
      type: 'all',
      location: '',
      minPrice: '',
      maxPrice: '',
      sort: '',
      page: 1
    })
  }

  const hasActiveFilters = appliedFilters.q || (appliedFilters.type && appliedFilters.type !== 'all') || appliedFilters.location || appliedFilters.minPrice || appliedFilters.maxPrice || appliedFilters.sort

  const handleSearchSubmit = (e) => {
    e.preventDefault()
    setAppliedFilters(prev => ({
      ...prev,
      q: keywordInput.trim(),
      page: 1
    }))
  }

  const handleClear = () => {
    setKeywordInput('')
    setAppliedFilters({
      q: '',
      page: 1
    })
  }

  const KEY_DETAIL_ICONS = {
    experience:    [HourglassTopIcon, GroupIcon],
    restaurant:    [RestaurantIcon, DiningIcon, GroupIcon],   
    accommodation: [HouseIcon, GroupIcon, HotelIcon],        
  }

  return (
    <div className="vd-explore">
      <div className="vd-page-header">
        <div className="vd-page-header__left">
          <h1>Explore Sri Lanka</h1>
          <p>Discover verified tours, authentic dining experiences, and island stays.</p>
        </div>
      </div>

      {/* ── Search Bar ── */}
      <form onSubmit={handleSearchSubmit} className="vd-search-hero">
        <div className="vd-search-input-wrap">
          <input
            type="text"
            className="vd-search-input vd-search-input--single"
            placeholder="Search experiences, restaurants, or locations (e.g. diving, Kandy, seafood)..."
            value={keywordInput}
            onChange={(e) => setKeywordInput(e.target.value)}
            aria-label="Search listings"
          />
          {keywordInput && (
            <>
            <button
              type="button"
              className="vd-search-clear"
              onClick={handleClear}
              aria-label="Clear search"
              title="Clear search"
            >
              <CloseIcon size={25}/>
            </button>

            <button
              type="button"
              className="vd-search-btn"
              onClick={handleSearchSubmit}
              aria-label="Search"
              title="Search"
            >
              <SearchIcon size={25}/>
            </button>
            </>
          )}
          {!keywordInput && (
            <button
              type="button"
              className="vd-search-btn"
              onClick={handleSearchSubmit}
              aria-label="Search"
              title="Search"
            >
              <SearchIcon/>
            </button>
          )}
        </div>
      </form>

      <div className="vd-filter-panel">
        {/* Category Pills */}
        <div className="vd-filter-section">
          <label className="vd-filter-label">Category</label>
          <div className="vd-type-pills">
            {[
              { key: 'all', label: 'All Listings' },
              { key: 'experience', label: <><KitesurfingIcon size={14} /> Experiences</> },
              { key: 'restaurant', label: <><RestaurantIcon size={14} /> Restaurant</> },
              { key: 'accommodation', label: <><HotelIcon size={14} /> Accommodation</> }
            ].map(t => (
              <button
                key={t.key}
                type="button"
                className={`vd-type-pill ${appliedFilters.type === t.key ? 'active' : ''}`}
                onClick={() => handleTypeChange(t.key)}
              >
                {t.label}
              </button>
            ))}
          </div>
        </div>

        <div className="vd-filter-section">
          <label className="vd-filter-label">Location</label>
          <div className="vd-location-pills">
            {['', 'Colombo', 'Kandy', 'Galle', 'Trincomalee', 'Mirissa', 'Ella'].map((loc) => (
              <button
                key={loc}
                type="button"
                className={`vd-loc-pill ${locationInput === loc ? 'active' : ''}`}
                onClick={() => handleLocationPreset(loc)}
              >
                {loc === '' ? <> <LocationOnIcon size={14}/>All Locations</> : loc}
              </button>
            ))}
          </div>
        </div>

        <div className="vd-filter-section vd-filter-section--price">
          <label className="vd-filter-label">Price Range (LKR) and Sort</label>
          <div className="vd-price-controls">
            <select className="vd-sort-select" value={sortInput} onChange={(e) => setSortInput(e.target.value)}>
              <option value="">Sort: Default</option>
              <option value="price_asc">Price: Low to High</option>
              <option value="price_desc">Price: High to Low</option>
            </select>

            <div className="vd-price-input-group">
              <input
                type="number"
                min="0"
                placeholder="Min LKR"
                className="vd-price-input"
                value={minPriceInput}
                onChange={(e) => setMinPriceInput(e.target.value)}
              />
              <span className="vd-price-separator">–</span>
              <input
                type="number"
                min="0"
                placeholder="Max LKR"
                className="vd-price-input"
                value={maxPriceInput}
                onChange={(e) => setMaxPriceInput(e.target.value)}
              />
            </div>

            <div className="vd-price-presets">
              <button type="button" className="vd-preset-btn" onClick={() => handlePricePreset(0, 5000)}>Under 5k</button>
              <button type="button" className="vd-preset-btn" onClick={() => handlePricePreset(5000, 15000)}>5k – 15k</button>
              <button type="button" className="vd-preset-btn" onClick={() => handlePricePreset(15000, '')}>15k+</button>
            </div>
          </div>
        </div>
      </div>

      <div className="vd-active-filter-bar">
        <div className="vd-active-tags">
          {appliedFilters.q && (
            <span className="vd-filter-tag">
              <> <SearchIcon/>&quot;{appliedFilters.q}&quot;</> <button type="button" onClick={() => setKeywordInput('')}><CloseIcon/></button>
            </span>
          )}
          {appliedFilters.type && appliedFilters.type !== 'all' && (
            <span className="vd-filter-tag">
               {appliedFilters.type} <button type="button" onClick={() => handleTypeChange('all')}><CloseIcon/></button>
            </span>
          )}
          {appliedFilters.location && (
            <span className="vd-filter-tag">
              <><LocationOnIcon/> {appliedFilters.location} </><button type="button" onClick={() => setLocationInput('')}><CloseIcon/></button>
            </span>
          )}
          {(appliedFilters.minPrice || appliedFilters.maxPrice) && (
            <span className="vd-filter-tag">
              <><MoneyIcon/> LKR {appliedFilters.minPrice || '0'} – {appliedFilters.maxPrice || 'Any'}</>
              <button type="button" onClick={() => { setMinPriceInput(''); setMaxPriceInput('') }}><CloseIcon/></button>
            </span>
          )}
          {appliedFilters.sort && (
            <span className="vd-filter-tag">
              <><RangeIcon/> {appliedFilters.sort === 'price_asc' ? 'Price: Low to High' : 'Price: High to Low'}</>
              <button type="button" onClick={() => setSortInput('')}><CloseIcon/></button>
            </span>
          )}
          {hasActiveFilters && (
            <button type="button" className="vd-clear-all-btn" onClick={clearAllFilters}>
              Clear All Filters
            </button>
          )}
        </div>
      </div>

      {/* Results Count Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '6px' }}>
        <span className="vd-results-count">
          {data.totalCount > 0 ? `Showing ${data.items.length} of ${data.totalCount} listings` : ''}
        </span>
      </div>

      {/* ── Listings Grid ── */}
      {loading ? (
        <div className="vd-loading-card">
          <div className="vd-spinner" />
          <span>Searching verified listings across Sri Lanka...</span>
        </div>
      ) : data.items.length === 0 ? (
        <div className="vd-empty-search">
          <div className="vd-empty-icon"><SearchIcon size={40} /></div>
          <h3>No listings matched your search</h3>
          <p>
            {appliedFilters.q
              ? `We couldn't find any listings matching "${appliedFilters.q}".`
              : "No active listings are currently available."}
          </p>
          <button type="button" className="vd-clear-btn" onClick={handleClear}>
            Clear Search & Browse All
          </button>
        </div>
      ) : (
        <div className="vd-grid">
          {data.items.map((item) => (
            <div key={item.id} className="vd-service-card">
              <div className="vd-service-card__header">
                <span className={`vd-type-badge vd-type-badge--${item.type.toLowerCase()}`}>
                  {item.type}
                </span>
                <span className="vd-service-card__provider">By {item.providerBusinessName}</span>
              </div>
              
              <div className="vd-service-card__body">
                <h3 className="vd-service-card__title">{item.title}</h3>
                <p className="vd-service-card__desc">
                  {item.description?.length > 110 ? `${item.description.slice(0, 160)}…` : item.description}
                </p>
                <div className="vd-service-card__details">
                  <div className="vd-card__details">
                    {item.keyDetail && (() => {
                      const icons = KEY_DETAIL_ICONS[item.type?.toLowerCase()] || []
                      return item.keyDetail.split('•').map((detail, idx) => {
                        const Icon = icons[idx] || GroupIcon
                        return (
                          <div key={idx} className="vd-service-card__detail-item">
                            <Icon size={14} /> {detail.trim()}
                          </div>
                        )
                      })
                    })()}
                  </div>
                  {item.scheduleInfo && (
                    <div className="vd-service-card__detail-item">
                      <AccessTimeFilledIcon size={14} /> {item.scheduleInfo}
                    </div>
                  )}
                  {item.location && (
                    <div className="vd-service-card__detail-item">
                      <LocationOnIcon size={14} /> {item.location}
                    </div>
                  )}
                </div>
              </div>

              <div className="vd-service-card__footer">
                <div className="vd-service-card__price">{item.priceFormatted}</div>
                <div className="vd-service-card__actions">
                  <button
                    type="button"
                    className="vd-view-btn"
                    onClick={() => setDetailItem(item)}
                  >
                    View Details
                  </button>
                  <button
                    type="button"
                    className="vd-book-btn"
                    onClick={() => setBookingItem(item)}
                  >
                    Book Now
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* ── Pagination Controls ── */}
      {data.totalPages > 1 && (() => {
        const goToPage = (p) => {
          setAppliedFilters(prev => {
            const next = Math.min(Math.max(1, p), data.totalPages)
            if (next === prev.page) return prev
            return { ...prev, page: next }
          })
        }

        const pageNumbers = getPageNumbers(data.page, data.totalPages)

        return (
          <div className="vd-pagination">
            <button
              type="button"
              className="vd-page-btn"
              disabled={!data.hasPreviousPage}
              onClick={() => goToPage(data.page - 1)}
              aria-label="Previous page"
            >
              ← Previous
            </button>

            <div className="vd-page-numbers">
              {pageNumbers.map((p, idx) => {
                if (p === 'left-ellipsis' || p === 'right-ellipsis') {
                  return (
                    <span key={`${p}-${idx}`} className="vd-page-ellipsis" aria-hidden="true">
                      …
                    </span>
                  )
                }

                const isActive = p === data.page
                return (
                  <button
                    key={p}
                    type="button"
                    className={`vd-page-num ${isActive ? 'active' : ''}`}
                    onClick={() => goToPage(p)}
                    disabled={isActive}
                    aria-label={`Go to page ${p}`}
                    aria-current={isActive ? 'page' : undefined}
                  >
                    {p}
                  </button>
                )
              })}
            </div>

            <button
              type="button"
              className="vd-page-btn"
              disabled={!data.hasNextPage}
              onClick={() => goToPage(data.page + 1)}
              aria-label="Next page"
            >
              Next →
            </button>
          </div>
        )
      })()}

      {/* ── Modals ── */}
      {detailItem && (
        <ServiceDetailModal
          item={detailItem}
          onClose={() => setDetailItem(null)}
          onOpenBooking={(item) => setBookingItem(item)}
        />
      )}

      {bookingItem && (
        <BookingAvailabilityModal
          item={bookingItem}
          onClose={() => setBookingItem(null)}
        />
      )}
    </div>
  )
}

function getPageNumbers(current, total, maxVisible = 5) {
  const pages = []

  if (total <= maxVisible + 2) {
    for (let i = 1; i <= total; i++) pages.push(i)
    return pages
  }

  pages.push(1)

  let start = Math.max(2, current - 1)
  let end   = Math.min(total - 1, current + 1)

  if (current <= 3) {
    start = 2
    end   = maxVisible - 1
  } else if (current >= total - 2) {
    start = total - (maxVisible - 2)
    end   = total - 1
  }

  if (start > 2) pages.push('left-ellipsis')
  for (let i = start; i <= end; i++) pages.push(i)
  if (end < total - 1) pages.push('right-ellipsis')

  pages.push(total)
  return pages
}

export default VisitorDashboard