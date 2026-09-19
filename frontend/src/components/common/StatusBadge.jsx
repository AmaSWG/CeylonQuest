import React from 'react'
import './StatusBadge.css'

export default function StatusBadge({ status, size = 'normal', className = '' }) {
  if (!status) return null

  const s = String(status).trim()
  const lower = s.toLowerCase()

  let typeClass = 'default'
  if (['active', 'approved', 'confirmed', 'completed', 'verified'].includes(lower)) {
    typeClass = 'success'
  } else if (['pending', 'in review', 'under review', 'processing'].includes(lower)) {
    typeClass = 'warning'
  } else if (['rejected', 'cancelled', 'canceled', 'suspended', 'inactive', 'failed'].includes(lower)) {
    typeClass = 'danger'
  } else if (['admin', 'provider', 'visitor'].includes(lower)) {
    typeClass = 'role-' + lower
  }

  return (
    <span className={`cq-status-badge cq-status-badge--${typeClass} cq-status-badge--${size} ${className}`}>
      {s}
    </span>
  )
}
