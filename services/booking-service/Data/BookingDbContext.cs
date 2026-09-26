using BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(
        DbContextOptions<BookingDbContext> options)
        : base(options)
    {
    }

    // =========================================================
    // Story 7.1 - Experience Bookings
    // =========================================================
    public DbSet<Booking> Bookings { get; set; }

    // =========================================================
    // Story 8.1 - Restaurant Reservations
    // =========================================================
    public DbSet<RestaurantReservation> RestaurantReservations { get; set; }

    // =========================================================
    // Accommodation Bookings
    // =========================================================
    public DbSet<AccommodationBooking> AccommodationBookings { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        // =========================================================
        // Story 7.1 - Experience Booking
        // =========================================================

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.Property(b => b.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(b => b.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(b => b.Status)
                .HasConversion<string>();

            entity.Property(b => b.PaymentStatus)
                .HasConversion<string>();

            entity.HasIndex(b => b.VisitorId);

            entity.HasIndex(b => b.ListingId);

            entity.HasIndex(b => b.Status);
        });


        // =========================================================
        // Story 8.1 - Restaurant Reservation
        // =========================================================

        modelBuilder.Entity<RestaurantReservation>(entity =>
        {
            entity.HasKey(r => r.Id);

            // Store ReservationStatus enum as text
            entity.Property(r => r.Status)
                .HasConversion<string>();

            // Restaurant price for one person
            entity.Property(r => r.PricePerPerson)
                .HasPrecision(18, 2);

            // PricePerPerson × PartySize
            entity.Property(r => r.TotalPrice)
                .HasPrecision(18, 2);

            // Useful indexes
            entity.HasIndex(r => r.VisitorId);

            entity.HasIndex(r => r.RestaurantId);

            entity.HasIndex(r => r.Status);

            // Restaurant + Date + Time Slot
            entity.HasIndex(r => new
            {
                r.RestaurantId,
                r.ReservationDate,
                r.TimeSlot
            });
        });


        // =========================================================
        // Accommodation Booking
        // =========================================================

        modelBuilder.Entity<AccommodationBooking>(entity =>
        {
            entity.HasKey(a => a.Id);

            // Store AccommodationBookingStatus enum as text
            entity.Property(a => a.Status)
                .HasConversion<string>();

            // Price for one night
            entity.Property(a => a.PricePerNight)
                .HasPrecision(18, 2);

            // PricePerNight × NumberOfNights
            entity.Property(a => a.TotalPrice)
                .HasPrecision(18, 2);

            // Cancellation refund percentage
            entity.Property(a => a.RefundPercentage)
                .HasPrecision(18, 2);

            // Cancellation refund amount
            entity.Property(a => a.RefundAmount)
                .HasPrecision(18, 2);

            // Useful indexes
            entity.HasIndex(a => a.VisitorId);

            entity.HasIndex(a => a.AccommodationId);

            entity.HasIndex(a => a.Status);

            // Accommodation + Check-in + Check-out
            entity.HasIndex(a => new
            {
                a.AccommodationId,
                a.CheckInDate,
                a.CheckOutDate
            });
        });
    }
}