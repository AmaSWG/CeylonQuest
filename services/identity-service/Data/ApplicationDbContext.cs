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
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
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

        modelBuilder.Entity<AdminAuditLog>(entity =>
       {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.ActionType)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(a => a.TargetType)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(a => a.PreviousStatus)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(a => a.NewStatus)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(a => new { a.TimestampUtc, a.Id });
            entity.HasIndex(a => a.AdministratorId);
            entity.HasIndex(a => a.TargetId);
        });

        // Detect concurrent status changes instead of recording an incorrect transition.
        modelBuilder.Entity<User>()
            .Property(u => u.IsActive)
            .IsConcurrencyToken();
    }
}