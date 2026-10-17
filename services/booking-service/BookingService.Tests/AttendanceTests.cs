using System.Security.Claims;
using BookingService.Controllers;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Models;
using BookingService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using BookingService.Events;
using Shared.Kafka;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace BookingService.Tests;

public class AttendanceTests
{
    private static readonly DateTime Now = new(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(Now); }
    private static BookingDbContext Db() => new(new DbContextOptionsBuilder<BookingDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static IPayableBooking Seed(BookingDbContext db, string type)
    {
        var id = Guid.NewGuid(); var listing = Guid.NewGuid(); var visitor = Guid.NewGuid();
        IPayableBooking booking = type switch
        {
            "Experience" => new Booking { Id = id, ListingId = listing, VisitorId = visitor, ScheduledEndAtUtc = Now.AddHours(-1), ProviderId = Guid.NewGuid(), Status = BookingStatus.Confirmed },
            "Restaurant" => new RestaurantReservation { Id = id, RestaurantId = listing, VisitorId = visitor, ScheduledEndAtUtc = Now.AddHours(-1), ProviderId = Guid.NewGuid(), Status = ReservationStatus.Confirmed },
            _ => new AccommodationBooking { Id = id, AccommodationId = listing, VisitorId = visitor, ScheduledEndAtUtc = Now.AddHours(-1), ProviderId = Guid.NewGuid(), Status = AccommodationBookingStatus.Confirmed }
        };
        booking.PaymentStatus = PaymentStatus.Paid;
        db.Add(booking); db.SaveChanges(); return booking;
    }
    private static Mock<ICatalogService> Catalog(IPayableBooking booking, bool owns = true)
    {
        var ids = new ProviderReportListingIds();
        if (owns) (booking.BookingType == "Experience" ? ids.ActivityIds : booking.BookingType == "Restaurant" ? ids.RestaurantIds : ids.AccommodationIds).Add(booking.ListingId);
        var mock = new Mock<ICatalogService>();
        mock.Setup(c => c.GetReportOwnedListingIdsAsync("provider-token")).ReturnsAsync(ids);
        return mock;
    }
    private static AttendanceController Controller(BookingDbContext db, ICatalogService catalog, Guid? user = null)
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, (user ?? Guid.NewGuid()).ToString()) }, "test"));
        http.Request.Headers.Authorization = "Bearer provider-token";
        return new(db, catalog, new Clock()) { ControllerContext = new() { HttpContext = http } };
    }

    [Theory]
    [InlineData("Experience")][InlineData("Restaurant")][InlineData("Accommodation")]
    public async Task PastDate_LeavesAttendancePending_AndProviderVerificationEnablesReview(string type)
    {
        await using var db = Db(); var booking = Seed(db, type); var verifier = Guid.NewGuid();
        Assert.Equal(AttendanceStatus.Pending, booking.AttendanceStatus);
        var controller = Controller(db, Catalog(booking).Object, verifier);
        Assert.IsType<OkObjectResult>(await controller.Verify(type, booking.Id, new("Attended"), default));
        Assert.Equal(BookingStatus.Completed, booking.Status); Assert.Equal(AttendanceStatus.Attended, booking.AttendanceStatus);
        Assert.Equal(PaymentStatus.Paid, booking.PaymentStatus); Assert.Equal(verifier, booking.AttendanceVerifiedByUserId);
        Assert.Equal(Now, booking.AttendanceVerifiedAtUtc);
        Assert.Equal("review.eligibility-granted", Assert.Single(db.ReviewOutboxMessages).Topic);
        var service = new ReviewService(db, Catalog(booking).Object, new Clock());
        var review = await service.CreateAsync(booking.VisitorId, new() { BookingId = booking.Id, BookingType = type, Rating = 5, Comment = "Great" });
        Assert.True(review.AttendanceVerified);
        Assert.Equal(2, await db.ReviewOutboxMessages.CountAsync());
    }

    [Theory]
    [InlineData("Experience")][InlineData("Restaurant")][InlineData("Accommodation")]
    public async Task AnotherProvider_CannotVerifyAttendance(string type)
    {
        await using var db = Db(); var booking = Seed(db, type);
        Assert.IsType<ForbidResult>(await Controller(db, Catalog(booking, false).Object).Verify(type, booking.Id, new("Attended"), default));
        Assert.Equal(AttendanceStatus.Pending, booking.AttendanceStatus); Assert.Empty(db.ReviewOutboxMessages);
    }

    [Theory]
    [InlineData(AttendanceStatus.Pending)][InlineData(AttendanceStatus.NoShow)]
    public async Task CompletedPastBooking_AllowsPendingAttendance_ButRejectsNoShow(AttendanceStatus status)
    {
        await using var db = Db(); var booking = Seed(db, "Experience"); booking.Status = BookingStatus.Completed; booking.AttendanceStatus = status;
        await db.SaveChangesAsync();
        var service = new ReviewService(db, Catalog(booking).Object, new Clock());
        var eligibility = await service.GetEligibilityAsync(booking.VisitorId, booking.ListingId, "Experience");
        if (status == AttendanceStatus.Pending)
        {
            Assert.Equal(booking.Id, Assert.Single(eligibility.EligibleBookings).BookingId);
            Assert.Null(eligibility.Message);
            var review = await service.CreateAsync(booking.VisitorId, new() { BookingId = booking.Id, BookingType = "Experience", Rating = 5, Comment = "Great" });
            Assert.False(review.AttendanceVerified);
            var saved = Assert.Single(db.ListingReviews);
            Assert.Equal(review.Id, saved.Id); Assert.Equal(booking.Id, saved.BookingId);
            Assert.Equal(5, saved.Rating); Assert.Equal("Great", saved.Comment);
            Assert.False(saved.AttendanceVerified);
            Assert.Equal("review.submitted", Assert.Single(db.ReviewOutboxMessages).Topic);
            Assert.Equal(AttendanceStatus.Pending, booking.AttendanceStatus);
            Assert.Null(booking.AttendanceVerifiedAtUtc);
            Assert.Null(booking.AttendanceVerifiedByUserId);
            return;
        }
        Assert.Empty(eligibility.EligibleBookings);
        Assert.Contains("NoShow", eligibility.Message);
        var ex = await Assert.ThrowsAsync<ReviewException>(() => service.CreateAsync(booking.VisitorId, new() { BookingId = booking.Id, BookingType = "Experience", Rating = 5, Comment = "Great" }));
        Assert.Equal(409, ex.StatusCode); Assert.Contains("NoShow", ex.Message);
        Assert.Empty(db.ListingReviews); Assert.Empty(db.ReviewOutboxMessages);
    }

    [Fact]
    public async Task NoShow_DoesNotInviteOrAllowProviderChanges()
    {
        await using var db = Db(); var booking = Seed(db, "Experience"); var controller = Controller(db, Catalog(booking).Object);
        Assert.IsType<OkObjectResult>(await controller.Verify("Experience", booking.Id, new("NoShow"), default));
        Assert.Empty(db.ReviewOutboxMessages);
        Assert.Equal(409, Assert.IsType<ObjectResult>(await controller.Verify("Experience", booking.Id, new("Attended"), default)).StatusCode);
    }

    [Theory]
    [InlineData("Experience")][InlineData("Restaurant")][InlineData("Accommodation")]
    public async Task FutureSchedule_RejectsVerification(string type)
    {
        await using var db = Db(); var booking = Seed(db, type);
        switch (booking) { case Booking b: b.ScheduledEndAtUtc = Now; break; case RestaurantReservation r: r.ScheduledEndAtUtc = Now; break; case AccommodationBooking a: a.ScheduledEndAtUtc = Now; break; }
        Assert.Equal(409, Assert.IsType<ObjectResult>(await Controller(db, Catalog(booking).Object).Verify(type, booking.Id, new("Attended"), default)).StatusCode);
        Assert.Equal(AttendanceStatus.Pending, booking.AttendanceStatus); Assert.Empty(db.ReviewOutboxMessages);
    }

    [Theory]
    [InlineData(true)][InlineData(false)]
    public async Task ProviderResponse_RequiresOwnershipAndPreservesVisitorFeedback(bool owns)
    {
        await using var db = Db(); var booking = Seed(db, "Experience");
        var review = new ListingReview { Id = Guid.NewGuid(), BookingId = booking.Id, BookingType = "Experience", ListingId = booking.ListingId, VisitorId = booking.VisitorId, Rating = 1, Comment = "Disappointing" };
        db.ListingReviews.Add(review); await db.SaveChangesAsync();
        var result = await Controller(db, Catalog(booking, owns).Object).Respond(review.Id, new("Thank you for your feedback"), default);
        if (owns) { Assert.IsType<OkObjectResult>(result); Assert.NotNull(review.ProviderRespondedByUserId); }
        else { Assert.IsType<ForbidResult>(result); Assert.Null(review.ProviderResponse); }
        Assert.Equal(1, review.Rating); Assert.Equal("Disappointing", review.Comment); Assert.Empty(db.ReviewOutboxMessages);
    }

    [Fact]
    public async Task VisitorDispute_RequiresOwnership_AndAdminResolutionAuditsAndInvites()
    {
        await using var db = Db(); var booking = Seed(db, "Experience");
        Assert.IsType<ForbidResult>(await Controller(db, Catalog(booking).Object).Dispute("Experience", booking.Id, new("I attended"), default));
        var visitor = Controller(db, Catalog(booking).Object, booking.VisitorId);
        Assert.IsType<OkObjectResult>(await visitor.Dispute("Experience", booking.Id, new("I attended"), default));
        var dispute = Assert.Single(db.AttendanceDisputes);
        Assert.Equal(409, Assert.IsType<ObjectResult>(await visitor.Dispute("Experience", booking.Id, new("Again"), default)).StatusCode);
        Assert.IsType<OkObjectResult>(await Controller(db, Catalog(booking).Object).Resolve(dispute.Id, new("Attended", "Evidence confirms attendance"), default));
        Assert.Equal(AttendanceStatus.Attended, booking.AttendanceStatus); Assert.NotNull(dispute.ResolvedByUserId);
        Assert.Single(db.ReviewOutboxMessages); Assert.Equal(PaymentStatus.Paid, booking.PaymentStatus);
    }

    [Theory]
    [InlineData("Experience")][InlineData("Restaurant")][InlineData("Accommodation")]
    public async Task CancelledBooking_CannotBeVerifiedOrInvited(string type)
    {
        await using var db = Db(); var booking = Seed(db, type); booking.Status = BookingStatus.Cancelled;
        await db.SaveChangesAsync();
        Assert.Equal(409, Assert.IsType<ObjectResult>(await Controller(db, Catalog(booking).Object).Verify(type, booking.Id, new("Attended"), default)).StatusCode);
        Assert.Equal(AttendanceStatus.Pending, booking.AttendanceStatus); Assert.Empty(db.ReviewOutboxMessages);
    }

    [Fact]
    public async Task EligibilityOutbox_RetriesBrokerFailureWithoutPublishingRatingEvent()
    {
        await using var db = Db(); var booking = Seed(db, "Experience");
        await Controller(db, Catalog(booking).Object).Verify("Experience", booking.Id, new("Attended"), default);
        var kafka = new Mock<IKafkaProducer>(MockBehavior.Strict);
        kafka.Setup(k => k.PublishAsync("review.eligibility-granted", booking.Id.ToString(), It.IsAny<ReviewEligibilityGrantedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Unavailable"));
        var publisher = new ReviewOutboxPublisher(db, kafka.Object, new Clock());
        await Assert.ThrowsAsync<IOException>(() => publisher.PublishAsync());
        Assert.Null(Assert.Single(db.ReviewOutboxMessages).PublishedAtUtc);
        kafka.Setup(k => k.PublishAsync("review.eligibility-granted", booking.Id.ToString(), It.IsAny<ReviewEligibilityGrantedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        await publisher.PublishAsync(); await publisher.PublishAsync();
        Assert.NotNull(Assert.Single(db.ReviewOutboxMessages).PublishedAtUtc);
        kafka.Verify(k => k.PublishAsync("review.eligibility-granted", booking.Id.ToString(), It.IsAny<ReviewEligibilityGrantedEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Theory]
    [InlineData(nameof(AttendanceController.Verify), "Provider")]
    [InlineData(nameof(AttendanceController.Respond), "Provider")]
    [InlineData(nameof(AttendanceController.Dispute), "Visitor")]
    [InlineData(nameof(AttendanceController.Resolve), "Admin")]
    [InlineData(nameof(AttendanceController.Disputes), "Admin")]
    public void Endpoints_RequireAppropriateRole(string method, string role)
        => Assert.Equal(role, Assert.Single(typeof(AttendanceController).GetMethod(method)!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()).Roles);

    private sealed class AttendanceMigration : booking_service.Migrations.AddAttendanceVerificationAndReviewResponses
    {
        public MigrationBuilder Operations() { var builder = new MigrationBuilder("Pomelo.EntityFrameworkCore.MySql"); base.Up(builder); return builder; }
    }

    [Fact]
    public void Migration_DefaultsHistoricalAttendanceToPending_AndPreservesExistingState()
    {
        var operations = new AttendanceMigration().Operations().Operations;
        var attendance = operations.OfType<AddColumnOperation>().Where(o => o.Name == "AttendanceStatus").ToList();
        Assert.Equal(3, attendance.Count); Assert.All(attendance, c => Assert.Equal("Pending", c.DefaultValue));
        Assert.DoesNotContain(operations, o => o is UpdateDataOperation or DeleteDataOperation or AlterColumnOperation);
        Assert.DoesNotContain(operations.OfType<AddColumnOperation>(), o => o.Name is "Status" or "PaymentStatus");
        Assert.Equal(false, Assert.Single(operations.OfType<AddColumnOperation>(), o => o.Name == "AttendanceVerified").DefaultValue);
        Assert.Equal("review.submitted", Assert.Single(operations.OfType<AddColumnOperation>(), o => o.Name == "Topic").DefaultValue);
    }
}
