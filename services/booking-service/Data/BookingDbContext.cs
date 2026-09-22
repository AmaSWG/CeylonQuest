using BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options)
        : base(options)
    {
    }

    // Story 7.1 - Experience bookings
    public DbSet<Booking> Bookings { get; set; }

    // Story 8.1 - Restaurant reservations
    public DbSet<RestaurantReservation> RestaurantReservations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        /*
         * Story 7.1 - Booking configuration
         */
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

        /*
         * Story 8.1 - Restaurant reservation configuration
         */
        modelBuilder.Entity<RestaurantReservation>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Status)
                .HasConversion<string>();

            entity.HasIndex(r => r.VisitorId);

            entity.HasIndex(r => r.RestaurantId);

            entity.HasIndex(r => r.Status);

            /*
             * Helps availability checks for a particular
             * restaurant, date and time slot.
             */
            entity.HasIndex(r => new
            {
                r.RestaurantId,
                r.ReservationDate,
                r.TimeSlot
            });
        });
    }
}