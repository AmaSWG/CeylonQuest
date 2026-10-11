import ListingReviews from '../../../components/ListingReviews'

export default function ProviderReviewsTab({ services = [], bookingType }) {
  return <section className="cq-provider-reviews"><h2>Reviews &amp; Ratings</h2>
    {services.length === 0 && <p>No listings available.</p>}
    {services.map(listing => <article key={listing.id}><h3>{listing.title || listing.name || listing.roomType || 'Listing'}</h3>
      <ListingReviews listingId={listing.id} bookingType={bookingType} allowResponse />
    </article>)}
  </section>
}
