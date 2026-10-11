using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using IdentityService.Controllers;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdentityService.Tests;

public class PlatformReviewsControllerTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static User AddUser(
        ApplicationDbContext db,
        UserRole role = UserRole.Visitor)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = role.ToString(),
            Email = $"{Guid.NewGuid()}@example.com",
            Role = role,
            IsActive = true
        };

        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    private static PlatformReviewsController Controller(
        ApplicationDbContext db,
        User user = null)
    {
        var controller = new PlatformReviewsController(db);

        var claims = user == null
            ? Array.Empty<Claim>()
            : new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(claims, user == null ? null : "Test"))
            }
        };

        return controller;
    }

    [Theory]
    [InlineData(UserRole.Visitor)]
    [InlineData(UserRole.Provider)]
    public async Task EligibleUser_SubmissionStoresIdentityAndUtcTimestamp(
        UserRole role)
    {
        await using var db = CreateDb();
        var user = AddUser(db, role);
        var before = DateTime.UtcNow;

        var result = await Controller(db, user).Create(
            new CreatePlatformReviewRequest
            {
                Rating = 5,
                Content = "  Great platform  "
            },
            CancellationToken.None);

        Assert.IsType<CreatedResult>(result.Result);

        var stored = await db.PlatformReviews.SingleAsync();

        Assert.Equal(user.Id, stored.UserId);
        Assert.Equal(5, stored.Rating);
        Assert.Equal("Great platform", stored.Content);
        Assert.NotEqual(Guid.Empty, stored.Id);
        Assert.Equal(DateTimeKind.Utc, stored.CreatedAtUtc.Kind);
        Assert.InRange(stored.CreatedAtUtc, before, DateTime.UtcNow);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task InvalidRating_IsRejected(int rating)
    {
        await using var db = CreateDb();
        var user = AddUser(db);

        var result = await Controller(db, user).Create(
            new CreatePlatformReviewRequest
            {
                Rating = rating,
                Content = "Valid feedback"
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.PlatformReviews);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task MissingContent_IsRejected(string content)
    {
        await using var db = CreateDb();
        var user = AddUser(db);

        var result = await Controller(db, user).Create(
            new CreatePlatformReviewRequest
            {
                Rating = 4,
                Content = content
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.PlatformReviews);
    }

    [Fact]
    public async Task ExcessiveContent_IsRejected()
    {
        await using var db = CreateDb();
        var user = AddUser(db);

        var result = await Controller(db, user).Create(
            new CreatePlatformReviewRequest
            {
                Rating = 4,
                Content = new string('a', 2001)
            },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(db.PlatformReviews);
    }

    [Fact]
    public async Task MissingIdentity_IsRejected()
    {
        await using var db = CreateDb();

        var result = await Controller(db).Create(
            new CreatePlatformReviewRequest
            {
                Rating = 4,
                Content = "Good platform"
            },
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
        Assert.Empty(db.PlatformReviews);
    }

    [Theory]
    [InlineData(UserRole.Admin, true, false)]
    [InlineData(UserRole.Visitor, false, false)]
    [InlineData(UserRole.Provider, true, true)]
    public async Task IneligibleAccount_IsRejected(
        UserRole role,
        bool active,
        bool requiresActivation)
    {
        await using var db = CreateDb();
        var user = AddUser(db, role);

        user.IsActive = active;
        user.RequiresPasswordChange = requiresActivation;
        await db.SaveChangesAsync();

        var result = await Controller(db, user).Create(
            new CreatePlatformReviewRequest
            {
                Rating = 4,
                Content = "Good platform"
            },
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        Assert.Empty(db.PlatformReviews);
    }

    [Fact]
    public async Task Retrieval_CombinesRoles_OrdersNewest_AndFiltersExactly()
    {
        await using var db = CreateDb();
        var visitor = AddUser(db);
        var provider = AddUser(db, UserRole.Provider);
        var start = DateTime.UtcNow.AddDays(-1);

        for (var index = 0; index < 5; index++)
        {
            db.PlatformReviews.Add(new PlatformReview
            {
                Id = Guid.NewGuid(),
                UserId = index % 2 == 0 ? visitor.Id : provider.Id,
                Rating = index % 2 == 0 ? 5 : 3,
                Content = $"Review {index}",
                CreatedAtUtc = start.AddMinutes(index)
            });
        }

        await db.SaveChangesAsync();
        var controller = Controller(db);

        var latestResult = await controller.Get(pageSize: 3);
        var latest = Assert.IsType<PlatformReviewPageResponse>(
            Assert.IsType<OkObjectResult>(latestResult.Result).Value);

        Assert.Equal(5, latest.TotalCount);
        Assert.Equal(
            new[] { "Review 4", "Review 3", "Review 2" },
            latest.Items.Select(review => review.Content).ToArray());

        Assert.Contains(latest.Items, review =>
            review.ReviewerName == "Test Visitor");
        Assert.Contains(latest.Items, review =>
            review.ReviewerName == "Test Provider");

        var filteredResult = await controller.Get(rating: 3, pageSize: 1);
        var filtered = Assert.IsType<PlatformReviewPageResponse>(
            Assert.IsType<OkObjectResult>(filteredResult.Result).Value);

        Assert.Equal(2, filtered.TotalCount);
        Assert.Single(filtered.Items);
        Assert.Equal("Review 3", filtered.Items[0].Content);
        Assert.All(filtered.Items, review => Assert.Equal(3, review.Rating));

        var nextResult = await controller.Get(rating: 3, page: 2, pageSize: 1);
        var next = Assert.IsType<PlatformReviewPageResponse>(
            Assert.IsType<OkObjectResult>(nextResult.Result).Value);

        Assert.Equal("Review 1", next.Items[0].Content);

        var emptyResult = await controller.Get(rating: 1);
        var empty = Assert.IsType<PlatformReviewPageResponse>(
            Assert.IsType<OkObjectResult>(emptyResult.Result).Value);

        Assert.Empty(empty.Items);
        Assert.Equal(0, empty.TotalCount);
    }

    [Theory]
    [InlineData(0, 1, 20)]
    [InlineData(6, 1, 20)]
    [InlineData(5, 0, 20)]
    [InlineData(5, 1, 0)]
    [InlineData(5, 1, 101)]
    public async Task InvalidRetrievalParameters_AreRejected(
        int rating,
        int page,
        int pageSize)
    {
        await using var db = CreateDb();

        var result = await Controller(db).Get(rating, page, pageSize);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public void Submission_DeclaresVisitorAndProviderAuthorization()
    {
        var method = typeof(PlatformReviewsController)
            .GetMethod(nameof(PlatformReviewsController.Create));

        var attribute = (AuthorizeAttribute)Attribute.GetCustomAttribute(
            method,
            typeof(AuthorizeAttribute));

        Assert.NotNull(attribute);
        Assert.Equal("Visitor,Provider", attribute.Roles);
    }
}