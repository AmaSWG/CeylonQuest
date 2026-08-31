import '../styles/Registration.css'
import { useState, useEffect } from 'react'
import { HomeIcon, VisibilityIcon, VisibilityOffIcon } from '../components/Icons'
import { apiUrl } from '../api/client'

const PASSWORD_REQUIREMENTS = 'Password must be at least 8 characters long and include an uppercase letter, a lowercase letter, a number, and a special character.'
const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z\s]).{8,}$/

function SuccessToast({ message, onClose }) {
  useEffect(() => {
    const timer = setTimeout(onClose, 5000)
    return () => clearTimeout(timer)
  }, [onClose])

  return (
    <div className="reg-toast reg-toast--success" role="alert" aria-live="polite">
      <div className="reg-toast__icon"></div>
      <div className="reg-toast__body">
        <p className="reg-toast__title">Registration Successful!</p>
        <p className="reg-toast__msg">{message}</p>
      </div>
      <button className="reg-toast__close" onClick={onClose} aria-label="Close notification"></button>
    </div>
  )
}

function validateRegistration(data) {
  if (!data.firstName) return 'First name is required.'
  if (!data.lastName) return 'Last name is required.'
  if (!data.email) return 'Email is required.'
  if (!data.phoneNumber) return 'Phone number is required.'
  if (!data.nationality) return 'Nationality is required.'
  if (!PASSWORD_PATTERN.test(data.password)) return PASSWORD_REQUIREMENTS
  if (data.password !== data.confirmPassword) return 'Passwords do not match.'
  return null
}

function Registration({ onApplyAsProvider, onCheckStatus, onLogin, onHome, onActivateProvider }) {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [toast, setToast] = useState(null)
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError(null)
    setToast(null)

    const form = event.target
    const data = {
      firstName: form.firstName.value.trim(),
      lastName: form.lastName.value.trim(),
      email: form.email.value.trim(),
      phoneNumber: form.phoneNumber.value.trim(),
      nationality: form.nationality.value.trim(),
      password: form.password.value,
      confirmPassword: form.confirmPassword.value,
      registrationType: 'Visitor'
    }

    const validationError = validateRegistration(data)
    if (validationError) {
      setError(validationError)
      return
    }

    setLoading(true)

    try {
      const resp = await fetch(apiUrl('/api/auth/register'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
      })

      if (resp.status === 201) {
        const body = await resp.json().catch(() => ({}))
        setToast(body.message || 'Your account has been created successfully.')
        form.reset()
        setTimeout(() => { onLogin && onLogin() }, 1800)
      } else if (resp.status === 409) {
        setError('Email already in use.')
      } else if (resp.status === 400) {
        const body = await resp.json().catch(() => ({}))
        const firstFieldError = body.errors && Object.values(body.errors).flat()[0]
        setError(firstFieldError || body.message || body.title || 'Validation error. Check your input.')
      } else {
        setError('Server error. Please try again later.')
      }
    } catch {
      setError('Network error. Please check your connection.')
    } finally {
      setLoading(false)
    }
  }


  return (
    <div className="registration-page">
      {toast && <SuccessToast message={toast} onClose={() => setToast(null)} />}

      <div className="registration-card">

        {onHome && (
          <button
            type="button"
            className="reg-home-btn"
            id="registration-home-btn"
            onClick={onHome}
            aria-label="Go back to home page"
          >
            <HomeIcon size={14} />
            Home
          </button>
        )}

        <div className="registration-header">
          <img src="/logo.png" alt="CeylonQuest" className="registration-header__logo-img" />
        </div>

        <form onSubmit={handleSubmit}>
          {error && <div className="form-error">{error}</div>}

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="firstName"><span className="required-star">*</span> First Name</label>
              <div className="field-wrap">
                <input
                  type="text"
                  id="firstName"
                  name="firstName"
                  placeholder="Enter your first name"
                  autoComplete="given-name"
                  required
                />
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="lastName"><span className="required-star">*</span> Last Name</label>
              <div className="field-wrap">
                <input
                  type="text"
                  id="lastName"
                  name="lastName"
                  placeholder="Enter your last name"
                  autoComplete="family-name"
                  required
                />
              </div>
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="email"><span className="required-star">*</span> Email Address</label>
            <div className="field-wrap">
              <input
                type="email"
                id="email"
                name="email"
                placeholder="Enter your email"
                autoComplete="email"
                required
              />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="phoneNumber"><span className="required-star">*</span> Phone Number</label>
            <div className="field-wrap">
              <input
                type="tel"
                id="phoneNumber"
                name="phoneNumber"
                placeholder="Enter your phone number"
                autoComplete="tel"
                required
              />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="nationality"><span className="required-star">*</span> Nationality</label>
            <div className="field-wrap">
              <input
                type="text"
                id="nationality"
                name="nationality"
                placeholder="Enter your nationality"
                autoComplete="country-name"
                required
              />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="password"><span className="required-star">*</span> Password</label>
            <div className="field-wrap">
              <div className="password-input-wrap">
                <input
                  type={showPassword ? 'text' : 'password'}
                  id="password"
                  name="password"
                  placeholder="Enter your password"
                  minLength="8"
                  autoComplete="new-password"
                  required
                />
                <button
                  type="button"
                  className="password-toggle-btn"
                  onClick={() => setShowPassword(v => !v)}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                  tabIndex="-1"
                >
                  {showPassword ? <VisibilityOffIcon size={18} /> : <VisibilityIcon size={18} />}
                </button>
              </div>
              <small>Must be 8+ characters with an uppercase letter, a lowercase letter, a number, and a special character.</small>
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="confirmPassword"><span className="required-star">*</span> Confirm Password</label>
            <div className="field-wrap">
              <div className="password-input-wrap">
                <input
                  type={showConfirmPassword ? 'text' : 'password'}
                  id="confirmPassword"
                  name="confirmPassword"
                  placeholder="Confirm your password"
                  minLength="8"
                  autoComplete="new-password"
                  required
                />
                <button
                  type="button"
                  className="password-toggle-btn"
                  onClick={() => setShowConfirmPassword(v => !v)}
                  aria-label={showConfirmPassword ? 'Hide password' : 'Show password'}
                  tabIndex="-1"
                >
                  {showConfirmPassword ? <VisibilityOffIcon size={18} /> : <VisibilityIcon size={18} />}
                </button>
              </div>
            </div>
          </div>

          <button type="submit" className="register-button" id="create-account" disabled={loading}>
            {loading ? 'Creating account...' : 'Create Account'}
          </button>
        </form>

        <p className="login-link">
          Already have an account? <a href="#" onClick={(e) => { e.preventDefault(); onLogin && onLogin(); }}>Login</a>
        </p>

        <div className="provider-section">
          <div className="provider-section__divider">
            <span className="provider-section__divider-line" />
            <span className="provider-section__divider-text">For Businesses</span>
            <span className="provider-section__divider-line" />
          </div>

          <div className="provider-section__content">
            <h3 className="provider-section__title">
              Are you a tourism service provider?
            </h3>
            <p className="provider-section__desc">
              Want to list your hotel, restaurant, tour, activity, or other
              tourism service on CeylonQuest?
            </p>
            <button
              type="button"
              className="provider-apply-button"
              id="apply-as-provider"
              onClick={onApplyAsProvider}
            >
              Apply as a Service Provider
            </button>
            {onCheckStatus && (
              <p style={{ marginTop: '12px', fontSize: '13px', color: '#64748b' }}>
                Already applied?{' '}
                <a
                  href="#"
                  id="check-provider-status-link"
                  style={{ color: '#123b5d', fontWeight: '700', textDecoration: 'underline' }}
                  onClick={(e) => { e.preventDefault(); onCheckStatus(); }}
                >
                  Check Application Status →
                </a>
              </p>
            )}
            {onActivateProvider && (
              <p style={{ marginTop: '6px', fontSize: '13px', color: '#64748b' }}>
                Approved Provider?{' '}
                <a
                  href="#"
                  id="activate-provider-link"
                  style={{ color: '#b8860b', fontWeight: '700', textDecoration: 'underline' }}
                  onClick={(e) => { e.preventDefault(); onActivateProvider(); }}
                >
                  Activate Account with OTP →
                </a>
              </p>
            )}
            <p className="provider-section__note">
              Applications are reviewed and approved by our admin team.
            </p>
          </div>
        </div>

      </div>
    </div>
  )
}

export default Registration
