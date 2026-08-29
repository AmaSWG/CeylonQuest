import React from 'react'
import '../styles/ConfirmModal.css'
import { DeleteSweepIcon } from './Icons'

export default function ConfirmModal({
  isOpen,
  title = 'Are you sure?',
  message = 'This action cannot be undone.',
  confirmText = 'Confirm',
  cancelText = 'Cancel',
  confirmVariant = 'danger',
  onConfirm,
  onCancel,
  loading = false
}) {
  if (!isOpen) return null

  return (
    <div className="cq-confirm-overlay" onClick={(e) => { if (e.target === e.currentTarget && !loading) onCancel() }}>
      <div className="cq-confirm-modal" role="dialog" aria-modal="true">
        <div className="cq-confirm-header">
          <div className={`cq-confirm-icon cq-confirm-icon--${confirmVariant}`}>
            <DeleteSweepIcon size={20} />
          </div>
          <h3 className="cq-confirm-title">{title}</h3>
          <button className="cq-confirm-close" onClick={onCancel} disabled={loading} aria-label="Close modal">
            &times;
          </button>
        </div>
        <div className="cq-confirm-body">
          <p className="cq-confirm-message">{message}</p>
        </div>
        <div className="cq-confirm-actions">
          <button
            type="button"
            className="cq-confirm-btn cq-confirm-btn--cancel"
            onClick={onCancel}
            disabled={loading}
          >
            {cancelText}
          </button>
          <button
            type="button"
            className={`cq-confirm-btn cq-confirm-btn--${confirmVariant}`}
            onClick={onConfirm}
            disabled={loading}
          >
            {loading ? 'Removing…' : confirmText}
          </button>
        </div>
      </div>
    </div>
  )
}
