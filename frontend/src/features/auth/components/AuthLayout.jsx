import React from 'react'
import './AuthLayout.css'
import { HomeIcon } from '../../../components/Icons'

function AuthLayout({
  children,
  title = 'Welcome to CeylonQuest',
  description = 'Discover Sri Lanka like never before. Experience authentic tourism, connect with local providers, and manage your bookings seamlessly.',
  onHome,
  onBack,
  backText = 'Back',
  subtitle,
  cardMaxWidth
}) {
  return (
    <div className="auth-page-container">
      <div className="auth-card" style={cardMaxWidth ? { maxWidth: cardMaxWidth } : undefined}>
        
        {/* Left Branding Panel */}
        <div className="auth-panel-left">
          <div className="auth-brand-content">
            {onHome && (
              <button
                type="button"
                className="auth-back-home-btn"
                onClick={onHome}
                aria-label="Back to home page"
              >
                <HomeIcon size={16} />
                <span>Home</span>
              </button>
            )}

            <div className="auth-brand-logo-wrap">
              <img src="/logo.png" alt="CeylonQuest Logo" className="auth-brand-logo" />
            </div>

            <h2 className="auth-brand-title">{title}</h2>
            {subtitle && <h4 className="auth-brand-subtitle">{subtitle}</h4>}
            <p className="auth-brand-desc">{description}</p>
          </div>
        </div>

        {/* Right Form Content Panel */}
        <div className="auth-panel-right">
          <div className="auth-top-nav">
            {onBack && (
              <button
                type="button"
                className="auth-nav-btn auth-nav-back"
                onClick={onBack}
              >
                ← {backText}
              </button>
            )}
            {onHome && (
              <button
                type="button"
                className="auth-nav-btn auth-nav-home"
                id="login-home-btn"
                onClick={onHome}
                aria-label="Go back to home page"
              >
                <HomeIcon size={14} />
                <span>Home</span>
              </button>
            )}
          </div>

          <div className="auth-form-slot">
            {children}
          </div>
        </div>

      </div>
    </div>
  )
}

export default AuthLayout
