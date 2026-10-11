import { NotificationsActiveIcon } from './Icons'
import { formatSriLankaTime } from '../api/bookingPayment'
import './NotificationPanel.css'

const labels = { 'booking.created': 'Booking created', 'payment.completed': 'Payment completed',
  'booking.canceled': 'Booking canceled', 'review.submitted': 'New review' }

export default function NotificationPanel({ state }) {
  const { items, unreadCount, totalCount, pageSize, page, setPage, loading, error, busy, refresh, markRead, markAllRead } = state
  return <section className="cq-notifications" aria-label="Notifications">
    <div className="cq-notifications__heading">
      <div><h1>Notifications</h1><p>{unreadCount} unread · Booking and payment updates</p></div>
      <button type="button" disabled={busy || loading || unreadCount === 0} onClick={markAllRead}>Mark all as read</button>
    </div>
    {error && <div className="cq-notifications__error" role="alert">{error} <button type="button" disabled={busy} onClick={refresh}>Retry</button></div>}
    {loading && <p role="status">Loading notifications…</p>}
    {!loading && !error && items.length === 0 && <div className="cq-notifications__empty"><NotificationsActiveIcon size={32} /><h2>No notifications yet</h2><p>Your updates will appear here.</p></div>}
    <div className="cq-notifications__list">
      {items.map(item => <article key={item.id} className={`cq-notifications__item ${item.isRead ? '' : 'cq-notifications__item--unread'}`}>
        <div className="cq-notifications__meta"><span>{labels[item.eventType] || 'Notification'}</span><span>{item.isRead ? 'Read' : 'Unread'}</span></div>
        <h2>{item.title}</h2><p>{item.message}</p>
        <div className="cq-notifications__meta"><time>{formatSriLankaTime(item.createdAtUtc)}</time>
          {!item.isRead && <button type="button" disabled={busy} onClick={() => markRead(item.id)}>Mark as read</button>}
        </div>
      </article>)}
    </div>
    {totalCount > pageSize && <nav className="cq-notifications__pagination" aria-label="Notification pages">
      <button type="button" disabled={page === 1 || loading || busy} onClick={() => setPage(page - 1)}>Previous</button>
      <span>Page {page} of {Math.ceil(totalCount / pageSize)}</span>
      <button type="button" disabled={page * pageSize >= totalCount || loading || busy} onClick={() => setPage(page + 1)}>Next</button>
    </nav>}
  </section>
}
