import React, { useState, useEffect } from 'react'
import HomeNavbar from '../components/HomeNavbar'
import HomeHero from '../components/HomeHero'
import HomeFeatures from '../components/HomeFeatures'
import HomeCtaBanner from '../components/HomeCtaBanner'
import HomeFooter from '../components/HomeFooter'
import './HomePage.css'

function HomePage({ onLogin, onRegister }) {
  const [scrolled, setScrolled] = useState(false)

  useEffect(() => {
    const handleScroll = () => setScrolled(window.scrollY > 40)
    window.addEventListener('scroll', handleScroll, { passive: true })
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  return (
    <div className="home-page">
      <HomeNavbar
        scrolled={scrolled}
        onLogin={onLogin}
        onRegister={onRegister}
      />

      <HomeHero onRegister={onRegister} />

      <HomeFeatures />

      <HomeCtaBanner
        onLogin={onLogin}
        onRegister={onRegister}
      />

      <HomeFooter
        onLogin={onLogin}
        onRegister={onRegister}
      />
    </div>
  )
}

export default HomePage
