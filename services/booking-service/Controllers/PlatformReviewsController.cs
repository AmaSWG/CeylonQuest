using System.Security.Claims;
using BookingService.DTOs;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Controllers;

[ApiController]
[Route("api/bookings/platform-reviews")]
public class PlatformReviewsController(IReviewService reviews) : ControllerBase
{
    /// <summary>Submit general platform feedback as an authenticated visitor.</summary>
    [HttpPost]
    [Authorize(Roles = "Visitor")]
    [ProducesResponseType(typeof(PlatformReviewResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(CreatePlatformReviewRequest request, CancellationToken token)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var visitorId)
            || visitorId == Guid.Empty) return Unauthorized();
        try
        {
            var review = await reviews.CreatePlatformAsync(visitorId, request, token);
            return CreatedAtAction(nameof(Get), review);
        }
        catch (ReviewException ex) { return Problem(statusCode: ex.StatusCode, detail: ex.Message); }
    }

    /// <summary>Public platform feedback with optional rating filter and pagination.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedPlatformReviewsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get([FromQuery] ReviewQuery query, CancellationToken token)
    {
        try { return Ok(await reviews.GetPlatformAsync(query, token)); }
        catch (ReviewException ex) { return Problem(statusCode: ex.StatusCode, detail: ex.Message); }
    }
}
