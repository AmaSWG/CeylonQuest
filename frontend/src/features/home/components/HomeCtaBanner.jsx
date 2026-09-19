import React from 'react'

function HomeCtaBanner({ onLogin, onRegister }) {
  return (
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
  )
}

export default HomeCtaBanner
