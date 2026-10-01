import React, { useState } from 'react'
import {
  WorkIcon,
  ManageSearchIcon,
  CheckCircleIcon,
  CancelIcon
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

function Modal({ title, onClose, wide = false, children }) {
  useEffect(() => {
    const handler = (e) => { if (e.key === 'Escape') onClose() }
    document.addEventListener('keydown', handler)
    return () => document.removeEventListener('keydown', handler)
  }, [onClose])

  return (
    <div className="ad-modal-overlay" onClick={(e) => { if (e.target === e.currentTarget) onClose() }}>
      <div className={`ad-modal ${wide ? 'ad-modal--wide' : ''}`} role="dialog" aria-modal="true">
        <div className="ad-modal__header">
          <h2 className="ad-modal__title">{title}</h2>
          <button className="ad-modal__close" onClick={onClose} aria-label="Close modal"></button>
        </div>
        <div className="ad-modal__body">{children}</div>
      </div>
    </div>
  )
}


export default function ProviderManagementTab({ token, onLogout, users = [], applications = [], onRefresh, showToast }) {
  const [search, setSearch] = useState('')
  const [selectedProvider, setSelectedProvider] = useState(null)
  const [actionLoading, setActionLoading] = useState(false)

  const appList = Array.isArray(applications) ? applications : []
  const userList = Array.isArray(users) ? users : []

  // Match provider users with their application details if available
  const providers = userList
    .filter(u => (u.role || '').toLowerCase() === 'provider')
    .map(p => {
      const app = appList.find(a => (a.email || '').toLowerCase() === (p.email || '').toLowerCase())
      const name = `${p.firstName || ''} ${p.lastName || ''}`.trim()
      return {
        ...p,
        businessName: app?.businessName || (name ? `${name} Services` : 'Tourism Service Provider'),
        serviceType: app?.serviceType || 'Tourism Services',
        location: app?.location || p.nationality || 'Sri Lanka',
        description: app?.description || 'Verified Tourism Service Provider'
      }
    })

  const [confirmProviderAction, setConfirmProviderAction] = useState(null) // { provider, newStatus }

  const handleToggleStatus = (provider) => {
    const newStatus = !provider.isActive
    setConfirmProviderAction({ provider, newStatus })
  }

  const executeToggleProviderStatus = async () => {
    if (!confirmProviderAction) return
    const { provider, newStatus } = confirmProviderAction
    setActionLoading(true)
    try {
      const resp = await fetch(apiUrl(`/api/admin/users/${provider.id}/status`), {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify({ isActive: newStatus })
      })
      if (resp.ok) {
        showToast(`Provider ${provider.businessName} is now ${newStatus ? 'Active' : 'Suspended'}.`)
        if (selectedProvider && selectedProvider.id === provider.id) {
          setSelectedProvider(prev => ({ ...prev, isActive: newStatus }))
        }
        setConfirmProviderAction(null)
        onRefresh && onRefresh()
      } else if (resp.status === 401) {
        onLogout && onLogout()
      } else {
        showToast('Failed to update provider status.')
      }
    } catch {
      showToast('Network error. Please check connection.')
    } finally {
      setActionLoading(false)
    }
  }

  const filtered = providers.filter(p => {
    const q = (search || '').toLowerCase().trim()
    if (!q) return true
    return (
      (p.businessName || '').toLowerCase().includes(q) ||
      (p.firstName || '').toLowerCase().includes(q) ||
      (p.lastName || '').toLowerCase().includes(q) ||
      (p.email || '').toLowerCase().includes(q) ||
      (p.location || '').toLowerCase().includes(q) ||
      (p.serviceType || '').toLowerCase().includes(q)
    )
  })

  return (
    <div className="ad-providers-tab">
      <ConfirmModal
        isOpen={Boolean(confirmProviderAction)}
        title={confirmProviderAction?.newStatus ? 'Re-activate Provider' : 'Suspend Provider'}
        message={`Are you sure you want to ${confirmProviderAction?.newStatus ? 're-activate' : 'suspend'} provider ${confirmProviderAction?.provider?.businessName}?`}
        confirmText={confirmProviderAction?.newStatus ? 'Re-activate' : 'Suspend Provider'}
        cancelText="Cancel"
        confirmVariant={confirmProviderAction?.newStatus ? 'primary' : 'danger'}
        onConfirm={executeToggleProviderStatus}
        onCancel={() => setConfirmProviderAction(null)}
        loading={actionLoading}
      />
      <div className="ad-page-header">
        <div className="ad-page-header__left">
          <h1>Provider Management</h1>
          <p>Monitor certified tourism operators, inspect service profiles, and manage account statuses.</p>
        </div>
      </div>

      {/* Provider Details Modal */}
      {selectedProvider && (
        <Modal title={`Provider Profile: ${selectedProvider.businessName}`} onClose={() => setSelectedProvider(null)} wide>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
            <div className="ad-identity" style={{ marginBottom: 0, paddingBottom: '16px' }}>
              <div className="ad-avatar">{initials(selectedProvider.firstName, selectedProvider.lastName)}</div>
              <div className="ad-identity__info">
                <h3 className="ad-identity__name">{selectedProvider.businessName}</h3>
                <p className="ad-identity__email">{selectedProvider.email}</p>
                <div style={{ display: 'flex', gap: '8px', marginTop: '6px' }}>
                  <span className="ad-badge ad-badge--provider">Certified Partner</span>
                  <span className={`ad-badge ad-badge--${selectedProvider.isActive ? 'active' : 'inactive'}`}>
                    {selectedProvider.isActive ? 'Active' : 'Suspended'}
                  </span>
                </div>
              </div>
            </div>

            <div className="ad-fields">
              <div className="ad-field">
                <span className="ad-field__label">Contact Person</span>
                <span className="ad-field__value">{selectedProvider.firstName} {selectedProvider.lastName}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Service Category</span>
                <span className="ad-field__value">{selectedProvider.serviceType}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Operating Location</span>
                <span className="ad-field__value"> {selectedProvider.location}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Phone Number</span>
                <span className="ad-field__value"> {selectedProvider.phoneNumber || '—'}</span>
              </div>
              <div className="ad-field pd-field--full">
                <span className="ad-field__label">Business Description</span>
                <span className="ad-field__value" style={{ background: '#faf8f3', padding: '12px 14px', borderRadius: '8px', border: '1px solid #ede8dc' }}>
                  {selectedProvider.description || 'No description provided.'}
                </span>
              </div>
            </div>

            <div style={{ paddingTop: '14px', borderTop: '1px solid #f0ece3', display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
              <button
                className={`ad-quick-btn ${selectedProvider.isActive ? 'ad-quick-btn--danger' : 'ad-quick-btn--success'}`}
                onClick={() => handleToggleStatus(selectedProvider)}
                disabled={actionLoading}
              >
                {selectedProvider.isActive ? 'Suspend Provider Account' : 'Reactivate Provider Account'}
              </button>
            </div>
          </div>
        </Modal>
      )}

      {/* Filter and Search Bar */}
      <div className="ad-filter-bar">
        <div className="ad-search-wrap">
          <span className="ad-search-icon"><ManageSearchIcon size={18} /></span>
          <input
            type="text"
            className="ad-search-input"
            placeholder="Search by business, contact, location..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
      </div>

      {/* Providers Table */}
      <div className="ad-card">
        <div className="ad-card__body" style={{ padding: 0 }}>
          {filtered.length === 0 ? (
            <div className="ad-empty">
              <div className="ad-empty__icon"><WorkIcon size={32} /></div>
              <p className="ad-empty__title">No approved providers</p>
              <p className="ad-empty__msg">Approved provider accounts will appear here.</p>
            </div>
          ) : (
            <div className="ad-table-wrap" style={{ border: 'none', borderRadius: 0 }}>
              <table className="ad-table">
                <thead>
                  <tr>
                    <th>Business Name</th>
                    <th>Contact Person</th>
                    <th>Category</th>
                    <th>Location</th>
                    <th>Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(provider => (
                    <tr key={provider.id}>
                      <td style={{ fontWeight: 700, color: '#123b5d' }}>{provider.businessName}</td>
                      <td>
                        <div>{provider.firstName} {provider.lastName}</div>
                        <div style={{ fontSize: '11.5px', color: '#888' }}>{provider.email}</div>
                      </td>
                      <td>{provider.serviceType}</td>
                      <td>{provider.location}</td>
                      <td>
                        <span className={`ad-badge ad-badge--${provider.isActive ? 'active' : 'inactive'}`}>
                          {provider.isActive ? 'Active' : 'Suspended'}
                        </span>
                      </td>
                      <td>
                        <div className="ad-row-actions">
                          <button className="ad-row-btn ad-row-btn--view" onClick={() => setSelectedProvider(provider)}>
                            Details
                          </button>
                          <button
                            className="ad-row-btn ad-row-btn--toggle"
                            onClick={() => handleToggleStatus(provider)}
                          >
                            {provider.isActive ? 'Suspend' : 'Activate'}
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

// ── 5. Bookings Overview Tab ──────────────────────────────────────────────────
