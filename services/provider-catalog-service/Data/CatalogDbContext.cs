using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Models;

namespace ProviderCatalogService.Data;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    public DbSet<ActivityListing> ActivityListings { get; set; }
    public DbSet<RestaurantListing> RestaurantListings { get; set; }
    public DbSet<AccommodationListing> AccommodationListings { get; set; }
	public DbSet<ProviderApplication> ProviderApplications { get; set; }
	public DbSet<Provider> Providers { get; set; }
	public DbSet<AvailabilitySlot> AvailabilitySlots { get; set; }
    public DbSet<BookingCapacityRelease> BookingCapacityReleases { get; set; }
    public DbSet<ReviewRatingContribution> ReviewRatingContributions { get; set; }
	
	protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ReviewRatingContribution>(entity =>
        {
            entity.HasKey(r => r.ReviewId);
            entity.HasIndex(r => r.EventId).IsUnique();
            entity.Property(r => r.BookingType).HasMaxLength(20).IsRequired();
            entity.HasIndex(r => new { r.ListingId, r.BookingType });
            entity.ToTable("ReviewRatingContributions", table =>
                table.HasCheckConstraint("CK_ReviewRatingContributions_Rating", "`Rating` BETWEEN 1 AND 5"));
        });
        foreach (var type in new[] { typeof(ActivityListing), typeof(RestaurantListing), typeof(AccommodationListing) })
        {
            modelBuilder.Entity(type).Property<decimal>(nameof(IListingRating.AverageRating)).HasPrecision(3, 2).HasDefaultValue(0m);
            modelBuilder.Entity(type).Property<long>(nameof(IListingRating.RatingSum)).HasDefaultValue(0L).IsConcurrencyToken();
            modelBuilder.Entity(type).Property<int>(nameof(IListingRating.ReviewCount)).HasDefaultValue(0).IsConcurrencyToken();
        }
        modelBuilder.Entity<BookingCapacityRelease>().HasKey(r => r.BookingId);

        modelBuilder.Entity<ActivityListing>()
            .Property(l => l.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<RestaurantListing>()
                    .Property(l => l.PricePerPerson)
                    .HasPrecision(18, 2);

        modelBuilder.Entity<AccommodationListing>()
                    .Property(l => l.PricePerNight)
                    .HasPrecision(18, 2);

        // Indexes for public search and browsing performance
        modelBuilder.Entity<ActivityListing>()
            .HasIndex(l => new { l.IsActive, l.CreatedAt });

        modelBuilder.Entity<ActivityListing>()
            .HasIndex(l => new { l.IsActive, l.Price });

        modelBuilder.Entity<ProviderApplication>()
                .Property(p => p.Status)
                .HasConversion<string>();

        modelBuilder.Entity<Provider>()
            .HasMany(p => p.ActivityListings)
            .WithOne(a => a.Provider)
            .HasForeignKey(a => a.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProviderApplication>()
            .HasIndex(p => p.Email);

        modelBuilder.Entity<Provider>()
            .HasIndex(p => p.Email);

        modelBuilder.Entity<AvailabilitySlot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ListingId, e.Date, e.TimeSlot }).IsUnique();
            entity.Property(e => e.TimeSlot).HasMaxLength(100);
            entity.Property(e => e.ListingType).HasMaxLength(50);
        });
    }
}
