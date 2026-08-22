import '../styles/Registration.css'
import { useState, useEffect } from 'react'

function SuccessToast({ message, onClose }) {
  useEffect(() => {
    const timer = setTimeout(onClose, 5000)
    return () => clearTimeout(timer)
  }, [onClose])

  return (
    <div className="reg-toast reg-toast--success" role="alert" aria-live="polite">
      <div className="reg-toast__icon">✓</div>
      <div className="reg-toast__body">
        <p className="reg-toast__title">Registration Successful!</p>
        <p className="reg-toast__msg">{message}</p>
      </div>
      <button className="reg-toast__close" onClick={onClose} aria-label="Close notification">✕</button>
    </div>
  )
}

function Registration({ onApplyAsProvider }) {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [toast, setToast] = useState(null)

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError(null)
    setToast(null)
    setLoading(true)

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

    try {
      const resp = await fetch('http://localhost:5000/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
      })

      if (resp.status === 201) {
        const body = await resp.json().catch(() => ({}))
        setToast(body.message || 'Your account has been created successfully.')
        form.reset()
      } else if (resp.status === 409) {
        setError('Email already in use.')
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
    <div className="registration-page">
      {toast && <SuccessToast message={toast} onClose={() => setToast(null)} />}

      <div className="registration-card">

        <div className="registration-header">
          <h1>CeylonQuest</h1>
          <h2>Create Your Account</h2>
          <p>Start your journey through Sri Lanka with CeylonQuest.</p>
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
                required
              />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="password"><span className="required-star">*</span> Password</label>
            <div className="field-wrap">
              <input
                type="password"
                id="password"
                name="password"
                placeholder="Enter your password"
                minLength="8"
                required
              />
              <small>Password must contain at least 8 characters.</small>
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="confirmPassword"><span className="required-star">*</span> Confirm Password</label>
            <div className="field-wrap">
              <input
                type="password"
                id="confirmPassword"
                name="confirmPassword"
                placeholder="Confirm your password"
                minLength="8"
                required
              />
            </div>
          </div>

          <button type="submit" className="register-button" id="create-account" disabled={loading}>
            {loading ? 'Creating account...' : 'Create Account'}
          </button>
        </form>

        <p className="login-link">
          Already have an account? <a href="#">Login</a>
        </p>

        <div className="provider-section">
          <div className="provider-section__divider">
            <span className="provider-section__divider-line" />
            <span className="provider-section__divider-text">For Businesses</span>
            <span className="provider-section__divider-line" />
          </div>

          <div className="provider-section__content">
            <div className="provider-section__icon">🏨</div>
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
