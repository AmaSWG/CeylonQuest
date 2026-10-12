import React, { useEffect, useState } from 'react'
import {
  GroupIcon,
  ManageSearchIcon,
  CheckCircleIcon,
  CancelIcon
} from '../../../components/Icons'
import ConfirmModal from '../../../components/ConfirmModal'
import { apiUrl } from '../../../api/client'
import { updateAdminUserStatus } from '../../../api/adminManagementApi'

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


export default function UserManagementTab({ token, onLogout, users, onRefresh, onUsersChange, usersLoading, usersError, showToast }) {
  const [search, setSearch] = useState('')
  const [filterRole, setFilterRole] = useState('all') // all | visitor | provider | admin
  const [filterStatus, setFilterStatus] = useState('all') // all | active | inactive
  const [selectedUser, setSelectedUser] = useState(null)
  const [actionLoading, setActionLoading] = useState(false)
  const [confirmUserAction, setConfirmUserAction] = useState(null) // { user, newStatus }

  const handleToggleUserStatus = (user) => {
    const newStatus = !user.isActive
    setConfirmUserAction({ user, newStatus })
  }

  const executeToggleUserStatus = async () => {
    if (!confirmUserAction || actionLoading) return

    const { user, newStatus } = confirmUserAction
    setActionLoading(true)

    try {
      const result = await updateAdminUserStatus(
        token,
        user.id,
        newStatus,
     )

      const isActive = result?.isActive ?? newStatus

      onUsersChange(previous =>
        previous.map(item =>
          item.id === user.id ? { ...item, isActive } : item,
        ),
      )

      setSelectedUser(previous =>
        previous?.id === user.id
          ? { ...previous, isActive }
          : previous,
        )

      setConfirmUserAction(null)

      showToast(
        isActive
          ? `${user.firstName} ${user.lastName} was reactivated successfully.`
          : `${user.firstName} ${user.lastName} was suspended successfully.`,
        'Success',
      )
    } catch (error) {
      showToast(
        error.message || 'Failed to update user status.',
        'Error',
      )
    } finally {
      setActionLoading(false)
    }
  }

  const userList = Array.isArray(users) ? users : []
  const filtered = userList.filter(u => {
    if (!u) return false
    const q = (search || '').toLowerCase().trim()
    const matchSearch =
      !q ||
      (u.firstName || '').toLowerCase().includes(q) ||
      (u.lastName || '').toLowerCase().includes(q) ||
      (u.email || '').toLowerCase().includes(q) ||
      (u.phoneNumber && u.phoneNumber.includes(q)) ||
      (u.nationality && u.nationality.toLowerCase().includes(q))
    if (!matchSearch) return false
    if (filterRole !== 'all' && (u.role || '').toLowerCase() !== filterRole.toLowerCase()) return false
    if (filterStatus === 'active' && !u.isActive) return false
    if (filterStatus === 'inactive' && u.isActive) return false
    return true
  })

  return (
    <div className="ad-users-tab">
      <ConfirmModal
        isOpen={Boolean(confirmUserAction)}
        onConfirm={executeToggleUserStatus}
        loading={actionLoading}
        loadingText="Updating…"
        confirmVariant={confirmUserAction?.newStatus ? 'primary' : 'danger'}
        title={confirmUserAction?.newStatus ? 'Reactivate User' : 'Suspend User'}
        message={`Are you sure you want to ${
          confirmUserAction?.newStatus ? 'reactivate' : 'suspend'
        } ${[
          confirmUserAction?.user?.firstName,
          confirmUserAction?.user?.lastName,
        ].filter(Boolean).join(' ') || 'this user'}?`}
        confirmText={confirmUserAction?.newStatus ? 'Reactivate User' : 'Suspend User'}
        onCancel={() => {
          if (!actionLoading) setConfirmUserAction(null)
        }}
      />
      <div className="ad-page-header">
        <div className="ad-page-header__left">
          <h1>User Management</h1>
          <p>View all registered platform accounts, assigned roles, and manage user statuses.</p>
        </div>
      </div>

      {/* User Details Modal */}
      {selectedUser && (
        <Modal title={`User Details: ${selectedUser.firstName} ${selectedUser.lastName}`} onClose={() => setSelectedUser(null)}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
            <div className="ad-identity" style={{ marginBottom: 0, paddingBottom: '16px' }}>
              <div className="ad-avatar">{initials(selectedUser.firstName, selectedUser.lastName)}</div>
              <div className="ad-identity__info">
                <h3 className="ad-identity__name">{selectedUser.firstName} {selectedUser.lastName}</h3>
                <p className="ad-identity__email">{selectedUser.email}</p>
                <div style={{ display: 'flex', gap: '8px', marginTop: '6px' }}>
                  <span className={`ad-badge ad-badge--${selectedUser.role.toLowerCase()}`}>{selectedUser.role}</span>
                  <span className={`ad-badge ad-badge--${selectedUser.isActive ? 'active' : 'inactive'}`}>
                    {selectedUser.isActive ? 'Active' : 'Suspended'}
                  </span>
                </div>
              </div>
            </div>

            <div className="ad-fields">
              <div className="ad-field">
                <span className="ad-field__label">User ID</span>
                <span className="ad-field__value" style={{ fontSize: '12px', fontFamily: 'monospace' }}>{selectedUser.id}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Assigned Role</span>
                <span className="ad-field__value">{selectedUser.role}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Phone Number</span>
                <span className="ad-field__value">{selectedUser.phoneNumber || '—'}</span>
              </div>
              <div className="ad-field">
                <span className="ad-field__label">Nationality</span>
                <span className="ad-field__value">{selectedUser.nationality || '—'}</span>
              </div>
              <div className="ad-field pd-field--full">
                <span className="ad-field__label">Registered On</span>
                <span className="ad-field__value">{formatDateTime(selectedUser.createdAt)}</span>
              </div>
            </div>

            <div style={{ paddingTop: '14px', borderTop: '1px solid #f0ece3', display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
              <button
                className={`ad-quick-btn ${selectedUser.isActive ? 'ad-quick-btn--danger' : 'ad-quick-btn--success'}`}
                onClick={() => handleToggleUserStatus(selectedUser)}
                disabled={actionLoading || usersLoading}
              >
                {selectedUser.isActive ? 'Suspend User Account' : 'Reactivate User Account'}
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
            placeholder="Search by name, email, phone, country..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <div className="ad-filter-pills">
          <button className={`ad-filter-pill ${filterRole === 'all' ? 'active' : ''}`} onClick={() => setFilterRole('all')}>
            All Roles ({users.length})
          </button>
          <button className={`ad-filter-pill ${filterRole === 'visitor' ? 'active' : ''}`} onClick={() => setFilterRole('visitor')}>
            Visitors ({users.filter(u => u.role === 'Visitor').length})
          </button>
          <button className={`ad-filter-pill ${filterRole === 'provider' ? 'active' : ''}`} onClick={() => setFilterRole('provider')}>
            Providers ({users.filter(u => u.role === 'Provider').length})
          </button>
          <button className={`ad-filter-pill ${filterRole === 'admin' ? 'active' : ''}`} onClick={() => setFilterRole('admin')}>
            Admins ({users.filter(u => u.role === 'Admin').length})
          </button>
        </div>
      </div>

      {/* Users Table */}
      <div className="ad-filter-pills" style={{ marginBottom: '16px' }}>
        {[
          ['all', 'All statuses'],
          ['active', 'Active'],
          ['inactive', 'Suspended'],
        ].map(([value, label]) => (
          <button
            key={value}
            type="button"
            className={`ad-filter-pill ${filterStatus === value ? 'active' : ''}`}
            onClick={() => setFilterStatus(value)}
          >
            {label}
          </button>
        ))}
        <button
          type="button"
          className="ad-row-btn ad-row-btn--view"
          onClick={onRefresh}
          disabled={usersLoading || actionLoading}
        >
          {usersLoading ? 'Loading…' : 'Refresh'}
        </button>
      </div>
      {usersLoading && <p role="status">Loading users…</p>}
      {usersError && (
        <p role="alert" style={{ color: '#b42318' }}>{usersError}</p>
      )}
      <div className="ad-card">
        <div className="ad-card__body" style={{ padding: 0 }}>
          {usersLoading || usersError ? null : filtered.length === 0 ? (
            <div className="ad-empty">
              <div className="ad-empty__icon"><GroupIcon size={32} /></div>
              <p className="ad-empty__title">No users found</p>
              <p className="ad-empty__msg">No registered user accounts match your search filters.</p>
            </div>
          ) : (
            <div className="ad-table-wrap" style={{ border: 'none', borderRadius: 0 }}>
              <table className="ad-table">
                <thead>
                  <tr>
                    <th>User</th>
                    <th>Contact Phone</th>
                    <th>Nationality</th>
                    <th>Role</th>
                    <th>Status</th>
                    <th>Registered</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map(user => (
                    <tr key={user.id}>
                      <td>
                        <div style={{ fontWeight: 700, color: '#123b5d' }}>{user.firstName} {user.lastName}</div>
                        <div style={{ fontSize: '11.5px', color: '#888' }}>{user.email}</div>
                      </td>
                      <td>{user.phoneNumber || '—'}</td>
                      <td>{user.nationality || '—'}</td>
                      <td>
                        <span className={`ad-badge ad-badge--${user.role.toLowerCase()}`}>
                          {user.role}
                        </span>
                      </td>
                      <td>
                        <span className={`ad-badge ad-badge--${user.isActive ? 'active' : 'inactive'}`}>
                          {user.isActive ? 'Active' : 'Suspended'}
                        </span>
                      </td>
                      <td>{formatDate(user.createdAt)}</td>
                      <td>
                        <div className="ad-row-actions">
                          <button className="ad-row-btn ad-row-btn--view" onClick={() => setSelectedUser(user)}>
                            View
                          </button>
                          <button
                            className="ad-row-btn ad-row-btn--toggle"
                            onClick={() => handleToggleUserStatus(user)}
                            disabled={actionLoading || usersLoading}
                          >
                            {user.isActive ? 'Suspend' : 'Reactivate'}
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

// ── 4. Provider Management Tab ────────────────────────────────────────────────

