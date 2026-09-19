import React, { useState } from 'react'
import {
  CalendarMonthIcon,
  ManageSearchIcon,
  CheckCircleIcon,
  CancelIcon
} from '../../../components/Icons'
import StatusBadge from '../../../components/common/StatusBadge'
import EmptyState from '../../../components/common/EmptyState'

function formatDate(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
}

function formatCurrency(amount) {
  return new Intl.NumberFormat('en-LK', { style: 'currency', currency: 'LKR', maximumFractionDigits: 0 }).format(amount || 0)
}

export default function ProviderBookingsTab({ bookings = [], onUpdateBookingStatus }) {
  const [filter, setFilter] = useState('all')
  const [search, setSearch] = useState('')

  const filtered = bookings.filter(b => {
    if (filter !== 'all' && b.status?.toLowerCase() !== filter.toLowerCase()) return false
    if (search.trim()) {
      const q = search.toLowerCase()
      const titleMatch = b.serviceTitle?.toLowerCase().includes(q)
      const nameMatch  = b.customerName?.toLowerCase().includes(q)
      const idMatch    = String(b.id || '').toLowerCase().includes(q)
      if (!titleMatch && !nameMatch && !idMatch) return false
    }
    return true
  })

  return (
    <div className="pd-bookings">
      <div className="pd-page-header">
        <div className="pd-page-header__left">
          <h1>Booking Management</h1>
          <p>Review, accept, or decline reservations for your services.</p>
        </div>
      </div>

      <div className="pd-toolbar">
        <div className="pd-search-input-wrap">
          <input
            type="text"
            className="pd-search-input"
            placeholder="Search bookings by customer or service…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        <div className="pd-filter-group">
          <button
            className={`pd-filter-btn ${filter === 'all' ? 'active' : ''}`}
            onClick={() => setFilter('all')}
          >
            All ({bookings.length})
          </button>
          <button
            className={`pd-filter-btn ${filter === 'pending' ? 'active' : ''}`}
            onClick={() => setFilter('pending')}
          >
            Pending ({bookings.filter(b => b.status?.toLowerCase() === 'pending').length})
          </button>
          <button
            className={`pd-filter-btn ${filter === 'confirmed' ? 'active' : ''}`}
            onClick={() => setFilter('confirmed')}
          >
            Confirmed ({bookings.filter(b => b.status?.toLowerCase() === 'confirmed').length})
          </button>
          <button
            className={`pd-filter-btn ${filter === 'completed' ? 'active' : ''}`}
            onClick={() => setFilter('completed')}
          >
            Completed ({bookings.filter(b => b.status?.toLowerCase() === 'completed').length})
          </button>
        </div>
      </div>

      {filtered.length === 0 ? (
        <EmptyState
          icon={CalendarMonthIcon}
          title="No bookings found"
          message={search || filter !== 'all' ? 'No bookings match your selected filter criteria.' : 'You have no customer reservations yet.'}
        />
      ) : (
        <div className="pd-table-card">
          <table className="pd-table">
            <thead>
              <tr>
                <th>Booking ID</th>
                <th>Service</th>
                <th>Customer</th>
                <th>Date</th>
                <th>Amount</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(b => (
                <tr key={b.id}>
                  <td className="pd-table__bold">#{b.id}</td>
                  <td>{b.serviceTitle || 'Listing Service'}</td>
                  <td>{b.customerName || b.visitorName || 'Visitor'}</td>
                  <td>{formatDate(b.bookingDate || b.date)}</td>
                  <td>{formatCurrency(b.totalAmount || b.amount)}</td>
                  <td><StatusBadge status={b.status || 'Pending'} /></td>
                  <td>
                    <div className="pd-row-actions">
                      {b.status?.toLowerCase() === 'pending' && (
                        <>
                          <button
                            className="pd-btn-sm pd-btn-sm--primary"
                            onClick={() => onUpdateBookingStatus(b.id, 'Confirmed')}
                            title="Confirm Booking"
                          >
                            <CheckCircleIcon size={14} /> Accept
                          </button>
                          <button
                            className="pd-btn-sm pd-btn-sm--danger"
                            onClick={() => onUpdateBookingStatus(b.id, 'Cancelled')}
                            title="Decline Booking"
                          >
                            <CancelIcon size={14} /> Decline
                          </button>
                        </>
                      )}
                      {b.status?.toLowerCase() === 'confirmed' && (
                        <button
                          className="pd-btn-sm pd-btn-sm--secondary"
                          onClick={() => onUpdateBookingStatus(b.id, 'Completed')}
                          title="Mark Completed"
                        >
                          Complete
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
