import React from 'react'
import { LandscapeIcon, MyLocationIcon, KitesurfingIcon } from '../../../components/Icons'

function HomeHero({ onRegister }) {
  return (
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

          {/* Platform stats */}
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
  )
}

export default HomeHero
