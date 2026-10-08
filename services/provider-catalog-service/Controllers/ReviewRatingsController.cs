using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProviderCatalogService.Services;

namespace ProviderCatalogService.Controllers;

[ApiController]
[Route("api/catalog/review-ratings")]
[Authorize(Roles = "Admin")]
public class ReviewRatingsController(ReviewRatingProjectionService projections) : ControllerBase
{
    [HttpPost("{listingId:guid}/rebuild")]
    public async Task<IActionResult> Rebuild(Guid listingId, [FromQuery] string bookingType, CancellationToken token)
    {
        try { await projections.RebuildAsync(listingId, bookingType, token); return NoContent(); }
        catch (ArgumentException ex) { return Problem(statusCode: 400, detail: ex.Message); }
        catch (KeyNotFoundException ex) { return Problem(statusCode: 404, detail: ex.Message); }
    }
}
