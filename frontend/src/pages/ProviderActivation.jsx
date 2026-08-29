import { useState, useEffect } from 'react'
import '../styles/ProviderActivation.css'

function ActivationToast({ message, type = 'success', onClose }) {
  useEffect(() => {
    const timer = setTimeout(onClose, 5000)
    return () => clearTimeout(timer)
  }, [onClose])

  return (
    <div className={`pact-toast pact-toast--${type}`} role="alert">
      <div className="pact-toast__icon">{type === 'success' ? '' : 'ℹ'}</div>
      <div className="pact-toast__body">
        <p className="pact-toast__title">{type === 'success' ? 'Success' : 'Notice'}</p>
        <p className="pact-toast__msg">{message}</p>
      </div>
      <button className="pact-toast__close" onClick={onClose} aria-label="Close notification"></button>
    </div>
  )
}

function ProviderActivation({ onLogin, onBack, onStatusCheck, initialEmail = '' }) {
  const [step, setStep] = useState('otp')
  const [email, setEmail] = useState(initialEmail)
  const [otp, setOtp] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)

  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [toast, setToast] = useState(null)

  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    const qEmail = params.get('email')
    const qOtp = params.get('otp')
    if (qEmail) setEmail(qEmail)
    if (qOtp) setOtp(qOtp)
  }, [])

  const handleVerifyOtp = async (e) => {
    e.preventDefault()
    setError(null)

    const cleanEmail = email.trim()
    const cleanOtp = otp.trim()

    if (!cleanEmail) {
      setError('Please enter your approved business email address.')
      return
    }
    if (!cleanOtp) {
      setError('Please enter the 6-digit OTP activation code you received.')
      return
    }

    setLoading(true)
    try {
      await new Promise(r => setTimeout(r, 400))
      setStep('personal-info')
      setToast('OTP code confirmed. Please complete your personal profile and set your password.')
    } catch {
      setError('Invalid or expired OTP. Please verify the code or check your application status.')
    } finally {
      setLoading(false)
    }
  }

  const handleCompleteActivation = async (e) => {
    e.preventDefault()
    setError(null)

    if (password !== confirmPassword) {
      setError('Passwords do not match. Please re-enter matching passwords.')
      return
    }
    if (password.length < 8) {
      setError('Password must be at least 8 characters long.')
      return
    }

    setLoading(true)
    try {
      const resp = await fetch('/api/auth/provider/activate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          email: email.trim(),
          otp: otp.trim(),
          newPassword: password,
          firstName: firstName.trim(),
          lastName: lastName.trim(),
          phoneNumber: phoneNumber.trim()
        })
      })

      if (resp.ok) {
        setStep('complete')
        setToast('Account activated successfully! You can now log in with your credentials.')
      } else if (resp.status === 400 || resp.status === 401) {
        const body = await resp.json().catch(() => ({}))
        setError(body.message || 'Activation failed. Please ensure your OTP is valid and not expired.')
      } else {
        setError('Server error during activation. Please try again or contact support.')
      }
    } catch {
      setError('Network error. Please check your connection.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="pact-page">
      {toast && <ActivationToast message={toast} onClose={() => setToast(null)} />}

      <div className="pact-card">
        <div className="pact-card__accent" />

        <div className="pact-header">
          <h1>Provider Account Activation</h1>
          <p>
            {step === 'otp' && 'Enter the activation OTP code sent to your approved business email to complete your registration.'}
            {step === 'personal-info' && 'Complete your personal profile and set your secure password to finalize your provider account.'}
            {step === 'complete' && 'Your provider account is active and ready for business!'}
          </p>
        </div>

        <div className="pact-steps-indicator" aria-label="Activation progress">
          <div className={`pact-indicator-step ${step === 'otp' ? 'active' : 'done'}`}>
            <span className="pact-indicator-num">{step === 'otp' ? '1' : ''}</span>
            <span className="pact-indicator-label">1. OTP Code</span>
          </div>
          <div className="pact-indicator-line" />
          <div className={`pact-indicator-step ${step === 'personal-info' ? 'active' : (step === 'complete' ? 'done' : '')}`}>
            <span className="pact-indicator-num">{step === 'complete' ? '' : '2'}</span>
            <span className="pact-indicator-label">2. Personal Details</span>
          </div>
          <div className="pact-indicator-line" />
          <div className={`pact-indicator-step ${step === 'complete' ? 'active' : ''}`}>
            <span className="pact-indicator-num">3</span>
            <span className="pact-indicator-label">3. Ready</span>
          </div>
        </div>

        {error && <div className="pact-error-box" role="alert"> {error}</div>}

        {step === 'otp' && (
          <form onSubmit={handleVerifyOtp} className="pact-form">
            <div className="form-group">
              <label htmlFor="pact-email">Approved Business Email</label>
              <div className="field-wrap">
                <input
                  type="email"
                  id="pact-email"
                  placeholder="e.g. contact@yourbusiness.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  autoComplete="email"
                  required
                />
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="pact-otp">6-Digit Activation OTP</label>
              <div className="field-wrap">
                <input
                  type="text"
                  id="pact-otp"
                  placeholder="Enter 6-digit code (e.g. 123456)"
                  value={otp}
                  onChange={(e) => setOtp(e.target.value)}
                  maxLength="10"
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  required
                  style={{ letterSpacing: '3px', fontSize: '18px', fontWeight: '700', textAlign: 'center' }}
                />
                <small>Check your email inbox for the approval confirmation &amp; OTP code.</small>
              </div>
            </div>

            <button
              type="submit"
              className="pact-submit-btn"
              id="pact-verify-otp-btn"
              disabled={loading}
            >
              {loading ? 'Verifying OTP…' : 'Verify OTP & Continue →'}
            </button>
          </form>
        )}

        {step === 'personal-info' && (
          <form onSubmit={handleCompleteActivation} className="pact-form">
            <div className="pact-email-pill">
              <span>Activating Account for:</span>
              <strong>{email}</strong>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label htmlFor="pact-first-name"><span className="pact-req">*</span> First Name</label>
                <div className="field-wrap">
                  <input
                    type="text"
                    id="pact-first-name"
                    placeholder="Enter your first name"
                    value={firstName}
                    onChange={(e) => setFirstName(e.target.value)}
                    autoComplete="given-name"
                    required
                  />
                </div>
              </div>
              <div className="form-group">
                <label htmlFor="pact-last-name"><span className="pact-req">*</span> Last Name</label>
                <div className="field-wrap">
                  <input
                    type="text"
                    id="pact-last-name"
                    placeholder="Enter your last name"
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                    autoComplete="family-name"
                    required
                  />
                </div>
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="pact-phone"><span className="pact-req">*</span> Personal Contact Phone Number</label>
              <div className="field-wrap">
                <input
                  type="tel"
                  id="pact-phone"
                  placeholder="e.g. 077 123 4567"
                  value={phoneNumber}
                  onChange={(e) => setPhoneNumber(e.target.value)}
                  autoComplete="tel"
                  required
                />
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label htmlFor="pact-password"><span className="pact-req">*</span> Create Password</label>
                <div className="field-wrap">
                  <input
                    type={showPassword ? 'text' : 'password'}
                    id="pact-password"
                    placeholder="Min 8 characters"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    minLength="8"
                    autoComplete="new-password"
                    required
                  />
                </div>
              </div>
              <div className="form-group">
                <label htmlFor="pact-confirm-password"><span className="pact-req">*</span> Confirm Password</label>
                <div className="field-wrap">
                  <input
                    type={showPassword ? 'text' : 'password'}
                    id="pact-confirm-password"
                    placeholder="Re-enter password"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    minLength="8"
                    autoComplete="new-password"
                    required
                  />
                </div>
              </div>
            </div>

            <div style={{ margin: '4px 0 16px 0', fontSize: '13px' }}>
              <label style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', cursor: 'pointer', color: '#475569' }}>
                <input
                  type="checkbox"
                  checked={showPassword}
                  onChange={(e) => setShowPassword(e.target.checked)}
                />
                Show password characters
              </label>
            </div>

            <div style={{ display: 'flex', gap: '12px' }}>
              <button
                type="button"
                className="pact-back-btn"
                onClick={() => setStep('otp')}
                disabled={loading}
              >
                ← Back
              </button>
              <button
                type="submit"
                className="pact-submit-btn"
                id="pact-complete-btn"
                style={{ flex: 1 }}
                disabled={loading}
              >
                {loading ? 'Completing Activation…' : 'Complete Activation & Set Password'}
              </button>
            </div>
          </form>
        )}

        {step === 'complete' && (
          <div className="pact-success-view">
            <div className="pact-success-icon"></div>
            <h2>Account Successfully Activated!</h2>
            <p>
              Your provider profile and credentials have been recorded. You can now
              sign in to access your CeylonQuest Provider Dashboard, list experiences, and manage bookings.
            </p>

            <button
              type="button"
              className="pact-submit-btn"
              onClick={onLogin}
              id="pact-login-redirect-btn"
              style={{ marginTop: '20px', width: '100%' }}
            >
              Sign In to Provider Portal →
            </button>
          </div>
        )}

        <div className="pact-footer">
          {onBack && (
            <button type="button" className="pact-footer-link" onClick={onBack}>
              ← Back to Registration
            </button>
          )}
          {onLogin && step !== 'complete' && (
            <button type="button" className="pact-footer-link" onClick={onLogin}>
              Already have an active password? Login →
            </button>
          )}
          {onStatusCheck && (
            <button type="button" className="pact-footer-link" onClick={onStatusCheck}>
              Check Application Status →
            </button>
          )}
        </div>

      </div>
    </div>
  )
}

export default ProviderActivation
