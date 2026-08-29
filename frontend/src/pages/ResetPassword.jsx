import '../styles/ResetPassword.css'
import { useState, useEffect } from 'react'
import { CheckCircleIcon, CancelIcon, HomeIcon } from '../components/Icons'

function PasswordRequirementsList({ password }) {
  const requirements = [
    { label: 'At least 8 characters', met: password.length >= 8 },
    { label: 'Contains uppercase letter (A-Z)', met: /[A-Z]/.test(password) },
    { label: 'Contains lowercase letter (a-z)', met: /[a-z]/.test(password) },
    { label: 'Contains number (0-9)', met: /\d/.test(password) },
    { label: 'Contains special character (!@#$%^&*)', met: /[^\da-zA-Z\s]/.test(password) }
  ]

  return (
    <div className="password-requirements">
      <p className="password-requirements__title">Password Requirements</p>
      <ul className="password-requirements__list">
        {requirements.map((req, idx) => (
          <li key={idx} className={`password-requirements__item ${req.met ? 'met' : ''}`}>
            <span className="password-requirements__check">
              {req.met ? <CheckCircleIcon size={14} color="#4f8a45" /> : <CancelIcon size={14} color="#b0a898" />}
            </span>
            {req.label}
          </li>
        ))}
      </ul>
    </div>
  )
}

function ResetPassword({ token: tokenProp, onBack }) {
  const getInitialToken = () => {
    if (tokenProp) return tokenProp
    const params = new URLSearchParams(window.location.search)
    return params.get('token') || ''
  }

  const token = getInitialToken()

  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showNewPassword, setShowNewPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [success, setSuccess] = useState(false)
  const [tokenValid, setTokenValid] = useState(Boolean(token))

  useEffect(() => {
    if (!token) {
      setTokenValid(false)
      setError('Password reset link is missing or invalid. Please request a new password reset.')
    } else {
      setTokenValid(true)
    }
  }, [token])

  const passwordsMatch = newPassword === confirmPassword && newPassword.length > 0
  const passwordMeetsRequirements = 
    newPassword.length >= 8 &&
    /[A-Z]/.test(newPassword) &&
    /[a-z]/.test(newPassword) &&
    /\d/.test(newPassword) &&
    /[^\da-zA-Z\s]/.test(newPassword)

  const isFormValid = passwordsMatch && passwordMeetsRequirements && token

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    if (!token) {
      setError('Password reset link is invalid or has expired. Please request a new password reset.')
      return
    }

    if (newPassword !== confirmPassword) {
      setError('Passwords do not match.')
      return
    }

    if (!passwordMeetsRequirements) {
      setError('Password does not meet the requirements.')
      return
    }

    setLoading(true)

    try {
      const resp = await fetch('/api/auth/reset-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          token: token,
          newPassword: newPassword,
          confirmPassword: confirmPassword
        })
      })

      if (resp.ok) {
        setSuccess(true)
        setNewPassword('')
        setConfirmPassword('')
      } else if (resp.status === 400) {
        const body = await resp.json().catch(() => ({}))
        setError(body.message || 'Unable to reset password. Please request a new link.')
      } else {
        setError('Server error. Please try again later.')
      }
    } catch (ex) {
      setError('Network error. Please check your connection.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="reset-password-page">
      <div className="reset-password-card">
        <div className="reset-password-header">
          <img src="/logo.png" alt="CeylonQuest" className="reset-password-header__logo-img" />
          <h2>Reset Your Password</h2>
          <p>Create a new password for your account.</p>
        </div>

        {success ? (
          <div className="reset-password-success">
            <div className="success-icon"></div>
            <h3>Password Reset Successful</h3>
            <p>Your password has been successfully reset. You can now log in with your new password.</p>
            <button 
              type="button" 
              className="reset-password-button reset-password-button--primary"
              onClick={onBack}
            >
              Back to Login
            </button>
          </div>
        ) : !tokenValid ? (
          <div className="reset-password-error-container">
            <div className="error-icon"></div>
            <h3>Invalid Link</h3>
            <p>This password reset link is invalid or has expired.</p>
            <button 
              type="button" 
              className="reset-password-button reset-password-button--primary"
              onClick={onBack}
            >
              Request New Reset Link
            </button>
          </div>
        ) : (
          <form className="reset-password-form" onSubmit={handleSubmit}>
            {error && (
              <div className="reset-password-error" role="alert">
                {error}
              </div>
            )}

            <div className="reset-password-field">
              <label htmlFor="newPassword">New Password</label>
              <div className="reset-password-input-group">
                <input
                  id="newPassword"
                  type={showNewPassword ? 'text' : 'password'}
                  placeholder="Enter new password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  disabled={loading}
                  autoComplete="new-password"
                />
                <button
                  type="button"
                  className="reset-password-toggle"
                  onClick={() => setShowNewPassword(!showNewPassword)}
                  aria-label={showNewPassword ? 'Hide password' : 'Show password'}
                  tabIndex="-1"
                >
                  {showNewPassword ? 'Hide' : 'Show'}
                </button>
              </div>
            </div>

            <div className="reset-password-field">
              <label htmlFor="confirmPassword">Confirm Password</label>
              <div className="reset-password-input-group">
                <input
                  id="confirmPassword"
                  type={showConfirmPassword ? 'text' : 'password'}
                  placeholder="Confirm new password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  disabled={loading}
                  autoComplete="new-password"
                />
                <button
                  type="button"
                  className="reset-password-toggle"
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  aria-label={showConfirmPassword ? 'Hide password' : 'Show password'}
                  tabIndex="-1"
                >
                  {showConfirmPassword ? 'Hide' : 'Show'}
                </button>
              </div>
              {newPassword && confirmPassword && (
                <p className={`reset-password-match ${passwordsMatch ? 'match' : 'mismatch'}`}>
                  {passwordsMatch ? 'Passwords match' : 'Passwords do not match'}
                </p>
              )}
            </div>

            {newPassword && <PasswordRequirementsList password={newPassword} />}

            <button
              type="submit"
              className="reset-password-button reset-password-button--primary"
              disabled={loading || !isFormValid}
            >
              {loading ? 'Resetting...' : 'Reset Password'}
            </button>

            <button
              type="button"
              className="reset-password-button reset-password-button--secondary"
              onClick={onBack}
              disabled={loading}
            >
              Back to Login
            </button>
          </form>
        )}
      </div>
    </div>
  )
}

export default ResetPassword
