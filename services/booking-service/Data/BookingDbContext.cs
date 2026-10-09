using BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Data;

public class BookingDbContext : DbContext
{
    public DbSet<ListingReview> ListingReviews { get; set; }
    public DbSet<PlatformReview> PlatformReviews { get; set; }
    public DbSet<ReviewOutboxMessage> ReviewOutboxMessages { get; set; }
    public DbSet<BookingCancellationMessage> BookingCancellationMessages { get; set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Modified).ToList())
        {
            if (entry.Entity is not IPayableBooking booking ||
                booking.Status != BookingStatus.Cancelled ||
                entry.Property("Status").OriginalValue?.ToString() == "Cancelled") continue;
            var evt = new BookingService.Events.BookingCanceledEvent
            {
                BookingId = booking.Id, ListingId = booking.ListingId,
                VisitorId = booking.VisitorId, ProviderUserId = booking.ProviderUserId,
                ListingType = booking.BookingType, BookingDate = booking.BookingDate.ToString("yyyy-MM-dd"),
                TimeSlot = booking.TimeSlot, ParticipantCount = booking.ParticipantCount,
                CanceledAt = booking.UpdatedAt, Reason = "Booking cancelled."
            };
            BookingCancellationMessages.Add(new BookingCancellationMessage
            {
                BookingId = booking.Id, Payload = System.Text.Json.JsonSerializer.Serialize(evt)
            });
        }
        // EF persists the state transition and release message in one transaction.
        return base.SaveChangesAsync(cancellationToken);
    }
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
    // Simulated Payment Transactions
    // =========================================================
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

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
        modelBuilder.Entity<PlatformReview>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Comment).HasMaxLength(2000).IsRequired();
            entity.HasIndex(r => new { r.Rating, r.CreatedAtUtc });
            entity.ToTable("PlatformReviews", table =>
                table.HasCheckConstraint("CK_PlatformReviews_Rating", "`Rating` BETWEEN 1 AND 5"));
        });
        modelBuilder.Entity<ListingReview>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.BookingType).HasMaxLength(20).IsRequired();
            entity.Property(r => r.Comment).HasMaxLength(2000).IsRequired();
            entity.HasIndex(r => new { r.BookingType, r.BookingId }).IsUnique();
            entity.HasIndex(r => new { r.ListingId, r.BookingType, r.Rating, r.CreatedAtUtc });
            entity.ToTable("ListingReviews", table =>
                table.HasCheckConstraint("CK_ListingReviews_Rating", "`Rating` BETWEEN 1 AND 5"));
        });
        modelBuilder.Entity<ReviewOutboxMessage>().HasKey(m => m.Id);
        modelBuilder.Entity<ReviewOutboxMessage>().Property(m => m.Payload).IsRequired();
        modelBuilder.Entity<ReviewOutboxMessage>().HasIndex(m => m.PublishedAtUtc);
        modelBuilder.Entity<BookingCancellationMessage>().HasKey(m => m.BookingId);

        // State transitions must compare the state originally read, including
        // when payment and expiry run in different service instances.
        foreach (var type in new[] { typeof(Booking), typeof(RestaurantReservation), typeof(AccommodationBooking) })
        {
            modelBuilder.Entity(type).Property("Status").IsConcurrencyToken();
            modelBuilder.Entity(type).Property("PaymentStatus").IsConcurrencyToken();
            modelBuilder.Entity(type).Property<DateTime>("CreatedAt").HasConversion(
                value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        }


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
        // Simulated Payment Transaction
        // =========================================================
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            // IMPORTANT:
            // Existing migration created this table using
            // the singular name "PaymentTransaction".
            entity.ToTable("PaymentTransaction");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Amount)
                .HasPrecision(18, 2);

            entity.Property(p => p.Status)
                .HasConversion<string>();

            entity.HasIndex(p => p.BookingId);

            entity.HasIndex(p => p.VisitorId);

            entity.HasIndex(p => p.TransactionReference)
                .IsUnique();

            entity.Property(p => p.BookingType).HasDefaultValue("Experience");
        });


        // =========================================================
        // Story 8.1 - Restaurant Reservation
        // =========================================================
        modelBuilder.Entity<RestaurantReservation>(entity =>
        {
            entity.Property(r => r.PaymentStatus).HasConversion<string>();
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
            entity.Property(a => a.PaymentStatus).HasConversion<string>();
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
