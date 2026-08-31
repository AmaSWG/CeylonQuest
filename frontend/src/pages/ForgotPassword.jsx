import '../styles/ForgotPassword.css'
import { useState } from 'react'
import { apiUrl } from '../api/client'

function ForgotPassword({ onBack }) {
  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [success, setSuccess] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)

    const trimmedEmail = email.trim()
    if (!trimmedEmail) {
      setError('Please enter your email address.')
      return
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(trimmedEmail)) {
      setError('Please enter a valid email address.')
      return
    }

    setLoading(true)

    try {
      const resp = await fetch(apiUrl('/api/auth/forgot-password'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: trimmedEmail })
      })

      if (resp.ok) {
        setSuccess(true)
        setEmail('')
      } else if (resp.status === 400 || resp.status === 422) {
        const body = await resp.json().catch(() => ({}))
        const first = body.errors && Object.values(body.errors).flat()[0]
        setError(first || body.message || 'Invalid email address.')
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
    <div className="forgot-password-page">
      <div className="forgot-password-card">
        <div className="forgot-password-header">
          <img src="/logo.png" alt="CeylonQuest" className="forgot-password-header__logo-img" />
          <h2>Forgot Your Password?</h2>
          <p>Enter your email address and we'll send you a link to reset your password.</p>
        </div>

        {success ? (
          <div className="forgot-password-success">
            <div className="success-icon"></div>
            <h3>Check Your Email</h3>
            <p>If an account exists with this email address, you will receive a password reset link. Please check your email (including your spam folder).</p>
            <button 
              type="button" 
              className="forgot-password-button forgot-password-button--primary"
              onClick={onBack}
            >
              Back to Login
            </button>
          </div>
        ) : (
          <form className="forgot-password-form" onSubmit={handleSubmit}>
            {error && (
              <div className="forgot-password-error" role="alert">
                {error}
              </div>
            )}

            <div className="forgot-password-field">
              <label htmlFor="email">Email Address</label>
              <input
                id="email"
                type="email"
                placeholder="you@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                disabled={loading}
                autoComplete="email"
              />
            </div>

            <button
              type="submit"
              className="forgot-password-button forgot-password-button--primary"
              disabled={loading}
            >
              {loading ? 'Sending...' : 'Send Reset Link'}
            </button>

            <button
              type="button"
              className="forgot-password-button forgot-password-button--secondary"
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

export default ForgotPassword
