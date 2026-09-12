using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Controllers;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;
using Xunit;

namespace ProviderCatalogService.Tests;

public class AccommodationListingsControllerTests
{
    private static CatalogDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }

    private static AccommodationListingsController CreateController(
        CatalogDbContext db,
        Guid identityUserId,
        string email = "hotel@example.com")
    {
        var controller = new AccommodationListingsController(db);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, identityUserId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, "Provider")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        return controller;
    }

    // ── 1. CreateListing_ApprovedProvider_Succeeds ──────────────────────────────
    [Fact]
    public async Task CreateListing_ApprovedProvider_Succeeds()
    {
        using var db = CreateInMemoryDbContext();
        var identityId = Guid.NewGuid();
        var email = "approved_hotelier@example.com";

        db.Providers.Add(new Provider
        {
            Id = Guid.NewGuid(),
            IdentityUserId = identityId,
            Email = email,
            BusinessName = "Southern Palms Villa",
            ServiceType = "Hotel / Accommodation"
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db, identityId, email);

        var request = new CreateAccommodationListingRequest
        {
            RoomType = "Deluxe Ocean View Villa",
            PropertyType = "Luxury Villa",
            Location = "Mirissa, Southern Province",
            PricePerNight = 24000,
            MaxGuests = 4,
            BedDetails = "1 King Bed + 2 Twin Beds",
            MinStayNights = 2,
            Amenities = "Infinity Pool, Buffet Breakfast, Free WiFi, Sea View, Airport Shuttle",
            BathroomDetails = "En-suite bathroom with outdoor Jacuzzi",
            Description = "Stunning cliffside private villa overlooking the Indian Ocean with direct beach access.",
            IsActive = true
        };

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedResult>(result);
        var listing = Assert.IsType<AccommodationListingResponse>(created.Value);
        Assert.Equal("Deluxe Ocean View Villa", listing.RoomType);
        Assert.Equal(24000, listing.PricePerNight);
        Assert.Equal(4, listing.MaxGuests);
    }

    // ── 2. CreateListing_PendingProvider_IsDenied ───────────────────────────────
    [Fact]
    public async Task CreateListing_PendingProvider_IsDenied()
    {
        using var db = CreateInMemoryDbContext();
        var pendingIdentityId = Guid.NewGuid();
        var controller = CreateController(db, pendingIdentityId, "pending_hotel@example.com");

        var request = new CreateAccommodationListingRequest
        {
            RoomType = "Standard Room",
            PropertyType = "Guest House",
            Location = "Ella",
            PricePerNight = 6000,
            MaxGuests = 2,
            BedDetails = "1 Queen Bed",
            Description = "Pending hotel attempt"
        };

        var result = await controller.Create(request);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // ── 3. CreateListing_RejectedProvider_IsDenied ──────────────────────────────
    [Fact]
    public async Task CreateListing_RejectedProvider_IsDenied()
    {
        using var db = CreateInMemoryDbContext();
        var rejectedIdentityId = Guid.NewGuid();
        var controller = CreateController(db, rejectedIdentityId, "rejected_hotel@example.com");

        var request = new CreateAccommodationListingRequest
        {
            RoomType = "Economy Room",
            PropertyType = "Motel",
            Location = "Kandy",
            PricePerNight = 4000,
            MaxGuests = 2,
            BedDetails = "1 Double Bed",
            Description = "Rejected hotel attempt"
        };

        var result = await controller.Create(request);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // ── 4. GetListings_ReturnsProviderListings ──────────────────────────────────
    [Fact]
    public async Task GetListings_ReturnsProviderListings()
    {
        using var db = CreateInMemoryDbContext();
        var myIdentityId = Guid.NewGuid();
        var myProviderId = Guid.NewGuid();
        var otherProviderId = Guid.NewGuid();

        db.Providers.Add(new Provider { Id = myProviderId, IdentityUserId = myIdentityId, Email = "mine@example.com", BusinessName = "My Resort", ServiceType = "Hotel" });
        db.Providers.Add(new Provider { Id = otherProviderId, IdentityUserId = Guid.NewGuid(), Email = "other@example.com", BusinessName = "Other Hotel", ServiceType = "Hotel" });

        db.AccommodationListings.Add(new AccommodationListing { Id = Guid.NewGuid(), ProviderId = myProviderId, RoomType = "Superior Suite", PricePerNight = 15000, Location = "Bentota" });
        db.AccommodationListings.Add(new AccommodationListing { Id = Guid.NewGuid(), ProviderId = myProviderId, RoomType = "Family Cottage", PricePerNight = 22000, Location = "Bentota" });
        db.AccommodationListings.Add(new AccommodationListing { Id = Guid.NewGuid(), ProviderId = otherProviderId, RoomType = "Competitor Room", PricePerNight = 10000, Location = "Colombo" });
        await db.SaveChangesAsync();

        var controller = CreateController(db, myIdentityId, "mine@example.com");

        var result = await controller.GetMyListings();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<AccommodationListingResponse>>(ok.Value);
        Assert.Equal(2, list.Count());
        Assert.All(list, l => Assert.Contains(l.RoomType, new[] { "Superior Suite", "Family Cottage" }));
    }

    // ── 5. UpdateListing_Owner_Succeeds ─────────────────────────────────────────
    [Fact]
    public async Task UpdateListing_Owner_Succeeds()
    {
        using var db = CreateInMemoryDbContext();
        var myIdentityId = Guid.NewGuid();
        var myProviderId = Guid.NewGuid();

        db.Providers.Add(new Provider { Id = myProviderId, IdentityUserId = myIdentityId, Email = "owner@example.com", BusinessName = "Owner Resort", ServiceType = "Hotel" });

        var listingId = Guid.NewGuid();
        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = listingId,
            ProviderId = myProviderId,
            RoomType = "Standard Suite",
            PropertyType = "Resort",
            Location = "Galle",
            PricePerNight = 12000,
            MaxGuests = 2,
            BedDetails = "1 Queen Bed",
            Description = "Original description"
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db, myIdentityId, "owner@example.com");

        var updateReq = new UpdateAccommodationListingRequest
        {
            RoomType = "Executive Ocean View Suite",
            PropertyType = "5-Star Resort",
            Location = "Galle Fort",
            PricePerNight = 18500,
            MaxGuests = 3,
            BedDetails = "1 King Bed + 1 Rollaway",
            MinStayNights = 1,
            Amenities = "Free Breakfast, Jacuzzi, Balcony, WiFi",
            BathroomDetails = "Marble en-suite bathroom",
            Description = "Newly renovated luxury suite with private panoramic terrace.",
            IsActive = true
        };

        var result = await controller.Update(listingId, updateReq);

        var ok = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<AccommodationListingResponse>(ok.Value);
        Assert.Equal("Executive Ocean View Suite", updated.RoomType);
        Assert.Equal(18500, updated.PricePerNight);
        Assert.Equal(3, updated.MaxGuests);
    }

    // ── 6. UpdateListing_NonOwner_IsDenied ──────────────────────────────────────
    [Fact]
    public async Task UpdateListing_NonOwner_IsDenied()
    {
        using var db = CreateInMemoryDbContext();
        var myIdentityId = Guid.NewGuid();
        var myProviderId = Guid.NewGuid();
        var otherProviderId = Guid.NewGuid();

        db.Providers.Add(new Provider { Id = myProviderId, IdentityUserId = myIdentityId, Email = "providerA@example.com", BusinessName = "Provider A", ServiceType = "Hotel" });
        db.Providers.Add(new Provider { Id = otherProviderId, IdentityUserId = Guid.NewGuid(), Email = "providerB@example.com", BusinessName = "Provider B", ServiceType = "Hotel" });

        var otherListingId = Guid.NewGuid();
        db.AccommodationListings.Add(new AccommodationListing { Id = otherListingId, ProviderId = otherProviderId, RoomType = "Provider B Luxury Villa", PricePerNight = 35000, Location = "Tangalle" });
        await db.SaveChangesAsync();

        var controller = CreateController(db, myIdentityId, "providerA@example.com");

        var updateReq = new UpdateAccommodationListingRequest
        {
            RoomType = "Hacked Villa Name",
            PropertyType = "Hostel",
            Location = "Colombo",
            PricePerNight = 500,
            MaxGuests = 1,
            BedDetails = "1 Single Bed",
            Description = "Unauthorized edit attempt"
        };

        var result = await controller.Update(otherListingId, updateReq);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);

        // Verify data was unchanged
        var unchanged = await db.AccommodationListings.FindAsync(otherListingId);
        Assert.Equal("Provider B Luxury Villa", unchanged!.RoomType);
    }

    // ── 7. DeleteListing_Owner_Succeeds ─────────────────────────────────────────
    [Fact]
    public async Task DeleteListing_Owner_Succeeds()
    {
        using var db = CreateInMemoryDbContext();
        var myIdentityId = Guid.NewGuid();
        var myProviderId = Guid.NewGuid();

        db.Providers.Add(new Provider { Id = myProviderId, IdentityUserId = myIdentityId, Email = "hotelier@example.com", BusinessName = "Hotelier Inn", ServiceType = "Hotel" });

        var listingId = Guid.NewGuid();
        db.AccommodationListings.Add(new AccommodationListing { Id = listingId, ProviderId = myProviderId, RoomType = "Old Bungalow", PricePerNight = 8000, Location = "Nuwara Eliya" });
        await db.SaveChangesAsync();

        var controller = CreateController(db, myIdentityId, "hotelier@example.com");

        var result = await controller.Delete(listingId);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await db.AccommodationListings.FindAsync(listingId));
    }

    // ── 8. DeleteListing_NonOwner_IsDenied ──────────────────────────────────────
    [Fact]
    public async Task DeleteListing_NonOwner_IsDenied()
    {
        using var db = CreateInMemoryDbContext();
        var myIdentityId = Guid.NewGuid();
        var myProviderId = Guid.NewGuid();
        var otherProviderId = Guid.NewGuid();

        db.Providers.Add(new Provider { Id = myProviderId, IdentityUserId = myIdentityId, Email = "providerA@example.com", BusinessName = "Provider A", ServiceType = "Hotel" });
        db.Providers.Add(new Provider { Id = otherProviderId, IdentityUserId = Guid.NewGuid(), Email = "providerB@example.com", BusinessName = "Provider B", ServiceType = "Hotel" });

        var otherListingId = Guid.NewGuid();
        db.AccommodationListings.Add(new AccommodationListing { Id = otherListingId, ProviderId = otherProviderId, RoomType = "Provider B Penthouse Suite", PricePerNight = 50000, Location = "Colombo 03" });
        await db.SaveChangesAsync();

        var controller = CreateController(db, myIdentityId, "providerA@example.com");

        var result = await controller.Delete(otherListingId);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);

        var stillExists = await db.AccommodationListings.FindAsync(otherListingId);
        Assert.NotNull(stillExists);
    }
}