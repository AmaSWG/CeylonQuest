using System;
using System.Threading.Tasks;
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdentityService.Tests;

public class ProviderApplicationServiceTests
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesApplicationSuccessfully()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var service = new ProviderApplicationService(db);

        var request = new ProviderApplicationRequest
        {
            FirstName = "Kasun",
            LastName = "Perera",
            Email = "kasun.tours@gmail.com",
            PhoneNumber = "+94 77 111 2222",
            BusinessName = "Kasun Ceylon Tours",
            ServiceType = "Tour Guide",
            Location = "Kandy",
            Description = "Experienced guide across central province",
            LegalDocumentFileName = "business_reg.pdf"
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("kasun.tours@gmail.com", result.Email);
        Assert.Equal(ProviderApplicationStatus.Pending, result.Status);

        var saved = await db.ProviderApplications.FirstOrDefaultAsync(p => p.Id == result.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsDuplicateApplicationException()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var service = new ProviderApplicationService(db);

        var request1 = new ProviderApplicationRequest
        {
            FirstName = "First",
            LastName = "Applicant",
            Email = "duplicate.provider@gmail.com",
            PhoneNumber = "+94 77 111 2222",
            BusinessName = "Business One",
            ServiceType = "Driver",
            Location = "Colombo",
            Description = "First application"
        };

        var request2 = new ProviderApplicationRequest
        {
            FirstName = "Second",
            LastName = "Applicant",
            Email = "duplicate.provider@gmail.com",
            PhoneNumber = "+94 77 333 4444",
            BusinessName = "Business Two",
            ServiceType = "Hotel",
            Location = "Galle",
            Description = "Second application with same email"
        };

        // Act
        await service.CreateAsync(request1);

        // Assert
        var ex = await Assert.ThrowsAsync<DuplicateApplicationException>(() => service.CreateAsync(request2));
        Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_IsCaseInsensitiveAndTrimsWhitespace()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var service = new ProviderApplicationService(db);

        var request1 = new ProviderApplicationRequest
        {
            FirstName = "Kasun",
            LastName = "Perera",
            Email = "Kasun.Provider@CeylonQuest.com",
            PhoneNumber = "+94 77 111 2222",
            BusinessName = "Business One",
            ServiceType = "Driver",
            Location = "Colombo"
        };

        var request2 = new ProviderApplicationRequest
        {
            FirstName = "Kasun",
            LastName = "Perera",
            Email = "   kasun.provider@ceylonquest.com   ",
            PhoneNumber = "+94 77 333 4444",
            BusinessName = "Business Two",
            ServiceType = "Driver",
            Location = "Colombo"
        };

        // Act
        await service.CreateAsync(request1);

        // Assert
        await Assert.ThrowsAsync<DuplicateApplicationException>(() => service.CreateAsync(request2));
    }
}
