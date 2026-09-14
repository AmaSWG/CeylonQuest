using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Controllers;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Services;
using Xunit;

namespace ProviderCatalogService.Tests;

public class AvailabilityControllerTests : IDisposable
{
    private readonly CatalogDbContext _db;
    private readonly AvailabilityService _service;
    private readonly AvailabilityController _controller;

    public AvailabilityControllerTests()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new CatalogDbContext(options);

        _service = new AvailabilityService(_db);

        _controller = new AvailabilityController(_service);
    }

    // Story 6.1
    // Invalid date should return 400 Bad Request
    [Fact]
    public async Task GetAvailability_InvalidDate_ReturnsBadRequest()
    {
        var listingId = Guid.NewGuid();

        var result = await _controller.GetAvailability(
            listingId,
            "invalid-date"
        );

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Story 6.1
    // Unknown listing should return 404
    [Fact]
    public async Task GetAvailability_UnknownListing_ReturnsNotFound()
    {
        var listingId = Guid.NewGuid();

        var result = await _controller.GetAvailability(
            listingId,
            "2026-09-20"
        );

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // Story 6.1
    // Zero capacity should be rejected
    [Fact]
    public async Task SetAvailability_ZeroCapacity_ReturnsBadRequest()
    {
        var request = new SetAvailabilityRequest
        {
            Date = "2026-09-20",
            TimeSlot = "09:00 AM - 11:00 AM",
            Capacity = 0
        };

        var result = await _controller.SetAvailability(
            Guid.NewGuid(),
            request
        );

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Story 6.1
    // Negative capacity should be rejected
    [Fact]
    public async Task SetAvailability_NegativeCapacity_ReturnsBadRequest()
    {
        var request = new SetAvailabilityRequest
        {
            Date = "2026-09-20",
            TimeSlot = "09:00 AM - 11:00 AM",
            Capacity = -5
        };

        var result = await _controller.SetAvailability(
            Guid.NewGuid(),
            request
        );

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Story 6.1
    // Invalid date format should return 400
    [Fact]
    public async Task SetAvailability_InvalidDate_ReturnsBadRequest()
    {
        var request = new SetAvailabilityRequest
        {
            Date = "wrong-date",
            TimeSlot = "09:00 AM - 11:00 AM",
            Capacity = 10
        };

        var result = await _controller.SetAvailability(
            Guid.NewGuid(),
            request
        );

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Story 6.1
    // Valid capacity should be saved successfully
    [Fact]
    public async Task SetAvailability_ValidRequest_ReturnsOk()
    {
        var listingId = Guid.NewGuid();

        var request = new SetAvailabilityRequest
        {
            Date = "2026-09-20",
            TimeSlot = "09:00 AM - 11:00 AM",
            Capacity = 10
        };

        var result = await _controller.SetAvailability(
            listingId,
            request
        );

        Assert.IsType<OkObjectResult>(result);

        var slot = await _db.AvailabilitySlots
            .FirstOrDefaultAsync(x =>
                x.ListingId == listingId);

        Assert.NotNull(slot);
        Assert.Equal(10, slot.TotalCapacity);
        Assert.Equal(10, slot.RemainingCapacity);
    }

    // Story 6.1
    // Simulated booking with invalid date should return 400
    [Fact]
    public async Task SimulateBooking_InvalidDate_ReturnsBadRequest()
    {
        var request = new SimulateBookingEventRequest
        {
            ListingId = Guid.NewGuid(),
            Date = "invalid-date",
            TimeSlot = "09:00 AM - 11:00 AM",
            GuestCount = 2
        };

        var result = await _controller.SimulateBooking(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Story 6.1
    // Simulated booking should deduct capacity
    [Fact]
    public async Task SimulateBooking_ValidRequest_ReturnsOk()
    {
        var listingId = Guid.NewGuid();

        var setRequest = new SetAvailabilityRequest
        {
            Date = "2026-09-20",
            TimeSlot = "09:00 AM - 11:00 AM",
            Capacity = 10
        };

        await _controller.SetAvailability(
            listingId,
            setRequest
        );

        var bookingRequest = new SimulateBookingEventRequest
        {
            ListingId = listingId,
            Date = "2026-09-20",
            TimeSlot = "09:00 AM - 11:00 AM",
            GuestCount = 3
        };

        var result = await _controller.SimulateBooking(
            bookingRequest
        );

        Assert.IsType<OkObjectResult>(result);

        var slot = await _db.AvailabilitySlots
            .FirstAsync(x =>
                x.ListingId == listingId);

        Assert.Equal(7, slot.RemainingCapacity);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}