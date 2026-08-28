import { useState } from 'react'
import './App.css'
import HomePage from './pages/HomePage'
import Registration from './pages/Registration'
import ProviderApplication from './pages/ProviderApplication'
import ProviderApplicationStatus from './pages/ProviderApplicationStatus'
import Login from './pages/Login'
import ForgotPassword from './pages/ForgotPassword'
import ResetPassword from './pages/ResetPassword'
import VisitorDashboard from './pages/VisitorDashboard'
import ProviderDashboard from './pages/ProviderDashboard'
import AdminDashboard from './pages/AdminDashboard'

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
      />
    )
  }

  if (page === 'provider-status') {
    return (
      <ProviderApplicationStatus
        onBack={() => setPage('registration')}
        onApply={() => setPage('provider-application')}
        onLogin={() => setPage('login')}
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
      onLogin={() => setPage('login')}
      onHome={() => setPage('home')}
    />
  )
}

export default App