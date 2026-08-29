import { useState, useEffect } from 'react'
import '../styles/ProviderApplicationStatus.css'

function formatDate(iso) {
  if (!iso) return '—'
  try {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  } catch {
    return iso
  }
}

function ProviderApplicationStatus({ onBack, onApply, onLogin, onActivate }) {
  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(false)
  const [result, setResult] = useState(null)
  const [error, setError] = useState(null)
  const [searchedEmail, setSearchedEmail] = useState('')

  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    const qEmail = params.get('email')
    if (qEmail) {
      setEmail(qEmail)
      handleLookup(qEmail)
    }
  }, [])

  const handleLookup = async (targetEmail) => {
    const cleanEmail = (targetEmail || email || '').trim()
    if (!cleanEmail) {
      setError('Please enter the email address used during your application submission.')
      return
    }

    setLoading(true)
    setError(null)
    setResult(null)
    setSearchedEmail(cleanEmail)

    try {
      const resp = await fetch(`/api/provider-applications/status?email=${encodeURIComponent(cleanEmail)}`)

      if (resp.ok) {
        const data = await resp.json()
        if (data.found === false) {
          setError(data.message || `No provider application was found for "${cleanEmail}". Please check the spelling or submit a new application.`)
        } else {
          setResult(data)
        }
      } else if (resp.status === 404) {
        setError(`No provider application was found for "${cleanEmail}". Please check the spelling or submit a new application.`)
      } else if (resp.status === 400) {
        const data = await resp.json().catch(() => ({}))
        setError(data.message || 'Please provide a valid email address.')
      } else {
        setError('Unable to fetch application status at this time. Please try again later.')
      }
    } catch {
      setError('Network error. Please check your internet connection and try again.')
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = (e) => {
    e.preventDefault()
    handleLookup(email)
  }

  const handleResetSearch = () => {
    setEmail('')
    setResult(null)
    setError(null)
    setSearchedEmail('')
  }

  const statusRaw = result?.status !== undefined ? String(result.status).trim() : ''
  const isApproved = statusRaw === '1' || statusRaw.toLowerCase() === 'approved'
  const isRejected = statusRaw === '2' || statusRaw.toLowerCase() === 'rejected'
  const isPending  = statusRaw === '0' || statusRaw.toLowerCase() === 'pending' || (!isApproved && !isRejected)
  const statusNormalized = isApproved ? 'approved' : (isRejected ? 'rejected' : 'pending')
  const displayStatus = isApproved ? 'Approved' : (isRejected ? 'Rejected' : 'Pending')

  return (
    <div className="pas-page">
      <div className="pas-container">

        <div className="pas-header">
          <div className="pas-logo" onClick={onBack} style={{ cursor: 'pointer' }}>
            <img src="/dashboard-logo.png" alt="CeylonQuest" className="pas-logo__img" />
            <span className="pas-logo__badge">Provider Verification</span>
          </div>
          <h1 className="pas-title">Track Application Status</h1>
          <p className="pas-subtitle">
            Enter the email address you used when submitting your provider registration to view real-time review progress.
          </p>
        </div>

        <div className="pas-card">
          <form className="pas-form" onSubmit={handleSubmit} id="pas-search-form">
            <div className="pas-input-group">
              <label htmlFor="pas-email" className="pas-label">
                Official Applicant Email Address
              </label>
              <div className="pas-input-wrapper">
                <span className="pas-input-icon"></span>
                <input
                  id="pas-email"
                  type="email"
                  className="pas-input"
                  placeholder="e.g. yourname@business.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  disabled={loading}
                  required
                />
                <button
                  type="submit"
                  className="pas-submit-btn"
                  id="pas-check-status-btn"
                  disabled={loading || !email.trim()}
                >
                  {loading ? (
                    <span className="pas-btn-loading">
                      <span className="pas-spinner" />
                      Checking…
                    </span>
                  ) : (
                    'Check Status →'
                  )}
                </button>
              </div>
            </div>
          </form>

          {error && !loading && (
            <div className="pas-error-box" role="alert">
              <div className="pas-error-icon"></div>
              <div className="pas-error-content">
                <p className="pas-error-title">Application Not Found</p>
                <p className="pas-error-msg">{error}</p>
                <div className="pas-error-actions">
                  {onApply && (
                    <button type="button" className="pas-link-btn" onClick={onApply}>
                      Submit a New Application →
                    </button>
                  )}
                </div>
              </div>
            </div>
          )}

          {loading && (
            <div className="pas-loading-state">
              <div className="pas-spinner pas-spinner--large" />
              <p>Searching application records for <strong>{searchedEmail}</strong>…</p>
            </div>
          )}

          {result && !loading && (
            <div className="pas-results" id="pas-results-section">
              
              <div className={`pas-status-banner pas-status-banner--${statusNormalized}`}>
                <div className="pas-status-banner__icon">
                  {isPending && ''}
                  {isApproved && ''}
                  {isRejected && ''}
                </div>
                <div className="pas-status-banner__info">
                  <div className="pas-status-tag-row">
                    <span className={`pas-status-badge pas-status-badge--${statusNormalized}`}>
                      Status: {displayStatus}
                    </span>
                    <span className="pas-status-date">Submitted on {formatDate(result.submittedAt)}</span>
                  </div>
                  <h2 className="pas-status-headline">
                    {isPending && 'Application Is Under Review'}
                    {isApproved && 'Application Approved!'}
                    {isRejected && 'Application Not Approved'}
                  </h2>
                  <p className="pas-status-msg">{result.message}</p>
                </div>
              </div>

              {isPending && (
                <div className="pas-pending-guide">
                  <h3 className="pas-section-heading">Verification Progress</h3>
                  <div className="pas-steps">
                    <div className="pas-step pas-step--done">
                      <div className="pas-step__bullet"></div>
                      <div className="pas-step__content">
                        <strong>Application Received</strong>
                        <span>Your documents &amp; business profile were recorded.</span>
                      </div>
                    </div>
                    <div className="pas-step pas-step--active">
                      <div className="pas-step__bullet">2</div>
                      <div className="pas-step__content">
                        <strong>Administrator Review</strong>
                        <span>Our team is validating your details &amp; credentials.</span>
                      </div>
                    </div>
                    <div className="pas-step">
                      <div className="pas-step__bullet">3</div>
                      <div className="pas-step__content">
                        <strong>Account Activation</strong>
                        <span>You will receive an OTP code to set up your password.</span>
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {isApproved && (
                <div className="pas-approved-guide">
                  <div className="pas-approved-callout">
                    <span className="pas-callout-icon"></span>
                    <div>
                      <strong>What happens next?</strong>
                      <p>
                        An activation OTP (One-Time Password) was dispatched to <strong>{result.email}</strong>.
                        Use your OTP to complete your personal contact profile and activate your provider portal.
                      </p>
                    </div>
                  </div>
                  <div className="pas-action-row" style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
                    {onActivate && (
                      <button
                        type="button"
                        className="pas-btn-primary"
                        id="pas-enter-otp-btn"
                        onClick={onActivate}
                      >
                        Enter OTP &amp; Complete Profile →
                      </button>
                    )}
                    {onLogin && (
                      <button
                        type="button"
                        className="pas-btn-secondary"
                        onClick={onLogin}
                      >
                        Sign In →
                      </button>
                    )}
                  </div>
                </div>
              )}

              {isRejected && (
                <div className="pas-rejected-guide">
                  <div className="pas-rejection-box">
                    <div className="pas-rejection-box__header">
                      <span className="pas-rejection-box__icon"></span>
                      <strong>Reason for Decision</strong>
                    </div>
                    <div className="pas-rejection-box__body">
                      <p className="pas-rejection-reason-text">
                        {result.rejectionReason || 'The application or submitted documents did not meet our verification criteria.'}
                      </p>
                    </div>
                  </div>
                  <p className="pas-rejection-help">
                    You are welcome to re-apply with updated documents or corrected business details.
                  </p>
                  {onApply && (
                    <div className="pas-action-row">
                      <button
                        type="button"
                        className="pas-btn-primary"
                        onClick={onApply}
                      >
                        Submit a New Application →
                      </button>
                    </div>
                  )}
                </div>
              )}

              <div className="pas-details-card">
                <h3 className="pas-section-heading">Application Summary</h3>
                <div className="pas-details-grid">
                  <div className="pas-detail-item">
                    <span className="pas-detail-label">Business Name</span>
                    <span className="pas-detail-val"> {result.businessName || '—'}</span>
                  </div>
                  <div className="pas-detail-item">
                    <span className="pas-detail-label">Service Category</span>
                    <span className="pas-detail-val"> {result.serviceType || '—'}</span>
                  </div>
                  <div className="pas-detail-item">
                    <span className="pas-detail-label">Applicant Email</span>
                    <span className="pas-detail-val"> {result.email}</span>
                  </div>
                  <div className="pas-detail-item">
                    <span className="pas-detail-label">Submission Date</span>
                    <span className="pas-detail-val"> {formatDate(result.submittedAt)}</span>
                  </div>
                </div>
              </div>

              <div className="pas-reset-wrap">
                <button
                  type="button"
                  className="pas-btn-secondary"
                  onClick={handleResetSearch}
                >
                   Check Another Application
                </button>
              </div>

            </div>
          )}

        </div>

        <div className="pas-footer-nav">
          {onBack && (
            <button type="button" className="pas-footer-btn" onClick={onBack}>
              ← Back to Registration
            </button>
          )}
          {onApply && (
            <button type="button" className="pas-footer-btn" onClick={onApply}>
              Apply as Provider →
            </button>
          )}
          {onLogin && (
            <button type="button" className="pas-footer-btn" onClick={onLogin}>
              Provider Login →
            </button>
          )}
        </div>

      </div>
    </div>
  )
}

export default ProviderApplicationStatus
