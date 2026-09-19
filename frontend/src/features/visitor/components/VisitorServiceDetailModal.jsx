import React, { useState, useEffect } from 'react'
import './VisitorServiceDetailModal.css'
import {
  CloseIcon,
  GroupIcon,
  RestaurantIcon,
  HotelIcon,
  HourglassTopIcon,
  AccessTimeFilledIcon,
  MyLocationIcon,
  LocationOnIcon,
  HouseIcon,
  DiningIcon
} from '../../../components/Icons'

export default function VisitorServiceDetailModal({ item, onClose, onOpenBooking }) {
  const [lightboxIndex, setLightboxIndex] = useState(null)

  const normalizeImgUrl = (url) => {
    if (!url) return ''
    if (url.includes('/provider-service-images/')) {
      const parts = url.split('/provider-service-images/')
      return `/api/catalog/images/${parts[1]}`
    }
    return url
  }

  const rawImages = item?.images
  let images = []
  if (Array.isArray(rawImages)) {
    images = rawImages.filter(Boolean)
  } else if (typeof rawImages === 'string' && rawImages.trim()) {
    try {
      const parsed = JSON.parse(rawImages)
      images = Array.isArray(parsed) ? parsed.filter(Boolean) : [rawImages]
    } catch {
      images = rawImages.split(',').map(s => s.trim()).filter(Boolean)
    }
  }

  images = images.map(normalizeImgUrl).filter(Boolean)

  // Preload all listing images into browser memory
  useEffect(() => {
    if (images && images.length > 0) {
      images.forEach((src) => {
        const img = new Image()
        img.src = src
      })
    }
  }, [item])

  // Keyboard navigation for lightbox
  useEffect(() => {
    if (lightboxIndex === null) return

    const handleKeyDown = (e) => {
      if (e.key === 'Escape') {
        setLightboxIndex(null)
      } else if (e.key === 'ArrowLeft') {
        setLightboxIndex((prev) => (prev > 0 ? prev - 1 : images.length - 1))
      } else if (e.key === 'ArrowRight') {
        setLightboxIndex((prev) => (prev < images.length - 1 ? prev + 1 : 0))
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [lightboxIndex, images.length])

  if (!item) return null

  const unit = item.unit || (item.type === 'Accommodation' ? 'night' : 'person')
  const isMoreThan3 = images.length > 3
  const displayImages = isMoreThan3 ? images.slice(0, 3) : images
  const remainingCount = images.length - 2

  return (
    <div className="vd-modal-overlay" onClick={onClose}>
      <div className="vd-detail-modal" onClick={(e) => e.stopPropagation()}>
        <div className="vd-detail-modal__header">
          <button className="vd-detail-modal__close" onClick={onClose} aria-label="Close">
            <CloseIcon size={16} />
          </button>
          <span className={`vd-type-badge vd-type-badge--${item.type.toLowerCase()}`}>
            {item.type}
          </span>
          <h2 className="vd-detail-modal__title">{item.title}</h2>
          <p className="vd-detail-modal__provider">By {item.providerBusinessName}</p>
        </div>

        <div className="vd-detail-modal__body">
          <p className="vd-detail-modal__desc">{item.description}</p>

          <div className="vd-detail-modal__list">
            <div className="vd-detail-row">
              <span className="vd-detail-row__label">
                <LocationOnIcon size={16} /> Location:
              </span>
              <span className="vd-detail-row__val">{item.location || 'Sri Lanka'}</span>
            </div>

            {item.type === 'Accommodation' && (
              <>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <HouseIcon size={16} /> Property Type:
                  </span>
                  <span className="vd-detail-row__val">{item.propertyType || item.category || 'Stay'}</span>
                </div>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <GroupIcon size={16} /> Guest Capacity:
                  </span>
                  <span className="vd-detail-row__val">Up to {item.maxGuests || 2} guests</span>
                </div>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <HotelIcon size={16} /> Bed Details:
                  </span>
                  <span className="vd-detail-row__val">{item.bedDetails || 'King / Queen Bed'}</span>
                </div>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <MyLocationIcon size={16} /> Amenities:
                  </span>
                  <span className="vd-detail-row__val">{item.amenities || 'WiFi, AC, En-suite Bathroom'}</span>
                </div>
              </>
            )}

            {item.type === 'Experience' && (
              <>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <AccessTimeFilledIcon size={16} /> Duration:
                  </span>
                  <span className="vd-detail-row__val">{item.duration || 'Flexible'}</span>
                </div>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <GroupIcon size={16} /> Group Size:
                  </span>
                  <span className="vd-detail-row__val">Up to {item.maxParticipants || 1} people</span>
                </div>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <HourglassTopIcon size={16} /> Schedule:
                  </span>
                  <span className="vd-detail-row__val">{item.scheduleInfo || item.availableDays || 'Daily'}</span>
                </div>
              </>
            )}

            {item.type === 'Restaurant' && (
              <>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <RestaurantIcon size={16} /> Cuisine:
                  </span>
                  <span className="vd-detail-row__val">{item.cuisineType || item.category || 'Specialty Cuisine'}</span>
                </div>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <DiningIcon size={16} /> Dining Style:
                  </span>
                  <span className="vd-detail-row__val">{item.diningStyle || 'Casual Dining'}</span>
                </div>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <GroupIcon size={16} /> Seating Capacity:
                  </span>
                  <span className="vd-detail-row__val">{item.seatingCapacity ? `${item.seatingCapacity} seats` : 'Table Seating'}</span>
                </div>
                <div className="vd-detail-row">
                  <span className="vd-detail-row__label">
                    <AccessTimeFilledIcon size={16} /> Hours:
                  </span>
                  <span className="vd-detail-row__val">{item.openingHours || '11:00 AM - 10:00 PM'}</span>
                </div>
              </>
            )}
          </div>

          {images.length > 0 && (
            <div className="vd-detail-modal__images">
              <div className={`vd-detail-image-row vd-detail-image-row--count-${displayImages.length}`}>
                {displayImages.map((img, idx) => {
                  const isLastWithOverlay = isMoreThan3 && idx === 2
                  return (
                    <div
                      key={idx}
                      className="vd-detail-img-wrap"
                      onClick={() => setLightboxIndex(idx)}
                      title="Click to view photo"
                    >
                      <img src={img} alt={`${item.title} photo ${idx + 1}`} className="vd-detail-modal__img" />
                      {isLastWithOverlay && (
                        <div className="vd-detail-img-overlay">
                          <span>+{remainingCount}</span>
                        </div>
                      )}
                    </div>
                  )
                })}
              </div>
            </div>
          )}

          <div className="vd-detail-modal__footer">
            <div className="vd-detail-price">
              <span className="vd-detail-price__label">Price</span>
              <div className="vd-detail-price__row">
                <span className="vd-detail-price__val">LKR {Number(item.price).toLocaleString()}</span>
                <span className="vd-detail-price__unit"> / {unit}</span>
              </div>
            </div>
            <button
              className="vd-btn-book"
              onClick={() => {
                onClose()
                onOpenBooking(item)
              }}
            >
              Book Now
            </button>
          </div>
        </div>

        {/* Lightbox / Full-size viewer */}
        {lightboxIndex !== null && (
          <div className="vd-lightbox-overlay" onClick={() => setLightboxIndex(null)}>
            <div className="vd-lightbox-content" onClick={(e) => e.stopPropagation()}>
              <button
                className="vd-lightbox-close"
                onClick={() => setLightboxIndex(null)}
                aria-label="Close image preview"
              >
                ✕
              </button>

              <img
                src={images[lightboxIndex]}
                alt={`Photo ${lightboxIndex + 1}`}
                className="vd-lightbox-img"
              />

              {images.length > 1 && (
                <div className="vd-lightbox-nav">
                  <button
                    type="button"
                    className="vd-lightbox-btn"
                    onClick={() => setLightboxIndex((lightboxIndex - 1 + images.length) % images.length)}
                  >
                    ‹
                  </button>
                  <span className="vd-lightbox-counter">
                    {lightboxIndex + 1} / {images.length}
                  </span>
                  <button
                    type="button"
                    className="vd-lightbox-btn"
                    onClick={() => setLightboxIndex((lightboxIndex + 1) % images.length)}
                  >
                    ›
                  </button>
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
