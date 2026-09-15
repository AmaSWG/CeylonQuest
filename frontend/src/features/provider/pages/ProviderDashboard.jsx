import { useState, useEffect, useCallback } from 'react'
import './ProviderDashboard.css'
import {
  DashboardIcon,
  StorefrontIcon,
  KitesurfingIcon,
  CalendarMonthIcon,
  NotificationsActiveIcon,
  PermIdentityIcon,
  BarChartIcon
} from '../../../components/Icons'
import InventoryReportView from '../../../components/InventoryReportView'
import DashboardLayout from '../../../components/DashboardLayout'
import { apiUrl, catalogUrl } from '../../../api/client'

import ProviderOverviewTab from '../components/ProviderOverviewTab'
import ProviderBusinessProfileTab from '../components/ProviderBusinessProfileTab'
import ProviderListingsTab from '../components/ProviderListingsTab'
import ProviderBookingsTab from '../components/ProviderBookingsTab'
import ProviderNotificationsTab from '../components/ProviderNotificationsTab'
import ProviderAccountTab from '../components/ProviderAccountTab'

function Toast({ message, title = 'Success', onClose }) {
  useEffect(() => {
    const t = setTimeout(onClose, 4000)
    return () => clearTimeout(t)
  }, [onClose])

  return (
    <div className="pd-toast" role="alert" aria-live="polite">
      <div className="pd-toast__icon"></div>
      <div className="pd-toast__body">
        <p className="pd-toast__title">{title}</p>
        <p className="pd-toast__msg">{message}</p>
      </div>
      <button className="pd-toast__close" onClick={onClose} aria-label="Close"></button>
    </div>
  )
}

function ProviderDashboard({ onLogout }) {
  const [activeTab, setActiveTab] = useState('overview')
  const [toast, setToast] = useState(null)
  const [providerInfo, setProviderInfo] = useState(null)
  const [services, setServices] = useState([])
  const [userProfile, setUserProfile] = useState(null)

  const [bookings, setBookings] = useState(() => {
    try {
      const saved = localStorage.getItem('ceylonquest_provider_bookings')
      return saved ? JSON.parse(saved) : []
    } catch {
      return []
    }
  })

  const [notifications, setNotifications] = useState(() => {
    try {
      const saved = localStorage.getItem('ceylonquest_provider_notifications')
      return saved ? JSON.parse(saved) : []
    } catch {
      return []
    }
  })

  const token = localStorage.getItem('authToken')

  useEffect(() => {
    try {
      localStorage.setItem('ceylonquest_provider_bookings', JSON.stringify(bookings))
    } catch {}
  }, [bookings])

  useEffect(() => {
    try {
      localStorage.setItem('ceylonquest_provider_notifications', JSON.stringify(notifications))
    } catch {}
  }, [notifications])

  const showToast = useCallback((msg) => setToast(msg), [])

  const fetchProviderInfo = useCallback(async () => {
    if (!token) return
    try {
      const resp = await fetch(catalogUrl('/api/catalog/provider/profile'), {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        setProviderInfo(await resp.json())
      }
    } catch {}
  }, [token])

  const serviceTypeLower = (providerInfo?.serviceType || '').toLowerCase()
  const isHotel =
    serviceTypeLower.includes('hotel') ||
    serviceTypeLower.includes('accommodat') ||
    serviceTypeLower.includes('villa') ||
    serviceTypeLower.includes('resort') ||
    serviceTypeLower.includes('room')
  const isRestaurant =
    serviceTypeLower.includes('restaurant') ||
    serviceTypeLower.includes('dining') ||
    serviceTypeLower.includes('dinner') ||
    serviceTypeLower.includes('food') ||
    serviceTypeLower.includes('cafe') ||
    serviceTypeLower.includes('catering')

  const catalogEndpoint = isHotel
    ? '/api/catalog/accommodation-listings'
    : isRestaurant
    ? '/api/catalog/restaurant-listings'
    : '/api/catalog/activity-listings'

  const fetchServices = useCallback(async () => {
    if (!token || !providerInfo) return
    try {
      const resp = await fetch(catalogUrl(catalogEndpoint), {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        const data = await resp.json()
        setServices(data)
      }
    } catch (err) {
      console.error('Failed to load listings', err)
    }
  }, [token, catalogEndpoint, providerInfo])

  useEffect(() => {
    fetchProviderInfo()
  }, [fetchProviderInfo])

  useEffect(() => {
    fetchServices()
  }, [fetchServices])

  const fetchUserProfile = useCallback(async () => {
    if (!token) return
    try {
      const resp = await fetch(apiUrl('/api/users/me'), {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        setUserProfile(await resp.json())
      }
    } catch {}
  }, [token])

  useEffect(() => {
    fetchUserProfile()
  }, [fetchUserProfile])

  const handleUpdateBookingStatus = (bookingId, newStatus) => {
    setBookings(prev => prev.map(b => b.id === bookingId ? { ...b, status: newStatus } : b))
    showToast(`Booking #${bookingId} marked as ${newStatus}.`)
  }

  const handleMarkAllNotificationsRead = () => {
    setNotifications(prev => prev.map(n => ({ ...n, read: true })))
    showToast('All notifications marked as read.')
  }

  const handleToggleNotificationRead = (notifId) => {
    setNotifications(prev => prev.map(n => n.id === notifId ? { ...n, read: !n.read } : n))
  }

  const handleClearNotifications = () => {
    setNotifications([])
    showToast('Notifications cleared.')
  }

  const handleLogout = useCallback(() => {
    localStorage.removeItem('authToken')
    localStorage.removeItem('userRole')
    onLogout && onLogout()
  }, [onLogout])

  const unreadNotifCount = notifications.filter(n => !n.read).length

  const navItems = [
    { key: 'overview',      icon: <DashboardIcon size={18} />,          label: 'Overview' },
    { key: 'business',      icon: <StorefrontIcon size={18} />,         label: 'Business Profile' },
    {
      key: 'services',
      icon: <KitesurfingIcon size={18} />,
      label: isHotel ? 'Rooms & Accommodations'
           : isRestaurant ? 'Menu & Dining'
           : 'Activities & Services'
    },
    { key: 'bookings',      icon: <CalendarMonthIcon size={18} />,       label: 'Bookings' },
    { key: 'reports',       icon: <BarChartIcon size={18} />,            label: 'Inventory Reports' },
    { key: 'notifications', icon: <NotificationsActiveIcon size={18} />, label: 'Notifications', badge: unreadNotifCount > 0 ? unreadNotifCount : null },
    { key: 'account',       icon: <PermIdentityIcon size={18} />,        label: 'Account' }
  ]

  return (
    <DashboardLayout
      roleBadge="Provider Portal"
      navItems={navItems}
      activeTab={activeTab}
      onSelectTab={setActiveTab}
      userProfile={userProfile}
      onLogout={handleLogout}
    >
      {toast && <Toast message={toast} onClose={() => setToast(null)} />}

      {activeTab === 'overview' && (
        <ProviderOverviewTab
          providerInfo={providerInfo}
          services={services}
          bookings={bookings}
          notifications={notifications}
          onNavigate={(tab) => setActiveTab(tab)}
        />
      )}

      {activeTab === 'business' && (
        <ProviderBusinessProfileTab
          token={token}
          onLogout={handleLogout}
          providerInfo={providerInfo}
          onUpdateSuccess={(updated) => setProviderInfo(updated)}
          showToast={showToast}
        />
      )}

      {activeTab === 'services' && (
        <ProviderListingsTab
          token={token}
          onLogout={handleLogout}
          services={services}
          isHotel={isHotel}
          isRestaurant={isRestaurant}
          catalogEndpoint={catalogEndpoint}
          onRefreshServices={fetchServices}
          showToast={showToast}
        />
      )}

      {activeTab === 'bookings' && (
        <ProviderBookingsTab
          bookings={bookings}
          onUpdateBookingStatus={handleUpdateBookingStatus}
        />
      )}

      {activeTab === 'reports' && (
        <InventoryReportView
          token={token}
          onLogout={handleLogout}
          isAdmin={false}
        />
      )}

      {activeTab === 'notifications' && (
        <ProviderNotificationsTab
          notifications={notifications}
          onMarkAllRead={handleMarkAllNotificationsRead}
          onToggleRead={handleToggleNotificationRead}
          onClearAll={handleClearNotifications}
        />
      )}

      {activeTab === 'account' && (
        <ProviderAccountTab
          token={token}
          onLogout={handleLogout}
          showToast={showToast}
          onProfileUpdate={setUserProfile}
        />
      )}
    </DashboardLayout>
  )
}

export default ProviderDashboard
