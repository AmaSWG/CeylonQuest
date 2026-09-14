using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Controllers;
using ProviderCatalogService.Data;
using ProviderCatalogService.Models;
using Xunit;

namespace ProviderCatalogService.Tests;

public class ProviderProfileControllerTests
{
    private static CatalogDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CatalogDbContext(options);
    }

    private static ProviderProfileController CreateController(CatalogDbContext db, Guid identityId, string email)
    {
        var controller = new ProviderProfileController(db);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, identityId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, "Provider")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return controller;
    }

    [Fact]
    public async Task GetMyProfile_ReturnsProfileForAuthenticatedProvider()
    {
        using var db = CreateDb();
        var identityId = Guid.NewGuid();
        var email = "chef@rest.com";

        db.Providers.Add(new Provider
        {
            Id = Guid.NewGuid(),
            IdentityUserId = identityId,
            BusinessName = "Cinnamon Spice Restaurant",
            Email = email,
            PhoneNumber = "+94112345678",
            ServiceType = "Restaurant",
            Location = "Colombo 07",
            Description = "Authentic fine dining"
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db, identityId, email);
        var result = await controller.GetMyProfile() as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task UpdateMyProfile_ValidData_UpdatesRecord()
    {
        using var db = CreateDb();
        var identityId = Guid.NewGuid();
        var email = "tour@safari.com";

        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            IdentityUserId = identityId,
            BusinessName = "Old Safari Name",
            Email = email,
            PhoneNumber = "0771112233",
            ServiceType = "Activity",
            Location = "Yala",
            Description = "Old description"
        };
        db.Providers.Add(provider);
        await db.SaveChangesAsync();

        var controller = CreateController(db, identityId, email);
        var updateDto = new UpdateProviderProfileRequest(
            "Yala Wilderness Expeditions",
            "Activity",
            "Yala National Park",
            "Updated thrilling safari tours",
            "+94779998877"
        );

        var result = await controller.UpdateMyProfile(updateDto) as OkObjectResult;
        Assert.NotNull(result);

        var updated = await db.Providers.FindAsync(provider.Id);
        Assert.Equal("Yala Wilderness Expeditions", updated!.BusinessName);
        Assert.Equal("Yala National Park", updated.Location);
    }
}