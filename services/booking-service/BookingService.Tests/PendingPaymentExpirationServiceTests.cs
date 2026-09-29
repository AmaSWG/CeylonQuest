using BookingService.Data;
using BookingService.Events;
using BookingService.Models;
using BookingService.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Kafka;

namespace BookingService.Tests;

public class PendingPaymentExpirationServiceTests
{
    [Fact]
    public async Task Expire_OnlyExpiresOldPendingBookings_AndPublishesReleaseOnce()
    {
        var now = DateTime.UtcNow;
        await using var db = CreateDb();
        var old = new Booking { Id = Guid.NewGuid(), VisitorId = Guid.NewGuid(), ListingId = Guid.NewGuid(), CreatedAt = now.AddMinutes(-16), Status = BookingStatus.PendingPayment };
        var recent = new Booking { Id = Guid.NewGuid(), VisitorId = Guid.NewGuid(), ListingId = Guid.NewGuid(), CreatedAt = now.AddMinutes(-14), Status = BookingStatus.PendingPayment };
        var paid = new Booking { Id = Guid.NewGuid(), VisitorId = Guid.NewGuid(), ListingId = Guid.NewGuid(), CreatedAt = now.AddHours(-1), Status = BookingStatus.Confirmed, PaymentStatus = PaymentStatus.Paid };
        db.AddRange(old, recent, paid);
        await db.SaveChangesAsync();
        var kafka = new Mock<IKafkaProducer>();
        var service = new PendingPaymentExpirationService(db, kafka.Object);

        Assert.Equal(1, await service.ExpireAsync(now));
        Assert.Equal(BookingStatus.Cancelled, old.Status);
        Assert.Equal(PaymentStatus.Failed, old.PaymentStatus);
        Assert.Equal(BookingStatus.PendingPayment, recent.Status);
        Assert.Equal(BookingStatus.Confirmed, paid.Status);
        Assert.Equal(0, await service.ExpireAsync(now));
        await service.PublishPendingCancellationsAsync();
        await service.PublishPendingCancellationsAsync();
        kafka.Verify(k => k.PublishAsync("booking.canceled", old.Id.ToString(),
            It.Is<BookingCanceledEvent>(e => e.ParticipantCount == old.ParticipantCount),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Restaurant", 3)]
    [InlineData("Accommodation", 1)]
    public async Task Expire_PublishesCorrectCapacityForEveryBookingType(string type, int expectedCapacity)
    {
        var now = DateTime.UtcNow;
        await using var db = CreateDb();
        IPayableBooking booking = type == "Restaurant"
            ? new RestaurantReservation { Id = Guid.NewGuid(), VisitorId = Guid.NewGuid(), RestaurantId = Guid.NewGuid(), PartySize = 3, CreatedAt = now.AddMinutes(-16) }
            : new AccommodationBooking { Id = Guid.NewGuid(), VisitorId = Guid.NewGuid(), AccommodationId = Guid.NewGuid(), GuestCount = 3, CreatedAt = now.AddMinutes(-16) };
        db.Add(booking);
        await db.SaveChangesAsync();
        var kafka = new Mock<IKafkaProducer>();

        await new PendingPaymentExpirationService(db, kafka.Object).ExpireAsync(now);
        await new PendingPaymentExpirationService(db, kafka.Object).PublishPendingCancellationsAsync();

        kafka.Verify(k => k.PublishAsync("booking.canceled", booking.Id.ToString(),
            It.Is<BookingCanceledEvent>(e => e.ListingType == type && e.ParticipantCount == expectedCapacity),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static BookingDbContext CreateDb() => new(new DbContextOptionsBuilder<BookingDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
