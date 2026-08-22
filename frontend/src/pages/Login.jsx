import '../styles/Login.css'
import { useState, useEffect } from 'react'

function LoginSuccessToast({ message, onClose }) {
  useEffect(() => {
    const timer = setTimeout(onClose, 4000)
    return () => clearTimeout(timer)
  }, [onClose])

  return (
    <div className="login-toast login-toast--success" role="alert" aria-live="polite">
      <div className="login-toast__icon">✓</div>
      <div className="login-toast__body">
        <p className="login-toast__title">Welcome back!</p>
        <p className="login-toast__msg">{message}</p>
      </div>
      <button className="login-toast__close" onClick={onClose} aria-label="Close notification">✕</button>
    </div>
  )
}

function Login({ onLoginSuccess, onBack }) {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [toast, setToast] = useState(null)
  const [showPassword, setShowPassword] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)
    setToast(null)
    setLoading(true)

    const form = e.target
    const data = {
      email:    form.email.value.trim(),
      password: form.password.value
    }

    try {
      const resp = await fetch('/api/auth/login', {
        method:  'POST',
        headers: { 'Content-Type': 'application/json' },
        body:    JSON.stringify(data)
      })

      if (resp.ok) {
        const body = await resp.json().catch(() => ({}))
        const token = body.accessToken || body.token || null
        if (token) {
          localStorage.setItem('authToken', token)
          setToast('You have been signed in successfully.')
          setTimeout(() => {
            if (onLoginSuccess) onLoginSuccess()
          }, 1800)
        } else {
          setError('Login succeeded but no token was received.')
        }
      } else if (resp.status === 401) {
        setError('Incorrect email or password. Please try again.')
      } else if (resp.status === 400) {
        const body = await resp.json().catch(() => ({}))
        setError(body.message || 'Validation error. Check your input.')
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
    <div className="login-page">
      {toast && <LoginSuccessToast message={toast} onClose={() => setToast(null)} />}

      <div className="login-card">

        <div className="login-header">
          <h1>CeylonQuest</h1>
          <h2>Welcome Back</h2>
          <p>Sign in to continue your journey through Sri Lanka.</p>
        </div>

        <form onSubmit={handleSubmit} className="login-form" noValidate>

          {error && <div className="login-error">{error}</div>}

          <div className="login-field">
            <label htmlFor="login-email">Email Address</label>
            <div className="login-input-wrap">
              <span className="login-input-icon" aria-hidden="true">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/>
                  <polyline points="22,6 12,13 2,6"/>
                </svg>
              </span>
              <input
                type="email"
                id="login-email"
                name="email"
                placeholder="Enter your email address"
                autoComplete="email"
                required
              />
            </div>
          </div>

          <div className="login-field">
            <div className="login-label-row">
              <label htmlFor="login-password">Password</label>
              <button type="button" className="login-forgot-btn" tabIndex={0}>
                Forgot password?
              </button>
            </div>
            <div className="login-input-wrap">
              <span className="login-input-icon" aria-hidden="true">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
                  <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
                  <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
                </svg>
              </span>
              <input
                type={showPassword ? 'text' : 'password'}
                id="login-password"
                name="password"
                placeholder="Enter your password"
                autoComplete="current-password"
                required
              />
              <button
                type="button"
                className="login-show-pass"
                onClick={() => setShowPassword(v => !v)}
                aria-label={showPassword ? 'Hide password' : 'Show password'}
              >
                {showPassword ? (
                  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94"/>
                    <path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19"/>
                    <line x1="1" y1="1" x2="23" y2="23"/>
                  </svg>
                ) : (
                  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                    <circle cx="12" cy="12" r="3"/>
                  </svg>
                )}
              </button>
            </div>
          </div>

          <button
            type="submit"
            className="login-submit-btn"
            id="sign-in"
            disabled={loading}
          >
            {loading ? 'Signing in…' : 'Sign In'}
          </button>

        </form>

        <p className="login-register-link">
          Don't have an account?{' '}
          <button
            type="button"
            className="login-register-link__btn"
            onClick={() => onBack && onBack()}
          >
            Create Account
          </button>
        </p>


      </div>
    </div>
  )
}

export default Login
