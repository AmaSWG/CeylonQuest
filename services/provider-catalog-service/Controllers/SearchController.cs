using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;

namespace ProviderCatalogService.Controllers;

[ApiController]
[Route("api/catalog/search")]
[AllowAnonymous]
public class SearchController : ControllerBase
{
    private readonly CatalogDbContext _db;

    public SearchController(CatalogDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] string? type, // "all" | "experience" | "restaurant" | "accommodation"
        [FromQuery] string? location,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 8)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 8;
        if (pageSize > 50) pageSize = 50;

        var keyword = q?.Trim().ToLower() ?? "";
        var filterType = type?.Trim().ToLower() ?? "all";
        var locFilter = location?.Trim().ToLower() ?? "";

        var results = new List<SearchResultItemDto>();

        // 1. Search Activities / Experiences
        if (filterType == "all" || filterType == "experience" || filterType == "activity")
        {
            var expQuery = _db.ActivityListings
                .AsNoTracking()
                .Include(a => a.Provider)
                .Where(a => a.IsActive);

            if (!string.IsNullOrEmpty(keyword))
            {
                expQuery = expQuery.Where(a =>
                    a.Title.ToLower().Contains(keyword) ||
                    a.Description.ToLower().Contains(keyword) ||
                    a.Location.ToLower().Contains(keyword) ||
                    (a.Duration != null && a.Duration.ToLower().Contains(keyword)));
            }

            if (!string.IsNullOrEmpty(locFilter))
            {
                expQuery = expQuery.Where(a => a.Location.ToLower().Contains(locFilter));
            }

            var experiences = await expQuery.Select(a => new SearchResultItemDto
            {
                Id = a.Id,
                Type = "Experience",
                Title = a.Title,
                Description = a.Description,
                Location = a.Location,
                Price = a.Price,
                PriceFormatted = $"LKR {a.Price:N0} / {a.Unit}",
                KeyDetail = $"Duration: {a.Duration ?? "Flexible"} • Max {a.MaxParticipants} pax",
                ScheduleInfo = a.TimeSlots ?? (a.AvailableDays ?? "Daily"),
                ProviderBusinessName = a.Provider != null ? a.Provider.BusinessName : "Certified Partner",
                CreatedAt = a.CreatedAt
            }).ToListAsync();

            results.AddRange(experiences);
        }

        // 2. Search Restaurants / Dining
        if (filterType == "all" || filterType == "restaurant" || filterType == "dining")
        {
            var restQuery = _db.RestaurantListings
                .AsNoTracking()
                .Include(r => r.Provider)
                .Where(r => r.IsActive);

            if (!string.IsNullOrEmpty(keyword))
            {
                restQuery = restQuery.Where(r =>
                    r.Name.ToLower().Contains(keyword) ||
                    r.Description.ToLower().Contains(keyword) ||
                    r.CuisineType.ToLower().Contains(keyword) ||
                    r.Location.ToLower().Contains(keyword) ||
                    r.DiningStyle.ToLower().Contains(keyword) ||
                    r.SetMenuDetails.ToLower().Contains(keyword));
            }

            if (!string.IsNullOrEmpty(locFilter))
            {
                restQuery = restQuery.Where(r => r.Location.ToLower().Contains(locFilter));
            }

            var restaurants = await restQuery.Select(r => new SearchResultItemDto
            {
                Id = r.Id,
                Type = "Restaurant",
                Title = r.Name,
                Description = r.Description,
                Location = r.Location,
                Price = r.PricePerPerson,
                PriceFormatted = $"LKR {r.PricePerPerson:N0} / person",
                KeyDetail = $"{r.CuisineType} • {r.DiningStyle} • {r.GroupSizeCategory}",
                ScheduleInfo = $"Hours: {r.OpeningHours}",
                ProviderBusinessName = r.Provider != null ? r.Provider.BusinessName : "Certified Partner",
                CreatedAt = r.CreatedAt
            }).ToListAsync();

            results.AddRange(restaurants);
        }

        // 3. Search Accommodations (if any)
        if (filterType == "all" || filterType == "accommodation" || filterType == "hotel")
        {
            var accQuery = _db.AccommodationListings
                .AsNoTracking()
                .Include(a => a.Provider)
                .Where(a => a.IsActive);

            if (!string.IsNullOrEmpty(keyword))
            {
                accQuery = accQuery.Where(a =>
                    a.RoomType.ToLower().Contains(keyword) ||
                    a.Description.ToLower().Contains(keyword) ||
                    a.PropertyType.ToLower().Contains(keyword) ||
                    a.Location.ToLower().Contains(keyword) ||
                    a.Amenities.ToLower().Contains(keyword));
            }

            if (!string.IsNullOrEmpty(locFilter))
            {
                accQuery = accQuery.Where(a => a.Location.ToLower().Contains(locFilter));
            }

            var accommodations = await accQuery.Select(a => new SearchResultItemDto
            {
                Id = a.Id,
                Type = "Accommodation",
                Title = a.RoomType,
                Description = a.Description,
                Location = a.Location,
                Price = a.PricePerNight,
                PriceFormatted = $"LKR {a.PricePerNight:N0} / night",
                KeyDetail = $"{a.PropertyType} • Max {a.MaxGuests} guests • {a.BedDetails}",
                ScheduleInfo = $"Min {a.MinStayNights} night stay",
                ProviderBusinessName = a.Provider != null ? a.Provider.BusinessName : "Certified Partner",
                CreatedAt = a.CreatedAt
            }).ToListAsync();

            results.AddRange(accommodations);
        }

        // Pagination
        var totalCount = results.Count;
        var pagedItems = results
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var response = new PaginatedResponse<SearchResultItemDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = pagedItems
        };

        return Ok(response);
    }
}