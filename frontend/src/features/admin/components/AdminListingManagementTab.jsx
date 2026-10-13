import { useCallback, useEffect, useState } from 'react'
import ConfirmModal from '../../../components/ConfirmModal'
import {
  getAdminListings,
  removeAdminListing,
} from '../../../api/adminManagementApi'

function listingStatus(listing) {
  if (listing.isRemoved || listing.status === 'Removed') return 'Removed'
  return listing.isActive ? 'Active' : 'Inactive'
}

function listingKey(listing) {
  return `${listing.bookingType}:${listing.id}`
}

export default function AdminListingManagementTab({ token, showToast }) {
  const [listings, setListings] = useState([])
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [actionError, setActionError] = useState('')
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('all')
  const [categoryFilter, setCategoryFilter] = useState('all')
  const [selectedListing, setSelectedListing] = useState(null)
  const [reason, setReason] = useState('')
  const [confirming, setConfirming] = useState(false)
  const [removing, setRemoving] = useState(false)

  const loadListings = useCallback(async (signal) => {
    setLoading(true)
    setLoadError('')

    try {
      const data = await getAdminListings(token, signal)

      if (!Array.isArray(data)) {
        throw new Error('The listing API returned an invalid response.')
      }

      if (!signal?.aborted) setListings(data)
    } catch (error) {
      if (error.name !== 'AbortError' && !signal?.aborted) {
        setLoadError(error.message || 'Failed to load listings.')
      }
    } finally {
      if (!signal?.aborted) setLoading(false)
    }
  }, [token])

  useEffect(() => {
    const controller = new AbortController()
    loadListings(controller.signal)
    return () => controller.abort()
  }, [loadListings])

  function selectListing(listing) {
    setSelectedListing(listing)
    setReason('')
    setActionError('')
    setConfirming(false)
  }

  async function confirmRemoval() {
    if (!selectedListing || removing) return

    if (!reason.trim() || reason.length > 1000) {
      setActionError('Enter a removal reason of 1–1000 characters.')
      return
    }

    setRemoving(true)
    setActionError('')

    try {
      await removeAdminListing(token, selectedListing, reason)

      const removedKey = listingKey(selectedListing)

      // Update immediately; a failed later refresh cannot leave this active.
      setListings(previous =>
        previous.map(listing =>
          listingKey(listing) === removedKey
            ? {
                ...listing,
                isActive: false,
                isRemoved: true,
                status: 'Removed',
              }
            : listing,
        ),
      )

      showToast(
        `${selectedListing.serviceName} was removed successfully.`,
        'Success',
      )

      setSelectedListing(null)
      setConfirming(false)
      setReason('')
    } catch (error) {
      setConfirming(false)
      setActionError(error.message || 'Failed to remove listing.')
      showToast(error.message || 'Failed to remove listing.', 'Error')
    } finally {
      setRemoving(false)
    }
  }

  const query = search.trim().toLowerCase()

  const filtered = listings.filter(listing => {
    const matchesSearch = !query || [
      listing.serviceName,
      listing.providerName,
      listing.id,
    ].some(value => String(value || '').toLowerCase().includes(query))

    return matchesSearch &&
      (statusFilter === 'all' ||
        listingStatus(listing) === statusFilter) &&
      (categoryFilter === 'all' ||
        listing.bookingType === categoryFilter)
  })

  return (
    <div>
      <div className="ad-page-header">
        <div className="ad-page-header__left">
          <h1>Listing Management</h1>
          <p>Review platform listings and remove active listings when required.</p>
        </div>
      </div>

      <div className="ad-filter-bar">
        <div className="ad-search-wrap">
          <input
            className="ad-search-input"
            aria-label="Search listings"
            placeholder="Search listing, provider, or listing ID"
            value={search}
            onChange={event => setSearch(event.target.value)}
          />
        </div>

        <select
          aria-label="Listing status"
          value={statusFilter}
          onChange={event => setStatusFilter(event.target.value)}
        >
          <option value="all">All statuses</option>
          <option value="Active">Active</option>
          <option value="Inactive">Inactive</option>
          <option value="Removed">Removed</option>
        </select>

        <select
          aria-label="Listing category"
          value={categoryFilter}
          onChange={event => setCategoryFilter(event.target.value)}
        >
          <option value="all">All categories</option>
          <option value="Experience">Experience</option>
          <option value="Restaurant">Restaurant</option>
          <option value="Accommodation">Accommodation</option>
        </select>

        <button
          type="button"
          className="ad-row-btn ad-row-btn--view"
          disabled={loading || removing || Boolean(selectedListing)}
          onClick={() => loadListings()}
        >
          {loading ? 'Loading…' : 'Refresh'}
        </button>
      </div>

      {loading && <p role="status">Loading listings…</p>}

      {loadError && (
        <p role="alert" style={{ color: '#b42318' }}>
          {loadError}
        </p>
      )}

      {selectedListing && (
        <section
          className="ad-card"
          aria-label="Listing removal details"
          style={{ marginBottom: '20px' }}
        >
          <div className="ad-card__body">
            <h2>Remove {selectedListing.serviceName}</h2>
            <p>
              Provider: {selectedListing.providerName}. The listing will be
              hidden from visitors and retained for administration.
            </p>

            <label htmlFor="listing-removal-reason">
              Removal reason
            </label>

            <textarea
              id="listing-removal-reason"
              value={reason}
              maxLength={1000}
              disabled={removing}
              onChange={event => setReason(event.target.value)}
              rows={3}
              style={{ display: 'block', width: '100%', margin: '12px 0' }}
            />

            {actionError && (
              <p role="alert" style={{ color: '#b42318' }}>
                {actionError}
              </p>
            )}

            <div className="ad-row-actions">
              <button
                type="button"
                className="ad-quick-btn ad-quick-btn--danger"
                disabled={removing || !reason.trim()}
                onClick={() => setConfirming(true)}
              >
                Continue to confirmation
              </button>

              <button
                type="button"
                className="ad-row-btn ad-row-btn--view"
                disabled={removing}
                onClick={() => {
                  setSelectedListing(null)
                  setActionError('')
                }}
              >
                Cancel
              </button>
            </div>
          </div>
        </section>
      )}

      <ConfirmModal
        isOpen={confirming}
        title="Remove Listing"
        message={`Remove "${selectedListing?.serviceName}" from the platform? Visitors will no longer see this listing.`}
        confirmText="Remove Listing"
        confirmVariant="danger"
        loading={removing}
        onConfirm={confirmRemoval}
        onCancel={() => {
          if (!removing) setConfirming(false)
        }}
      />

      {!loading && !loadError && (
        <div className="ad-card">
          <div className="ad-card__body" style={{ padding: 0 }}>
            {filtered.length === 0 ? (
              <div className="ad-empty">
                <p className="ad-empty__title">No listings found</p>
                <p className="ad-empty__msg">
                  No listings match the selected filters.
                </p>
              </div>
            ) : (
              <div className="ad-table-wrap">
                <table className="ad-table  ad-listings-table">
                  <thead>
                    <tr>
                      <th>Listing</th>
                      <th>Provider</th>
                      <th>Category</th>
                      <th>Status</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filtered.map(listing => {
                      const status = listingStatus(listing)

                      return (
                        <tr key={listingKey(listing)}>
                          <td>
                            <strong>{listing.serviceName}</strong>
                            <div style={{ fontSize: '12px' }}>{listing.id}</div>
                          </td>
                          <td>{listing.providerName}</td>
                          <td>{listing.bookingType}</td>
                          <td>
                            <span
                              className={`ad-badge ad-badge--${
                                status === 'Active' ? 'active' : 'inactive'
                              }`}
                            >
                              {status}
                            </span>
                          </td>
                          <td>
                            {status === 'Active' ? (
                              <button
                                type="button"
                                className="ad-quick-btn ad-quick-btn--danger"
                                disabled={removing || Boolean(selectedListing)}
                                onClick={() => selectListing(listing)}
                              >
                                Remove Listing
                              </button>
                            ) : (
                              <span>—</span>
                            )}
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  )
}