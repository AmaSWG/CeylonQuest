import React, { useState, useEffect } from 'react'
import {
  StorefrontIcon,
  CreateIcon,
  MyLocationIcon,
  CheckCircleIcon
} from '../../../components/Icons'
import { catalogUrl } from '../../../api/client'

export default function BusinessProfileTab({ token, onLogout, providerInfo, onUpdateSuccess, showToast }) {
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

// ── 3. Listing Management Tab (Activity / Restaurant / Accommodation) ─────────
