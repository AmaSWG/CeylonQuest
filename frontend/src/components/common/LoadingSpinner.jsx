import React from 'react'
import './LoadingSpinner.css'

export default function LoadingSpinner({ label = 'Loading…', fullPage = false, className = '' }) {
  return (
    <div className={`cq-loading ${fullPage ? 'cq-loading--full' : ''} ${className}`}>
      <div className="cq-spinner" />
      {label && <span className="cq-loading__label">{label}</span>}
    </div>
  )
}
