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

public class ActivityListingsControllerTests
{
    private static CatalogDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }

    private static ActivityListingsController CreateController(
        CatalogDbContext db,
        Guid identityUserId,
        string email = "provider@example.com")
    {
        var controller = new ActivityListingsController(db);

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

    // ============================================================
    // CREATE
    // ============================================================

    /// <summary>
    /// Verifies that an approved provider can successfully create a new activity listing
    /// </summary>
    [Fact]
    public async Task CreateListing_ApprovedProvider_Succeeds()
    {
        using var db = CreateInMemoryDbContext();

        var identityId = Guid.NewGuid();
        var email = "approved@example.com";

        db.Providers.Add(new Provider
        {
            Id = Guid.NewGuid(),
            IdentityUserId = identityId,
            Email = email,
            BusinessName = "Ocean Tours",
            ServiceType = "Diving"
        });

        await db.SaveChangesAsync();

        var controller = CreateController(db, identityId, email);

        var request = new CreateActivityListingRequest
        {
            Title = "Snorkeling Adventure",
            Description = "Full day coral reef tour",
            Price = 5000,
            Unit = "Per Person",
            Location = "Trincomalee",
            MaxParticipants = 8,
            Duration = "3 Hours",
            AvailableDays = "Daily",
            TimeSlots = "08:00 (3 Hours), 13:00 (3 Hours)",
            IsActive = true
        };

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedResult>(result);
        var listing = Assert.IsType<ActivityListingResponse>(created.Value);

        Assert.Equal("Snorkeling Adventure", listing.Title);
        Assert.Equal(5000, listing.Price);
        Assert.Equal("Trincomalee", listing.Location);
    }

    /// <summary>
    /// Verifies that a provider without an approved profile is denied listing creation
    /// </summary>
    [Fact]
    public async Task CreateListing_PendingProvider_IsDenied()
    {
        using var db = CreateInMemoryDbContext();

        var pendingIdentityId = Guid.NewGuid();

        var controller = CreateController(
            db,
            pendingIdentityId,
            "pending@example.com");

        var request = new CreateActivityListingRequest
        {
            Title = "Safari Tour",
            Description = "Pending provider attempt",
            Price = 4000,
            Unit = "Per Person",
            Location = "Yala"
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

        var rejectedIdentityId = Guid.NewGuid();

        var controller = CreateController(
            db,
            rejectedIdentityId,
            "rejected@example.com");

        var request = new CreateActivityListingRequest
        {
            Title = "Rejected Tour",
            Description = "Rejected provider attempt",
            Price = 2500,
            Unit = "Per Person",
            Location = "Galle"
        };

        var result = await controller.Create(request);

        var objectResult = Assert.IsType<ObjectResult>(result);

        Assert.Equal(403, objectResult.StatusCode);
    }

    // ============================================================
    // GET MY LISTINGS
    // ============================================================

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
            BusinessName = "Mine",
            ServiceType = "Tour"
        });

        db.Providers.Add(new Provider
        {
            Id = otherProviderId,
            IdentityUserId = Guid.NewGuid(),
            Email = "other@example.com",
            BusinessName = "Other",
            ServiceType = "Tour"
        });

        db.ActivityListings.Add(new ActivityListing
        {
            Id = Guid.NewGuid(),
            ProviderId = myProviderId,
            Title = "My Listing 1",
            Price = 100,
            Unit = "Per Person",
            Location = "Kandy"
        });

        db.ActivityListings.Add(new ActivityListing
        {
            Id = Guid.NewGuid(),
            ProviderId = myProviderId,
            Title = "My Listing 2",
            Price = 200,
            Unit = "Per Person",
            Location = "Ella"
        });

        db.ActivityListings.Add(new ActivityListing
        {
            Id = Guid.NewGuid(),
            ProviderId = otherProviderId,
            Title = "Other Listing",
            Price = 300,
            Unit = "Per Person",
            Location = "Jaffna"
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            myIdentityId,
            "mine@example.com");

        var result = await controller.GetMyListings();

        var ok = Assert.IsType<OkObjectResult>(result);

        var list =
            Assert.IsAssignableFrom<IEnumerable<ActivityListingResponse>>(
                ok.Value);

        Assert.Equal(2, list.Count());

        Assert.All(
            list,
            l => Assert.Contains(
                l.Title,
                new[] { "My Listing 1", "My Listing 2" }));
    }

    /// <summary>
    /// Verifies that the owner of a listing can successfully update it
    /// </summary>
    [Fact]
    public async Task UpdateListing_Owner_Succeeds()
    {
        using var db = CreateInMemoryDbContext();

        var myIdentityId = Guid.NewGuid();
        var myProviderId = Guid.NewGuid();

        db.Providers.Add(new Provider
        {
            Id = myProviderId,
            IdentityUserId = myIdentityId,
            Email = "owner@example.com",
            BusinessName = "Owner Business",
            ServiceType = "Tour"
        });

        var listingId = Guid.NewGuid();

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            ProviderId = myProviderId,
            Title = "Original Title",
            Description = "Original Desc",
            Price = 1000,
            Unit = "Per Person",
            Location = "Colombo"
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            myIdentityId,
            "owner@example.com");

        var updateReq = new UpdateActivityListingRequest
        {
            Title = "Updated Title by Owner",
            Description = "Updated Desc",
            Price = 1500,
            Unit = "Per Person",
            Location = "Negombo",
            Duration = "2 Hours",
            AvailableDays = "Weekends",
            TimeSlots = "10:00 (2 Hours)",
            IsActive = true
        };

        var result =
            await controller.Update(listingId, updateReq);

        var ok = Assert.IsType<OkObjectResult>(result);

        var updated =
            Assert.IsType<ActivityListingResponse>(ok.Value);

        Assert.Equal(
            "Updated Title by Owner",
            updated.Title);

        Assert.Equal(1500, updated.Price);
        Assert.Equal("Negombo", updated.Location);
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
            ServiceType = "Tour"
        });

        db.Providers.Add(new Provider
        {
            Id = otherProviderId,
            IdentityUserId = Guid.NewGuid(),
            Email = "providerB@example.com",
            BusinessName = "Provider B",
            ServiceType = "Tour"
        });

        var otherListingId = Guid.NewGuid();

        db.ActivityListings.Add(new ActivityListing
        {
            Id = otherListingId,
            ProviderId = otherProviderId,
            Title = "Provider B Listing",
            Price = 300,
            Unit = "Per Person",
            Location = "Galle"
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            myIdentityId,
            "providerA@example.com");

        var updateReq = new UpdateActivityListingRequest
        {
            Title = "Hacked Title",
            Description = "Hacked Desc",
            Price = 999,
            Unit = "Per Person",
            Location = "Galle"
        };

        var result =
            await controller.Update(
                otherListingId,
                updateReq);

        var forbidden =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(403, forbidden.StatusCode);

        var unchanged =
            await db.ActivityListings.FindAsync(
                otherListingId);

        Assert.Equal(
            "Provider B Listing",
            unchanged!.Title);
    }

    /// <summary>
    /// Verifies that the owner of a listing can successfully delete it
    /// </summary>
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
            BusinessName = "Owner",
            ServiceType = "Tour"
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            identityId,
            "owner@example.com");

        var request = new UpdateActivityListingRequest
        {
            Title = "Updated",
            Description = "Updated Description",
            Price = 1000,
            Unit = "Per Person",
            Location = "Colombo"
        };

        var result = await controller.Update(
            Guid.NewGuid(),
            request);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ============================================================
    // DELETE
    // ============================================================

    [Fact]
    public async Task DeleteListing_Owner_Succeeds()
    {
        using var db = CreateInMemoryDbContext();

        var myIdentityId = Guid.NewGuid();
        var myProviderId = Guid.NewGuid();

        db.Providers.Add(new Provider
        {
            Id = myProviderId,
            IdentityUserId = myIdentityId,
            Email = "owner@example.com",
            BusinessName = "Owner",
            ServiceType = "Tour"
        });

        var listingId = Guid.NewGuid();

        db.ActivityListings.Add(new ActivityListing
        {
            Id = listingId,
            ProviderId = myProviderId,
            Title = "Listing To Delete",
            Price = 100,
            Unit = "Per Person",
            Location = "Colombo"
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            myIdentityId,
            "owner@example.com");

        var result =
            await controller.Delete(listingId);

        Assert.IsType<NoContentResult>(result);

        Assert.Null(
            await db.ActivityListings.FindAsync(
                listingId));
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
            ServiceType = "Tour"
        });

        db.Providers.Add(new Provider
        {
            Id = otherProviderId,
            IdentityUserId = Guid.NewGuid(),
            Email = "providerB@example.com",
            BusinessName = "Provider B",
            ServiceType = "Tour"
        });

        var otherListingId = Guid.NewGuid();

        db.ActivityListings.Add(new ActivityListing
        {
            Id = otherListingId,
            ProviderId = otherProviderId,
            Title = "Provider B's Precious Listing",
            Price = 5000,
            Unit = "Per Person",
            Location = "Sigiriya"
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            myIdentityId,
            "providerA@example.com");

        var result =
            await controller.Delete(otherListingId);

        var forbidden =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(403, forbidden.StatusCode);

        var stillExists =
            await db.ActivityListings.FindAsync(
                otherListingId);

        Assert.NotNull(stillExists);
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
            BusinessName = "Owner",
            ServiceType = "Tour"
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            identityId,
            "owner@example.com");

        var result =
            await controller.Delete(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ============================================================
    // PUBLIC / VISITOR LISTINGS
    // ============================================================

    [Fact]
    public async Task GetPublicListings_ReturnsOnlyActiveListings()
    {
        using var db = CreateInMemoryDbContext();

        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            IdentityUserId = Guid.NewGuid(),
            Email = "public@example.com",
            BusinessName = "Public Tours",
            ServiceType = "Tour"
        };

        db.Providers.Add(provider);

        db.ActivityListings.Add(new ActivityListing
        {
            Id = Guid.NewGuid(),
            ProviderId = provider.Id,
            Title = "Active Diving Tour",
            Description = "Ocean diving experience",
            Price = 5000,
            Unit = "Per Person",
            Location = "Trincomalee",
            IsActive = true
        });

        db.ActivityListings.Add(new ActivityListing
        {
            Id = Guid.NewGuid(),
            ProviderId = provider.Id,
            Title = "Inactive Tour",
            Description = "Should not appear",
            Price = 2000,
            Unit = "Per Person",
            Location = "Colombo",
            IsActive = false
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            provider.IdentityUserId!.Value,
            provider.Email);

        var result =
            await controller.GetPublicListings(
                null,
                null,
                null);

        var ok =
            Assert.IsType<OkObjectResult>(result);

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
            BusinessName = "Adventure Tours",
            ServiceType = "Tour"
        };

        db.Providers.Add(provider);

        db.ActivityListings.Add(new ActivityListing
        {
            Id = Guid.NewGuid(),
            ProviderId = provider.Id,
            Title = "Diving Adventure",
            Description = "Amazing diving experience",
            Price = 4500,
            Unit = "Per Person",
            Location = "Trincomalee",
            IsActive = true
        });

        db.ActivityListings.Add(new ActivityListing
        {
            Id = Guid.NewGuid(),
            ProviderId = provider.Id,
            Title = "Mountain Tour",
            Description = "Hill country tour",
            Price = 9000,
            Unit = "Per Person",
            Location = "Kandy",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var controller = CreateController(
            db,
            provider.IdentityUserId!.Value,
            provider.Email);

        var result =
            await controller.GetPublicListings(
                "diving",
                "Trincomalee",
                5000);

        var ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(ok.Value);
    }
}