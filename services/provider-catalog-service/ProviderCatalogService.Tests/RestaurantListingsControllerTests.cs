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

public class RestaurantListingsControllerTests
{
    private static CatalogDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }

    private static RestaurantListingsController CreateController(
        CatalogDbContext db,
        Guid identityUserId,
        string email = "restaurant@example.com")
    {
        var controller = new RestaurantListingsController(db);

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
        var email = "approved_chef@example.com";

        db.Providers.Add(new Provider
        {
            Id = Guid.NewGuid(),
            IdentityUserId = identityId,
            Email = email,
            BusinessName = "Cinnamon Bay Restaurant",
            ServiceType = "Restaurant"
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db, identityId, email);

        var request = new CreateRestaurantListingRequest
        {
            Name = "Beachfront Seafood Dinner",
            Description = "Romantic 4-course lobster and jumbo prawn dinner on the sand.",
            CuisineType = "Seafood / Sri Lankan",
            DiningStyle = "Set Menu",
            Location = "Bentota Beach",
            PricePerPerson = 6500,
            PriceRange = "$$$ (Fine Dining)",
            OpeningHours = "06:30 PM - 11:00 PM",
            SetMenuDetails = "1. Crab Soup, 2. Grilled Jumbo Prawns, 3. Catch of the day, 4. Coconut Pudding",
            DietaryOptions = "Halal, Pescatarian",
            SeatingCapacity = 30,
            IsActive = true
        };

        var result = await controller.Create(request);

        var created = Assert.IsType<CreatedResult>(result);
        var listing = Assert.IsType<RestaurantListingResponse>(created.Value);
        Assert.Equal("Beachfront Seafood Dinner", listing.Name);
        Assert.Equal(6500, listing.PricePerPerson);
        Assert.Equal("Bentota Beach", listing.Location);
    }

    // ── 2. CreateListing_PendingProvider_IsDenied ───────────────────────────────
    [Fact]
    public async Task CreateListing_PendingProvider_IsDenied()
    {
        using var db = CreateInMemoryDbContext();
        var pendingIdentityId = Guid.NewGuid();
        var controller = CreateController(db, pendingIdentityId, "pending_rest@example.com");

        var request = new CreateRestaurantListingRequest
        {
            Name = "Pending Cafe",
            Description = "Pending cafe description",
            CuisineType = "Cafe",
            Location = "Colombo",
            PricePerPerson = 1200,
            OpeningHours = "08:00 AM - 08:00 PM"
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
        var controller = CreateController(db, rejectedIdentityId, "rejected_rest@example.com");

        var request = new CreateRestaurantListingRequest
        {
            Name = "Rejected Bistro",
            Description = "Rejected bistro description",
            CuisineType = "Bistro",
            Location = "Galle",
            PricePerPerson = 2500,
            OpeningHours = "11:00 AM - 10:00 PM"
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

        db.Providers.Add(new Provider { Id = myProviderId, IdentityUserId = myIdentityId, Email = "mine@example.com", BusinessName = "My Restaurant", ServiceType = "Restaurant" });
        db.Providers.Add(new Provider { Id = otherProviderId, IdentityUserId = Guid.NewGuid(), Email = "other@example.com", BusinessName = "Other Dining", ServiceType = "Restaurant" });

        db.RestaurantListings.Add(new RestaurantListing { Id = Guid.NewGuid(), ProviderId = myProviderId, Name = "Lunch Buffet", PricePerPerson = 3000, Location = "Colombo" });
        db.RestaurantListings.Add(new RestaurantListing { Id = Guid.NewGuid(), ProviderId = myProviderId, Name = "Dinner Set", PricePerPerson = 4500, Location = "Colombo" });
        db.RestaurantListings.Add(new RestaurantListing { Id = Guid.NewGuid(), ProviderId = otherProviderId, Name = "Competitor Bistro", PricePerPerson = 5000, Location = "Kandy" });
        await db.SaveChangesAsync();

        var controller = CreateController(db, myIdentityId, "mine@example.com");

        var result = await controller.GetMyListings();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<RestaurantListingResponse>>(ok.Value);
        Assert.Equal(2, list.Count());
        Assert.All(list, l => Assert.Contains(l.Name, new[] { "Lunch Buffet", "Dinner Set" }));
    }

    // ── 5. UpdateListing_Owner_Succeeds ─────────────────────────────────────────
    [Fact]
    public async Task UpdateListing_Owner_Succeeds()
    {
        using var db = CreateInMemoryDbContext();
        var myIdentityId = Guid.NewGuid();
        var myProviderId = Guid.NewGuid();

        db.Providers.Add(new Provider { Id = myProviderId, IdentityUserId = myIdentityId, Email = "owner@example.com", BusinessName = "Owner Diner", ServiceType = "Restaurant" });

        var listingId = Guid.NewGuid();
        db.RestaurantListings.Add(new RestaurantListing
        {
            Id = listingId,
            ProviderId = myProviderId,
            Name = "Original Buffet",
            Description = "Original buffet description",
            CuisineType = "Sri Lankan",
            DiningStyle = "Buffet",
            Location = "Galle",
            PricePerPerson = 2800,
            OpeningHours = "12:00 PM - 03:00 PM"
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db, myIdentityId, "owner@example.com");

        var updateReq = new UpdateRestaurantListingRequest
        {
            Name = "Grand Seafood & Curries Buffet",
            Description = "Expanded premium buffet with 20+ authentic dishes.",
            CuisineType = "Authentic Sri Lankan Seafood",
            DiningStyle = "Buffet",
            Location = "Galle Fort",
            PricePerPerson = 3500,
            PriceRange = "$$ (Moderate)",
            OpeningHours = "12:00 PM - 04:00 PM",
            SetMenuDetails = "Full Crab, Prawn, Fish, and Vegetable curry spread with hopper station.",
            DietaryOptions = "Halal, Vegetarian available",
            SeatingCapacity = 60,
            IsActive = true
        };

        var result = await controller.Update(listingId, updateReq);

        var ok = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<RestaurantListingResponse>(ok.Value);
        Assert.Equal("Grand Seafood & Curries Buffet", updated.Name);
        Assert.Equal(3500, updated.PricePerPerson);
        Assert.Equal("Galle Fort", updated.Location);
    }

    // ── 6. UpdateListing_NonOwner_IsDenied ──────────────────────────────────────
    [Fact]
    public async Task UpdateListing_NonOwner_IsDenied()
    {
        using var db = CreateInMemoryDbContext();
        var myIdentityId = Guid.NewGuid();
        var myProviderId = Guid.NewGuid();
        var otherProviderId = Guid.NewGuid();

        db.Providers.Add(new Provider { Id = myProviderId, IdentityUserId = myIdentityId, Email = "providerA@example.com", BusinessName = "Provider A", ServiceType = "Restaurant" });
        db.Providers.Add(new Provider { Id = otherProviderId, IdentityUserId = Guid.NewGuid(), Email = "providerB@example.com", BusinessName = "Provider B", ServiceType = "Restaurant" });

        var otherListingId = Guid.NewGuid();
        db.RestaurantListings.Add(new RestaurantListing { Id = otherListingId, ProviderId = otherProviderId, Name = "Provider B Dining", PricePerPerson = 4000, Location = "Colombo" });
        await db.SaveChangesAsync();

        var controller = CreateController(db, myIdentityId, "providerA@example.com");

        var updateReq = new UpdateRestaurantListingRequest
        {
            Name = "Hacked Restaurant Name",
            Description = "Unauthorized edit attempt",
            CuisineType = "Fast Food",
            DiningStyle = "Dine-in",
            Location = "Colombo",
            PricePerPerson = 500,
            OpeningHours = "24/7"
        };

        var result = await controller.Update(otherListingId, updateReq);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);

        // Verify data remains untouched
        var unchanged = await db.RestaurantListings.FindAsync(otherListingId);
        Assert.Equal("Provider B Dining", unchanged!.Name);
    }

    // ── 7. DeleteListing_Owner_Succeeds ─────────────────────────────────────────
    [Fact]
    public async Task DeleteListing_Owner_Succeeds()
    {
        using var db = CreateInMemoryDbContext();
        var myIdentityId = Guid.NewGuid();
        var myProviderId = Guid.NewGuid();

        db.Providers.Add(new Provider { Id = myProviderId, IdentityUserId = myIdentityId, Email = "chef@example.com", BusinessName = "Chef Bistro", ServiceType = "Restaurant" });

        var listingId = Guid.NewGuid();
        db.RestaurantListings.Add(new RestaurantListing { Id = listingId, ProviderId = myProviderId, Name = "Listing To Delete", PricePerPerson = 2000, Location = "Kandy" });
        await db.SaveChangesAsync();

        var controller = CreateController(db, myIdentityId, "chef@example.com");

        var result = await controller.Delete(listingId);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await db.RestaurantListings.FindAsync(listingId));
    }

    // ── 8. DeleteListing_NonOwner_IsDenied ──────────────────────────────────────
    [Fact]
    public async Task DeleteListing_NonOwner_IsDenied()
    {
        using var db = CreateInMemoryDbContext();
        var myIdentityId = Guid.NewGuid();
        var myProviderId = Guid.NewGuid();
        var otherProviderId = Guid.NewGuid();

        db.Providers.Add(new Provider { Id = myProviderId, IdentityUserId = myIdentityId, Email = "providerA@example.com", BusinessName = "Provider A", ServiceType = "Restaurant" });
        db.Providers.Add(new Provider { Id = otherProviderId, IdentityUserId = Guid.NewGuid(), Email = "providerB@example.com", BusinessName = "Provider B", ServiceType = "Restaurant" });

        var otherListingId = Guid.NewGuid();
        db.RestaurantListings.Add(new RestaurantListing { Id = otherListingId, ProviderId = otherProviderId, Name = "Provider B's Signature Grill", PricePerPerson = 8000, Location = "Ella" });
        await db.SaveChangesAsync();

        var controller = CreateController(db, myIdentityId, "providerA@example.com");

        var result = await controller.Delete(otherListingId);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);

        var stillExists = await db.RestaurantListings.FindAsync(otherListingId);
        Assert.NotNull(stillExists);
    }
}