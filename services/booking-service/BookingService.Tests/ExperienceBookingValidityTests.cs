using System.Security.Claims;
using BookingService.Controllers;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Kafka;

namespace BookingService.Tests;

public class ExperienceBookingValidityTests
{
    [Theory]
    [InlineData("2026-10-31", false)]
    [InlineData("2026-11-01", true)]
    [InlineData("2027-01-01", false)]
    public async Task CreateBooking_EnforcesListingValidityRange(string dateText, bool accepted)
    {
        await using var db = new BookingDbContext(new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var listingId = Guid.NewGuid();
        var catalog = new Mock<ICatalogService>();
        catalog.Setup(c => c.GetListingAsync(listingId)).ReturnsAsync(new CatalogListingResponse
        {
            Id = listingId, Title = "Test", Price = 100, MaxParticipants = 10, IsActive = true,
            ValidFrom = new DateTime(2026, 11, 1), ValidUntil = new DateTime(2026, 12, 31)
        });
        catalog.Setup(c => c.GetAvailabilityAsync(listingId, It.IsAny<DateOnly>())).ReturnsAsync(new CatalogAvailabilityResponse
        {
            IsOperatingDay = true,
            Slots = new() { new CatalogAvailabilitySlot { TimeSlot = "09:00", RemainingCapacity = 10 } }
        });
        catalog.Setup(c => c.ReserveCapacityAsync(listingId, It.IsAny<DateOnly>(), "09:00", 2)).ReturnsAsync(true);
        var controller = new BookingsController(db, catalog.Object, Mock.Of<IKafkaProducer>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
                    }, "test"))
                }
            }
        };

        var result = await controller.CreateBooking(new CreateBookingRequest
        {
            ListingId = listingId, BookingDate = DateOnly.Parse(dateText), TimeSlot = "09:00", ParticipantCount = 2
        });

        if (accepted) Assert.IsType<CreatedAtActionResult>(result);
        else Assert.IsType<BadRequestObjectResult>(result);
    }
}
