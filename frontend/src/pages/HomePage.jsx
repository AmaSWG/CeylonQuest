import '../styles/HomePage.css'
import { useState, useEffect } from 'react'
import {
  LandscapeIcon,
  MyLocationIcon,
  KitesurfingIcon,
  VerifiedUserIcon,
  BoltIcon,
  StorefrontIcon,
  EmailIcon,
  LocalPhoneIcon,
  AlarmIcon
} from '../components/Icons'

/**
 * HomePage — CeylonQuest Landing Page
 *
 * Props:
 *   onLogin    () => void  Navigate to the Login page
 *   onRegister () => void  Navigate to the Registration page
 */
function HomePage({ onLogin, onRegister }) {
  const [scrolled, setScrolled] = useState(false)

  useEffect(() => {
    const handleScroll = () => setScrolled(window.scrollY > 40)
    window.addEventListener('scroll', handleScroll, { passive: true })
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  return (
    <div className="home-page">

      {/* ── Navbar ── */}
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

      {/* ── Hero ── */}
      <section className="home-hero-section" aria-labelledby="hero-heading">
        <div className="home-hero">
          <div className="home-hero__content">
            <h1 id="hero-heading" className="home-hero__title">
              Discover Sri Lanka<br />
              <span className="home-hero__title--accent">Like Never Before</span>
            </h1>

            <p className="home-hero__sub">
              Explore hidden gems, book authentic experiences, and connect with
              trusted local providers — all in one place.
            </p>

            <div className="home-hero__actions">
              <button
                id="hero-start-btn"
                className="home-btn home-btn--primary"
                onClick={onRegister}
                aria-label="Create a free account and start exploring"
              >
                Start Exploring
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                  <path d="M5 12h14M12 5l7 7-7 7" />
                </svg>
              </button>
            </div>

            {/* Stats */}
            <div className="home-hero__stats" aria-label="Platform statistics">
              <div className="home-stat">
                <span className="home-stat__num">100+</span>
                <span className="home-stat__label">Experiences</span>
              </div>
              <div className="home-stat__divider" aria-hidden="true" />
              <div className="home-stat">
                <span className="home-stat__num">50+</span>
                <span className="home-stat__label">Providers</span>
              </div>
              <div className="home-stat__divider" aria-hidden="true" />
              <div className="home-stat">
                <span className="home-stat__num">9</span>
                <span className="home-stat__label">Provinces</span>
              </div>
            </div>
          </div>

          {/* Floating destination cards */}
          <div className="home-hero__visual" aria-hidden="true">
            <div className="home-hero__orb home-hero__orb--1" />
            <div className="home-hero__orb home-hero__orb--2" />
            <div className="home-hero__card home-hero__card--1">
              <span className="home-hero__card-icon"><LandscapeIcon size={18} /></span>
              <span>Sigiriya Rock</span>
            </div>
            <div className="home-hero__card home-hero__card--2">
              <span className="home-hero__card-icon"><MyLocationIcon size={18} /></span>
              <span>Elephant Safari</span>
            </div>
            <div className="home-hero__card home-hero__card--3">
              <span className="home-hero__card-icon"><KitesurfingIcon size={18} /></span>
              <span>Mirissa Beach</span>
            </div>
          </div>
        </div>
      </section>

      {/* ── Features ── */}
      <section className="home-features" aria-labelledby="features-heading">
        <div className="home-features__inner">
          <p className="home-features__eyebrow">Everything you need</p>
          <h2 id="features-heading" className="home-features__title">
            Your Complete Sri Lanka Journey
          </h2>
          <div className="home-features__grid">
            <article className="home-feature-card">
              <div className="home-feature-card__icon" aria-hidden="true">
                <LandscapeIcon size={24} />
              </div>
              <h3>Explore Destinations</h3>
              <p>
                From ancient temples to lush tea estates — discover Sri Lanka's
                most breathtaking locations curated by local experts.
              </p>
            </article>

            <article className="home-feature-card home-feature-card--highlight">
              <div className="home-feature-card__icon" aria-hidden="true">
                <BoltIcon size={24} />
              </div>
              <h3>Book Experiences</h3>
              <p>
                Reserve unique tours, activities, and stays directly with verified
                local providers at the best prices.
              </p>
            </article>

            <article className="home-feature-card">
              <div className="home-feature-card__icon" aria-hidden="true">
                <StorefrontIcon size={24} />
              </div>
              <h3>Meet Providers</h3>
              <p>
                Connect with trusted hotels, restaurants, guides, and tour
                operators across the entire island.
              </p>
            </article>
          </div>
        </div>
      </section>

      {/* ── CTA Banner ── */}
      <section className="home-cta-banner" aria-labelledby="cta-heading">
        <div className="home-cta-banner__inner">
          <h2 id="cta-heading">
            Ready to explore the Pearl of the Indian Ocean?
          </h2>
          <p>
            Join thousands of travellers who've discovered Sri Lanka through
            CeylonQuest.
          </p>
          <div className="home-cta-banner__actions">
            <button
              id="cta-register-btn"
              className="home-btn home-btn--gold"
              onClick={onRegister}
              aria-label="Create a free CeylonQuest account"
            >
              Create Free Account
            </button>
            <button
              id="cta-login-btn"
              className="home-btn home-btn--outline-light"
              onClick={onLogin}
              aria-label="Sign in to your existing account"
            >
              Already a member? Sign In
            </button>
          </div>
        </div>
      </section>

      {/* ── Footer ── */}
      <footer className="home-footer">
        <div className="home-footer__inner">

          {/* Brand column */}
          <div className="home-footer__col home-footer__col--brand">
            <span className="home-nav__logo home-footer__logo">
              <img src="/dashboard-logo.png" alt="CeylonQuest" className="home-footer__logo-img" />
            </span>
            <p className="home-footer__tagline">
              Discover Sri Lanka like never before. Your trusted platform for
              authentic local travel experiences.
            </p>
          </div>

          {/* Contact column */}
          <div className="home-footer__col">
            <h3 className="home-footer__col-title">Contact Us</h3>
            <ul className="home-footer__contact-list">
              <li>
                <span className="home-footer__contact-icon" aria-hidden="true"><EmailIcon size={16} /></span>
                <a href="mailto:adminceylonquest@gmail.com" className="home-footer__link">
                  adminceylonquest@gmail.com
                </a>
              </li>
              <li>
                <span className="home-footer__contact-icon" aria-hidden="true"><LocalPhoneIcon size={16} /></span>
                <a href="tel:+94778922525" className="home-footer__link">
                  +94 77 892 2525
                </a>
              </li>
              <li>
                <span className="home-footer__contact-icon" aria-hidden="true"><MyLocationIcon size={16} /></span>
                <span>Colombo, Sri Lanka</span>
              </li>
              <li>
                <span className="home-footer__contact-icon" aria-hidden="true"><AlarmIcon size={16} /></span>
                <span>Mon – Fri, 9 AM – 6 PM (SLST)</span>
              </li>
            </ul>
          </div>

          {/* Quick links column */}
          <div className="home-footer__col">
            <h3 className="home-footer__col-title">Quick Links</h3>
            <ul className="home-footer__links-list">
              <li>
                <button className="home-footer__nav-btn" onClick={onRegister}>
                  Create Account
                </button>
              </li>
              <li>
                <button className="home-footer__nav-btn" onClick={onLogin}>
                  Login
                </button>
              </li>
              <li>
                <a href="mailto:adminceylonquest@gmail.com" className="home-footer__link">
                  Become a Provider
                </a>
              </li>
              <li>
                <a href="mailto:adminceylonquest@gmail.com" className="home-footer__link">
                  Support
                </a>
              </li>
            </ul>
          </div>

        </div>

        {/* Bottom bar */}
        <div className="home-footer__bottom">
          <p className="home-footer__copy">
            © {new Date().getFullYear()} CeylonQuest. All rights reserved.
          </p>
        </div>
      </footer>

    </div>
  )
}

export default HomePage
