using System.Security.Claims;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Controllers;

/// <summary>
/// Manages service / activity entries for the authenticated Provider.
/// All endpoints are restricted to users with the Provider role.
/// Data isolation is enforced: each operation filters by the authenticated user's ID.
/// </summary>
[ApiController]
[Route("api/provider/prices")]
[Authorize(Roles = "Provider")]
public class ProviderServicePricesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProviderServicePricesController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /api/provider/prices
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var providerId = GetProviderId();
        if (providerId is null) return Unauthorized();

        var prices = await _db.ProviderServicePrices
            .Where(p => p.ProviderId == providerId.Value)
            .OrderBy(p => p.ServiceName)
            .Select(p => MapToResponse(p))
            .ToListAsync();

        return Ok(prices);
    }

    // POST /api/provider/prices
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProviderServicePriceRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var providerId = GetProviderId();
        if (providerId is null) return Unauthorized();

        var price = new ProviderServicePrice
        {
            Id           = Guid.NewGuid(),
            ProviderId   = providerId.Value,
            ServiceName  = request.ServiceName.Trim(),
            Description  = request.Description?.Trim() ?? string.Empty,
            PricePerUnit = request.PricePerUnit,
            Unit         = request.Unit.Trim(),
            IsActive     = request.IsActive,
            UpdatedAt    = DateTime.UtcNow
        };

        _db.ProviderServicePrices.Add(price);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { }, MapToResponse(price));
    }

    // PUT /api/provider/prices/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProviderServicePriceRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var providerId = GetProviderId();
        if (providerId is null) return Unauthorized();

        var price = await _db.ProviderServicePrices
            .FirstOrDefaultAsync(p => p.Id == id && p.ProviderId == providerId.Value);

        if (price is null) return NotFound(new { message = "Price entry not found." });

        price.ServiceName  = request.ServiceName.Trim();
        price.Description  = request.Description?.Trim() ?? string.Empty;
        price.PricePerUnit = request.PricePerUnit;
        price.Unit         = request.Unit.Trim();
        price.IsActive     = request.IsActive;
        price.UpdatedAt    = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(MapToResponse(price));
    }

    // DELETE /api/provider/prices/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var providerId = GetProviderId();
        if (providerId is null) return Unauthorized();

        var price = await _db.ProviderServicePrices
            .FirstOrDefaultAsync(p => p.Id == id && p.ProviderId == providerId.Value);

        if (price is null) return NotFound(new { message = "Price entry not found." });

        _db.ProviderServicePrices.Remove(price);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private Guid? GetProviderId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static ProviderServicePriceResponse MapToResponse(ProviderServicePrice p) =>
        new ProviderServicePriceResponse
        {
            Id           = p.Id,
            ProviderId   = p.ProviderId,
            ServiceName  = p.ServiceName,
            Description  = p.Description,
            PricePerUnit = p.PricePerUnit,
            Unit         = p.Unit,
            IsActive     = p.IsActive,
            UpdatedAt    = p.UpdatedAt
        };
}
