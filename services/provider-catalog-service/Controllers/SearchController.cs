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

    private static string NormalizeImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        if (url.Contains("/provider-service-images/"))
        {
            var fileName = System.IO.Path.GetFileName(new Uri(url).LocalPath);
            return $"/api/catalog/images/{fileName}";
        }
        return url;
    }

    private static List<string> ParseImages(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
        raw = raw.Trim();
        List<string> result = new();
        if (raw.StartsWith("[") && raw.EndsWith("]"))
        {
            try
            {
                var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(raw);
                if (list != null) result = list.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }
            catch { }
        }
        else
        {
            result = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
        return result.Select(NormalizeImageUrl).ToList();
    }

    /// <summary>
    /// Searches across activity, restaurant, and accommodation listings with
    /// optional keyword, type, location, price, day, sorting, and pagination filters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? q = null,
        [FromQuery] string? type = null,
        [FromQuery] string? location = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? sort = null,
        [FromQuery] string? sortOrder = null,
        [FromQuery] string? availableDay = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 8)
    {
        // Clamp pagination values to safe bounds
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 8;
        if (pageSize > 50) pageSize = 50;

        // Normalize all incoming filter values for case-insensitive comparison
        var keyword = q?.Trim().ToLower() ?? "";
        var filterType = type?.Trim().ToLower() ?? "all";
        var locFilter = location?.Trim().ToLower() ?? "";
        var dayFilter = availableDay?.Trim().ToLower() ?? "";
        var activeSort = (sort ?? sortOrder)?.Trim().ToLower() ?? "";

        var results = new List<SearchResultItemDto>();

        // Search Activities / Experiences
        if (filterType == "all" || filterType == "experience" || filterType == "activity")
        {
            var expQuery = _db.ActivityListings
                .AsNoTracking()
                .Include(a => a.Provider)
                .Where(a => a.IsActive);

            // Apply the keyword search across the most relevant text fields
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

            if (minPrice.HasValue && minPrice.Value > 0)
            {
                expQuery = expQuery.Where(a => a.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                expQuery = expQuery.Where(a => a.Price <= maxPrice.Value);
            }

            if (!string.IsNullOrEmpty(dayFilter))
            {
                expQuery = expQuery.Where(a => a.AvailableDays != null && a.AvailableDays.ToLower().Contains(dayFilter));
            }

            var expList = await expQuery.ToListAsync();
            var experiences = expList.Select(a => new SearchResultItemDto
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
                Unit = a.Unit,
                Category = "Experience",
                Duration = a.Duration,
                MaxParticipants = a.MaxParticipants,
                AvailableDays = a.AvailableDays,
                TimeSlots = a.TimeSlots,
                CreatedAt = a.CreatedAt,
                Images = ParseImages(a.Images)
            }).ToList();

            results.AddRange(experiences);
        }

        // Search Restaurants / Dining
        if (filterType == "all" || filterType == "restaurant" || filterType == "dining")
        {
            var restQuery = _db.RestaurantListings
                .AsNoTracking()
                .Include(r => r.Provider)
                .Where(r => r.IsActive);

            // Apply the keyword search across the most relevant text fields
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

            if (minPrice.HasValue && minPrice.Value > 0)
            {
                restQuery = restQuery.Where(r => r.PricePerPerson >= minPrice.Value);
            }
            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                restQuery = restQuery.Where(r => r.PricePerPerson <= maxPrice.Value);
            }

            var restList = await restQuery.ToListAsync();
            var restaurants = restList.Select(r => new SearchResultItemDto
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
                Unit = "person",
                Category = r.CuisineType,
                CuisineType = r.CuisineType,
                DiningStyle = r.DiningStyle,
                OpeningHours = r.OpeningHours,
                SeatingCapacity = r.SeatingCapacity,
                SetMenuDetails = r.SetMenuDetails,
                DietaryOptions = r.DietaryOptions,
                CreatedAt = r.CreatedAt,
                Images = ParseImages(r.Images)
            }).ToList();

            results.AddRange(restaurants);
        }

        // Search Accommodations
        if (filterType == "all" || filterType == "accommodation" || filterType == "hotel")
        {
            var accQuery = _db.AccommodationListings
                .AsNoTracking()
                .Include(a => a.Provider)
                .Where(a => a.IsActive);

            // Apply the keyword search across the most relevant text fields
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

            if (minPrice.HasValue && minPrice.Value > 0)
            {
                accQuery = accQuery.Where(a => a.PricePerNight >= minPrice.Value);
            }
            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                accQuery = accQuery.Where(a => a.PricePerNight <= maxPrice.Value);
            }

            var accList = await accQuery.ToListAsync();
            var accommodations = accList.Select(a => new SearchResultItemDto
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
                Unit = "night",
                Category = a.PropertyType,
                PropertyType = a.PropertyType,
                MaxGuests = a.MaxGuests,
                BedDetails = a.BedDetails,
                MinStayNights = a.MinStayNights,
                Amenities = a.Amenities,
                BathroomDetails = a.BathroomDetails,
                CreatedAt = a.CreatedAt,
                Images = ParseImages(a.Images)
            }).ToList();

            results.AddRange(accommodations);
        }

        // Pagination
        var totalCount = results.Count;

        // Apply the requested sort order, defaulting to newest first
        IEnumerable<SearchResultItemDto> ordered = activeSort switch
        {
            "price_asc"  => results.OrderBy(x => x.Price),
            "price_desc" => results.OrderByDescending(x => x.Price),
            _            => results.OrderByDescending(x => x.CreatedAt)
        };

        var pagedItems = ordered
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