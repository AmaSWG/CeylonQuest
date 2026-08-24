using IdentityService.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<ProviderApplication> ProviderApplications => Set<ProviderApplication>();
    public DbSet<ProviderTimeSlot> ProviderTimeSlots => Set<ProviderTimeSlot>();
    public DbSet<ProviderServicePrice> ProviderServicePrices => Set<ProviderServicePrice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}