using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;

namespace ProviderCatalogService.Controllers;

[ApiController]
[Route("api/catalog/activity-listings")]
[Authorize(Roles = "Provider")]
public class ListingsController : ControllerBase
{
    private readonly CatalogDbContext _db;

    public ListingsController(CatalogDbContext db)
    {
        _db = db;
    }

    // POST /api/catalog/activity-listings
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateActivityListingRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var identityUserId = GetIdentityUserId();

        if (identityUserId is null)
			return Unauthorized();
		
		// Provider row is created on approval; IdentityUserId is set when the account is linked.
		var provider = await _db.Providers
			.FirstOrDefaultAsync(p => p.IdentityUserId == identityUserId.Value);
		
		if (provider is null)
		{
			return Forbid();
		}

        var listing = new ActivityListing
        {
            Id = Guid.NewGuid(),
            ProviderId = provider.Id,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            Unit = request.Unit.Trim(),
            Location = request.Location.Trim(),
			MaxParticipants = request.MaxParticipants,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.ActivityListings.Add(listing);

        await _db.SaveChangesAsync();

        return Created(
            $"/api/catalog/activity-listings/{listing.Id}",
            listing
        );
    }

    private Guid? GetIdentityUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");

        return Guid.TryParse(raw, out var id) ? id : null;
    }
}