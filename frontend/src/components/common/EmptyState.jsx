import React from 'react'
import './EmptyState.css'

export default function EmptyState({
  icon: Icon,
  title = 'No items found',
  message,
  actionLabel,
  onAction,
  actionId,
  className = ''
}) {
  return (
    <div className={`cq-empty-state ${className}`}>
      {Icon && (
        <div className="cq-empty-state__icon-wrap">
          <Icon size={38} className="cq-empty-state__icon" />
        </div>
      )}
      <h3 className="cq-empty-state__title">{title}</h3>
      {message && <p className="cq-empty-state__message">{message}</p>}
      {actionLabel && onAction && (
        <button
          className="cq-empty-state__action"
          onClick={onAction}
          id={actionId}
          type="button"
        >
          {actionLabel}
        </button>
      )}
    </div>
  )
}
