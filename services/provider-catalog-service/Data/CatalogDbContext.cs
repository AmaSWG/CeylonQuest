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
	
	protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<ActivityListing>()
        .Property(l => l.Price)
        .HasPrecision(18, 2);

<<<<<<< Updated upstream
    modelBuilder.Entity<RestaurantListing>()
                .Property(l => l.PricePerPerson)
                .HasPrecision(18, 2);

    modelBuilder.Entity<AccommodationListing>()
                .Property(l => l.PricePerNight)
                .HasPrecision(18, 2);
=======
    // Indexes for public search and browsing performance
    modelBuilder.Entity<ActivityListing>()
        .HasIndex(l => new { l.IsActive, l.CreatedAt });

    modelBuilder.Entity<ActivityListing>()
        .HasIndex(l => new { l.IsActive, l.Price });
>>>>>>> Stashed changes
	
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
}
}