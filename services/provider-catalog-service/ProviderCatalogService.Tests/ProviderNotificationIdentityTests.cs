using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Controllers;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;

namespace ProviderCatalogService.Tests;

public class ProviderNotificationIdentityTests
{
    [Fact]
    public async Task PublicActivityLookupLoadsProviderIdentityInFreshContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var providerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var listingId = Guid.NewGuid();
        await using (var seed = new CatalogDbContext(options))
        {
            seed.Providers.Add(new Provider { Id = providerId, IdentityUserId = userId });
            seed.ActivityListings.Add(new ActivityListing
            { Id = listingId, ProviderId = providerId, IsActive = true });
            await seed.SaveChangesAsync();
        }
        await using var db = new CatalogDbContext(options);
        var result = await new ActivityListingsController(db).GetPublicListingById(listingId);
        var dto = Assert.IsType<ActivityListingResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(userId, dto.ProviderUserId);
    }
}
