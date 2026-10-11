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
    public DbSet<PlatformReview> PlatformReviews => Set<PlatformReview>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<ProviderTimeSlot> ProviderTimeSlots => Set<ProviderTimeSlot>();
    public DbSet<ProviderServicePrice> ProviderServicePrices => Set<ProviderServicePrice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<PlatformReview>(entity =>
        {
            entity.HasKey(review => review.Id);

            entity.Property(review => review.Content)
                .IsRequired()
                .HasMaxLength(2000);

            entity.HasOne(review => review.User)
                .WithMany()
                .HasForeignKey(review => review.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(review => review.CreatedAtUtc);

            entity.HasIndex(review => new
            {
             review.Rating,
                review.CreatedAtUtc
            });

            entity.ToTable("PlatformReviews", table =>
            {
                table.HasCheckConstraint(
                    "CK_PlatformReviews_Rating",
                    "`Rating` BETWEEN 1 AND 5");
            });
        });
    }
}