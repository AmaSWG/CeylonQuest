using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProviderCatalogService.Controllers;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;
using Xunit;

namespace ProviderCatalogService.Tests;

public class SearchControllerTests
{
    private static CatalogDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CatalogDbContext(options);
    }

    private static async Task SeedSampleListingsAsync(CatalogDbContext db)
    {
        var providerId = Guid.NewGuid();
        db.Providers.Add(new Provider
        {
            Id = providerId,
            BusinessName = "Ceylon Ocean & Dine Adventures",
            Email = "partner@example.com"
        });

        // Activity: Pigeon Island Diving in Trincomalee
        db.ActivityListings.Add(new ActivityListing
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            Title = "Pigeon Island Coral Diving",
            Description = "Spectacular scuba diving tour among reef sharks and corals.",
            Location = "Trincomalee",
            Price = 7500,
            Unit = "Per Person",
            Duration = "3 Hours",
            IsActive = true
        });

        // Activity: Kite Surfing in Kalpitiya
        db.ActivityListings.Add(new ActivityListing
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            Title = "Kite Surfing Lesson",
            Description = "Learn how to kitesurf with certified instructors.",
            Location = "Kalpitiya",
            Price = 9000,
            Unit = "Per Person",
            Duration = "2 Hours",
            IsActive = true
        });

        // Inactive Activity: Closed Diving Cave (Should NEVER appear in search)
        db.ActivityListings.Add(new ActivityListing
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            Title = "Hidden Closed Diving Cave",
            Description = "Secret diving location closed for conservation.",
            Location = "Trincomalee",
            Price = 12000,
            Unit = "Per Person",
            IsActive = false
        });

        // Restaurant: Seafood in Colombo
        db.RestaurantListings.Add(new RestaurantListing
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            Name = "The Lagoon Seafood Feast",
            Description = "Fresh crab and jumbo prawn seafood dining experience.",
            CuisineType = "Seafood",
            DiningStyle = "Set Menu",
            Location = "Colombo",
            PricePerPerson = 4500,
            OpeningHours = "12:00 PM - 10:00 PM",
            GroupSizeCategory = "Table for Two",
            IsActive = true
        });

        // Accommodation: Luxury Villa in Mirissa
        db.AccommodationListings.Add(new AccommodationListing
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            RoomType = "Cliffside Ocean Villa",
            Description = "Private luxury villa with direct beach access.",
            PropertyType = "Villa",
            Location = "Mirissa",
            PricePerNight = 25000,
            MaxGuests = 4,
            BedDetails = "2 King Beds",
            MinStayNights = 2,
            IsActive = true
        });

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Verifies that a keyword search returns the matching listing across
    /// the combined activity and restaurant results
    /// </summary>
    [Fact]
    public async Task Search_KeywordMatch_ReturnsMatchingExperiencesAndRestaurants()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        var result = await controller.Search(q: "diving", type: "all", location: null, page: 1, pageSize: 10);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(1, paged.TotalCount);
        var item = paged.Items.First();
        Assert.Equal("Pigeon Island Coral Diving", item.Title);
        Assert.Equal("Experience", item.Type);
    }

    /// <summary>
    /// Verifies that a keyword matching no listings returns an empty
    /// result set with a zero total count
    /// </summary>
    [Fact]
    public async Task Search_NoMatch_ReturnsEmptyResultsWithZeroTotal()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        var result = await controller.Search(q: "NonExistentKeywordXYZ", type: "all", location: null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(0, paged.TotalCount);
        Assert.Empty(paged.Items);
    }

    /// <summary>
    /// Verifies that an empty keyword returns all active listings across
    /// all categories in a paginated response
    /// </summary>
    [Fact]
    public async Task Search_NoKeyword_ReturnsAllActiveListingsPaginated()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        var result = await controller.Search(q: "", type: "all", location: null, page: 1, pageSize: 10);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(4, paged.TotalCount);
        Assert.Equal(4, paged.Items.Count());
    }

    /// <summary>
    /// Verifies that the first page respects the requested page size and
    /// reports the correct pagination flags
    /// </summary>
    [Fact]
    public async Task Search_Pagination_Page1_RespectsPageAndPageSize()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        var result = await controller.Search(q: "", type: "all", location: null, page: 1, pageSize: 2);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(4, paged.TotalCount);
        Assert.Equal(2, paged.Items.Count());
        Assert.Equal(2, paged.TotalPages);
        Assert.True(paged.HasNextPage);
        Assert.False(paged.HasPreviousPage);
    }

    /// <summary>
    /// Verifies that the second page returns only the remaining items
    /// and reports the correct pagination flags
    /// </summary>
    [Fact]
    public async Task Search_Pagination_Page2_ReturnsRemainingItems()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        var result = await controller.Search(q: "", type: "all", location: null, page: 2, pageSize: 3);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(4, paged.TotalCount);
        Assert.Single(paged.Items);
        Assert.True(paged.HasPreviousPage);
        Assert.False(paged.HasNextPage);
    }

    /// <summary>
    /// Verifies that inactive listings never appear in search results
    /// </summary>
    [Fact]
    public async Task Search_InactiveListings_AreExcludedFromResults()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        // "Hidden Closed Diving Cave" is inactive
        var result = await controller.Search(q: "Hidden Closed", type: "all", location: null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(0, paged.TotalCount);
    }

    /// <summary>
    /// Verifies that the type filter restricts the results to the
    /// requested listing category
    /// </summary>
    [Fact]
    public async Task Search_TypeFilter_ReturnsOnlyRequestedType()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        var result = await controller.Search(q: "", type: "restaurant", location: null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(1, paged.TotalCount);
        Assert.Equal("Restaurant", paged.Items.First().Type);
    }

    /// <summary>
    /// Verifies that the location filter restricts the results to the
    /// requested location
    /// </summary>
    [Fact]
    public async Task Search_LocationFilter_ReturnsOnlyMatchingLocation()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        var result = await controller.Search(q: "", type: "all", location: "Trincomalee");

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(1, paged.TotalCount);
        Assert.Equal("Trincomalee", paged.Items.First().Location);
    }

    /// <summary>
    /// Verifies that keyword matching is case-insensitive
    /// </summary>
    [Fact]
    public async Task Search_KeywordMatch_IsCaseInsensitive()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        var resLower = await controller.Search(q: "seafood", type: "all", location: null);
        var resUpper = await controller.Search(q: "SEAFOOD", type: "all", location: null);
        var resMixed = await controller.Search(q: "SeaFood", type: "all", location: null);

        var pagedLower = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(((OkObjectResult)resLower).Value);
        var pagedUpper = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(((OkObjectResult)resUpper).Value);
        var pagedMixed = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(((OkObjectResult)resMixed).Value);

        Assert.Equal(1, pagedLower.TotalCount);
        Assert.Equal(pagedLower.TotalCount, pagedUpper.TotalCount);
        Assert.Equal(pagedLower.TotalCount, pagedMixed.TotalCount);
    }

    /// <summary>
    /// Verifies that a partial keyword matches listings by substring
    /// </summary>
    [Fact]
    public async Task Search_PartialKeyword_MatchesSubstring()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        var result = await controller.Search(q: "sur", type: "all", location: null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(1, paged.TotalCount);
        Assert.Contains("Surfing", paged.Items.First().Title);
    }

    /// <summary>
    /// Verifies that combining keyword, type, and location filters returns
    /// only listings matching all criteria
    /// </summary>
    [Fact]
    public async Task Search_CombinedFilters_ReturnsIntersection()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        var result = await controller.Search(q: "coral", type: "experience", location: "Trincomalee");

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(1, paged.TotalCount);
        Assert.Equal("Pigeon Island Coral Diving", paged.Items.First().Title);
    }

    /// <summary>
    /// Verifies that searching an empty database returns a zero total
    /// without throwing
    /// </summary>
    [Fact]
    public async Task Search_EmptyDatabase_ReturnsZeroTotalWithoutThrowing()
    {
        using var db = CreateInMemoryDbContext();
        var controller = new SearchController(db);

        var result = await controller.Search(q: "safari", type: "all", location: "Yala");

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(0, paged.TotalCount);
        Assert.Empty(paged.Items);
    }

    /// <summary>
    /// Verifies that requesting a page beyond the available pages returns
    /// an empty item collection with the correct total count
    /// </summary>
    [Fact]
    public async Task Search_PageBeyondTotalPages_ReturnsEmptyItems()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        // Page 99 when there are only 4 items
        var result = await controller.Search(q: "", type: "all", location: null, page: 99, pageSize: 10);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(4, paged.TotalCount);
        Assert.Empty(paged.Items);
    }

    /// <summary>
    /// Verifies that a keyword present only in the description still
    /// matches the listing
    /// </summary>
    [Fact]
    public async Task Search_KeywordInDescriptionOnly_ReturnsListing()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        // "sharks" is ONLY in the description
        var result = await controller.Search(q: "sharks", type: "all", location: null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(1, paged.TotalCount);
        Assert.Equal("Pigeon Island Coral Diving", paged.Items.First().Title);
    }

    /// <summary>
    /// Verifies that invalid page and pageSize values are clamped to
    /// safe defaults within the allowed bounds
    /// </summary>
    [Fact]
    public async Task Search_InvalidPageAndPageSize_ClampsToSafeDefaults()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        // Negative page and pageSize
        var result = await controller.Search(q: "", type: "all", location: null, page: -2, pageSize: -10);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(1, paged.Page);
        Assert.Equal(9, paged.PageSize);
        Assert.Equal(4, paged.TotalCount);
        Assert.Equal(4, paged.Items.Count());

        // Oversized pageSize > 50 should clamp to 50
        var resultOversized = await controller.Search(q: "", type: "all", location: null, page: 1, pageSize: 500);
        var pagedOversized = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(((OkObjectResult)resultOversized).Value);
        Assert.Equal(50, pagedOversized.PageSize);
    }

    /// <summary>
    /// Verifies that an unrecognized type filter returns an empty result set
    /// </summary>
    [Fact]
    public async Task Search_UnrecognizedTypeFilter_ReturnsEmptyResults()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        var result = await controller.Search(q: "", type: "unknown_category_xyz", location: null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(0, paged.TotalCount);
        Assert.Empty(paged.Items);
    }

    /// <summary>
    /// Verifies that combined location, category, and price filters return
    /// only listings satisfying the full intersection of criteria
    /// </summary>
    [Fact]
    public async Task Search_CombinedFilters_LocationCategoryAndPrice_ReturnsMatchingIntersection()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);
        // Filter: Type = Experience, Location = Trincomalee, Price between 5,000 and 8,000 LKR
        var result = await controller.Search(
            q: null,
            type: "experience",
            location: "Trincomalee",
            minPrice: 5000,
            maxPrice: 8000
        );
        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);
        // Matches Pigeon Island Diving (Price: 7,500, Location: Trincomalee, Type: Experience)
        Assert.Equal(1, paged.TotalCount);
        var item = paged.Items.First();
        Assert.Equal("Pigeon Island Coral Diving", item.Title);
        Assert.Equal("Trincomalee", item.Location);
        Assert.Equal(7500, item.Price);
    }

    /// <summary>
    /// Verifies that clearing previously applied filters returns the full
    /// unfiltered list of active listings
    /// </summary>
    [Fact]
    public async Task Search_ClearFilters_ReturnsFullUnfilteredList()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);
        // First apply restrictive filter
        var filteredResult = await controller.Search(q: null, type: "experience", location: "Kalpitiya", minPrice: 1000, maxPrice: 10000);
        var filteredPaged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(((OkObjectResult)filteredResult).Value);
        Assert.Equal(1, filteredPaged.TotalCount);
        // Now clear all filters (q: null, type: "all", location: null, minPrice: null, maxPrice: null)
        var clearedResult = await controller.Search(q: null, type: "all", location: null, minPrice: null, maxPrice: null);
        var clearedPaged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(((OkObjectResult)clearedResult).Value);
        // Returns all 4 active listings
        Assert.Equal(4, clearedPaged.TotalCount);
    }

    /// <summary>
    /// Verifies that the minPrice filter excludes listings priced below
    /// the provided threshold
    /// </summary>
    [Fact]
    public async Task Search_MinPriceFilter_ExcludesItemsBelowThreshold()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);
        // Only listings >= 8,000 LKR (Kite Surfing: 9000, Villa: 25000)
        var result = await controller.Search(q: null, type: "all", location: null, minPrice: 8000);
        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);
        Assert.Equal(2, paged.TotalCount);
        Assert.All(paged.Items, item => Assert.True(item.Price >= 8000));
    }

    /// <summary>
    /// Verifies that the maxPrice filter excludes listings priced above
    /// the provided threshold
    /// </summary>
    [Fact]
    public async Task Search_MaxPriceFilter_ExcludesItemsAboveThreshold()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);
        // Only listings <= 5,000 LKR (The Lagoon Seafood Feast: 4500)
        var result = await controller.Search(q: null, type: "all", location: null, maxPrice: 5000);
        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);
        Assert.Equal(1, paged.TotalCount);
        Assert.Equal("The Lagoon Seafood Feast", paged.Items.First().Title);
        Assert.Equal(4500, paged.Items.First().Price);
    }

    /// <summary>
    /// Verifies that combining filters with no matching intersection
    /// returns an empty result set
    /// </summary>
    [Fact]
    public async Task Search_CombinedFilters_NoIntersection_ReturnsZeroResults()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);
        // Type = Restaurant, Location = Trincomalee (Seafood restaurant is in Colombo)
        var result = await controller.Search(q: null, type: "restaurant", location: "Trincomalee");
        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);
        Assert.Equal(0, paged.TotalCount);
        Assert.Empty(paged.Items);
    }

    /// <summary>
    /// Verifies that sorting by price ascending orders results from
    /// cheapest to most expensive
    /// </summary>
    [Fact]
    public async Task Search_SortByPriceAsc_ReturnsCheapestFirst()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        var result = await controller.Search(q: null, type: "all", location: null, sortOrder: "price_asc");

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        var prices = paged.Items.Select(x => x.Price).ToList();

        // Assert prices are in strictly non-decreasing order: 4500 <= 7500 <= 9000 <= 25000
        for (int i = 0; i < prices.Count - 1; i++)
        {
            Assert.True(prices[i] <= prices[i + 1], $"Price {prices[i]} should be <= {prices[i + 1]}");
        }
    }

    /// <summary>
    /// Verifies that sorting by price descending orders results from
    /// most expensive to cheapest
    /// </summary>
    [Fact]
    public async Task Search_SortByPriceDesc_ReturnsMostExpensiveFirst()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        var result = await controller.Search(q: null, type: "all", location: null, sortOrder: "price_desc");

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        var prices = paged.Items.Select(x => x.Price).ToList();

        // Assert prices are in strictly non-increasing order: 25000 >= 9000 >= 7500 >= 4500
        for (int i = 0; i < prices.Count - 1; i++)
        {
            Assert.True(prices[i] >= prices[i + 1], $"Price {prices[i]} should be >= {prices[i + 1]}");
        }
    }

    /// <summary>
    /// Verifies that the default sort order returns the newest listings first
    /// </summary>
    [Fact]
    public async Task Search_SortByDefault_ReturnsNewestCreatedFirst()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        var result = await controller.Search(q: null, type: "all", location: null, sortOrder: null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        var dates = paged.Items.Select(x => x.CreatedAt).ToList();
        for (int i = 0; i < dates.Count - 1; i++)
        {
            Assert.True(dates[i] >= dates[i + 1]);
        }
    }

    /// <summary>
    /// Verifies that an unrecognized sort order falls back to the default
    /// ordering without failing
    /// </summary>
    [Fact]
    public async Task Search_UnrecognizedSortOrder_FallsBackToDefaultOrder()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        var result = await controller.Search(q: null, type: "all", location: null, sortOrder: "unsupported_sort");

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(4, paged.TotalCount);
        Assert.NotEmpty(paged.Items);
    }

    /// <summary>
    /// Verifies that applying a filter and sort together returns the
    /// correctly filtered and ordered results
    /// </summary>
    [Fact]
    public async Task Search_SortAndFilterCombined_FiltersAndSortsCorrectly()
    {
        using var db = CreateInMemoryDbContext();
        await SeedSampleListingsAsync(db);
        var controller = new SearchController(db);

        // Experiences sorted by price ascending (Pigeon Island: 7500, Kite Surfing: 9000)
        var result = await controller.Search(q: null, type: "experience", location: null, sortOrder: "price_asc");

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(ok.Value);

        Assert.Equal(2, paged.TotalCount);
        Assert.Equal(7500, paged.Items.First().Price);
        Assert.Equal(9000, paged.Items.Last().Price);
    }
}