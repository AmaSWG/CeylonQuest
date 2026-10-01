using Microsoft.EntityFrameworkCore;
using PaymentNotificationService.Models;

namespace PaymentNotificationService.Data;

public class PaymentNotificationDbContext(DbContextOptions<PaymentNotificationDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<FailedEvent> FailedEvents => Set<FailedEvent>();
    public DbSet<BookingContext> BookingContexts => Set<BookingContext>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Notification>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EventKey).HasMaxLength(160).IsRequired();
            e.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Message).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.RefundStatus).HasMaxLength(64);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.RefundAmount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.EventKey, x.RecipientUserId }).IsUnique();
            e.HasIndex(x => new { x.RecipientUserId, x.IsRead, x.CreatedAtUtc });
        });
        model.Entity<ProcessedEvent>().HasKey(x => x.EventKey);

        model.Entity<ProcessedEvent>().Property(x => x.EventKey).HasMaxLength(160);

        model.Entity<ProcessedEvent>().Property(x => x.Topic).HasMaxLength(64);

        model.Entity<FailedEvent>().HasKey(x => x.EventKey);

        model.Entity<FailedEvent>().Property(x => x.EventKey).HasMaxLength(160);

        model.Entity<FailedEvent>().Property(x => x.Topic).HasMaxLength(64);
        
        model.Entity<BookingContext>().HasKey(x => x.BookingId);
    }
}
