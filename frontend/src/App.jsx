import { useState } from 'react'
import './App.css'
import Registration from './pages/Registration'
import ProviderApplication from './pages/ProviderApplication'
import Login from './pages/Login'
import VisitorDashboard from './pages/VisitorDashboard'

function App() {
  const storedRole = localStorage.getItem('userRole')
  const hasToken   = Boolean(localStorage.getItem('authToken'))

  const initialPage = () => {
    if (hasToken && storedRole === 'Visitor')  return 'visitor-dashboard'
    if (hasToken && storedRole === 'Provider') return 'visitor-dashboard'
    if (hasToken && storedRole === 'Admin')    return 'visitor-dashboard'
    return 'registration'
  }

  const [page, setPage] = useState(initialPage)

  const handleLoginSuccess = (role) => {
    if (role === 'Visitor') {
      setPage('visitor-dashboard')
    } else if (role === 'Provider') {
      setPage('visitor-dashboard')
    } else if (role === 'Admin') {
      setPage('visitor-dashboard')
    } else {
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
      />
    )
  }

  if (page === 'visitor-dashboard') {
    return <VisitorDashboard onLogout={handleLogout} />
  }

  return (
    <Registration
      onApplyAsProvider={() => setPage('provider-application')}
      onLogin={() => setPage('login')}
    />
  )
}

export default App