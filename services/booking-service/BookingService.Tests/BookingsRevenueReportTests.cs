using System.ComponentModel.DataAnnotations;
using System.Net;
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

public class BookingsRevenueReportTests
{
    private static readonly DateOnly Day = new(2026, 9, 15);

    private static BookingDbContext CreateDb() => new(new DbContextOptionsBuilder<BookingDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static void Seed(BookingDbContext db, Guid listingId, DateOnly? date = null)
    {
        foreach (var status in Enum.GetValues<BookingStatus>())
            db.Bookings.Add(new Booking
            {
                Id = Guid.NewGuid(), ListingId = listingId, ListingTitle = "Experience",
                BookingDate = date ?? Day, Status = status, TotalAmount = 100m, RefundAmount = 10m,
                PaymentStatus = PaymentStatus.Unpaid
            });
        foreach (var status in Enum.GetValues<ReservationStatus>())
            db.RestaurantReservations.Add(new RestaurantReservation
            {
                Id = Guid.NewGuid(), RestaurantId = listingId, RestaurantName = "Restaurant",
                ReservationDate = date ?? Day, Status = status, TotalPrice = 200m, RefundAmount = 20m
            });
        foreach (var status in Enum.GetValues<AccommodationBookingStatus>())
            db.AccommodationBookings.Add(new AccommodationBooking
            {
                Id = Guid.NewGuid(), AccommodationId = listingId, AccommodationName = "Stay",
                CheckInDate = date ?? Day, CheckOutDate = (date ?? Day).AddDays(2),
                Status = status, TotalPrice = 300m, RefundAmount = 30m
            });
        db.SaveChanges();
    }

    private static Mock<ICatalogService> Catalog(Guid listingId)
    {
        var mock = new Mock<ICatalogService>(MockBehavior.Strict);
        mock.Setup(c => c.GetReportOwnedListingIdsAsync("provider-token")).ReturnsAsync(new ProviderReportListingIds
        {
            ActivityIds = new() { listingId }, RestaurantIds = new() { listingId }, AccommodationIds = new() { listingId }
        });
        return mock;
    }

    [Fact]
    public async Task AllTypes_AreScoped_AndRevenueUsesBookingValueLessRefunds()
    {
        using var db = CreateDb();
        var ownedId = Guid.NewGuid();
        Seed(db, ownedId);
        Seed(db, Guid.NewGuid());
        var report = await new BookingsRevenueReportService(db, Catalog(ownedId).Object)
            .GenerateAsync("provider-token", new());
        Assert.Equal(9, report.TotalBookings);
        Assert.Equal(900m, report.TotalRevenue);
        Assert.Equal(3, report.StatusCounts["Confirmed"]);
        Assert.Equal(3, report.StatusCounts["Cancelled"]);
        Assert.Equal(2, report.StatusCounts["Completed"]);
        Assert.Equal(1, report.StatusCounts["PendingPayment"]);
        Assert.Equal(4, report.BookingTypeCounts["Experience"]);
        Assert.Equal(2, report.BookingTypeCounts["Restaurant"]);
        Assert.Equal(3, report.BookingTypeCounts["Accommodation"]);
        Assert.All(report.Records.Where(r => r.Status is "Cancelled" or "PendingPayment"), r => Assert.Equal(0m, r.Revenue));
    }

    [Theory]
    [InlineData("Experience", "Confirmed", 90)]
    [InlineData("Restaurant", "Confirmed", 180)]
    [InlineData("Accommodation", "Completed", 270)]
    [InlineData("experience", "confirmed", 90)]
    public async Task CombinedFilters_UseInclusiveServiceDate(string type, string status, int revenue)
    {
        using var db = CreateDb();
        var id = Guid.NewGuid();
        Seed(db, id);
        Seed(db, id, Day.AddDays(-1));
        Seed(db, id, Day.AddDays(1));
        var report = await new BookingsRevenueReportService(db, Catalog(id).Object).GenerateAsync("provider-token",
            new() { StartDate = Day, EndDate = Day, Status = status, BookingType = type });
        Assert.Single(report.Records);
        Assert.Equal(revenue, report.TotalRevenue);
        Assert.Equal(Day, report.Records[0].Date);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OneSidedDateFilters_AreInclusive(bool startOnly)
    {
        using var db = CreateDb();
        var id = Guid.NewGuid();
        Seed(db, id, Day.AddDays(-1)); Seed(db, id); Seed(db, id, Day.AddDays(1));
        var report = await new BookingsRevenueReportService(db, Catalog(id).Object).GenerateAsync("provider-token",
            new() { StartDate = startOnly ? Day : null, EndDate = startOnly ? null : Day });
        Assert.Equal(18, report.TotalBookings);
    }

    [Theory]
    [InlineData("Restaurant", "Completed")]
    [InlineData("Accommodation", "PendingPayment")]
    public async Task ValidButInapplicableStatuses_ReturnEmpty(string type, string status)
    {
        using var db = CreateDb(); var id = Guid.NewGuid(); Seed(db, id);
        var report = await new BookingsRevenueReportService(db, Catalog(id).Object).GenerateAsync("provider-token",
            new() { BookingType = type, Status = status });
        Assert.Equal(0, report.TotalBookings); Assert.Equal(0m, report.TotalRevenue);
        Assert.Empty(report.Records); Assert.Empty(report.StatusCounts); Assert.Empty(report.BookingTypeCounts);
        Assert.False(report.HasData);
        Assert.Equal("No data available for the selected criteria.", report.Message);
    }

    [Fact]
    public async Task NoOwnedInventory_NeverReturnsOtherProvidersData()
    {
        using var db = CreateDb(); Seed(db, Guid.NewGuid());
        var catalog = new Mock<ICatalogService>();
        catalog.Setup(c => c.GetReportOwnedListingIdsAsync("provider-token")).ReturnsAsync(new ProviderReportListingIds());
        var report = await new BookingsRevenueReportService(db, catalog.Object).GenerateAsync("provider-token", new());
        Assert.Equal(0, report.TotalBookings); Assert.Equal(0m, report.TotalRevenue);
    }

    [Theory]
    [InlineData("Bad", null)]
    [InlineData("0", null)]
    [InlineData(null, "Bad")]
    [InlineData(null, "Experience,Restaurant")]
    public async Task InvalidFilters_FailBeforeOwnershipLookup(string? status, string? type)
    {
        using var db = CreateDb(); var catalog = new Mock<ICatalogService>(MockBehavior.Strict);
        await Assert.ThrowsAsync<ValidationException>(() => new BookingsRevenueReportService(db, catalog.Object)
            .GenerateAsync("provider-token", new() { Status = status, BookingType = type }));
        catalog.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReversedDateRange_IsRejected()
    {
        using var db = CreateDb();
        await Assert.ThrowsAsync<ValidationException>(() => new BookingsRevenueReportService(db, new Mock<ICatalogService>().Object)
            .GenerateAsync("provider-token", new() { StartDate = Day.AddDays(1), EndDate = Day }));
    }

    [Fact]
    public async Task OwnershipFailure_AbortsReport()
    {
        using var db = CreateDb(); Seed(db, Guid.NewGuid());
        var catalog = new Mock<ICatalogService>();
        catalog.Setup(c => c.GetReportOwnedListingIdsAsync("provider-token")).ThrowsAsync(new HttpRequestException());
        await Assert.ThrowsAsync<HttpRequestException>(() => new BookingsRevenueReportService(db, catalog.Object)
            .GenerateAsync("provider-token", new()));
    }

    private static ProviderBookingReportsController Controller(IBookingsRevenueReportService service, string token = "Bearer provider-token")
    {
        var controller = new ProviderBookingReportsController(service)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.Request.Headers.Authorization = token;
        return controller;
    }

    [Fact]
    public void Endpoint_RequiresProviderRole()
    {
        var auth = Assert.Single(typeof(ProviderBookingReportsController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.Equal("Provider", ((AuthorizeAttribute)auth).Roles);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Bearer ")]
    [InlineData("Basic credentials")]
    public async Task MissingBearerToken_ReturnsUnauthorized(string token)
    {
        var service = new Mock<IBookingsRevenueReportService>(MockBehavior.Strict);
        Assert.IsType<UnauthorizedObjectResult>(await Controller(service.Object, token).GetReport(new()));
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Controller_ReturnsEmptySuccess_Validation400_AndLookup503()
    {
        var service = new Mock<IBookingsRevenueReportService>();
        service.Setup(s => s.GenerateAsync("provider-token", It.IsAny<BookingsRevenueReportQuery>())).ReturnsAsync(new BookingsRevenueReportResponse());
        Assert.IsType<OkObjectResult>(await Controller(service.Object).GetReport(new()));
        service.Setup(s => s.GenerateAsync("provider-token", It.IsAny<BookingsRevenueReportQuery>())).ThrowsAsync(new ValidationException("Invalid filter"));
        Assert.IsType<BadRequestObjectResult>(await Controller(service.Object).GetReport(new()));
        service.Setup(s => s.GenerateAsync("provider-token", It.IsAny<BookingsRevenueReportQuery>())).ThrowsAsync(new HttpRequestException());
        var failure = Assert.IsType<ObjectResult>(await Controller(service.Object).GetReport(new()));
        Assert.Equal(503, failure.StatusCode);
    }

    private sealed class CatalogHandler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;
        public string Body { get; init; } = "[]";
        public List<string> Paths { get; } = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("provider-token", request.Headers.Authorization?.Parameter);
            Paths.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(Body) });
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task StrictCatalogLookup_RejectsHttpFailures(HttpStatusCode status)
    {
        using var client = new HttpClient(new CatalogHandler { Status = status }) { BaseAddress = new Uri("http://catalog") };
        await Assert.ThrowsAsync<HttpRequestException>(() => new CatalogService(client).GetReportOwnedListingIdsAsync("provider-token"));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("invalid-json")]
    public async Task StrictCatalogLookup_RejectsInvalidPayloads(string body)
    {
        using var client = new HttpClient(new CatalogHandler { Body = body }) { BaseAddress = new Uri("http://catalog") };
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => new CatalogService(client).GetReportOwnedListingIdsAsync("provider-token"));
    }

    [Fact]
    public async Task StrictCatalogLookup_UsesAllThreeAuthenticatedEndpoints()
    {
        var handler = new CatalogHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://catalog") };
        var ids = await new CatalogService(client).GetReportOwnedListingIdsAsync("provider-token");
        Assert.Empty(ids.ActivityIds); Assert.Empty(ids.RestaurantIds); Assert.Empty(ids.AccommodationIds);
        Assert.Equal(new[] { "/api/catalog/activity-listings", "/api/catalog/restaurant-listings", "/api/catalog/accommodation-listings" }, handler.Paths);
    }
}
