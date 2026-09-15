import React, { useState, useEffect } from 'react'
import {
  DocumentScannerIcon,
  CheckCircleIcon,
  CancelIcon,
  ManageSearchIcon,
  CalendarMonthIcon
} from '../../../components/Icons'
import ConfirmModal from '../../../components/ConfirmModal'
import { apiUrl, catalogUrl } from '../../../api/client'

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


export default function ProviderApplicationsTab({ token, onLogout, applications = [], onRefresh, showToast }) {
  const [search, setSearch] = useState('')
  const [filterStatus, setFilterStatus] = useState('all') // all | pending | approved | rejected
  const [selectedApp, setSelectedApp] = useState(null)
  const [downloadingId, setDownloadingId] = useState(null)

  const [confirmApproveApp, setConfirmApproveApp] = useState(null)
  const [confirmRejectApp, setConfirmRejectApp] = useState(null)
  const [rejectionReason, setRejectionReason] = useState('')
  const [customRejectionReason, setCustomRejectionReason] = useState('') // Added missing state
  const [rejectionError, setRejectionError] = useState('')
  const [actionLoading, setActionLoading] = useState(false)

  const commonRejectionReasons = [
    'Business does not meet certification requirements',
    'Invalid or unverifiable business registration',
    'Service category does not match provided documentation',
    'Location verification failed',
    'Negative reviews or complaints from previous customers',
    'Applicant did not respond to verification requests',
    'Business is not operational in the specified location',
    'Insufficient insurance or liability coverage'
  ]

    const handleDownloadDocument = async (app, index = 0, customFileName = null) => {
    if (!app) return
    const downloadKey = `${app.id}_${index}`
    setDownloadingId(downloadKey)
    try {
      const resp = await fetch(catalogUrl(`/api/catalog/admin/providers/${app.id}/document?index=${index}`), {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        const blob = await resp.blob()
        const url = window.URL.createObjectURL(blob)
        const a = document.createElement('a')
        a.href = url
        let fileName = customFileName || app.legalDocumentFileName || `${(app.businessName || 'application').replace(/\s+/g, '_')}_document.pdf`
        const disposition = resp.headers.get('Content-Disposition')
        if (disposition && disposition.includes('filename=')) {
          const match = disposition.match(/filename="?([^";]+)"?/)
          if (match && match[1]) fileName = match[1]
        }
        a.download = fileName
        document.body.appendChild(a)
        a.click()
        a.remove()
        window.URL.revokeObjectURL(url)
        showToast && showToast(`Downloaded: ${fileName}`)
      } else {
        showToast && showToast('Unable to download document file.')
      }
    } catch {
      showToast && showToast('Download failed. Please check network connection.')
    } finally {
      setDownloadingId(null)
    }
  }
  // Approve Application
  const executeApprove = async () => {
    if (!confirmApproveApp) return
    setActionLoading(true)
    try {
      const resp = await fetch(catalogUrl(`/api/catalog/admin/providers/${confirmApproveApp.id}/approve`), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        }
      })
      if (resp.ok) {
        showToast('Approved — provider will receive OTP')
        setSelectedApp(null)
        setConfirmApproveApp(null)
        onRefresh && onRefresh()
      } else if (resp.status === 401) {
        onLogout && onLogout()
      } else {
        showToast('Error approving application.')
      }
    } catch {
      showToast('Error approving application.')
    } finally {
      setActionLoading(false)
    }
  }

  // Reject Application
  const executeReject = async () => {
    if (!confirmRejectApp) return

    let finalReason = ''
    if (rejectionReason && rejectionReason !== 'other') {
      finalReason = rejectionReason
      if (customRejectionReason.trim()) {
        finalReason += ` — ${customRejectionReason.trim()}`
      }
    } else if (customRejectionReason.trim()) {
      finalReason = customRejectionReason.trim()
    }

    if (!finalReason) {
      setRejectionError('Please select a reason or provide detailed feedback.')
      return
    }

    setActionLoading(true)
    setRejectionError('')

    try {
      const resp = await fetch(catalogUrl(`/api/catalog/admin/providers/${confirmRejectApp.id}/reject`), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({ rejectionReason: finalReason })
      })
      if (resp.ok) {
        showToast('Application rejected.')
        setSelectedApp(null)
        setConfirmRejectApp(null)
        setRejectionReason('')
        setCustomRejectionReason('')
        onRefresh && onRefresh()
      } else if (resp.status === 401) {
        onLogout && onLogout()
      } else {
        showToast('Error rejecting application.')
      }
    } catch {
      showToast('Error rejecting application.')
    } finally {
      setActionLoading(false)
    }
  }

  const list = Array.isArray(applications) ? applications : []
  const filtered = list.filter(a => {
    if (!a) return false
    const q = (search || '').toLowerCase().trim()
    const matchSearch =
      !q ||
      (a.businessName || '').toLowerCase().includes(q) ||
      (a.firstName || '').toLowerCase().includes(q) ||
      (a.lastName || '').toLowerCase().includes(q) ||
      (a.email || '').toLowerCase().includes(q) ||
      (a.serviceType || '').toLowerCase().includes(q) ||
      (a.location || '').toLowerCase().includes(q)
    if (!matchSearch) return false
    if (filterStatus !== 'all' && (a.status || '').toLowerCase() !== filterStatus.toLowerCase()) return false
    return true
  })

  return (
    <div className="ad-applications-tab">
      <div className="ad-page-header">
        <div className="ad-page-header__left">
          <h1>Provider Applications</h1>
          <p>Review submitted tourism provider applications, business information, and download legal documents.</p>
        </div>
      </div>

      {/* Review Details Modal */}
      {selectedApp && (
        <Modal title={`Application Details: ${selectedApp.businessName}`} onClose={() => setSelectedApp(null)} wide>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', paddingBottom: '16px', borderBottom: '1px solid #f0ece3' }}>
              <div>
                <h3 style={{ margin: '0 0 4px', color: '#123b5d', fontSize: '18px' }}>{selectedApp.businessName}</h3>
                <span style={{ fontSize: '13px', color: '#777' }}>Submitted: {formatDateTime(selectedApp.createdAt)}</span>
              </div>
              <span className={`ad-badge ad-badge--${selectedApp.status.toLowerCase()}`} style={{ fontSize: '12px', padding: '5px 12px' }}>
                {selectedApp.status}
              </span>
            </div>

            <div className="ad-fields">
              <div className="ad-field">
                <span className="ad-field__label">Business / Contact</span>
                <span className="ad-field__value"><StorefrontIcon size={15} className="ad-icon-spacing" /> {selectedApp.businessName || 'Business Application'}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Contact Email</span>
                <span className="ad-field__value"><EmailIcon size={15} className="ad-icon-spacing" /> {selectedApp.email}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Applicant Personal Details</span>
                <span className="ad-field__value">
                  <PermIdentityIcon size={15} className="ad-icon-spacing" />
                  {(selectedApp.firstName || selectedApp.lastName)
                    ? `${selectedApp.firstName || ''} ${selectedApp.lastName || ''}`.trim()
                    : 'To be completed by Provider via OTP activation'}
                </span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Contact Phone</span>
                <span className="ad-field__value">
                  <LocalPhoneIcon size={15} className="ad-icon-spacing" />
                  {selectedApp.phoneNumber || 'Completed upon OTP activation'}
                </span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Service Category</span>
                <span className="ad-field__value"><WorkIcon size={15} className="ad-icon-spacing" /> {selectedApp.serviceType}</span>
              </div>
              <div className="ad-field pd-field--full">
                <span className="ad-field__label">Operating Location</span>
                <span className="ad-field__value"><MyLocationIcon size={15} className="ad-icon-spacing" /> {selectedApp.location}</span>
              </div>
              <div className="ad-field pd-field--full">
                <span className="ad-field__label">Business Description</span>
                <span className="ad-field__value" style={{ background: '#faf8f3', padding: '12px 14px', borderRadius: '8px', border: '1px solid #ede8dc', lineHeight: 1.6 }}>
                  {selectedApp.description || 'No description provided.'}
                </span>
              </div>
                            <div className="ad-field pd-field--full">
                <span className="ad-field__label">
                  Legal & Registration Documents ({(() => {
                    try {
                      const docs = JSON.parse(selectedApp.legalDocumentsJson || '[]')
                      return docs.length > 0 ? docs.length : 1
                    } catch { return 1 }
                  })()} files)
                </span>
                
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  {(() => {
                    let docList = []
                    try {
                      docList = JSON.parse(selectedApp.legalDocumentsJson || '[]')
                    } catch { }

                    if (docList && docList.length > 0) {
                      return docList.map((doc, idx) => (
                        <div 
                          key={idx} 
                          style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '12px', background: '#faf8f3', padding: '10px 14px', borderRadius: '8px', border: '1px solid #ede8dc' }}
                        >
                          <span style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13.5px' }}>
                            <DocumentScannerIcon size={16} /> 
                            <strong style={{ color: '#123b5d' }}>{doc.OriginalFileName || `Document #${idx + 1}`}</strong>
                          </span>
                          <button
                            type="button"
                            className="ad-quick-btn ad-quick-btn--primary"
                            style={{ padding: '6px 12px', fontSize: '12px' }}
                            onClick={() => handleDownloadDocument(selectedApp, idx, doc.OriginalFileName)}
                            disabled={downloadingId === `${selectedApp.id}_${idx}`}
                          >
                            <DocumentScannerIcon size={14} /> {downloadingId === `${selectedApp.id}_${idx}` ? 'Downloading…' : 'Download Document'}
                          </button>
                        </div>
                      ))
                    }

                    // Fallback for single legacy document
                    return (
                      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '12px', background: '#faf8f3', padding: '10px 14px', borderRadius: '8px', border: '1px solid #ede8dc' }}>
                        <span style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13.5px' }}>
                          <DocumentScannerIcon size={16} /> 
                          <strong style={{ color: '#123b5d' }}>{selectedApp.legalDocumentFileName || 'Standard Registration Record'}</strong>
                        </span>
                        <button
                          type="button"
                          className="ad-quick-btn ad-quick-btn--primary"
                          style={{ padding: '6px 12px', fontSize: '12px' }}
                          onClick={() => handleDownloadDocument(selectedApp, 0)}
                          disabled={downloadingId === `${selectedApp.id}_0`}
                        >
                          <DocumentScannerIcon size={14} /> {downloadingId === `${selectedApp.id}_0` ? 'Downloading…' : 'Download Document'}
                        </button>
                      </div>
                    )
                  })()}
                </div>
              </div>
            </div>

            <div style={{ paddingTop: '16px', borderTop: '1px solid #f0ece3', display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
              {selectedApp.status?.toLowerCase() === 'pending' && (
                <>
                  <button
                    type="button"
                    className="ad-quick-btn"
                    style={{ backgroundColor: '#4F8A45', color: '#ffffff', border: 'none' }}
                    onClick={() => setConfirmApproveApp(selectedApp)}
                  >
                    <CheckCircleIcon size={16} className="ad-icon-spacing-4" /> Approve
                  </button>
                  <button
                    type="button"
                    className="ad-quick-btn ad-quick-btn--danger"
                    onClick={() => {
                      setConfirmRejectApp(selectedApp)
                      setRejectionReason('')
                      setCustomRejectionReason('')
                      setRejectionError('')
                    }}
                  >
                    <CancelIcon size={16} className="ad-icon-spacing-4" /> Reject
                  </button>
                </>
              )}
              <button className="ad-cancel-btn" onClick={() => setSelectedApp(null)}>
                Close Review
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
            placeholder="Search by business, applicant, location..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <div className="ad-filter-pills">
          <button className={`ad-filter-pill ${filterStatus === 'all' ? 'active' : ''}`} onClick={() => setFilterStatus('all')}>
            All ({list.length})
          </button>
          <button className={`ad-filter-pill ${filterStatus === 'pending' ? 'active' : ''}`} onClick={() => setFilterStatus('pending')}>
            Pending ({list.filter(a => (a.status || '').toLowerCase() === 'pending').length})
          </button>
          <button className={`ad-filter-pill ${filterStatus === 'approved' ? 'active' : ''}`} onClick={() => setFilterStatus('approved')}>
            Approved ({list.filter(a => (a.status || '').toLowerCase() === 'approved').length})
          </button>
          <button className={`ad-filter-pill ${filterStatus === 'rejected' ? 'active' : ''}`} onClick={() => setFilterStatus('rejected')}>
            Rejected ({list.filter(a => (a.status || '').toLowerCase() === 'rejected').length})
          </button>
        </div>
      </div>

      {/* Applications Table */}
      <div className="ad-card">
        <div className="ad-card__body" style={{ padding: 0 }}>
          {filtered.length === 0 ? (
            <div className="ad-empty">
              <div className="ad-empty__icon"><DocumentScannerIcon size={32} /></div>
              <p className="ad-empty__title">No applications found</p>
              <p className="ad-empty__msg">No application records match your filter criteria.</p>
            </div>
          ) : (
            <div className="ad-table-wrap" style={{ border: 'none', borderRadius: 0 }}>
              <table className="ad-table">
                <thead>
                  <tr>
                    <th>Business Name</th>
                    <th>Applicant</th>
                    <th>Service Category</th>
                    <th>Location</th>
                    <th>Submitted</th>
                    <th>Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(app => (
                    <tr key={app.id}>
                      <td style={{ fontWeight: 700, color: '#123b5d' }}>{app.businessName}</td>
                      <td>
                        <div style={{ fontWeight: 600, color: '#334155' }}>
                          {(app.firstName || app.lastName) ? `${app.firstName || ''} ${app.lastName || ''}`.trim() : 'Business Contact'}
                        </div>
                        <div style={{ fontSize: '11.5px', color: '#64748b' }}>{app.email}</div>
                      </td>
                      <td>{app.serviceType}</td>
                      <td>{app.location}</td>
                      <td>{formatDate(app.createdAt)}</td>
                      <td>
                        <span className={`ad-badge ad-badge--${app.status.toLowerCase()}`}>
                          {app.status}
                        </span>
                      </td>
                      <td>
                        <div className="ad-row-actions">
                          <button className="ad-row-btn ad-row-btn--view" onClick={() => setSelectedApp(app)}>
                            Review Details
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

      {/* Confirmation Modals */}
      <ConfirmModal
        isOpen={Boolean(confirmApproveApp)}
        title="Approve Provider Application"
        message={`Are you sure you want to approve the application for ${confirmApproveApp?.businessName}?`}
        confirmText="Approve"
        cancelText="Cancel"
        confirmVariant="primary"
        onConfirm={executeApprove}
        onCancel={() => setConfirmApproveApp(null)}
        loading={actionLoading}
      />

      {confirmRejectApp && (
        <Modal
          title={`Reject Application: ${confirmRejectApp.businessName}`}
          onClose={() => {
            setConfirmRejectApp(null)
            setRejectionReason('')
            setCustomRejectionReason('')
            setRejectionError('')
          }}
        >
          <div className="ad-confirm-reject-form">
            <p style={{ marginBottom: '12px', color: '#4a5568' }}>
              Are you sure you want to reject the application for <strong>{confirmRejectApp.businessName}</strong>?
            </p>

            <label style={{ display: 'block', fontWeight: 600, marginBottom: '6px', fontSize: '13px' }}>
              Primary Rejection Reason
            </label>
            <select
              value={rejectionReason}
              onChange={(e) => {
                setRejectionReason(e.target.value)
                if (rejectionError) setRejectionError('')
              }}
              style={{
                width: '100%',
                padding: '10px',
                borderRadius: '6px',
                border: '1px solid #cbd5e1',
                fontSize: '14px',
                marginBottom: '12px',
                backgroundColor: '#fff',
                boxSizing: 'border-box'
              }}
              disabled={actionLoading}
            >
              <option value="">Select a reason (optional)...</option>
              {commonRejectionReasons.map((reason, index) => (
                <option key={index} value={reason}>{reason}</option>
              ))}
              <option value="other">Other</option>
            </select>

            <label style={{ display: 'block', fontWeight: 600, marginBottom: '6px', fontSize: '13px' }}>
              Additional Feedback
            </label>
            <textarea
              rows={4}
              value={customRejectionReason}
              onChange={(e) => {
                setCustomRejectionReason(e.target.value)
                if (rejectionError) setRejectionError('')
              }}
              placeholder="Provide further explanation or notes."
              style={{
                width: '100%',
                padding: '10px',
                borderRadius: '6px',
                border: rejectionError ? '1px solid #e53e3e' : '1px solid #cbd5e1',
                fontSize: '14px',
                resize: 'vertical',
                boxSizing: 'border-box'
              }}
              disabled={actionLoading}
            />

            {rejectionError && (
              <p style={{ color: '#e53e3e', fontSize: '12px', marginTop: '4px' }}>
                {rejectionError}
              </p>
            )}

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '16px' }}>
              <button
                type="button"
                className="ad-cancel-btn"
                onClick={() => {
                  setConfirmRejectApp(null)
                  setRejectionReason('')
                  setCustomRejectionReason('')
                  setRejectionError('')
                }}
                disabled={actionLoading}
              >
                Cancel
              </button>
              <button
                type="button"
                className="ad-quick-btn ad-quick-btn--danger"
                onClick={executeReject}
                disabled={actionLoading || (!rejectionReason && !customRejectionReason.trim())}
              >
                {actionLoading ? 'Rejecting...' : 'Reject Application'}
              </button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}

// ── 3. User Management Tab ────────────────────────────────────────────────────
