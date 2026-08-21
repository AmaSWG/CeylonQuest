import { useState } from 'react'
import './App.css'
import Registration from './pages/Registration'
import ProviderApplication from './pages/ProviderApplication'

function App() {
  const [page, setPage] = useState('registration')

  if (page === 'provider-application') {
    return <ProviderApplication onBack={() => setPage('registration')} />
  }

  return <Registration onApplyAsProvider={() => setPage('provider-application')} />
}

export default App