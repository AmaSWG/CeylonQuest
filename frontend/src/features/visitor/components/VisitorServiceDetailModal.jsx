import React from 'react'
import './VisitorServiceDetailModal.css'
import {
  CloseIcon,
  GroupIcon,
  KitesurfingIcon,
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
  if (!item) return null

  const icons = {
    experience: [HourglassTopIcon, GroupIcon],
    restaurant: [RestaurantIcon, DiningIcon, GroupIcon],
    accommodation: [HouseIcon, GroupIcon, HotelIcon],
  }[item.type?.toLowerCase()] || []

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

          <div className="vd-detail-modal__grid">
            <div className="vd-detail-item">
              <LocationOnIcon size={16} />
              <div>
                <div className="vd-detail-item__label">Location</div>
                <div className="vd-detail-item__val">{item.location} ({item.region})</div>
              </div>
            </div>

            <div className="vd-detail-item">
              <MyLocationIcon size={16} />
              <div>
                <div className="vd-detail-item__label">Category</div>
                <div className="vd-detail-item__val">{item.category}</div>
              </div>
            </div>

            {item.type === 'Experience' && (
              <>
                <div className="vd-detail-item">
                  <AccessTimeFilledIcon size={16} />
                  <div>
                    <div className="vd-detail-item__label">Duration</div>
                    <div className="vd-detail-item__val">{item.duration}</div>
                  </div>
                </div>
                <div className="vd-detail-item">
                  <GroupIcon size={16} />
                  <div>
                    <div className="vd-detail-item__label">Group Size</div>
                    <div className="vd-detail-item__val">Up to {item.maxParticipants} people</div>
                  </div>
                </div>
              </>
            )}

            {item.type === 'Restaurant' && (
              <>
                <div className="vd-detail-item">
                  <DiningIcon size={16} />
                  <div>
                    <div className="vd-detail-item__label">Cuisine</div>
                    <div className="vd-detail-item__val">{item.cuisineType}</div>
                  </div>
                </div>
                <div className="vd-detail-item">
                  <GroupIcon size={16} />
                  <div>
                    <div className="vd-detail-item__label">Total Tables</div>
                    <div className="vd-detail-item__val">{item.totalTables} tables ({item.seatingCapacity} seats)</div>
                  </div>
                </div>
              </>
            )}

            {item.type === 'Accommodation' && (
              <>
                <div className="vd-detail-item">
                  <HouseIcon size={16} />
                  <div>
                    <div className="vd-detail-item__label">Room Type</div>
                    <div className="vd-detail-item__val">{item.roomType}</div>
                  </div>
                </div>
                <div className="vd-detail-item">
                  <GroupIcon size={16} />
                  <div>
                    <div className="vd-detail-item__label">Capacity</div>
                    <div className="vd-detail-item__val">{item.totalRooms} rooms ({item.maxGuestsPerRoom} guests/room)</div>
                  </div>
                </div>
              </>
            )}
          </div>

          <div className="vd-detail-modal__footer">
            <div className="vd-detail-price">
              <span className="vd-detail-price__label">Price</span>
              <span className="vd-detail-price__val">LKR {Number(item.price).toLocaleString()}</span>
              <span className="vd-detail-price__unit">/ {item.priceUnit}</span>
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
      </div>
    </div>
  )
}
