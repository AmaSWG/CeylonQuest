import React, { useState, useEffect, useCallback } from 'react'
import './VisitorExploreTab.css'
import {
  SearchIcon,
  GroupIcon,
  KitesurfingIcon,
  RestaurantIcon,
  HotelIcon,
  HourglassTopIcon,
  AccessTimeFilledIcon,
  MyLocationIcon,
  LocationOnIcon,
  MoneyIcon,
  HouseIcon,
  DiningIcon
} from '../../../components/Icons'
import { catalogUrl } from '../../../api/client'
import VisitorServiceDetailModal from './VisitorServiceDetailModal'
import VisitorBookingModal from './VisitorBookingModal'
import LoadingSpinner from '../../../components/common/LoadingSpinner'
import EmptyState from '../../../components/common/EmptyState'

function getPageNumbers(current, total, maxVisible = 5) {
  if (total <= maxVisible) {
    return Array.from({ length: total }, (_, i) => i + 1)
  }
  const half = Math.floor(maxVisible / 2)
  let start = Math.max(1, current - half)
  let end = Math.min(total, start + maxVisible - 1)
  if (end - start + 1 < maxVisible) {
    start = Math.max(1, end - maxVisible + 1)
  }
  const pages = []
  for (let p = start; p <= end; p++) pages.push(p)
  return pages
}

export default function VisitorExploreTab() {
  const [searchTerm, setSearchTerm]       = useState('')
  const [serviceType, setServiceType]     = useState('all')
  const [category, setCategory]           = useState('all')
  const [region, setRegion]               = useState('all')
  const [priceRange, setPriceRange]       = useState('all')
  const [viewMode, setViewMode]           = useState('grid')
  const [currentPage, setCurrentPage]     = useState(1)
  const itemsPerPage = 8

  const [services, setServices]           = useState([])
  const [loading, setLoading]             = useState(true)
  const [loadError, setLoadError]         = useState(null)
  const [selectedDetail, setSelectedDetail] = useState(null)
  const [selectedBooking, setSelectedBooking] = useState(null)

  const fetchExploreServices = useCallback(async () => {
    setLoading(true)
    setLoadError(null)
    try {
      const resp = await fetch(catalogUrl('/api/catalog/search?pageSize=50'))
      if (resp.ok) {
        const data = await resp.json()
        setServices(Array.isArray(data) ? data : (data.items || []))
      } else {
        setLoadError('Failed to load services. Please try again.')
      }
    } catch {
      setLoadError('Network error. Unable to reach catalog service.')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchExploreServices()
  }, [fetchExploreServices])

  const filtered = services.filter((s) => {
    if (serviceType !== 'all' && s.type.toLowerCase() !== serviceType) return false
    if (category !== 'all' && s.category !== category) return false
    if (region !== 'all' && s.region !== region) return false

    if (priceRange !== 'all') {
      const p = Number(s.price)
      if (priceRange === 'budget' && p >= 5000) return false
      if (priceRange === 'mid' && (p < 5000 || p > 20000)) return false
      if (priceRange === 'luxury' && p <= 20000) return false
    }

    if (searchTerm.trim()) {
      const q = searchTerm.toLowerCase()
      const titleMatch    = s.title?.toLowerCase().includes(q)
      const descMatch     = s.description?.toLowerCase().includes(q)
      const locMatch      = s.location?.toLowerCase().includes(q)
      const providerMatch = s.providerBusinessName?.toLowerCase().includes(q)
      if (!titleMatch && !descMatch && !locMatch && !providerMatch) return false
    }

    return true
  })

  const totalPages  = Math.max(1, Math.ceil(filtered.length / itemsPerPage))
  const paginated   = filtered.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage)

  const handleFilterChange = (setter) => (e) => {
    setter(e.target.value)
    setCurrentPage(1)
  }

  return (
    <div className="vd-explore">
      <div className="vd-page-header">
        <h1>Explore Ceylon</h1>
        <p>Discover Sri Lanka’s authentic experiences, stays, and dining options.</p>
      </div>

      <div className="vd-search-bar">
        <SearchIcon size={18} className="vd-search-bar__icon" />
        <input
          type="text"
          placeholder="Search by title, description, location, or provider…"
          value={searchTerm}
          onChange={(e) => { setSearchTerm(e.target.value); setCurrentPage(1) }}
          className="vd-search-bar__input"
        />
        {searchTerm && (
          <button className="vd-search-bar__clear" onClick={() => { setSearchTerm(''); setCurrentPage(1) }}>✕</button>
        )}
      </div>

      <div className="vd-filters">
        <select value={serviceType} onChange={handleFilterChange(setServiceType)} className="vd-filter-select">
          <option value="all">All Service Types</option>
          <option value="experience">Experiences</option>
          <option value="restaurant">Restaurants</option>
          <option value="accommodation">Accommodations</option>
        </select>

        <select value={region} onChange={handleFilterChange(setRegion)} className="vd-filter-select">
          <option value="all">All Regions</option>
          <option value="Western">Western</option>
          <option value="Central">Central</option>
          <option value="Southern">Southern</option>
          <option value="Northern">Northern</option>
          <option value="Eastern">Eastern</option>
          <option value="North Western">North Western</option>
          <option value="North Central">North Central</option>
          <option value="Uva">Uva</option>
          <option value="Sabaragamuwa">Sabaragamuwa</option>
        </select>

        <select value={priceRange} onChange={handleFilterChange(setPriceRange)} className="vd-filter-select">
          <option value="all">Any Price</option>
          <option value="budget">Budget (&lt; LKR 5,000)</option>
          <option value="mid">Mid-range (LKR 5,000 – 20,000)</option>
          <option value="luxury">Luxury (&gt; LKR 20,000)</option>
        </select>
      </div>

      {loading && <LoadingSpinner label="Loading explore listings…" fullPage />}

      {loadError && !loading && (
        <div className="vd-form-error">{loadError}</div>
      )}

      {!loading && !loadError && paginated.length === 0 && (
        <EmptyState
          icon={SearchIcon}
          title="No services match your filters"
          message="Try adjusting your search criteria, clearing filters, or exploring other categories."
          actionLabel="Clear All Filters"
          onAction={() => {
            setSearchTerm('')
            setServiceType('all')
            setCategory('all')
            setRegion('all')
            setPriceRange('all')
            setCurrentPage(1)
          }}
        />
      )}

      {!loading && !loadError && paginated.length > 0 && (
        <>
          <div className="vd-services-grid">
            {paginated.map((item) => (
              <div key={item.id} className="vd-service-card" onClick={() => setSelectedDetail(item)}>
                <div className="vd-service-card__header">
                  <span className={`vd-type-badge vd-type-badge--${item.type.toLowerCase()}`}>
                    {item.type}
                  </span>
                  <span className="vd-service-card__region">{item.region}</span>
                </div>

                <div className="vd-service-card__body">
                  <h3 className="vd-service-card__title">{item.title}</h3>
                  <p className="vd-service-card__provider">By {item.providerBusinessName}</p>
                  <p className="vd-service-card__desc">{item.description}</p>
                </div>

                <div className="vd-service-card__footer">
                  <div className="vd-service-card__price">
                    <span className="vd-service-card__amount">LKR {Number(item.price).toLocaleString()}</span>
                    <span className="vd-service-card__unit">/{item.priceUnit}</span>
                  </div>
                  <button
                    className="vd-service-card__action-btn"
                    onClick={(e) => {
                      e.stopPropagation()
                      setSelectedBooking(item)
                    }}
                  >
                    Book Now
                  </button>
                </div>
              </div>
            ))}
          </div>

          {totalPages > 1 && (
            <div className="vd-pagination">
              <button
                className="vd-pagination__btn"
                disabled={currentPage === 1}
                onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
              >
                Previous
              </button>
              {getPageNumbers(currentPage, totalPages).map(p => (
                <button
                  key={p}
                  className={`vd-pagination__page ${currentPage === p ? 'vd-pagination__page--active' : ''}`}
                  onClick={() => setCurrentPage(p)}
                >
                  {p}
                </button>
              ))}
              <button
                className="vd-pagination__btn"
                disabled={currentPage === totalPages}
                onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
              >
                Next
              </button>
            </div>
          )}
        </>
      )}

      {selectedDetail && (
        <VisitorServiceDetailModal
          item={selectedDetail}
          onClose={() => setSelectedDetail(null)}
          onOpenBooking={(it) => setSelectedBooking(it)}
        />
      )}

      {selectedBooking && (
        <VisitorBookingModal
          item={selectedBooking}
          onClose={() => setSelectedBooking(null)}
        />
      )}
    </div>
  )
}
