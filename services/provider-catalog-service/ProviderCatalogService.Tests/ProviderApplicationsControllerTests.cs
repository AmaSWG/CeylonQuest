#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProviderCatalogService.Controllers;
using ProviderCatalogService.Data;
using ProviderCatalogService.DTOs;
using ProviderCatalogService.Models;
using Shared.Storage;
using Xunit;

namespace ProviderCatalogService.Tests;

public class ProviderApplicationsControllerTests
{
    private class FakeBlobStorage : IBlobStorageService
    {
        public List<string> UploadedBlobs { get; } = new();

        public Task<string> UploadAsync(Stream content, string blobName, string containerName, string contentType)
        {
            UploadedBlobs.Add(blobName);
            return Task.FromResult($"https://fake.blob/{containerName}/{blobName}");
        }

        public Task<bool> DeleteAsync(string blobName, string containerName) => Task.FromResult(true);

        public Task<Stream?> OpenReadAsync(string blobName, string containerName) => Task.FromResult<Stream?>(null);

        public string GenerateSasUri(string blobName, string containerName, TimeSpan expiry)
            => $"https://fake.blob/{containerName}/{blobName}?sas=token";
    }

    private static CatalogDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CatalogDbContext(options);
    }

    private static IFormFile CreateMockFile(string fileName, string content = "fake-file-bytes")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "LegalDocuments", fileName);
    }

    [Fact]
    public async Task Submit_ValidApplication_CreatesRecordAndReturns201()
    {
        using var db = CreateDb();
        var blob = new FakeBlobStorage();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var controller = new ProviderApplicationsController(db, blob, config);

        var request = new CreateProviderApplicationRequest
        {
            BusinessName = "Blue Lagoon Villa",
            Email = "bluelagoon@example.com",
            PhoneNumber = "+94771234567",
            ServiceType = "Hotel",
            Location = "Bentota",
            Description = "Luxury beachfront boutique hotel",
            LegalDocuments = new List<IFormFile> { CreateMockFile("br_cert.pdf"), CreateMockFile("tax.png") }
        };

        var result = await controller.Submit(request) as CreatedResult;
        Assert.NotNull(result);
        Assert.Equal(201, result.StatusCode);

        var saved = await db.ProviderApplications.FirstOrDefaultAsync(a => a.Email == "bluelagoon@example.com");
        Assert.NotNull(saved);
        Assert.Equal(2, blob.UploadedBlobs.Count);
    }

    [Fact]
    public async Task Submit_NoFiles_ReturnsBadRequest()
    {
        using var db = CreateDb();
        var blob = new FakeBlobStorage();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var controller = new ProviderApplicationsController(db, blob, config);

        var request = new CreateProviderApplicationRequest
        {
            BusinessName = "No Docs Hotel",
            Email = "nodocs@example.com",
            PhoneNumber = "123",
            ServiceType = "Hotel",
            Location = "Colombo",
            Description = "Test desc",
            LegalDocuments = new List<IFormFile>()
        };

        var result = await controller.Submit(request) as BadRequestObjectResult;
        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task GetStatus_ReturnsCorrectStatus()
    {
        using var db = CreateDb();
        var blob = new FakeBlobStorage();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var controller = new ProviderApplicationsController(db, blob, config);

        db.ProviderApplications.Add(new ProviderApplication
        {
            Id = Guid.NewGuid(),
            BusinessName = "Kandy View",
            Email = "kandy@example.com",
            PhoneNumber = "123",
            ServiceType = "Hotel",
            Location = "Kandy",
            Description = "Desc",
            Status = ProviderStatus.Pending
        });
        await db.SaveChangesAsync();

        var okResult = await controller.GetStatus("kandy@example.com") as OkObjectResult;
        Assert.NotNull(okResult);

        var notFoundResult = await controller.GetStatus("unknown@example.com") as NotFoundObjectResult;
        Assert.NotNull(notFoundResult);
    }
}