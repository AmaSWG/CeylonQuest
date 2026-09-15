import React from 'react'
import {
  NotificationsActiveIcon,
  CheckCircleIcon,
  DeleteSweepIcon
} from '../../../components/Icons'
import EmptyState from '../../../components/common/EmptyState'

function formatDate(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
}

export default function ProviderNotificationsTab({
  notifications = [],
  onMarkAllRead,
  onToggleRead,
  onClearAll
}) {
  return (
    <div className="pd-notifications-view">
      <div className="pd-page-header">
        <div className="pd-page-header__left">
          <h1>Notifications</h1>
          <p>System alerts, booking updates, and announcements.</p>
        </div>
        {notifications.length > 0 && (
          <div className="pd-page-header__actions">
            <button className="pd-btn-secondary" onClick={onMarkAllRead}>
              <CheckCircleIcon size={15} /> Mark All Read
            </button>
            <button className="pd-btn-secondary pd-btn-secondary--danger" onClick={onClearAll}>
              <DeleteSweepIcon size={15} /> Clear All
            </button>
          </div>
        )}
      </div>

      {notifications.length === 0 ? (
        <EmptyState
          icon={NotificationsActiveIcon}
          title="No notifications"
          message="You're all caught up! New bookings and platform updates will appear here."
        />
      ) : (
        <div className="pd-notifs-list">
          {notifications.map(n => (
            <div
              key={n.id}
              className={`pd-notif-card ${!n.read ? 'pd-notif-card--unread' : ''}`}
              onClick={() => onToggleRead(n.id)}
            >
              <div className="pd-notif-card__icon">
                <NotificationsActiveIcon size={18} />
              </div>
              <div className="pd-notif-card__body">
                <div className="pd-notif-card__header">
                  <h4 className="pd-notif-card__title">{n.title || 'Notification'}</h4>
                  <span className="pd-notif-card__time">{formatDate(n.createdAt || n.timestamp)}</span>
                </div>
                <p className="pd-notif-card__msg">{n.message || n.text}</p>
              </div>
              {!n.read && <span className="pd-notif-unread-dot" title="Unread" />}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
