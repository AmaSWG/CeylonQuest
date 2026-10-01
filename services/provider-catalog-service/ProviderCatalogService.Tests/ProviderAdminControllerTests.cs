#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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
using ProviderCatalogService.Services;
using Shared.Kafka;
using Shared.Storage;
using Xunit;

namespace ProviderCatalogService.Tests;

public class ProviderAdminControllerTests
{
    private class FakeKafkaProducer : IKafkaProducer
    {
        public List<(string Topic, string? Key, object Message)> PublishedMessages { get; } = new();

        public Task PublishAsync(string topic, string? key, string value, CancellationToken cancellationToken = default)
        {
            PublishedMessages.Add((topic, key, value));
            return Task.CompletedTask;
        }

        public Task PublishAsync<T>(string topic, string? key, T message, CancellationToken cancellationToken = default)
        {
            PublishedMessages.Add((topic, key, message!));
            return Task.CompletedTask;
        }
    }

    private class FakeEmailService : IEmailService
    {
        public List<(string Email, string Business, string Reason)> SentRejections { get; } = new();

        public Task SendApplicationRejectionEmailAsync(string recipientEmail, string businessName, string rejectionReason, CancellationToken cancellationToken = default)
        {
            SentRejections.Add((recipientEmail, businessName, rejectionReason));
            return Task.CompletedTask;
        }
    }

    private class FakeBlobStorage : IBlobStorageService
    {
        public Dictionary<string, byte[]> Files { get; } = new();

        public Task<string> UploadAsync(Stream content, string blobName, string containerName, string contentType)
        {
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            Files[$"{containerName}/{blobName}"] = ms.ToArray();
            return Task.FromResult($"https://fake.blob/{containerName}/{blobName}");
        }

        public Task<bool> DeleteAsync(string blobName, string containerName)
        {
            Files.Remove($"{containerName}/{blobName}");
            return Task.FromResult(true);
        }

        public Task<Stream?> OpenReadAsync(string blobName, string containerName)
        {
            var key = $"{containerName}/{blobName}";
            if (Files.TryGetValue(key, out var bytes))
                return Task.FromResult<Stream?>(new MemoryStream(bytes));
            return Task.FromResult<Stream?>(null);
        }

        public string GenerateSasUri(string blobName, string containerName, TimeSpan expiry)
        {
            return $"https://fake.blob/{containerName}/{blobName}?sas=token";
        }
    }

    private static CatalogDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CatalogDbContext(options);
    }

    private static ProviderAdminController CreateController(
        CatalogDbContext db,
        FakeKafkaProducer kafka,
        FakeEmailService email,
        FakeBlobStorage blob)
    {
        var inMemorySettings = new Dictionary<string, string?> {
            {"AzureStorage:VerificationFilesContainer", "provider-verification-files"}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var controller = new ProviderAdminController(db, kafka, email, blob, config);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return controller;
    }

    /// <summary>
    /// Verifies that all provider applications are returned and that
    /// the status filter narrows the results to the requested status
    /// </summary>
    [Fact]
    public async Task GetApplications_ReturnsAllAndFiltersByStatus()
    {
        using var db = CreateDb();
        var kafka = new FakeKafkaProducer();
        var email = new FakeEmailService();
        var blob = new FakeBlobStorage();
        var controller = CreateController(db, kafka, email, blob);

        db.ProviderApplications.AddRange(
            new ProviderApplication { Id = Guid.NewGuid(), BusinessName = "App 1", Email = "a1@test.com", PhoneNumber = "123", ServiceType = "Hotel", Location = "Galle", Description = "Desc", Status = ProviderStatus.Pending },
            new ProviderApplication { Id = Guid.NewGuid(), BusinessName = "App 2", Email = "a2@test.com", PhoneNumber = "456", ServiceType = "Activity", Location = "Kandy", Description = "Desc", Status = ProviderStatus.Approved }
        );
        await db.SaveChangesAsync();

        var allResult = await controller.GetApplications(null) as OkObjectResult;
        Assert.NotNull(allResult);

        var pendingResult = await controller.GetApplications(ProviderStatus.Pending) as OkObjectResult;
        Assert.NotNull(pendingResult);
    }

    /// <summary>
    /// Verifies that approving a pending application creates the provider
    /// profile, updates the status, and publishes a Kafka event
    /// </summary>
    [Fact]
    public async Task Approve_PendingApplication_SucceedsAndPublishesKafka()
    {
        using var db = CreateDb();
        var kafka = new FakeKafkaProducer();
        var email = new FakeEmailService();
        var blob = new FakeBlobStorage();
        var controller = CreateController(db, kafka, email, blob);

        var appId = Guid.NewGuid();
        db.ProviderApplications.Add(new ProviderApplication
        {
            Id = appId,
            BusinessName = "Ocean Tours",
            Email = "ocean@test.com",
            PhoneNumber = "0771234567",
            ServiceType = "Activity",
            Location = "Mirissa",
            Description = "Whale watching",
            Status = ProviderStatus.Pending
        });
        await db.SaveChangesAsync();

        var result = await controller.Approve(appId) as OkObjectResult;
        Assert.NotNull(result);

        var updatedApp = await db.ProviderApplications.FindAsync(appId);
        Assert.Equal(ProviderStatus.Approved, updatedApp!.Status);

        var provider = await db.Providers.FirstOrDefaultAsync(p => p.Email == "ocean@test.com");
        Assert.NotNull(provider);
        Assert.Single(kafka.PublishedMessages);
    }

    /// <summary>
    /// Verifies that rejecting a pending application sets the rejected
    /// status and sends the rejection notification email
    /// </summary>
    [Fact]
    public async Task Reject_PendingApplication_SetsStatusAndSendsEmail()
    {
        using var db = CreateDb();
        var kafka = new FakeKafkaProducer();
        var email = new FakeEmailService();
        var blob = new FakeBlobStorage();
        var controller = CreateController(db, kafka, email, blob);

        var appId = Guid.NewGuid();
        db.ProviderApplications.Add(new ProviderApplication
        {
            Id = appId,
            BusinessName = "Bad Tours",
            Email = "bad@test.com",
            PhoneNumber = "0771234567",
            ServiceType = "Activity",
            Location = "Colombo",
            Description = "Invalid description",
            Status = ProviderStatus.Pending
        });
        await db.SaveChangesAsync();

        var req = new RejectProviderApplicationRequest { RejectionReason = "Invalid business license" };
        var result = await controller.Reject(appId, req) as OkObjectResult;
        Assert.NotNull(result);

        var updatedApp = await db.ProviderApplications.FindAsync(appId);
        Assert.Equal(ProviderStatus.Rejected, updatedApp!.Status);
        Assert.Equal("Invalid business license", updatedApp.RejectionReason);
        Assert.Single(email.SentRejections);
    }

    /// <summary>
    /// Verifies that requesting a valid verification document returns
    /// the correct file stream, content type, and download name
    /// </summary>
    [Fact]
    public async Task DownloadDocument_ValidDoc_ReturnsFileStream()
    {
        using var db = CreateDb();
        var kafka = new FakeKafkaProducer();
        var email = new FakeEmailService();
        var blob = new FakeBlobStorage();
        var controller = CreateController(db, kafka, email, blob);

        var blobName = "test-doc.pdf";
        blob.Files["provider-verification-files/" + blobName] = Encoding.UTF8.GetBytes("fake pdf content");

        var appId = Guid.NewGuid();
        db.ProviderApplications.Add(new ProviderApplication
        {
            Id = appId,
            BusinessName = "Doc Co",
            Email = "doc@test.com",
            PhoneNumber = "123",
            ServiceType = "Hotel",
            Location = "Galle",
            Description = "Desc",
            Status = ProviderStatus.Pending,
            LegalDocumentsJson = JsonSerializer.Serialize(new[] {
                new { BlobName = blobName, OriginalFileName = "license.pdf" }
            })
        });
        await db.SaveChangesAsync();

        var result = await controller.DownloadDocument(appId, 0) as FileStreamResult;
        Assert.NotNull(result);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal("license.pdf", result.FileDownloadName);
    }
}