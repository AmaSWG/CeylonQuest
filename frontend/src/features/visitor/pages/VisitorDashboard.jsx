import { useState, useEffect, useCallback } from 'react'
import './VisitorDashboard.css'
import {
  PermIdentityIcon,
  CalendarMonthIcon,
  SettingsIcon,
  PublicIcon,
  CheckCircleIcon
} from '../../../components/Icons'
import DashboardLayout from '../../../components/DashboardLayout'
import VisitorExploreTab from '../components/VisitorExploreTab'
import VisitorProfileTab from '../components/VisitorProfileTab'
import { apiUrl } from '../../../api/client'

function SuccessToast({ toast, onClose }) {
  useEffect(() => {
    const t = setTimeout(onClose, 4000)
    return () => clearTimeout(t)
  }, [onClose])

  const title = typeof toast === 'object' ? (toast.title || 'Success') : 'Success'
  const message = typeof toast === 'object' ? toast.message : toast

  return (
    <div className="vd-toast" role="alert" aria-live="polite">
      <div className="vd-toast__icon"><CheckCircleIcon /></div>
      <div className="vd-toast__body">
        <p className="vd-toast__title">{title}</p>
        <p className="vd-toast__msg">{message}</p>
      </div>
      <button className="vd-toast__close" onClick={onClose} aria-label="Close"></button>
    </div>
  )
}

function VisitorDashboard({ onLogout }) {
  const [activePage, setActivePage] = useState('profile')
  const [profile, setProfile]       = useState(null)
  const [loadError, setLoadError]   = useState(null)
  const [loading, setLoading]       = useState(true)
  const [toast, setToast]           = useState(null)

  const token = localStorage.getItem('authToken')

  const fetchProfile = useCallback(async () => {
    const currentToken = localStorage.getItem('authToken')
    if (!currentToken) {
      onLogout && onLogout()
      return
    }
    setLoading(true)
    setLoadError(null)
    try {
      const resp = await fetch(apiUrl('/api/users/me'), {
        headers: { Authorization: `Bearer ${currentToken}` }
      })
      if (resp.ok) {
        const data = await resp.json()
        setProfile(data)
      } else if (resp.status === 401) {
        setLoadError('Session expired or unauthorized. Please log in again.')
        setTimeout(() => { onLogout && onLogout() }, 2000)
      } else {
        setLoadError('Failed to load profile. Please try again.')
      }
    } catch {
      setLoadError('Network error. Please check your connection.')
    } finally {
      setLoading(false)
    }
  }, [onLogout])

  useEffect(() => {
    fetchProfile()
  }, [fetchProfile])

  const handleLogout = useCallback(() => {
    localStorage.removeItem('authToken')
    localStorage.removeItem('userRole')
    onLogout && onLogout()
  }, [onLogout])

  const showToast = useCallback((msg, title) => {
    setToast(title ? { title, message: msg } : msg)
  }, [])

  const navItems = [
    { key: 'profile',  icon: <PermIdentityIcon size={18} />,  label: 'My Profile' },
    { key: 'explore',  icon: <PublicIcon size={18} />,        label: 'Explore and Search' },
    { key: 'bookings', icon: <CalendarMonthIcon size={18} />, label: 'My Bookings', disabled: true, comingSoon: true },
    { key: 'settings', icon: <SettingsIcon size={18} />,      label: 'Settings',    disabled: true, comingSoon: true }
  ]

  return (
    <DashboardLayout
      roleBadge="Visitor"
      navItems={navItems}
      activeTab={activePage}
      onSelectTab={setActivePage}
      userProfile={profile}
      onLogout={handleLogout}
    >
      {toast && <SuccessToast toast={toast} onClose={() => setToast(null)} />}

      {activePage === 'explore' ? (
        <VisitorExploreTab showToast={showToast} />
      ) : (
        <VisitorProfileTab
          profile={profile}
          loading={loading}
          loadError={loadError}
          token={token}
          onProfileUpdated={(updated) => setProfile(updated)}
          showToast={(msg) => showToast(msg, 'Profile Updated')}
        />
      )}
    </DashboardLayout>
  )
}

export default VisitorDashboard