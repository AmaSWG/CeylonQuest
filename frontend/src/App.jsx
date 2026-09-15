import { useState } from 'react'
import './App.css'

// Home Feature
import HomePage from './features/home/pages/HomePage'

// Auth Feature
import Login from './features/auth/pages/Login'
import Registration from './features/auth/pages/Registration'
import ForgotPassword from './features/auth/pages/ForgotPassword'
import ResetPassword from './features/auth/pages/ResetPassword'
import ProviderActivation from './features/auth/pages/ProviderActivation'

// Provider Feature
import ProviderApplication from './features/provider/pages/ProviderApplication'
import ProviderApplicationStatus from './features/provider/pages/ProviderApplicationStatus'
import ProviderDashboard from './features/provider/pages/ProviderDashboard'

// Visitor Feature
import VisitorDashboard from './features/visitor/pages/VisitorDashboard'

// Admin Feature
import AdminDashboard from './features/admin/pages/AdminDashboard'

function App() {
  const storedRole = localStorage.getItem('userRole')
  const hasToken   = Boolean(localStorage.getItem('authToken'))

  const initialPage = () => {
    const params = new URLSearchParams(window.location.search)
    const path = window.location.pathname.toLowerCase()

    if (path.includes('reset-password') || params.has('token') || params.get('page') === 'reset-password') {
      return 'reset-password'
    }
    if (path.includes('forgot-password') || params.get('page') === 'forgot-password') {
      return 'forgot-password'
    }
    if (path.includes('provider-activate') || path.includes('provider-activation') || params.get('page') === 'provider-activate') {
      return 'provider-activate'
    }
    if (path.includes('provider-status') || path.includes('application-status') || params.get('page') === 'provider-status' || params.get('page') === 'application-status') {
      return 'provider-status'
    }
    if (path.includes('provider-application') || params.get('page') === 'provider-application') {
      return 'provider-application'
    }
    if (path.includes('login') || params.get('page') === 'login') {
      return 'login'
    }
    if (hasToken && storedRole === 'Visitor')  return 'visitor-dashboard'
    if (hasToken && storedRole === 'Provider') return 'provider-dashboard'
    if (hasToken && storedRole === 'Admin')    return 'admin-dashboard'
    return 'home'
  }

  const [page, setPage] = useState(initialPage)

  const handleLoginSuccess = (role) => {
    if (role === 'Provider') {
      setPage('provider-dashboard')
    } else if (role === 'Admin') {
      setPage('admin-dashboard')
    } else {
      // Visitor (and any unknown role) go to visitor dashboard
      setPage('visitor-dashboard')
    }
  }

  const handleLogout = () => {
    localStorage.removeItem('authToken')
    localStorage.removeItem('userRole')
    setPage('home')
  }

  if (page === 'provider-application') {
    return (
      <ProviderApplication
        onBack={() => setPage('registration')}
        onCheckStatus={() => setPage('provider-status')}
        onActivate={() => setPage('provider-activate')}
      />
    )
  }

  if (page === 'provider-status') {
    return (
      <ProviderApplicationStatus
        onBack={() => setPage('registration')}
        onApply={() => setPage('provider-application')}
        onLogin={() => setPage('login')}
        onActivate={() => setPage('provider-activate')}
      />
    )
  }

  if (page === 'provider-activate') {
    return (
      <ProviderActivation
        onLogin={() => setPage('login')}
        onBack={() => setPage('registration')}
        onStatusCheck={() => setPage('provider-status')}
      />
    )
  }

  if (page === 'login') {
    return (
      <Login
        onLoginSuccess={handleLoginSuccess}
        onBack={() => setPage('registration')}
        onForgotPassword={() => setPage('forgot-password')}
        onHome={() => setPage('home')}
        onActivateProvider={() => setPage('provider-activate')}
      />
    )
  }

  if (page === 'forgot-password') {
    return (
      <ForgotPassword
        onBack={() => setPage('login')}
      />
    )
  }

  if (page === 'reset-password') {
    const params = new URLSearchParams(window.location.search)
    const token = params.get('token') || ''
    return (
      <ResetPassword
        token={token}
        onBack={() => {
          if (window.location.search || window.location.pathname !== '/') {
            window.history.pushState({}, '', '/')
          }
          setPage('login')
        }}
      />
    )
  }

  if (page === 'visitor-dashboard') {
    return <VisitorDashboard onLogout={handleLogout} />
  }

  if (page === 'provider-dashboard') {
    return <ProviderDashboard onLogout={handleLogout} />
  }

  if (page === 'admin-dashboard') {
    return <AdminDashboard onLogout={handleLogout} />
  }

  if (page === 'home') {
    return (
      <HomePage
        onLogin={() => setPage('login')}
        onRegister={() => setPage('registration')}
      />
    )
  }

  return (
    <Registration
      onApplyAsProvider={() => setPage('provider-application')}
      onCheckStatus={() => setPage('provider-status')}
      onActivateProvider={() => setPage('provider-activate')}
      onLogin={() => setPage('login')}
      onHome={() => setPage('home')}
    />
  )
}

export default App