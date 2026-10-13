import React, { useState, useEffect } from 'react'

export default function ImageLightboxModal({ images, initialIndex = 0, onClose }) {
  const [currentIndex, setCurrentIndex] = useState(initialIndex)

  // Keyboard navigation (Escape, Left, Right arrows)
  useEffect(() => {
    const handleKeyDown = (e) => {
      if (e.key === 'Escape') onClose()
      if (e.key === 'ArrowRight') nextImage()
      if (e.key === 'ArrowLeft') prevImage()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [currentIndex, images.length])

  const nextImage = () => {
    setCurrentIndex((prev) => (prev + 1) % images.length)
  }

  const prevImage = () => {
    setCurrentIndex((prev) => (prev - 1 + images.length) % images.length)
  }

  return (
    <div className="lightbox-overlay" onClick={onClose}>
      <div className="lightbox-content" onClick={(e) => e.stopPropagation()}>
        {/* Close Button */}
        <button className="lightbox-close" onClick={onClose} aria-label="Close">✕</button>

        {/* Previous Button */}
        {images.length > 1 && (
          <button className="lightbox-nav lightbox-nav--prev" onClick={prevImage} aria-label="Previous">
            ‹
          </button>
        )}

        {/* Main Image */}
        <div className="lightbox-image-wrap">
          <img src={images[currentIndex]} alt={`Photo ${currentIndex + 1}`} className="lightbox-main-img" />
          <div className="lightbox-counter">
            {currentIndex + 1} / {images.length}
          </div>
        </div>

        {/* Next Button */}
        {images.length > 1 && (
          <button className="lightbox-nav lightbox-nav--next" onClick={nextImage} aria-label="Next">
            ›
          </button>
        )}
      </div>
    </div>
  )
}