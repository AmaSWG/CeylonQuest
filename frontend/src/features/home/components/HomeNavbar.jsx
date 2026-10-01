import React from 'react'

function HomeNavbar({ scrolled, onLogin, onRegister }) {
  return (
    <nav className={`home-nav${scrolled ? ' home-nav--scrolled' : ''}`} role="navigation" aria-label="Main navigation">
      <div className="home-nav__inner">
        <span className="home-nav__logo" aria-label="CeylonQuest">
          <img src="/logo.png" alt="CeylonQuest" className="home-nav__logo-img" />
        </span>
        <div className="home-nav__actions">
          <button
            id="nav-login-btn"
            className="home-nav__login"
            onClick={onLogin}
            aria-label="Go to login page"
          >
            Login
          </button>
          <button
            id="nav-getstarted-btn"
            className="home-nav__cta"
            onClick={onRegister}
            aria-label="Create a new account"
          >
            Get Started
          </button>
        </div>
      </div>
    </nav>
  )
}

export default HomeNavbar
