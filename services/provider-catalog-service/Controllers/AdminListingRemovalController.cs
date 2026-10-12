using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;


namespace ProviderCatalogService.Controllers;

[ApiController]
[Route("api/catalog/admin/listings")]
[Authorize(Roles = "Admin")]
public class AdminListingRemovalController : ControllerBase
{
    private readonly CatalogDbContext _db;

    public AdminListingRemovalController(CatalogDbContext db)
    {
        _db = db;
    }

    [HttpPost("{listingType}/{id:guid}/remove")]
    [ProducesResponseType(
        typeof(ListingRemovalResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remove(
        string listingType,
        Guid id,
        [FromBody] RemoveListingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        // Explicit checks also support direct controller unit tests.
        if (!request.ConfirmRemoval)
        {
            return BadRequest(new
            {
                message = "Listing removal must be confirmed."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Reason) ||
            request.Reason.Length > 1000)
        {
            return BadRequest(new
            {
                message = "A removal reason of 1–1000 characters is required."
            });
        }

        var administratorClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("nameid");

        if (!Guid.TryParse(administratorClaim, out var administratorId))
        {
            return Unauthorized(new
            {
                message = "A valid administrator identity is required."
            });
        }

        object? listing;
        string canonicalType;

        switch (listingType.Trim().ToLowerInvariant())
        {
            case "experience":
                canonicalType = "Experience";
                listing = await _db.ActivityListings
                    .SingleOrDefaultAsync(
                        l => l.Id == id, cancellationToken);
                break;

            case "restaurant":
                canonicalType = "Restaurant";
                listing = await _db.RestaurantListings
                    .SingleOrDefaultAsync(
                        l => l.Id == id, cancellationToken);
                break;

            case "accommodation":
                canonicalType = "Accommodation";
                listing = await _db.AccommodationListings
                    .SingleOrDefaultAsync(
                        l => l.Id == id, cancellationToken);
                break;

            default:
                return BadRequest(new
                {
                    message =
                        "ListingType must be Experience, Restaurant, or Accommodation."
                });
        }

        if (listing == null)
        {
            return NotFound(new
            {
                message = "Listing not found."
            });
        }

        var entry = _db.Entry<object>(listing);

        if (entry.Property<bool>("IsRemoved").CurrentValue)
        {
            return Conflict(new
            {
                message = "The listing has already been removed."
            });
        }

        if (!entry.Property<bool>("IsActive").CurrentValue)
        {
            return Conflict(new
            {
                message = "Only active listings can be removed."
            });
        }

        var removedAtUtc = DateTime.UtcNow;

        entry.Property<bool>("IsRemoved").CurrentValue = true;
        entry.Property<bool>("IsActive").CurrentValue = false;

        var audit = new ListingRemovalAudit
        {
            ListingId = id,
            ListingType = canonicalType,
            AdministratorId = administratorId,
            RemovedAtUtc = removedAtUtc,
            PreviousStatus = "Active",
            NewStatus = "Removed",
            Reason = request.Reason.Trim()
        };

        _db.ListingRemovalAudits.Add(audit);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                message =
                    "The listing changed during removal. Refresh and try again."
            });
        }

        return Ok(new ListingRemovalResponse
        {
            ListingId = id,
            ListingType = canonicalType,
            Status = "Removed",
            IsActive = false,
            AuditId = audit.Id,
            RemovedAtUtc = removedAtUtc
        });
    }
}
