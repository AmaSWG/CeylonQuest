import { useState, useEffect, useCallback } from 'react'
import './AdminDashboard.css'
import {
  DashboardIcon,
  DocumentScannerIcon,
  GroupIcon,
  WorkIcon,
  CalendarMonthIcon,
  BarChartIcon,
  NotificationsActiveIcon,
  PermIdentityIcon
} from '../../../components/Icons'
import DashboardLayout from '../../../components/DashboardLayout'
import { apiUrl, catalogUrl } from '../../../api/client'

import AdminOverviewTab from '../components/AdminOverviewTab'
import AdminProviderApplicationsTab from '../components/AdminProviderApplicationsTab'
import AdminUserManagementTab from '../components/AdminUserManagementTab'
import AdminProviderManagementTab from '../components/AdminProviderManagementTab'
import AdminBookingsOverviewTab from '../components/AdminBookingsOverviewTab'
import AdminReportsTab from '../components/AdminReportsTab'
import AdminNotificationsTab from '../components/AdminNotificationsTab'
import AdminAccountTab from '../components/AdminAccountTab'

function Toast({ message, title, onClose }) {
  const isError = message.toLowerCase().includes('error') || message.toLowerCase().includes('failed');
  const displayTitle = title || (isError ? 'Error' : 'Success');

  useEffect(() => {
    const t = setTimeout(onClose, 4000)
    return () => clearTimeout(t)
  }, [onClose])

  return (
    <div 
      className={`ad-toast ${isError ? 'ad-toast--error' : ''}`} 
      role="alert" 
      aria-live="polite"
    >
      <div className="ad-toast__icon"></div>
      <div className="ad-toast__body">
        <p className="ad-toast__title">{displayTitle}</p>
        <p className="ad-toast__msg">{message}</p>
      </div>
      <button className="ad-toast__close" onClick={onClose} aria-label="Close notification"></button>
    </div>
  )
}

function AdminDashboard({ onLogout }) {
  const [activeTab, setActiveTab] = useState('overview')
  const [toast, setToast] = useState(null)
  const [userProfile, setUserProfile] = useState(null)

  const [stats, setStats] = useState({
    totalUsers: 0,
    activeProviders: 0,
    pendingApplications: 0,
    totalBookings: 0,
    totalRevenue: 0,
  })

  const [users, setUsers] = useState([])
  const [applications, setApplications] = useState([])
  const [bookings, setBookings] = useState([])

  const [notifications, setNotifications] = useState(() => {
    try {
      const saved = localStorage.getItem('ceylonquest_admin_notifications')
      return saved ? JSON.parse(saved) : []
    } catch {
      return []
    }
  })

  const token = localStorage.getItem('authToken')

  useEffect(() => {
    try {
      localStorage.setItem('ceylonquest_admin_notifications', JSON.stringify(notifications))
    } catch {}
  }, [notifications])

  const showToast = useCallback((msg, title) => setToast({ message: msg, title }), [])

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

  const fetchUsers = useCallback(async () => {
    if (!token) return
    try {
      const resp = await fetch(apiUrl('/api/users'), {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        setUsers(await resp.json())
      }
    } catch {}
  }, [token])

  const fetchApplications = useCallback(async () => {
    if (!token) return
    try {
      const resp = await fetch(catalogUrl('/api/catalog/admin/applications'), {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        setApplications(await resp.json())
      }
    } catch {}
  }, [token])

  const fetchBookings = useCallback(async () => {
    if (!token) return
    try {
      const resp = await fetch(catalogUrl('/api/catalog/admin/bookings'), {
        headers: { Authorization: `Bearer ${token}` }
      })
      if (resp.ok) {
        setBookings(await resp.json())
      }
    } catch {}
  }, [token])

  useEffect(() => {
    fetchUsers()
    fetchApplications()
    fetchBookings()
  }, [fetchUsers, fetchApplications, fetchBookings])

  useEffect(() => {
    const totalUsers = users.length
    const activeProviders = users.filter(u => u.role === 'Provider' && u.isActive).length
    const pendingApplications = applications.filter(a => a.status === 'Pending').length
    const totalBookings = bookings.length
    const totalRevenue = bookings
      .filter(b => b.status === 'Completed' || b.status === 'Confirmed')
      .reduce((sum, b) => sum + (b.totalAmount || 0), 0)

    setStats({
      totalUsers,
      activeProviders,
      pendingApplications,
      totalBookings,
      totalRevenue,
    })
  }, [users, applications, bookings])

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
  const pendingAppsCount = applications.filter(a => a.status === 'Pending').length

  const navItems = [
    { key: 'overview',      icon: <DashboardIcon size={18} />,          label: 'Overview' },
    { key: 'applications',  icon: <DocumentScannerIcon size={18} />,    label: 'Provider Applications', badge: pendingAppsCount > 0 ? pendingAppsCount : null },
    { key: 'users',         icon: <GroupIcon size={18} />,              label: 'User Management' },
    { key: 'providers',     icon: <WorkIcon size={18} />,               label: 'Provider Management' },
    { key: 'bookings',      icon: <CalendarMonthIcon size={18} />,       label: 'Bookings' },
    { key: 'reports',       icon: <BarChartIcon size={18} />,            label: 'Reports & Analytics' },
    { key: 'notifications', icon: <NotificationsActiveIcon size={18} />, label: 'Notifications', badge: unreadNotifCount > 0 ? unreadNotifCount : null },
    { key: 'account',       icon: <PermIdentityIcon size={18} />,        label: 'Account' },
  ]

  return (
    <DashboardLayout
      roleBadge="Administrator"
      navItems={navItems}
      activeTab={activeTab}
      onSelectTab={setActiveTab}
      userProfile={userProfile}
      onLogout={handleLogout}
    >
      {toast && <Toast message={toast.message} title={toast.title} onClose={() => setToast(null)} />}

      {activeTab === 'overview' && (
        <AdminOverviewTab
          stats={stats}
          users={users}
          applications={applications}
          bookings={bookings}
          onNavigate={(tab) => setActiveTab(tab)}
        />
      )}

      {activeTab === 'applications' && (
        <AdminProviderApplicationsTab
          token={token}
          onLogout={handleLogout}
          applications={applications}
          onRefresh={fetchApplications}
          showToast={showToast}
        />
      )}

      {activeTab === 'users' && (
        <AdminUserManagementTab
          token={token}
          onLogout={handleLogout}
          users={users}
          onRefresh={fetchUsers}
          showToast={showToast}
        />
      )}

      {activeTab === 'providers' && (
        <AdminProviderManagementTab
          token={token}
          onLogout={handleLogout}
          users={users}
          applications={applications}
          onRefresh={fetchUsers}
          showToast={showToast}
        />
      )}

      {activeTab === 'bookings' && (
        <AdminBookingsOverviewTab
          bookings={bookings}
        />
      )}

      {activeTab === 'reports' && (
        <AdminReportsTab
          token={token}
          onLogout={handleLogout}
        />
      )}

      {activeTab === 'notifications' && (
        <AdminNotificationsTab
          notifications={notifications}
          onMarkAllRead={handleMarkAllNotificationsRead}
          onToggleRead={handleToggleNotificationRead}
          onClearAll={handleClearNotifications}
        />
      )}

      {activeTab === 'account' && (
        <AdminAccountTab
          token={token}
          onLogout={handleLogout}
          showToast={showToast}
          onProfileUpdate={setUserProfile}
        />
      )}
    </DashboardLayout>
  )
}

export default AdminDashboard
