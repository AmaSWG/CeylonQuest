using System.Security.Claims;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/platform-reviews")]
public class PlatformReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PlatformReviewsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    [Authorize(Roles = "Visitor,Provider")]
    public async Task<ActionResult<PlatformReviewResponse>> Create(
        CreatePlatformReviewRequest request,
        CancellationToken cancellationToken)
    {
        var rawUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(rawUserId, out var userId))
        {
            return Unauthorized();
        }

        var user = await _db.Users
            .SingleOrDefaultAsync(
                account => account.Id == userId,
                cancellationToken);

        if (user == null)
        {
            return Unauthorized();
        }

        // Check current account state as well as the JWT role.
        if (!user.IsActive ||
            user.RequiresPasswordChange ||
            (user.Role != UserRole.Visitor &&
             user.Role != UserRole.Provider))
        {
            return Forbid();
        }

        var content = request.Content?.Trim() ?? string.Empty;

        if (request.Rating < 1 || request.Rating > 5)
        {
            return BadRequest(new
            {
                message = "Rating must be a whole number from 1 to 5."
            });
        }

        if (content.Length < 1 || content.Length > 2000)
        {
            return BadRequest(new
            {
                message = "Review content must contain 1–2,000 characters."
            });
        }

        var review = new PlatformReview
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Rating = request.Rating,
            Content = content,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.PlatformReviews.Add(review);
        await _db.SaveChangesAsync(cancellationToken);

        var response = new PlatformReviewResponse(
            review.Id,
            GetDisplayName(user.FirstName, user.LastName),
            user.ProfilePictureUrl,
            review.Rating,
            review.Content,
            review.CreatedAtUtc);

        return Created("/api/platform-reviews", response);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PlatformReviewPageResponse>> Get(
        [FromQuery] int? rating = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (rating.HasValue && (rating.Value < 1 || rating.Value > 5))
        {
            return BadRequest(new
            {
                message = "The rating filter must be between 1 and 5."
            });
        }

        if (page < 1 || pageSize < 1 || pageSize > 100 ||
            ((long)page - 1) * pageSize > int.MaxValue)
        {
            return BadRequest(new
            {
                message = "Invalid pagination. Page size must be between 1 and 100."
            });
        }

        var query = _db.PlatformReviews.AsNoTracking();

        if (rating.HasValue)
        {
            query = query.Where(review => review.Rating == rating.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(review => review.CreatedAtUtc)
            .ThenByDescending(review => review.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(review => new
            {
                review.Id,
                review.User.FirstName,
                review.User.LastName,
                review.User.ProfilePictureUrl,
                review.Rating,
                review.Content,
                review.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(row => new PlatformReviewResponse(
            row.Id,
            GetDisplayName(row.FirstName, row.LastName),
            row.ProfilePictureUrl,
            row.Rating,
            row.Content,
            row.CreatedAtUtc)).ToList();

        return Ok(new PlatformReviewPageResponse(
            items,
            totalCount,
            page,
            pageSize));
    }

    private static string GetDisplayName(string firstName, string lastName)
    {
        var name = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? "CeylonQuest user" : name;
    }
}