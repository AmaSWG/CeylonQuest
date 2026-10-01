import React from 'react'
import { LandscapeIcon, BoltIcon, StorefrontIcon } from '../../../components/Icons'

function HomeFeatures() {
  return (
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
            <div className="home-feature-card__icon" aria-hidden="true" className="home-feature-icon-gold">
              <BoltIcon size={24} />
            </div>
            <h3 className="home-feature-icon-gold">Book Experiences</h3>
            <p className="home-feature-icon-gold">
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
  )
}

export default HomeFeatures
