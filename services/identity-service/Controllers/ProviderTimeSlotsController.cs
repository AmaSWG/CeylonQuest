using System.Security.Claims;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Controllers;

/// <summary>
/// Manages available time slots for the authenticated Provider.
/// All endpoints are restricted to users with the Provider role.
/// Data isolation is enforced: each operation filters by the authenticated user's ID.
/// </summary>
[ApiController]
[Route("api/provider/timeslots")]
[Authorize(Roles = "Provider")]
public class ProviderTimeSlotsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProviderTimeSlotsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET /api/provider/timeslots
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var providerId = GetProviderId();
        if (providerId is null) return Unauthorized();

        var slots = await _db.ProviderTimeSlots
            .Where(s => s.ProviderId == providerId.Value)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .Select(s => MapToResponse(s))
            .ToListAsync();

        return Ok(slots);
    }

    // POST /api/provider/timeslots
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProviderTimeSlotRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var providerId = GetProviderId();
        if (providerId is null) return Unauthorized();

        var slot = new ProviderTimeSlot
        {
            Id          = Guid.NewGuid(),
            ProviderId  = providerId.Value,
            Date        = request.Date,
            StartTime   = request.StartTime,
            EndTime     = request.EndTime,
            IsAvailable = request.IsAvailable,
            CreatedAt   = DateTime.UtcNow
        };

        _db.ProviderTimeSlots.Add(slot);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { }, MapToResponse(slot));
    }

    // PUT /api/provider/timeslots/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProviderTimeSlotRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var providerId = GetProviderId();
        if (providerId is null) return Unauthorized();

        var slot = await _db.ProviderTimeSlots
            .FirstOrDefaultAsync(s => s.Id == id && s.ProviderId == providerId.Value);

        if (slot is null) return NotFound(new { message = "Time slot not found." });

        slot.Date        = request.Date;
        slot.StartTime   = request.StartTime;
        slot.EndTime     = request.EndTime;
        slot.IsAvailable = request.IsAvailable;

        await _db.SaveChangesAsync();

        return Ok(MapToResponse(slot));
    }

    // DELETE /api/provider/timeslots/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var providerId = GetProviderId();
        if (providerId is null) return Unauthorized();

        var slot = await _db.ProviderTimeSlots
            .FirstOrDefaultAsync(s => s.Id == id && s.ProviderId == providerId.Value);

        if (slot is null) return NotFound(new { message = "Time slot not found." });

        _db.ProviderTimeSlots.Remove(slot);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private Guid? GetProviderId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static ProviderTimeSlotResponse MapToResponse(ProviderTimeSlot s) =>
        new ProviderTimeSlotResponse
        {
            Id          = s.Id,
            ProviderId  = s.ProviderId,
            Date        = s.Date,
            StartTime   = s.StartTime,
            EndTime     = s.EndTime,
            IsAvailable = s.IsAvailable,
            CreatedAt   = s.CreatedAt
        };
}
