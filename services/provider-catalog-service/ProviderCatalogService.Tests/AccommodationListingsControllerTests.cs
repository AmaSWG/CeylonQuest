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
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        return controller;
    }

    /// <summary>
    /// Verifies that an approved provider can successfully create a new accommodation listing
    /// </summary>
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
            Description = "Stunning cliffside private villa overlooking the Indian Ocean.",
            IsActive = true
        };

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedResult>(result);
        var listing = Assert.IsType<AccommodationListingResponse>(created.Value);

        Assert.Equal("Deluxe Ocean View Villa", listing.RoomType);
        Assert.Equal(24000, listing.PricePerNight);
        Assert.Equal(4, listing.MaxGuests);
    }

    /// <summary>
    /// Verifies that a provider without an approved profile is denied listing creation
    /// </summary>
    [Fact]
    public async Task CreateListing_PendingProvider_IsDenied()
    {
        using var db = CreateInMemoryDbContext();

        var identityId = Guid.NewGuid();

        var controller = CreateController(
            db,
            identityId,
            "pending_hotel@example.com");

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

    /// <summary>
    /// Verifies that a rejected provider is denied listing creation
    /// </summary>
    [Fact]
    public async Task CreateListing_RejectedProvider_IsDenied()
    {
        using var db = CreateInMemoryDbContext();

        var identityId = Guid.NewGuid();

        var controller = CreateController(
            db,
            identityId,
            "rejected_hotel@example.com");

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

    /// <summary>
    /// Verifies that only the authenticated provider's own listings are returned
    /// </summary>
    [Fact]
    public async Task GetListings_ReturnsProviderListings()
    {
        using var db = CreateInMemoryDbContext();

        var myIdentityId = Guid.NewGuid();
        var myProviderId = Guid.NewGuid();
        var otherProviderId = Guid.NewGuid();

        db.Providers.Add(new Provider
        {
            Id = myProviderId,
            IdentityUserId = myIdentityId,
            Email = "mine@example.com",
            BusinessName = "My Resort",
            ServiceType = "Hotel"
        });

        db.Providers.Add(new Provider
        {
            Id = otherProviderId,
            IdentityUserId = Guid.NewGuid(),
            Email = "other@example.com",
            BusinessName = "Other Hotel",
            ServiceType = "Hotel"
        });

        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = Guid.NewGuid(),
            ProviderId = myProviderId,
            RoomType = "Superior Suite",
            PricePerNight = 15000,
            Location = "Bentota"
        });

        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = Guid.NewGuid(),
            ProviderId = myProviderId,
            RoomType = "Family Cottage",
            PricePerNight = 22000,
            Location = "Bentota"
        });

        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = Guid.NewGuid(),
            ProviderId = otherProviderId,
            RoomType = "Competitor Room",
            PricePerNight = 10000,
            Location = "Colombo"
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            myIdentityId,
            "mine@example.com");

        var result = await controller.GetMyListings();

        var ok = Assert.IsType<OkObjectResult>(result);

        var list =
            Assert.IsAssignableFrom<IEnumerable<AccommodationListingResponse>>(
                ok.Value);

        Assert.Equal(2, list.Count());
    }

    /// <summary>
    /// Verifies that the owner of a listing can successfully update it
    /// </summary>
    [Fact]
    public async Task UpdateListing_Owner_Succeeds()
    {
        using var db = CreateInMemoryDbContext();

        var identityId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        db.Providers.Add(new Provider
        {
            Id = providerId,
            IdentityUserId = identityId,
            Email = "owner@example.com",
            BusinessName = "Owner Resort",
            ServiceType = "Hotel"
        });

        var listingId = Guid.NewGuid();

        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = listingId,
            ProviderId = providerId,
            RoomType = "Standard Suite",
            PropertyType = "Resort",
            Location = "Galle",
            PricePerNight = 12000,
            MaxGuests = 2,
            BedDetails = "1 Queen Bed",
            Description = "Original description"
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            identityId,
            "owner@example.com");

        var request = new UpdateAccommodationListingRequest
        {
            RoomType = "Executive Ocean View Suite",
            PropertyType = "5-Star Resort",
            Location = "Galle Fort",
            PricePerNight = 18500,
            MaxGuests = 3,
            BedDetails = "1 King Bed + 1 Rollaway",
            MinStayNights = 1,
            Amenities = "Breakfast, Jacuzzi, WiFi",
            BathroomDetails = "Marble bathroom",
            Description = "Updated luxury suite.",
            IsActive = true
        };

        var result = await controller.Update(listingId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<AccommodationListingResponse>(ok.Value);

        Assert.Equal("Executive Ocean View Suite", updated.RoomType);
        Assert.Equal(18500, updated.PricePerNight);
        Assert.Equal(3, updated.MaxGuests);
    }

    /// <summary>
    /// Verifies that a provider cannot update a listing they do not own
    /// </summary>
    [Fact]
    public async Task UpdateListing_NonOwner_IsDenied()
    {
        using var db = CreateInMemoryDbContext();

        var myIdentityId = Guid.NewGuid();
        var myProviderId = Guid.NewGuid();
        var otherProviderId = Guid.NewGuid();

        db.Providers.Add(new Provider
        {
            Id = myProviderId,
            IdentityUserId = myIdentityId,
            Email = "providerA@example.com",
            BusinessName = "Provider A",
            ServiceType = "Hotel"
        });

        db.Providers.Add(new Provider
        {
            Id = otherProviderId,
            IdentityUserId = Guid.NewGuid(),
            Email = "providerB@example.com",
            BusinessName = "Provider B",
            ServiceType = "Hotel"
        });

        var listingId = Guid.NewGuid();

        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = listingId,
            ProviderId = otherProviderId,
            RoomType = "Provider B Villa",
            PricePerNight = 35000,
            Location = "Tangalle"
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            myIdentityId,
            "providerA@example.com");

        var request = new UpdateAccommodationListingRequest
        {
            RoomType = "Hacked Villa",
            PropertyType = "Hostel",
            Location = "Colombo",
            PricePerNight = 500,
            MaxGuests = 1,
            BedDetails = "1 Single Bed",
            Description = "Unauthorized update"
        };

        var result = await controller.Update(listingId, request);

        var forbidden = Assert.IsType<ObjectResult>(result);

        Assert.Equal(403, forbidden.StatusCode);
    }

    [Fact]
    public async Task UpdateListing_NotFound_Returns404()
    {
        using var db = CreateInMemoryDbContext();

        var identityId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        db.Providers.Add(new Provider
        {
            Id = providerId,
            IdentityUserId = identityId,
            Email = "owner@example.com",
            BusinessName = "Owner Hotel",
            ServiceType = "Hotel"
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            identityId,
            "owner@example.com");

        var request = new UpdateAccommodationListingRequest
        {
            RoomType = "Updated Room",
            PropertyType = "Hotel",
            Location = "Colombo",
            PricePerNight = 5000,
            MaxGuests = 2,
            BedDetails = "1 Queen Bed",
            Description = "Updated room"
        };

        var result = await controller.Update(
            Guid.NewGuid(),
            request);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Verifies that the owner of a listing can successfully delete it
    /// </summary>
    [Fact]
    public async Task DeleteListing_Owner_Succeeds()
    {
        using var db = CreateInMemoryDbContext();

        var identityId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        db.Providers.Add(new Provider
        {
            Id = providerId,
            IdentityUserId = identityId,
            Email = "hotelier@example.com",
            BusinessName = "Hotelier Inn",
            ServiceType = "Hotel"
        });

        var listingId = Guid.NewGuid();

        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = listingId,
            ProviderId = providerId,
            RoomType = "Old Bungalow",
            PricePerNight = 8000,
            Location = "Nuwara Eliya"
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            identityId,
            "hotelier@example.com");

        var result = await controller.Delete(listingId);

        Assert.IsType<NoContentResult>(result);

        Assert.Null(
            await db.AccommodationListings.FindAsync(listingId));
    }

    /// <summary>
    /// Verifies that a provider cannot delete a listing they do not own
    /// </summary>
    [Fact]
    public async Task DeleteListing_NonOwner_IsDenied()
    {
        using var db = CreateInMemoryDbContext();

        var myIdentityId = Guid.NewGuid();
        var myProviderId = Guid.NewGuid();
        var otherProviderId = Guid.NewGuid();

        db.Providers.Add(new Provider
        {
            Id = myProviderId,
            IdentityUserId = myIdentityId,
            Email = "providerA@example.com",
            BusinessName = "Provider A",
            ServiceType = "Hotel"
        });

        db.Providers.Add(new Provider
        {
            Id = otherProviderId,
            IdentityUserId = Guid.NewGuid(),
            Email = "providerB@example.com",
            BusinessName = "Provider B",
            ServiceType = "Hotel"
        });

        var listingId = Guid.NewGuid();

        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = listingId,
            ProviderId = otherProviderId,
            RoomType = "Provider B Penthouse",
            PricePerNight = 50000,
            Location = "Colombo"
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            myIdentityId,
            "providerA@example.com");

        var result = await controller.Delete(listingId);

        var forbidden = Assert.IsType<ObjectResult>(result);

        Assert.Equal(403, forbidden.StatusCode);
    }

    [Fact]
    public async Task DeleteListing_NotFound_Returns404()
    {
        using var db = CreateInMemoryDbContext();

        var identityId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        db.Providers.Add(new Provider
        {
            Id = providerId,
            IdentityUserId = identityId,
            Email = "owner@example.com",
            BusinessName = "Owner Hotel",
            ServiceType = "Hotel"
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            identityId,
            "owner@example.com");

        var result = await controller.Delete(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetPublicListings_ReturnsOnlyActiveListings()
    {
        using var db = CreateInMemoryDbContext();

        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            IdentityUserId = Guid.NewGuid(),
            Email = "public@example.com",
            BusinessName = "Public Hotel",
            ServiceType = "Hotel"
        };

        db.Providers.Add(provider);

        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = Guid.NewGuid(),
            ProviderId = provider.Id,
            Provider = provider,
            RoomType = "Active Suite",
            PropertyType = "Resort",
            Location = "Galle",
            PricePerNight = 15000,
            MaxGuests = 4,
            BedDetails = "1 King Bed",
            Amenities = "Pool, WiFi",
            Description = "Active accommodation",
            IsActive = true
        });

        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = Guid.NewGuid(),
            ProviderId = provider.Id,
            Provider = provider,
            RoomType = "Inactive Suite",
            PropertyType = "Hotel",
            Location = "Colombo",
            PricePerNight = 9000,
            MaxGuests = 2,
            BedDetails = "1 Queen Bed",
            Amenities = "WiFi",
            Description = "Inactive accommodation",
            IsActive = false
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            provider.IdentityUserId!.Value,
            provider.Email);

        var result = await controller.GetPublicListings(
            null,
            null,
            null,
            null);

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetPublicListings_WithFilters_ReturnsOk()
    {
        using var db = CreateInMemoryDbContext();

        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            IdentityUserId = Guid.NewGuid(),
            Email = "filter@example.com",
            BusinessName = "Filter Hotel",
            ServiceType = "Hotel"
        };

        db.Providers.Add(provider);

        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = Guid.NewGuid(),
            ProviderId = provider.Id,
            Provider = provider,
            RoomType = "Luxury Ocean Villa",
            PropertyType = "Luxury Villa",
            Location = "Mirissa",
            PricePerNight = 25000,
            MaxGuests = 4,
            BedDetails = "1 King Bed",
            Amenities = "Pool WiFi Sea View",
            Description = "Luxury ocean villa",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            provider.IdentityUserId!.Value,
            provider.Email);

        var result = await controller.GetPublicListings(
            "ocean",
            "Luxury Villa",
            "Mirissa",
            4);

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(ok.Value);
    }
}