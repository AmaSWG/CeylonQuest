import React from 'react'
import { EmailIcon, LocalPhoneIcon, MyLocationIcon, AlarmIcon } from '../../../components/Icons'

function HomeFooter({ onLogin, onRegister }) {
  return (
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
  )
}

export default HomeFooter
