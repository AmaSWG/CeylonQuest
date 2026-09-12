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

        // 1. Activity: Pigeon Island Diving in Trincomalee
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

        // 2. Activity: Kite Surfing in Kalpitiya
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

        // 3. Inactive Activity: Closed Diving Cave (Should NEVER appear in search)
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

        // 4. Restaurant: Seafood in Colombo
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

        // 5. Accommodation: Luxury Villa in Mirissa
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
        Assert.Equal(8, paged.PageSize);
        Assert.Equal(4, paged.TotalCount);
        Assert.Equal(4, paged.Items.Count());

        // Oversized pageSize > 50 should clamp to 50
        var resultOversized = await controller.Search(q: "", type: "all", location: null, page: 1, pageSize: 500);
        var pagedOversized = Assert.IsType<PaginatedResponse<SearchResultItemDto>>(((OkObjectResult)resultOversized).Value);
        Assert.Equal(50, pagedOversized.PageSize);
    }

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
}