import { useState, useEffect, useCallback } from 'react'

import './ProviderDashboard.css'

import {
  DashboardIcon,
  StorefrontIcon,
  KitesurfingIcon,
  CalendarMonthIcon,
  NotificationsActiveIcon,
  PermIdentityIcon,
  BarChartIcon,
  CheckCircleIcon,
  CancelIcon
} from '../../../components/Icons'

import InventoryReportView from '../../../components/InventoryReportView'
import DashboardLayout from '../../../components/DashboardLayout'

import {
  apiUrl,
  catalogUrl,
  bookingUrl
} from '../../../api/client'

import ProviderOverviewTab from '../components/ProviderOverviewTab'
import ProviderBusinessProfileTab from '../components/ProviderBusinessProfileTab'
import ProviderListingsTab from '../components/ProviderListingsTab'
import ProviderBookingsTab from '../components/ProviderBookingsTab'
import ProviderNotificationsTab from '../components/ProviderNotificationsTab'
import ProviderAccountTab from '../components/ProviderAccountTab'


/* =========================================================
   Toast
   ========================================================= */

function Toast({
  message,
  title = 'Success',
  onClose
}) {
  const isError =
    typeof message === 'string' &&
    (
      message.toLowerCase().includes('error') ||
      message.toLowerCase().includes('failed') ||
      title.toLowerCase().includes('error')
    )

  useEffect(() => {
    const timer = setTimeout(onClose, 4000)

    return () => clearTimeout(timer)
  }, [onClose])

  return (
    <div
      className={`pd-toast ${isError
          ? 'pd-toast--error'
          : 'pd-toast--success'
        }`}
      role="alert"
      aria-live="polite"
    >
      <div className="pd-toast__icon">
        {isError ? (
          <CancelIcon size={20} />
        ) : (
          <CheckCircleIcon size={20} />
        )}
      </div>

      <div className="pd-toast__body">
        <p className="pd-toast__title">
          {title}
        </p>

        <p className="pd-toast__msg">
          {message}
        </p>
      </div>

      <button
        className="pd-toast__close"
        onClick={onClose}
        aria-label="Close notification"
      />
    </div>
  )
}


/* =========================================================
   Provider Dashboard
   ========================================================= */

function ProviderDashboard({ onLogout }) {
  const [activeTab, setActiveTab] =
    useState('overview')

  const [toast, setToast] =
    useState(null)

  const [providerInfo, setProviderInfo] =
    useState(null)

  const [services, setServices] =
    useState([])

  const [userProfile, setUserProfile] =
    useState(null)


  /* =======================================================
     Story 9.1 - Provider Bookings
     ======================================================= */

  const [bookings, setBookings] =
    useState([])

  const [bookingsLoading, setBookingsLoading] =
    useState(false)

  const [bookingsError, setBookingsError] =
    useState(null)


  /* =======================================================
     Notifications
     ======================================================= */

  const [notifications, setNotifications] =
    useState(() => {
      try {
        const saved =
          localStorage.getItem(
            'ceylonquest_provider_notifications'
          )

        return saved
          ? JSON.parse(saved)
          : []
      } catch {
        return []
      }
    })


  /* =======================================================
     Authentication
     ======================================================= */

  const token =
    localStorage.getItem('authToken')


  /* =======================================================
     Save notifications
     ======================================================= */

  useEffect(() => {
    try {
      localStorage.setItem(
        'ceylonquest_provider_notifications',
        JSON.stringify(notifications)
      )
    } catch {
      // Ignore localStorage errors
    }
  }, [notifications])


  /* =======================================================
     Toast helper
     ======================================================= */

  const showToast = useCallback(
    (message) => {
      setToast(message)
    },
    []
  )


  /* =======================================================
     Logout
     ======================================================= */

  const handleLogout = useCallback(() => {
    localStorage.removeItem('authToken')
    localStorage.removeItem('userRole')

    if (onLogout) {
      onLogout()
    }
  }, [onLogout])


  /* =======================================================
     Provider Profile
     ======================================================= */

  const fetchProviderInfo =
    useCallback(async () => {
      const currentToken =
        localStorage.getItem('authToken')

      if (!currentToken) {
        return
      }

      try {
        const response = await fetch(
          catalogUrl(
            '/api/catalog/provider/profile'
          ),
          {
            headers: {
              Authorization:
                `Bearer ${currentToken}`
            }
          }
        )

        if (response.status === 401) {
          handleLogout()
          return
        }

        if (response.ok) {
          const data =
            await response.json()

          setProviderInfo(data)
        }
      } catch (error) {
        console.error(
          'Failed to load provider information:',
          error
        )
      }
    }, [handleLogout])


  /* =======================================================
     Determine Provider Service Type
     ======================================================= */

  const serviceTypeLower =
    (
      providerInfo?.serviceType || ''
    ).toLowerCase()

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


  /* =======================================================
     Provider Catalog Endpoint
     ======================================================= */

  const catalogEndpoint =
    isHotel
      ? '/api/catalog/accommodation-listings'
      : isRestaurant
        ? '/api/catalog/restaurant-listings'
        : '/api/catalog/activity-listings'


  /* =======================================================
     Fetch Provider Services
     ======================================================= */

  const fetchServices =
    useCallback(async () => {
      const currentToken =
        localStorage.getItem('authToken')

      if (
        !currentToken ||
        !providerInfo
      ) {
        return
      }

      try {
        const response = await fetch(
          catalogUrl(catalogEndpoint),
          {
            headers: {
              Authorization:
                `Bearer ${currentToken}`
            }
          }
        )

        if (response.status === 401) {
          handleLogout()
          return
        }

        if (response.ok) {
          const data =
            await response.json()

          setServices(
            Array.isArray(data)
              ? data
              : []
          )
        }
      } catch (error) {
        console.error(
          'Failed to load listings:',
          error
        )
      }
    }, [
      catalogEndpoint,
      providerInfo,
      handleLogout
    ])


  /* =======================================================
     Fetch User Profile
     ======================================================= */

  const fetchUserProfile =
    useCallback(async () => {
      const currentToken =
        localStorage.getItem('authToken')

      if (!currentToken) {
        return
      }

      try {
        const response = await fetch(
          apiUrl('/api/users/me'),
          {
            headers: {
              Authorization:
                `Bearer ${currentToken}`
            }
          }
        )

        if (response.status === 401) {
          handleLogout()
          return
        }

        if (response.ok) {
          const data =
            await response.json()

          setUserProfile(data)
        }
      } catch (error) {
        console.error(
          'Failed to load user profile:',
          error
        )
      }
    }, [handleLogout])


  /* =======================================================
     Story 9.1
     Fetch Provider Bookings + Restaurant Reservations
     ======================================================= */

  const fetchProviderBookings =
    useCallback(async () => {
      const currentToken =
        localStorage.getItem('authToken')

      if (!currentToken) {
        setBookings([])
        setBookingsError(
          'Authentication is required.'
        )

        return
      }

      setBookingsLoading(true)
      setBookingsError(null)

      try {
        const response = await fetch(
          bookingUrl(
            '/api/provider-bookings/my'
          ),
          {
            method: 'GET',

            headers: {
              Authorization:
                `Bearer ${currentToken}`,
              Accept: 'application/json'
            }
          }
        )


        /* -----------------------------------------------
           Session expired
           ----------------------------------------------- */

        if (response.status === 401) {
          setBookings([])

          setBookingsError(
            'Your session has expired. Please log in again.'
          )

          return
        }


        /* -----------------------------------------------
           User is not a provider
           ----------------------------------------------- */

        if (response.status === 403) {
          setBookings([])

          setBookingsError(
            'You are not authorized to access provider bookings.'
          )

          return
        }


        /* -----------------------------------------------
           Other backend error
           ----------------------------------------------- */

        if (!response.ok) {
          let errorMessage =
            'Unable to load customer bookings and reservations.'

          try {
            const errorData =
              await response.json()

            if (errorData?.message) {
              errorMessage =
                errorData.message
            }
          } catch {
            // Response may not contain JSON
          }

          throw new Error(errorMessage)
        }


        /* -----------------------------------------------
           Successful response
           ----------------------------------------------- */

        const data =
          await response.json()

        setBookings(
          Array.isArray(data)
            ? data
            : []
        )
      } catch (error) {
        console.error(
          'Failed to load provider bookings:',
          error
        )

        setBookings([])

        setBookingsError(
          error?.message ||
          'Unable to load customer bookings and reservations.'
        )
      } finally {
        setBookingsLoading(false)
      }
    }, [])


  /* =======================================================
     Initial Requests
     ======================================================= */

  useEffect(() => {
    fetchProviderInfo()
  }, [fetchProviderInfo])


  useEffect(() => {
    fetchUserProfile()
  }, [fetchUserProfile])


  /* =======================================================
     Load services after provider information is available
     ======================================================= */

  useEffect(() => {
    if (providerInfo) {
      fetchServices()
    }
  }, [
    providerInfo,
    fetchServices
  ])


  /* =======================================================
     Load provider bookings when Bookings tab opens
     ======================================================= */

  useEffect(() => {
    if (activeTab === 'bookings') {
      fetchProviderBookings()
    }
  }, [
    activeTab,
    fetchProviderBookings
  ])


  /* =======================================================
     Notification Actions
     ======================================================= */

  const handleMarkAllNotificationsRead =
    () => {
      setNotifications(
        previous =>
          previous.map(
            notification => ({
              ...notification,
              read: true
            })
          )
      )

      showToast(
        'All notifications marked as read.'
      )
    }


  const handleToggleNotificationRead =
    (notificationId) => {
      setNotifications(
        previous =>
          previous.map(
            notification =>
              notification.id ===
                notificationId
                ? {
                  ...notification,
                  read:
                    !notification.read
                }
                : notification
          )
      )
    }


  const handleClearNotifications =
    () => {
      setNotifications([])

      showToast(
        'Notifications cleared.'
      )
    }


  /* =======================================================
     Notification Count
     ======================================================= */

  const unreadNotifCount =
    notifications.filter(
      notification =>
        !notification.read
    ).length


  /* =======================================================
     Navigation
     ======================================================= */

  const navItems = [
    {
      key: 'overview',
      icon:
        <DashboardIcon size={18} />,
      label: 'Overview'
    },

    {
      key: 'business',
      icon:
        <StorefrontIcon size={18} />,
      label: 'Business Profile'
    },

    {
      key: 'services',
      icon:
        <KitesurfingIcon size={18} />,

      label:
        isHotel
          ? 'Rooms & Accommodations'
          : isRestaurant
            ? 'Menu & Dining'
            : 'Activities & Services'
    },

    {
      key: 'bookings',
      icon:
        <CalendarMonthIcon size={18} />,
      label: 'Bookings'
    },

    {
      key: 'reports',
      icon:
        <BarChartIcon size={18} />,
      label: 'Inventory Reports'
    },

    {
      key: 'notifications',
      icon:
        <NotificationsActiveIcon
          size={18}
        />,
      label: 'Notifications',

      badge:
        unreadNotifCount > 0
          ? unreadNotifCount
          : null
    },

    {
      key: 'account',
      icon:
        <PermIdentityIcon size={18} />,
      label: 'Account'
    }
  ]


  /* =======================================================
     Render
     ======================================================= */

  return (
    <DashboardLayout
      roleBadge="Provider Portal"
      navItems={navItems}
      activeTab={activeTab}
      onSelectTab={setActiveTab}
      userProfile={userProfile}
      onLogout={handleLogout}
    >

      {/* Toast */}

      {toast && (
        <Toast
          message={toast}
          onClose={() =>
            setToast(null)
          }
        />
      )}


      {/* =================================================
          Overview
          ================================================= */}

      {activeTab === 'overview' && (
        <ProviderOverviewTab
          providerInfo={providerInfo}
          services={services}
          bookings={bookings}
          notifications={notifications}
          onNavigate={
            tab =>
              setActiveTab(tab)
          }
        />
      )}


      {/* =================================================
          Business Profile
          ================================================= */}

      {activeTab === 'business' && (
        <ProviderBusinessProfileTab
          token={token}
          onLogout={handleLogout}
          providerInfo={providerInfo}

          onUpdateSuccess={
            updated =>
              setProviderInfo(updated)
          }

          showToast={showToast}
        />
      )}


      {/* =================================================
          Provider Services
          ================================================= */}

      {activeTab === 'services' && (
        <ProviderListingsTab
          token={token}
          onLogout={handleLogout}
          services={services}
          isHotel={isHotel}
          isRestaurant={isRestaurant}
          catalogEndpoint={
            catalogEndpoint
          }
          onRefreshServices={
            fetchServices
          }
          showToast={showToast}
        />
      )}


      {/* =================================================
          Story 9.1
          Provider Booking Management
          ================================================= */}

      {activeTab === 'bookings' && (
        <ProviderBookingsTab
          bookings={bookings}
          loading={bookingsLoading}
          error={bookingsError}
          onRetry={
            fetchProviderBookings
          }
        />
      )}


      {/* =================================================
          Inventory Reports
          ================================================= */}

      {activeTab === 'reports' && (
        <InventoryReportView
          token={token}
          onLogout={handleLogout}
          isAdmin={false}
        />
      )}


      {/* =================================================
          Notifications
          ================================================= */}

      {activeTab ===
        'notifications' && (
          <ProviderNotificationsTab
            notifications={
              notifications
            }

            onMarkAllRead={
              handleMarkAllNotificationsRead
            }

            onToggleRead={
              handleToggleNotificationRead
            }

            onClearAll={
              handleClearNotifications
            }
          />
        )}


      {/* =================================================
          Account
          ================================================= */}

      {activeTab === 'account' && (
        <ProviderAccountTab
          token={token}
          onLogout={handleLogout}
          showToast={showToast}

          onProfileUpdate={
            setUserProfile
          }
        />
      )}

    </DashboardLayout>
  )
}

export default ProviderDashboard