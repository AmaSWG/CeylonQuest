import { useState } from 'react'
import './App.css'
import Registration from './pages/Registration'
import ProviderApplication from './pages/ProviderApplication'
import Login from './pages/Login'

function App() {
  const [page, setPage] = useState('registration')

  if (page === 'provider-application') {
    return <ProviderApplication onBack={() => setPage('registration')} />
  }

  if (page === 'login') {
    return <Login onLoginSuccess={() => setPage('registration')} onBack={() => setPage('registration')} />
  }

  return <Registration onApplyAsProvider={() => setPage('provider-application')} onLogin={() => setPage('login')} />
}

export default App