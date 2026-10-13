#nullable enable

using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Controllers;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;
using Xunit;

namespace ProviderCatalogService.Tests;

public class ListingRemovalTests
{
    private static CatalogDbContext CreateDb() => new(
        new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static object AddListing(
        CatalogDbContext db,
        string type,
        Guid id,
        bool active = true)
    {
        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            BusinessName = "Test provider"
        };
        db.Providers.Add(provider);

        switch (type)
        {
            case "Experience":
                var experience = new ActivityListing
                {
                    Id = id,
                    Title = "Test experience",
                    ProviderId = provider.Id,
                    Provider = provider,
                    IsActive = active
                };
                db.ActivityListings.Add(experience);
                return experience;

            case "Restaurant":
                var restaurant = new RestaurantListing
                {
                    Id = id,
                    Name = "Test restaurant",
                    ProviderId = provider.Id,
                    Provider = provider,
                    IsActive = active
                };
                db.RestaurantListings.Add(restaurant);
                return restaurant;

            case "Accommodation":
                var accommodation = new AccommodationListing
                {
                    Id = id,
                    RoomType = "Test room",
                    ProviderId = provider.Id,
                    Provider = provider,
                    IsActive = active
                };
                db.AccommodationListings.Add(accommodation);
                return accommodation;

            default:
                throw new ArgumentException("Invalid listing type.");
        }
    }

    private static AdminListingRemovalController Controller(
        CatalogDbContext db,
        Guid administratorId) => new(db)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[]
                    {
                        new Claim(
                            ClaimTypes.NameIdentifier,
                            administratorId.ToString()),
                        new Claim(ClaimTypes.Role, "Admin")
                    },
                    "Test"))
            }
        }
    };

    private static RemoveListingRequest Request() => new()
    {
        ConfirmRemoval = true,
        Reason = "Removed following administrator review."
    };

    [Theory]
    [InlineData("Experience")]
    [InlineData("Restaurant")]
    [InlineData("Accommodation")]
    public async Task RemovalPreservesRecordAndCreatesAudit(string type)
    {
        using var db = CreateDb();
        var id = Guid.NewGuid();
        var administrator = Guid.NewGuid();

        AddListing(db, type, id);
        await db.SaveChangesAsync();

        var result = await Controller(db, administrator)
            .Remove(type, id, Request());

        var response = Assert.IsType<ListingRemovalResponse>(
            Assert.IsType<OkObjectResult>(result).Value);

        Assert.Equal("Removed", response.Status);
        Assert.False(response.IsActive);

        db.ChangeTracker.Clear();

        object? stored = type switch
        {
            "Experience" => await db.ActivityListings.FindAsync(id),
            "Restaurant" => await db.RestaurantListings.FindAsync(id),
            _ => await db.AccommodationListings.FindAsync(id)
        };

        Assert.NotNull(stored);
        Assert.True(db.Entry<object>(stored!).Property<bool>("IsRemoved").CurrentValue);
        Assert.False(db.Entry<object>(stored!).Property<bool>("IsActive").CurrentValue);

        var audit = await db.ListingRemovalAudits.SingleAsync();

        Assert.Equal(id, audit.ListingId);
        Assert.Equal(type, audit.ListingType);
        Assert.Equal(administrator, audit.AdministratorId);
        Assert.Equal("Active", audit.PreviousStatus);
        Assert.Equal("Removed", audit.NewStatus);
        Assert.Equal(Request().Reason, audit.Reason);
    }

    [Theory]
    [InlineData("Experience")]
    [InlineData("Restaurant")]
    [InlineData("Accommodation")]
    public async Task RemovedListingIsExcludedFromSearch(string type)
    {
        using var db = CreateDb();
        var removedId = Guid.NewGuid();
        var visibleId = Guid.NewGuid();

        AddListing(db, type, removedId);
        AddListing(db, type, visibleId);
        await db.SaveChangesAsync();

        await Controller(db, Guid.NewGuid())
            .Remove(type, removedId, Request());

        var result = await new SearchController(db).Search(type: type);

        var response = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(
            Assert.IsType<OkObjectResult>(result).Value);

        Assert.DoesNotContain(response.Items, item => item.Id == removedId);
        Assert.Contains(response.Items, item => item.Id == visibleId);
    }

    [Theory]
    [InlineData("Experience")]
    [InlineData("Restaurant")]
    [InlineData("Accommodation")]
    public async Task RemovedFlagHidesListingEvenIfActiveFlagIsTrue(string type)
    {
        using var db = CreateDb();
        var id = Guid.NewGuid();
        var listing = AddListing(db, type, id);

        db.Entry<object>(listing).Property<bool>("IsRemoved").CurrentValue = true;
        await db.SaveChangesAsync();

        var result = await new SearchController(db).Search(type: type);

        var response = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(
            Assert.IsType<OkObjectResult>(result).Value);

        Assert.DoesNotContain(response.Items, item => item.Id == id);
    }

    [Fact]
    public async Task MissingListingReturnsNotFoundWithoutAudit()
    {
        using var db = CreateDb();

        var result = await Controller(db, Guid.NewGuid())
            .Remove("Experience", Guid.NewGuid(), Request());

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Empty(await db.ListingRemovalAudits.ToListAsync());
    }

    [Fact]
    public async Task ConfirmationIsRequired()
    {
        using var db = CreateDb();
        var id = Guid.NewGuid();
        AddListing(db, "Experience", id);
        await db.SaveChangesAsync();

        var request = Request();
        request.ConfirmRemoval = false;

        var result = await Controller(db, Guid.NewGuid())
            .Remove("Experience", id, request);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.False((await db.ActivityListings.FindAsync(id))!.IsRemoved);
        Assert.Empty(await db.ListingRemovalAudits.ToListAsync());
    }

    [Fact]
    public async Task InactiveListingCannotBeRemoved()
    {
        using var db = CreateDb();
        var id = Guid.NewGuid();
        AddListing(db, "Experience", id, active: false);
        await db.SaveChangesAsync();

        var result = await Controller(db, Guid.NewGuid())
            .Remove("Experience", id, Request());

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Empty(await db.ListingRemovalAudits.ToListAsync());
    }

    [Fact]
    public async Task RepeatedRemovalDoesNotCreateAnotherAudit()
    {
        using var db = CreateDb();
        var id = Guid.NewGuid();
        AddListing(db, "Experience", id);
        await db.SaveChangesAsync();

        var controller = Controller(db, Guid.NewGuid());

        Assert.IsType<OkObjectResult>(
            await controller.Remove("Experience", id, Request()));

        Assert.IsType<ConflictObjectResult>(
            await controller.Remove("Experience", id, Request()));

        Assert.Single(await db.ListingRemovalAudits.ToListAsync());
    }

    [Fact]
    public void EndpointRequiresAdministratorRole()
    {
        var attributes = typeof(AdminListingRemovalController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true);

        var attribute = Assert.IsType<AuthorizeAttribute>(
            Assert.Single(attributes));

        Assert.Equal("Admin", attribute.Roles);
    }
}
