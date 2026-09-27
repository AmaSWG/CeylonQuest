using System.ComponentModel.DataAnnotations;
using BookingService.Controllers;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Models;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BookingService.Tests;

public class AdminBookingsRevenueReportTests
{
    private static readonly DateOnly Day = new(2026, 9, 15);

    private static BookingDbContext CreateDb() => new(new DbContextOptionsBuilder<BookingDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Mock<ICatalogService> Catalog(Guid activityId, Guid restaurantId, Guid accommodationId, Guid providerId)
    {
        var mock = new Mock<ICatalogService>(MockBehavior.Strict);
        mock.Setup(c => c.GetAdminListingsAsync("admin-token")).ReturnsAsync(new List<CatalogAdminListingResponse>
        {
            new() { Id = activityId, BookingType = "Experience", ServiceName = "Safari", ProviderId = providerId, ProviderName = "Ceylon Trails" },
            new() { Id = restaurantId, BookingType = "Restaurant", ServiceName = "Ocean Table", ProviderId = providerId, ProviderName = "Ceylon Trails" },
            new() { Id = accommodationId, BookingType = "Accommodation", ServiceName = "Hill Villa", ProviderId = providerId, ProviderName = "Ceylon Trails" }
        });
        return mock;
    }

    [Fact]
    public async Task CalculatesAdminMetricsAndAggregations()
    {
        using var db = CreateDb();
        var providerId = Guid.NewGuid(); var activityId = Guid.NewGuid(); var restaurantId = Guid.NewGuid(); var accommodationId = Guid.NewGuid();
        db.Bookings.AddRange(
            new Booking { ListingId = activityId, ListingTitle = "Safari", BookingDate = Day, Status = BookingStatus.Confirmed, TotalAmount = 100, RefundAmount = 10 },
            new Booking { ListingId = activityId, ListingTitle = "Safari", BookingDate = Day, Status = BookingStatus.Cancelled, TotalAmount = 100, RefundAmount = 100 });
        db.RestaurantReservations.Add(new RestaurantReservation { RestaurantId = restaurantId, RestaurantName = "Ocean Table", ReservationDate = Day, Status = ReservationStatus.Confirmed, TotalPrice = 200, RefundAmount = 20 });
        db.AccommodationBookings.Add(new AccommodationBooking { AccommodationId = accommodationId, AccommodationName = "Hill Villa", CheckInDate = Day, CheckOutDate = Day.AddDays(2), Status = AccommodationBookingStatus.Completed, TotalPrice = 300, RefundAmount = 30 });
        await db.SaveChangesAsync();

        var report = await new AdminBookingsRevenueReportService(db, Catalog(activityId, restaurantId, accommodationId, providerId).Object)
            .GenerateAsync("admin-token", new() { StartDate = Day, EndDate = Day });

        Assert.Equal(4, report.TotalBookings);
        Assert.Equal(540m, report.TotalRevenue);
        Assert.Equal(25m, report.CancellationRate);
        Assert.Equal("Safari", report.MostBookedService!.ServiceName);
        Assert.Equal("Ceylon Trails", report.TopProviderByBookings!.ProviderName);
        Assert.Equal(50m, report.BookingTypeDistribution.Single(x => x.Name == "Experience").Percentage);
        Assert.Equal(25m, report.StatusDistribution.Single(x => x.Name == "Cancelled").Percentage);
        Assert.Single(report.RevenueOverTime);
        Assert.Equal(3, report.Records.Count(x => x.Revenue > 0));

        // Bookings Over Time: 4 bookings all on the same day => 1 period
        Assert.Single(report.BookingsOverTime);
        Assert.Equal(4, report.BookingsOverTime[0].BookingCount);
        Assert.Equal(Day.ToString("yyyy-MM-dd"), report.BookingsOverTime[0].Period);

        // Revenue by Booking Type: 3 types present (cancelled Experience has 0 revenue but is still counted)
        var expType = report.RevenueByBookingType.Single(x => x.BookingType == "Experience");
        var restType = report.RevenueByBookingType.Single(x => x.BookingType == "Restaurant");
        var accType = report.RevenueByBookingType.Single(x => x.BookingType == "Accommodation");
        Assert.Equal(90m, expType.Revenue);    // 100 - 10 refund (Confirmed); Cancelled contributes 0
        Assert.Equal(180m, restType.Revenue);  // 200 - 20
        Assert.Equal(270m, accType.Revenue);   // 300 - 30
        Assert.Equal(2, expType.BookingCount); // Confirmed + Cancelled
        Assert.Equal(1, restType.BookingCount);
        Assert.Equal(1, accType.BookingCount);
    }

    [Fact]
    public async Task AppliesCombinedFiltersAndReturnsNoDataSafely()
    {
        using var db = CreateDb();
        var providerId = Guid.NewGuid(); var id = Guid.NewGuid();
        db.Bookings.Add(new Booking { ListingId = id, ListingTitle = "Safari", BookingDate = Day, Status = BookingStatus.Confirmed, TotalAmount = 100 });
        await db.SaveChangesAsync();
        var service = new AdminBookingsRevenueReportService(db, Catalog(id, Guid.NewGuid(), Guid.NewGuid(), providerId).Object);

        var filtered = await service.GenerateAsync("admin-token", new() { StartDate = Day, EndDate = Day, BookingType = "experience", Status = "confirmed", ProviderId = providerId });
        Assert.Equal(1, filtered.TotalBookings);

        var empty = await service.GenerateAsync("admin-token", new() { StartDate = Day.AddDays(1), EndDate = Day.AddDays(1) });
        Assert.False(empty.HasData);
        Assert.Equal(0, empty.TotalBookings);
        Assert.Equal(0m, empty.TotalRevenue);
        Assert.Equal("No data available for the selected criteria.", empty.Message);
        Assert.Empty(empty.Records);

        // New aggregations should return empty lists safely when there is no data
        Assert.Empty(empty.BookingsOverTime);
        Assert.Empty(empty.RevenueByBookingType);
    }

    [Fact]
    public async Task CalculatesGrowthAgainstEqualPreviousPeriod()
    {
        using var db = CreateDb();
        var id = Guid.NewGuid();
        db.Bookings.AddRange(
            new Booking { ListingId = id, ListingTitle = "Safari", BookingDate = Day.AddDays(-1), Status = BookingStatus.Confirmed, TotalAmount = 100 },
            new Booking { ListingId = id, ListingTitle = "Safari", BookingDate = Day, Status = BookingStatus.Confirmed, TotalAmount = 100 },
            new Booking { ListingId = id, ListingTitle = "Safari", BookingDate = Day, Status = BookingStatus.Confirmed, TotalAmount = 100 });
        await db.SaveChangesAsync();
        var report = await new AdminBookingsRevenueReportService(db, Catalog(id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Object)
            .GenerateAsync("admin-token", new() { StartDate = Day, EndDate = Day });
        Assert.Equal(100m, report.BookingGrowthPercentage);
    }

    [Fact]
    public async Task RejectsInvalidFiltersBeforeCatalogLookup()
    {
        using var db = CreateDb(); var catalog = new Mock<ICatalogService>(MockBehavior.Strict);
        await Assert.ThrowsAsync<ValidationException>(() => new AdminBookingsRevenueReportService(db, catalog.Object).GenerateAsync("admin-token", new() { BookingType = "Bad" }));
        await Assert.ThrowsAsync<ValidationException>(() => new AdminBookingsRevenueReportService(db, catalog.Object).GenerateAsync("admin-token", new() { StartDate = Day.AddDays(1), EndDate = Day }));
        catalog.VerifyNoOtherCalls();
    }

    [Fact]
    public void EndpointRequiresAdminRole()
    {
        var auth = Assert.Single(typeof(AdminBookingReportsController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.Equal("Admin", ((AuthorizeAttribute)auth).Roles);
    }

    [Fact]
    public async Task MissingBearerTokenIsRejected()
    {
        var service = new Mock<IAdminBookingsRevenueReportService>(MockBehavior.Strict);
        var controller = new AdminBookingReportsController(service.Object) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.Request.Headers.Authorization = "Basic credentials";
        Assert.IsType<UnauthorizedObjectResult>(await controller.GetReport(new()));
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BookingsOverTimeGroupsByDateCorrectly()
    {
        using var db = CreateDb();
        var id = Guid.NewGuid();
        var day2 = Day.AddDays(1);
        db.Bookings.AddRange(
            new Booking { ListingId = id, ListingTitle = "Tour", BookingDate = Day, Status = BookingStatus.Confirmed, TotalAmount = 100 },
            new Booking { ListingId = id, ListingTitle = "Tour", BookingDate = Day, Status = BookingStatus.Confirmed, TotalAmount = 100 },
            new Booking { ListingId = id, ListingTitle = "Tour", BookingDate = day2, Status = BookingStatus.Confirmed, TotalAmount = 100 });
        await db.SaveChangesAsync();

        var report = await new AdminBookingsRevenueReportService(db, Catalog(id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Object)
            .GenerateAsync("admin-token", new() { StartDate = Day, EndDate = day2 });

        Assert.Equal(2, report.BookingsOverTime.Count);
        Assert.Equal(2, report.BookingsOverTime.First(x => x.Period == Day.ToString("yyyy-MM-dd")).BookingCount);
        Assert.Equal(1, report.BookingsOverTime.First(x => x.Period == day2.ToString("yyyy-MM-dd")).BookingCount);
    }

    [Fact]
    public async Task RevenueByBookingTypeAggregatesCorrectly()
    {
        using var db = CreateDb();
        var actId = Guid.NewGuid(); var restId = Guid.NewGuid(); var providerId = Guid.NewGuid();
        db.Bookings.Add(new Booking { ListingId = actId, ListingTitle = "Tour", BookingDate = Day, Status = BookingStatus.Confirmed, TotalAmount = 500 });
        db.RestaurantReservations.Add(new RestaurantReservation { RestaurantId = restId, RestaurantName = "Cafe", ReservationDate = Day, Status = ReservationStatus.Confirmed, TotalPrice = 300 });
        await db.SaveChangesAsync();

        var catalogMock = new Mock<ICatalogService>(MockBehavior.Strict);
        catalogMock.Setup(c => c.GetAdminListingsAsync("admin-token")).ReturnsAsync(new List<CatalogAdminListingResponse>
        {
            new() { Id = actId, BookingType = "Experience", ServiceName = "Tour", ProviderId = providerId, ProviderName = "P" },
            new() { Id = restId, BookingType = "Restaurant", ServiceName = "Cafe", ProviderId = providerId, ProviderName = "P" }
        });

        var report = await new AdminBookingsRevenueReportService(db, catalogMock.Object)
            .GenerateAsync("admin-token", new() { StartDate = Day, EndDate = Day });

        var exp = report.RevenueByBookingType.Single(x => x.BookingType == "Experience");
        var rest = report.RevenueByBookingType.Single(x => x.BookingType == "Restaurant");
        Assert.Equal(500m, exp.Revenue);
        Assert.Equal(300m, rest.Revenue);
        Assert.Equal(1, exp.BookingCount);
        Assert.Equal(1, rest.BookingCount);
        // Accommodation not in result when no accommodation bookings exist
        Assert.DoesNotContain(report.RevenueByBookingType, x => x.BookingType == "Accommodation");
    }
}