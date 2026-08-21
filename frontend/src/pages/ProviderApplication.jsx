import { useState } from 'react'
import '../styles/ProviderApplication.css'

function ProviderApplication({ onBack }) {
  const [fileName, setFileName] = useState('')

  const handleFileChange = (e) => {
    const file = e.target.files[0]
    setFileName(file ? file.name : '')
  }

  const handleSubmit = (event) => {
    event.preventDefault()
    console.log('Provider application submitted')
  }

  return (
    <div className="provider-app-page">
      <div className="provider-app-card">

        {/* Top accent bar */}
        <div className="provider-app-card__accent" />

        {/* Header */}
        <div className="provider-app-header">
          <div className="provider-app-logo">
            <span className="provider-app-logo__brand">CeylonQuest</span>
            <span className="provider-app-logo__badge">Provider Portal</span>
          </div>
          <h1>Apply as a Service Provider</h1>
          <p>
            List your hotel, restaurant, tour, activity, or other tourism service
            on CeylonQuest. Your application will be reviewed by our admin team
            before your business is listed.
          </p>
          <div className="provider-app-status-badge">
            <span className="provider-app-status-badge__icon">⏳</span>
            Application → Pending Review → Admin Approval → Listed
          </div>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="provider-app-form">

          {/* ── Personal / Account details ── */}
          <div className="provider-app-section">
            <h2 className="provider-app-section__title">Your Account Details</h2>

            <div className="form-row">
              <div className="form-group">
                <label htmlFor="pa-firstName"><span className="provider-app-required-star">*</span> First Name</label>
                <div className="field-wrap">
                  <input
                    type="text"
                    id="pa-firstName"
                    name="firstName"
                    placeholder="Enter your first name"
                    required
                  />
                </div>
              </div>
              <div className="form-group">
                <label htmlFor="pa-lastName"><span className="provider-app-required-star">*</span> Last Name</label>
                <div className="field-wrap">
                  <input
                    type="text"
                    id="pa-lastName"
                    name="lastName"
                    placeholder="Enter your last name"
                    required
                  />
                </div>
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="pa-email"><span className="provider-app-required-star">*</span> Email Address</label>
              <div className="field-wrap">
                <input
                  type="email"
                  id="pa-email"
                  name="email"
                  placeholder="Enter your email"
                  required
                />
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="pa-phone"><span className="provider-app-required-star">*</span> Phone Number</label>
              <div className="field-wrap">
                <input
                  type="tel"
                  id="pa-phone"
                  name="phoneNumber"
                  placeholder="Enter your phone number"
                  required
                />
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label htmlFor="pa-password"><span className="provider-app-required-star">*</span> Password</label>
                <div className="field-wrap">
                  <input
                    type="password"
                    id="pa-password"
                    name="password"
                    placeholder="Create a password"
                    minLength="8"
                    required
                  />
                  <small>Minimum 8 characters.</small>
                </div>
              </div>
              <div className="form-group">
                <label htmlFor="pa-confirmPassword"><span className="provider-app-required-star">*</span> Confirm Password</label>
                <div className="field-wrap">
                  <input
                    type="password"
                    id="pa-confirmPassword"
                    name="confirmPassword"
                    placeholder="Confirm your password"
                    minLength="8"
                    required
                  />
                </div>
              </div>
            </div>
          </div>

          {/* ── Business details ── */}
          <div className="provider-app-section">
            <h2 className="provider-app-section__title">Business Details</h2>

            <div className="form-group">
              <label htmlFor="pa-businessName"><span className="provider-app-required-star">*</span> Business Name</label>
              <div className="field-wrap">
                <input
                  type="text"
                  id="pa-businessName"
                  name="businessName"
                  placeholder="Enter your business or property name"
                  required
                />
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="pa-serviceType"><span className="provider-app-required-star">*</span> Type of Service</label>
              <div className="field-wrap">
                <select id="pa-serviceType" name="serviceType" required>
                  <option value="">Select a service type</option>
                  <option value="hotel">Hotel / Accommodation</option>
                  <option value="restaurant">Restaurant / Dining</option>
                  <option value="tour">Tour Operator</option>
                  <option value="activity">Activity / Adventure</option>
                  <option value="transport">Transport</option>
                  <option value="other">Other</option>
                </select>
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="pa-location"><span className="provider-app-required-star">*</span> Business Location</label>
              <div className="field-wrap">
                <input
                  type="text"
                  id="pa-location"
                  name="location"
                  placeholder="City, District or Region in Sri Lanka"
                  required
                />
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="pa-description"><span className="provider-app-required-star">*</span> Description</label>
              <div className="field-wrap">
                <textarea
                  id="pa-description"
                  name="description"
                  placeholder="Briefly describe your business and the services you offer..."
                  rows="4"
                  required
                />
              </div>
            </div>

            {/* Legal document upload */}
            <div className="form-group">
              <label htmlFor="pa-legalDoc"><span className="provider-app-required-star">*</span> Legal Document</label>
              <div className="field-wrap">
                <div className="file-upload-zone">
                  <input
                    type="file"
                    id="pa-legalDoc"
                    name="legalDocument"
                    accept=".pdf,.jpg,.jpeg,.png"
                    onChange={handleFileChange}
                    required
                  />
                  <div className="file-upload-icon">📄</div>
                  {fileName ? (
                    <span className="file-upload-label">{fileName}</span>
                  ) : (
                    <>
                      <span className="file-upload-label">Click to upload</span>
                      <span className="file-upload-hint">PDF, JPEG, or PNG accepted</span>
                    </>
                  )}
                </div>
                <small>
                  Upload a business registration certificate, trade licence, or
                  equivalent legal document.
                </small>
              </div>
            </div>
          </div>

          {/* Info notice */}
          <div className="provider-app-notice">
            <span className="provider-app-notice__icon">ℹ️</span>
            <p>
              Your application will be reviewed by the CeylonQuest admin team.
              You will be notified by email once your application has been approved or rejected.
              Approval may take 2–5 business days.
            </p>
          </div>

          {/* Submit */}
          <button type="submit" className="provider-app-submit-btn" id="submit-application">
            Submit Application
          </button>
        </form>

        {/* Back link */}
        <p className="provider-app-back">
          <button
            type="button"
            className="provider-app-back__btn"
            onClick={onBack}
            id="back-to-registration"
          >
            ← Back to Registration
          </button>
        </p>

      </div>
    </div>
  )
}

export default ProviderApplication
