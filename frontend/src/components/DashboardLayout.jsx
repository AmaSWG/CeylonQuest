import React, { useState, useEffect } from 'react'
import './DashboardLayout.css'
import { LogoutIcon, MenuIcon, CloseIcon } from './Icons'
import { apiUrl } from '../api/client'

function initials(first, last) {
  return `${(first || '').charAt(0)}${(last || '').charAt(0)}`.toUpperCase() || '?'
}

function formatAvatarUrl(url) {
  if (!url) return null
  if (url.startsWith('/uploads/avatars/')) {
    const fileName = url.split('/').pop()
    return apiUrl(`/api/users/avatar/${fileName}`)
  }
  return url
}

export default function DashboardLayout({
  roleBadge = 'Portal',
  navItems = [],
  activeTab,
  onSelectTab,
  userProfile,
  onLogout,
  children
}) {
  const [mobileOpen, setMobileOpen] = useState(false)

  // Close sidebar on window resize if it grows past mobile breakpoint
  useEffect(() => {
    const handleResize = () => {
      if (window.innerWidth > 900) {
        setMobileOpen(false)
      }
    }
    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [])

  const handleNavClick = (item) => {
    if (item.disabled) return
    onSelectTab && onSelectTab(item.key)
    setMobileOpen(false)
  }

  const handleLogoutClick = () => {
    setMobileOpen(false)
    onLogout && onLogout()
  }

  return (
    <div className="cq-dashboard-wrapper">
      
      {/* Floating Mobile Toggle Button (Visible only when sidebar is hidden) */}
      <button
        type="button"
        className="cq-mobile-toggle-btn"
        id="dashboard-mobile-menu-btn"
        onClick={() => setMobileOpen(true)}
        aria-label="Open Navigation Menu"
      >
        <MenuIcon size={20} />
      </button>

      {/* Backdrop overlay for mobile drawer */}
      <div
        className={`cq-sidebar-overlay ${mobileOpen ? 'open' : ''}`}
        onClick={() => setMobileOpen(false)}
        aria-hidden="true"
      />

      {/* Vertical Sidebar */}
      <aside className={`cq-sidebar ${mobileOpen ? 'cq-sidebar--mobile-open' : ''}`}>
        
        <div className="cq-sidebar__header">
          <div className="cq-sidebar__brand">
            <img src="/dashboard-logo.png" alt="CeylonQuest" className="cq-sidebar__logo-img" />
            <span className="cq-sidebar__role">{roleBadge}</span>
          </div>
          <button
            type="button"
            className="cq-sidebar__close-btn"
            onClick={() => setMobileOpen(false)}
            aria-label="Close navigation"
          >
            <CloseIcon size={18} />
          </button>
        </div>

        {/* Vertical Navigation List */}
        <ul className="cq-sidebar__nav">
          {navItems.map(item => {
            const isActive = activeTab === item.key
            return (
              <li key={item.key}>
                <button
                  type="button"
                  className={`cq-nav-btn ${isActive ? 'active' : ''} ${item.disabled ? 'disabled' : ''}`}
                  onClick={() => handleNavClick(item)}
                  id={`nav-${item.key}`}
                  disabled={item.disabled}
                >
                  <span className="cq-nav-icon">{item.icon}</span>
                  <span className="cq-nav-label">{item.label}</span>
                  {item.badge && <span className="cq-nav-badge">{item.badge}</span>}
                  {item.comingSoon && <span className="cq-nav-soon">Soon</span>}
                </button>
              </li>
            )
          })}
        </ul>

        {/* User Profile & Logout in Sidebar Footer */}
        <div className="cq-sidebar__footer">
          {userProfile && (
            <div className="cq-sidebar-user">
              <div className="cq-sidebar-avatar">
                {userProfile.profilePictureUrl ? (
                  <img src={formatAvatarUrl(userProfile.profilePictureUrl)} alt="" className="cq-avatar-img" />
                ) : (
                  <span>{initials(userProfile.firstName, userProfile.lastName)}</span>
                )}
              </div>
              <div className="cq-sidebar-user__info">
                <div className="cq-sidebar-user__name">
                  {userProfile.firstName} {userProfile.lastName}
                </div>
                <div className="cq-sidebar-user__email">{userProfile.email}</div>
              </div>
            </div>
          )}

          <button
            type="button"
            className="cq-logout-btn"
            id="pd-logout-btn"
            onClick={handleLogoutClick}
          >
            <span className="cq-nav-icon"><LogoutIcon size={18} /></span>
            <span>Log Out</span>
          </button>
        </div>

      </aside>

      {/* Main Content Area */}
      <main className="cq-dashboard-main">
        {children}
      </main>

    </div>
  )
}
