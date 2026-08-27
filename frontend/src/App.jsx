import { useState } from 'react'
import './App.css'
import Registration from './pages/Registration'
import ProviderApplication from './pages/ProviderApplication'
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
    if (path.includes('login') || params.get('page') === 'login') {
      return 'login'
    }
    if (hasToken && storedRole === 'Visitor')  return 'visitor-dashboard'
    if (hasToken && storedRole === 'Provider') return 'provider-dashboard'
    if (hasToken && storedRole === 'Admin')    return 'admin-dashboard'
    return 'registration'
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
    setPage('registration')
  }

  if (page === 'provider-application') {
    return <ProviderApplication onBack={() => setPage('registration')} />
  }

  if (page === 'login') {
    return (
      <Login
        onLoginSuccess={handleLoginSuccess}
        onBack={() => setPage('registration')}
        onForgotPassword={() => setPage('forgot-password')}
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

  return (
    <Registration
      onApplyAsProvider={() => setPage('provider-application')}
      onLogin={() => setPage('login')}
    />
  )
}

export default App