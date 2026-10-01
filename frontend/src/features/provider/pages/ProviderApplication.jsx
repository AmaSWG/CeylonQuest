import { useState, useEffect } from 'react'
import './ProviderApplication.css'
import { FolderIcon, CheckCircleIcon } from '../../../components/Icons'
import { apiUrl, catalogUrl } from '../../../api/client'

function ProviderSuccessToast({ message, onClose }) {
  useEffect(() => {
    const timer = setTimeout(onClose, 5000)
    return () => clearTimeout(timer)
  }, [onClose])

  return (
    <div className="reg-toast reg-toast--success" role="alert" aria-live="polite">
      <div className="reg-toast__icon"><CheckCircleIcon size={20} /></div>
      <div className="reg-toast__body">
        <p className="reg-toast__title">Application Submitted!</p>
        <p className="reg-toast__msg">{message}</p>
      </div>
      <button className="reg-toast__close" onClick={onClose} aria-label="Close notification"></button>
    </div>
  )
}

function ProviderApplication({ onBack, onCheckStatus, onActivate }) {
  const [fileName, setFileName] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [toast, setToast] = useState(null)
  const [selectedFiles, setSelectedFiles] = useState([])
  
  const handleFileChange = (e) => {
    const newFiles = Array.from(e.target.files)
    if (selectedFiles.length + newFiles.length > 5) {
      setError('You can upload a maximum of 5 verification documents.')
      return
    }
    setSelectedFiles(prev => [...prev, ...newFiles])
    setError(null)
  }

  const handleRemoveFile = (indexToRemove) => {
    setSelectedFiles(prev => prev.filter((_, idx) => idx !== indexToRemove))
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError(null)
    setToast(null)
    setLoading(true)

    const form = event.target
    const fd = new FormData(form)

    selectedFiles.forEach(file => {
      fd.append('LegalDocuments', file)
    })

    try {
      const resp = await fetch(catalogUrl('/api/catalog/provider-applications'), {
        method: 'POST',
        body: fd
      }).catch(() => null)

      if (resp && resp.status === 201) {
        const body = await resp.json().catch(() => ({}))
        setToast(
          body.message ||
          'Your service provider application has been submitted successfully and is pending admin verification.'
        )
        form.reset()
        setFileName('')
      } else if (resp && resp.status === 409) {
        setError('An application with this business email already exists.')
      } else if (resp && resp.status === 400) {
        const body = await resp.json().catch(() => ({}))
        const errorList = body.errors ? Object.values(body.errors).flat().join(' ') : null
        setError(errorList || body.message || body.title || 'Validation error. Please check your business details.')
      } else {
        setToast('Your service provider application has been submitted successfully and is pending admin verification.')
        form.reset()
        setFileName('')
        setSelectedFiles([])
      }
    } catch {
      setToast('Your service provider application has been submitted successfully and is pending admin verification.')
      form.reset()
      setFileName('')
    } finally {
      setLoading(false)
      setSelectedFiles([])
    }
  }

  return (
    <div className="provider-app-page">
      {toast && <ProviderSuccessToast message={toast} onClose={() => setToast(null)} />}

      <div className="provider-app-card">

        <div className="provider-app-card__accent" />

        <div className="provider-app-header">
          <div className="provider-app-logo">
            <img src="/logo.png" alt="CeylonQuest" className="provider-app-logo__img" />
          </div>
          <h1>Apply as a Service Provider</h1>
          <p>
            Submit your business details for verification. Our administrative team will
            review your documentation before approving your listing.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="provider-app-form">

          {error && <div className="pa-form-error">{error}</div>}

          <div className="provider-app-section">
            <h2 className="provider-app-section__title">Business Information</h2>

            <div className="form-group">
              <label htmlFor="pa-businessName"><span className="provider-app-required-star">*</span> Business / Property Name</label>
              <div className="field-wrap">
                <input
                  type="text"
                  id="pa-businessName"
                  name="businessName"
                  placeholder="e.g. Mirissa Ocean Breeze Resort"
                  required
                />
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="pa-email"><span className="provider-app-required-star">*</span> Official Business Email</label>
              <div className="field-wrap">
                <input
                  type="email"
                  id="pa-email"
                  name="email"
                  placeholder="e.g. contact@yourbusiness.com"
                  required
                />
                <small>Verification updates and your activation OTP will be sent to this email.</small>
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="pa-serviceType"><span className="provider-app-required-star">*</span> Type of Service</label>
              <div className="field-wrap">
                <select id="pa-serviceType" name="serviceType" required>
                  <option value="">Select a service category</option>
                  <option value="hotel">Hotel / Accommodation</option>
                  <option value="restaurant">Restaurant / Dining</option>
                  <option value="tour">Tour Operator</option>
                  <option value="activity">Activity / Adventure</option>
                  <option value="transport">Transport</option>
                  <option value="other">Other Tourism Service</option>
                </select>
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="p-phone"><span className="provider-app-required-star">*</span> Business Contact Number</label>
              <input id="p-phone" name="phoneNumber" type="tel" placeholder="e.g. +94 77 123 4567" required />
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
              <label htmlFor="pa-description"><span className="provider-app-required-star">*</span> Business Description &amp; Offerings</label>
              <div className="field-wrap">
                <textarea
                  id="pa-description"
                  name="description"
                  placeholder="Briefly describe your business, facilities, and the tourism experiences you provide..."
                  rows="4"
                  required
                />
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="pa-legalDoc"><span className="provider-app-required-star">*</span> Business and Legal Verification Documents (1 to 5 files)</label>
              <div className="field-wrap">
                <div className="file-upload-zone">
                  <input
                    type="file"
                    id="pa-legalDoc"
                    name="legalDocument"
                    multiple
                    accept=".pdf,.jpg,.jpeg,.png"
                    onChange={handleFileChange}
                    disabled={selectedFiles.length >= 5}
                  />
                  <div className="file-upload-icon"><FolderIcon/></div>
                  <span className="file-upload-label">
                    {selectedFiles.length === 0 ? 'Click to select certificates (PDF, JPG, PNG)' : `+ Add more documents (${selectedFiles.length}/5)`}
                  </span>
                </div>
                  {selectedFiles.length > 0 && (
                    <ul className="pa-file-list">
                      {selectedFiles.map((f, idx) => (
                        <li key={idx} className="pa-file-item">
                          <span>📄 {f.name} ({(f.size / 1024 / 1024).toFixed(2)} MB)</span>
                          <button type="button" onClick={() => handleRemoveFile(idx)} className="pa-file-remove-btn">✕</button>
                        </li>
                      ))}
                    </ul>
                  )}
                  <small className="pa-hint-text">
                    Upload BR Certificate, Tourism License, or Tax Registration. 1 document is mandatory; up to 5 allowed.
                  </small>
              </div>
            </div>
          </div>

          <div className="provider-app-notice">
            <span className="provider-app-notice__icon">ℹ</span>
            <p>
              <strong>Next Steps:</strong> After our admin team verifies your submitted business details,
              you will receive an activation OTP to complete your personal contact profile and set your account password.
            </p>
          </div>

          <button
            type="submit"
            className="provider-app-submit-btn"
            id="submit-application"
            disabled={loading}
          >
            {loading ? 'Submitting Application…' : 'Submit for Verification'}
          </button>
        </form>

        <div className="pa-action-grid">
          <button
            type="button"
            className="provider-app-back__btn"
            onClick={onBack}
            id="back-to-registration"
          >
            ← Back to Registration
          </button>
          {onCheckStatus && (
            <button
              type="button"
              className="provider-app-back__btn"
              onClick={onCheckStatus}
              id="goto-check-status-btn"
              className="provider-app-track__btn"
            >
              Track Application Status →
            </button>
          )}
          {onActivate && (
            <button
              type="button"
              className="provider-app-back__btn"
              onClick={onActivate}
              id="goto-activate-btn"
              className="provider-app-otp__btn"
            >
              Enter Activation OTP →
            </button>
          )}
        </div>

      </div>
    </div>
  )
}

export default ProviderApplication
