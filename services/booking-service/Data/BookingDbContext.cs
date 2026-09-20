using BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
    }
}