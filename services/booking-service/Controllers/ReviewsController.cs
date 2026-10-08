using System.Security.Claims;
using BookingService.DTOs;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Controllers;

[ApiController]
[Route("api/bookings/reviews")]
public class ReviewsController(IReviewService reviews) : ControllerBase
{
    /// <summary>Review an owned, completed booking after its scheduled end.</summary>
    [HttpPost]
    [Authorize(Roles = "Visitor")]
    [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create(CreateReviewRequest request, CancellationToken token)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var visitorId)
            || visitorId == Guid.Empty)
            return Unauthorized();
        try
        {
            var review = await reviews.CreateAsync(visitorId, request, token);
            return CreatedAtAction(nameof(Get), new { listingId = review.ListingId }, review);
        }
        catch (ReviewException ex) { return Problem(statusCode: ex.StatusCode, detail: ex.Message); }
    }

    /// <summary>Public reviews, newest first; rating filters do not change the overall summary.</summary>
    [HttpGet("{listingId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedReviewsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get(Guid listingId, [FromQuery] ReviewQuery query, CancellationToken token)
    {
        if (listingId == Guid.Empty) return BadRequest();
        return Ok(await reviews.GetAsync(listingId, query, token));
    }

    /// <summary>Current average and count calculated from all saved listing reviews.</summary>
    [HttpGet("{listingId:guid}/summary")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ReviewSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Summary(Guid listingId, [FromQuery] ReviewQuery query, CancellationToken token)
    {
        if (listingId == Guid.Empty) return BadRequest();
        return Ok(await reviews.GetSummaryAsync(listingId, query.BookingType, token));
    }
}
