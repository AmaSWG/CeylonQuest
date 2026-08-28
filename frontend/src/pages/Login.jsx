import { useState, useEffect } from 'react'
import '../styles/Login.css'

function LoginSuccessToast({ message, onClose }) {
  useEffect(() => {
    const timer = setTimeout(onClose, 4000)
    return () => clearTimeout(timer)
  }, [onClose])

  return (
    <div className="login-toast login-toast--success" role="alert">
      <div className="login-toast__icon">✓</div>
      <div className="login-toast__body">
        <p className="login-toast__title">Welcome back!</p>
        <p className="login-toast__msg">{message}</p>
      </div>
      <button className="login-toast__close" onClick={onClose} aria-label="Close notification">✕</button>
    </div>
  )
}

function Login({ onLoginSuccess, onBack, onForgotPassword, onHome, onActivateProvider }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [toast, setToast] = useState(null)
  const [showPassword, setShowPassword] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError(null)
    setLoading(true)

    try {
      const resp = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      })

      if (resp.ok) {
        const data = await resp.json()
        const token = data.accessToken || data.token
        const role  = data.role

        if (token) localStorage.setItem('authToken', token)
        if (role)  localStorage.setItem('userRole', role)

        setToast('Login successful! Redirecting...')
        setTimeout(() => {
          onLoginSuccess && onLoginSuccess(role)
        }, 800)
      } else if (resp.status === 401) {
        const data = await resp.json().catch(() => ({}))
        setError(data.message || 'Invalid email or password.')
      } else if (resp.status === 400) {
        const data = await resp.json().catch(() => ({}))
        setError(data.message || 'Please check your login details.')
      } else {
        setError('Login failed. Please try again later.')
      }
    } catch {
      setError('Network error. Please check your connection.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="login-page">
      {toast && <LoginSuccessToast message={toast} onClose={() => setToast(null)} />}

      <div className="login-card">

        {onHome && (
          <button
            type="button"
            className="login-home-btn"
            id="login-home-btn"
            onClick={onHome}
            aria-label="Go back to home page"
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
              <polyline points="9 22 9 12 15 12 15 22" />
            </svg>
            Home
          </button>
        )}

        <div className="login-header">
          <h1>CeylonQuest</h1>
          <h2>Welcome Back</h2>
          <p>Sign in to continue your journey through Sri Lanka.</p>
        </div>

        <form onSubmit={handleSubmit} className="login-form">

          {error && (
            <div className="login-error" role="alert">
              {error}
            </div>
          )}

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
                placeholder="Enter your email"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>
          </div>

          <div className="login-field">
            <div className="login-label-row">
              <label htmlFor="login-password">Password</label>
              <button
                type="button"
                className="login-forgot-btn"
                onClick={() => onForgotPassword && onForgotPassword()}
                tabIndex={0}
              >
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
                value={password}
                onChange={(e) => setPassword(e.target.value)}
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
            id="login-button"
            disabled={loading}
          >
            {loading ? 'Logging in…' : 'Login'}
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

        {onActivateProvider && (
          <div style={{ marginTop: '16px', paddingTop: '14px', borderTop: '1px dashed #e2e8f0', textAlign: 'center' }}>
            <p style={{ margin: 0, fontSize: '13px', color: '#64748b' }}>
              Approved Service Provider?{' '}
              <button
                type="button"
                onClick={onActivateProvider}
                id="login-activate-provider-btn"
                style={{ background: 'none', border: 'none', color: '#123b5d', fontWeight: '700', cursor: 'pointer', padding: 0, textDecoration: 'underline' }}
              >
                Activate Account with OTP →
              </button>
            </p>
          </div>
        )}

      </div>
    </div>
  )
}

export default Login
