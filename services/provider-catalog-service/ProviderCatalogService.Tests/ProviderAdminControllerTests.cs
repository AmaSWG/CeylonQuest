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
using Moq;

namespace ProviderCatalogService.Tests;

public class ProviderAdminControllerTests
{
    private static string? GetMessage(IActionResult result)
    {
        var objResult = Assert.IsAssignableFrom<ObjectResult>(result);
        return JsonSerializer.SerializeToElement(objResult.Value).GetProperty("message").GetString();
    }

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
        IKafkaProducer kafka,
        IEmailService email,
        IBlobStorageService blob)
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

    private static async Task<ProviderApplication> SeedApplication(CatalogDbContext db, ProviderStatus status = ProviderStatus.Pending)
    {
        var app = new ProviderApplication { Id = Guid.NewGuid(), Email = "tours@example.com",
            BusinessName = "Island Tours", Status = status, Description = "Lagoon tours", Location = "Galle",
            PhoneNumber = "0771234567", ServiceType = "Activity" };
        db.ProviderApplications.Add(app);
        await db.SaveChangesAsync();
        return app;
    }

    [Theory]
    [InlineData(true, ProviderStatus.Approved)]
    [InlineData(false, ProviderStatus.Approved)]
    [InlineData(true, ProviderStatus.Rejected)]
    [InlineData(false, ProviderStatus.Rejected)]
    [InlineData(true, (ProviderStatus)99)]
    [InlineData(false, (ProviderStatus)99)]
    public async Task Review_NonPendingStatus_RejectsWithoutSideEffects(bool approve, ProviderStatus status)
    {
        using var db = CreateDb();
        var app = await SeedApplication(db, status);
        var kafka = new Mock<IKafkaProducer>(MockBehavior.Strict);
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        var blob = new Mock<IBlobStorageService>(MockBehavior.Strict);
        var controller = CreateController(db, kafka.Object, email.Object, blob.Object);
        var result = approve ? await controller.Approve(app.Id) : await controller.Reject(app.Id,
            new RejectProviderApplicationRequest { RejectionReason = "Missing license" });
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var expectedMsg = approve ? "Only pending applications can be approved." : "Only pending applications can be rejected.";
        Assert.Equal(expectedMsg, GetMessage(badRequest));
        db.ChangeTracker.Clear();
        var saved = await db.ProviderApplications.SingleAsync();
        Assert.Equal(status, saved.Status);
        Assert.Null(saved.ReviewedAt);
        Assert.Null(saved.RejectionReason);
        Assert.Empty(db.Providers);
        kafka.VerifyNoOtherCalls(); email.VerifyNoOtherCalls(); blob.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Review_MissingApplication_ReturnsNotFoundWithoutSideEffects(bool approve)
    {
        using var db = CreateDb();
        var kafka = new Mock<IKafkaProducer>(MockBehavior.Strict);
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        var blob = new Mock<IBlobStorageService>(MockBehavior.Strict);
        var controller = CreateController(db, kafka.Object, email.Object, blob.Object);
        Assert.IsType<NotFoundResult>(approve ? await controller.Approve(Guid.NewGuid()) :
            await controller.Reject(Guid.NewGuid(), new RejectProviderApplicationRequest { RejectionReason = "Invalid" }));
        Assert.Empty(db.ProviderApplications); Assert.Empty(db.Providers);
        kafka.VerifyNoOtherCalls(); email.VerifyNoOtherCalls(); blob.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    public async Task Reject_BlankReason_LeavesApplicationPendingAndNeverSendsEmail(string? reason)
    {
        using var db = CreateDb();
        var app = await SeedApplication(db);
        var kafka = new Mock<IKafkaProducer>(MockBehavior.Strict);
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        var blob = new Mock<IBlobStorageService>(MockBehavior.Strict);
        var controller = CreateController(db, kafka.Object, email.Object, blob.Object);
        var result = await controller.Reject(app.Id, new RejectProviderApplicationRequest { RejectionReason = reason! });
Assert.Equal("A rejection reason is required.", GetMessage(result)); 
        Assert.IsType<BadRequestObjectResult>(await controller.Reject(app.Id,
            new RejectProviderApplicationRequest { RejectionReason = reason! }));
        db.ChangeTracker.Clear();
        var saved = await db.ProviderApplications.SingleAsync();
        Assert.Equal(ProviderStatus.Pending, saved.Status);
        Assert.Null(saved.ReviewedAt); Assert.Null(saved.RejectionReason); Assert.Empty(db.Providers);
        email.Verify(e => e.SendApplicationRejectionEmailAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        kafka.VerifyNoOtherCalls(); email.VerifyNoOtherCalls(); blob.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reject_ReasonIsTrimmed_PersistsBeforeEmailEvenWhenEmailFails(bool emailFails)
    {
        using var db = CreateDb();
        var app = await SeedApplication(db);
        var kafka = new Mock<IKafkaProducer>(MockBehavior.Strict);
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        var blob = new Mock<IBlobStorageService>(MockBehavior.Strict);
        var failure = new IOException("Simulated email outage");
        var setup = email.Setup(e => e.SendApplicationRejectionEmailAsync(app.Email, app.BusinessName,
            "Missing license", CancellationToken.None));
        if (emailFails) setup.ThrowsAsync(failure); else setup.Returns(Task.CompletedTask);
        var controller = CreateController(db, kafka.Object, email.Object, blob.Object);
        var before = DateTime.UtcNow;
        if (emailFails) Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => controller.Reject(app.Id,
            new RejectProviderApplicationRequest { RejectionReason = "  Missing license  " })));
        else Assert.IsType<OkObjectResult>(await controller.Reject(app.Id,
            new RejectProviderApplicationRequest { RejectionReason = "  Missing license  " }));
        db.ChangeTracker.Clear();
        var saved = await db.ProviderApplications.SingleAsync();
        Assert.Equal(ProviderStatus.Rejected, saved.Status);
        Assert.Equal("Missing license", saved.RejectionReason);
        Assert.InRange(saved.ReviewedAt!.Value, before, DateTime.UtcNow);
        Assert.Empty(db.Providers);
        email.Verify(e => e.SendApplicationRejectionEmailAsync(app.Email, app.BusinessName,
            "Missing license", CancellationToken.None), Times.Once);
        kafka.VerifyNoOtherCalls(); email.VerifyNoOtherCalls(); blob.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Approve_PersistsProfileAndEventPayload_AndRetryDoesNotRepublish(bool kafkaFails)
    {
        using var db = CreateDb();
        var app = await SeedApplication(db);
        var kafka = new Mock<IKafkaProducer>(MockBehavior.Strict);
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        var blob = new Mock<IBlobStorageService>(MockBehavior.Strict);
        object? payload = null;
        var failure = new IOException("Simulated Kafka outage");
        var publish = kafka.Setup(k => k.PublishAsync("provider.approved", app.Id.ToString(), It.IsAny<It.IsAnyType>(), CancellationToken.None))
            .Callback(new InvocationAction(i => payload = i.Arguments[2]));
        if (kafkaFails) publish.ThrowsAsync(failure); else publish.Returns(Task.CompletedTask);
        var controller = CreateController(db, kafka.Object, email.Object, blob.Object);
        if (kafkaFails) Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => controller.Approve(app.Id)));
        else Assert.IsType<OkObjectResult>(await controller.Approve(app.Id));
        db.ChangeTracker.Clear();
        var saved = await db.ProviderApplications.SingleAsync();
        var provider = await db.Providers.SingleAsync();
        Assert.Equal(ProviderStatus.Approved, saved.Status);
        Assert.Equal(app.Email, provider.Email); Assert.Equal(app.BusinessName, provider.BusinessName);
        Assert.Equal(app.Description, provider.Description); Assert.Equal(app.Location, provider.Location);
        Assert.Equal(app.PhoneNumber, provider.PhoneNumber); Assert.Equal(app.ServiceType, provider.ServiceType);
        Assert.Null(provider.IdentityUserId);
        var json = JsonSerializer.SerializeToElement(payload);
        Assert.Equal(app.Id, json.GetProperty("ApplicationId").GetGuid());
        Assert.Equal(app.Email, json.GetProperty("Email").GetString());
        Assert.Equal(app.BusinessName, json.GetProperty("BusinessName").GetString());
        Assert.Equal(saved.ReviewedAt, json.GetProperty("ApprovedAt").GetDateTime());
        Assert.IsType<BadRequestObjectResult>(await controller.Approve(app.Id));
        Assert.Single(db.Providers);
        kafka.Verify(k => k.PublishAsync("provider.approved", app.Id.ToString(), It.IsAny<It.IsAnyType>(), CancellationToken.None), Times.Once);
        kafka.VerifyNoOtherCalls(); email.VerifyNoOtherCalls(); blob.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Approve_ExistingProvider_ReturnsConflictWithoutPublishingOrChangingApplication()
    {
        using var db = CreateDb();
        var app = await SeedApplication(db);
        db.Providers.Add(new Provider { Id = Guid.NewGuid(), Email = app.Email });
        await db.SaveChangesAsync();
        var kafka = new Mock<IKafkaProducer>(MockBehavior.Strict);
        var email = new Mock<IEmailService>(MockBehavior.Strict);
        var blob = new Mock<IBlobStorageService>(MockBehavior.Strict);
        var result = await CreateController(db, kafka.Object, email.Object, blob.Object).Approve(app.Id);
Assert.Equal("A provider with this email already exists.", GetMessage(result));
        Assert.IsType<ConflictObjectResult>(await CreateController(db, kafka.Object, email.Object, blob.Object).Approve(app.Id));
        db.ChangeTracker.Clear();
        Assert.Equal(ProviderStatus.Pending, (await db.ProviderApplications.SingleAsync()).Status);
        Assert.Null((await db.ProviderApplications.SingleAsync()).ReviewedAt);
        Assert.Single(db.Providers);
        kafka.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<It.IsAnyType>(),
            It.IsAny<CancellationToken>()), Times.Never);
        kafka.VerifyNoOtherCalls(); email.VerifyNoOtherCalls(); blob.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Verifies that all provider applications are returned and that
    /// the status filter narrows the results to the requested status
    /// </summary>
    [Fact]
    public async Task GetApplications_FiltersStrictlyByStatus_AndOrdersByDescendingSubmission()
    {
        using var db = CreateDb();
        var controller = CreateController(db, new FakeKafkaProducer(), new FakeEmailService(), new FakeBlobStorage());

        var olderDate = DateTime.UtcNow.AddDays(-2);
        var newerDate = DateTime.UtcNow;

        db.ProviderApplications.AddRange(
            new ProviderApplication { Id = Guid.NewGuid(), BusinessName = "Older Pending", Email = "old@test.com", PhoneNumber = "123", ServiceType = "Hotel", Location = "Galle", Description = "Desc", Status = ProviderStatus.Pending, SubmittedAt = olderDate },
            new ProviderApplication { Id = Guid.NewGuid(), BusinessName = "Newer Pending", Email = "new@test.com", PhoneNumber = "456", ServiceType = "Hotel", Location = "Galle", Description = "Desc", Status = ProviderStatus.Pending, SubmittedAt = newerDate },
            new ProviderApplication { Id = Guid.NewGuid(), BusinessName = "Approved One", Email = "app@test.com", PhoneNumber = "789", ServiceType = "Hotel", Location = "Galle", Description = "Desc", Status = ProviderStatus.Approved, SubmittedAt = newerDate }
        );
        await db.SaveChangesAsync();

        // Verify status filter excludes Approved items (kills == to != mutation)
        var pendingResult = Assert.IsType<OkObjectResult>(await controller.GetApplications(ProviderStatus.Pending));
        var pendingList = Assert.IsAssignableFrom<System.Collections.IEnumerable>(pendingResult.Value).Cast<object>().ToList();
        Assert.Equal(2, pendingList.Count);

        // Verify newest first (kills OrderByDescending to OrderBy mutation)
        var firstItem = JsonSerializer.SerializeToElement(pendingList[0]);
        Assert.Equal("Newer Pending", firstItem.GetProperty("BusinessName").GetString());
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

    [Fact]
    public async Task DownloadDocument_WithCustomContainerConfiguration_UsesConfiguredContainer()
    {
        using var db = CreateDb();
        var blob = new FakeBlobStorage();
        blob.Files["custom-verification-files/test.pdf"] = Encoding.UTF8.GetBytes("pdf content");

        var inMemorySettings = new Dictionary<string, string?> {
            {"AzureStorage:VerificationFilesContainer", "custom-verification-files"}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var app = new ProviderApplication {
            Id = Guid.NewGuid(), Email = "test@doc.com", BusinessName = "Test Co",
            LegalDocumentsJson = JsonSerializer.Serialize(new[] { new { BlobName = "test.pdf", OriginalFileName = "test.pdf" } })
        };
        db.ProviderApplications.Add(app);
        await db.SaveChangesAsync();

        var controller = new ProviderAdminController(db, new FakeKafkaProducer(), new FakeEmailService(), blob, config) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.DownloadDocument(app.Id, 0) as FileStreamResult;
        Assert.NotNull(result);
        Assert.True(blob.Files.ContainsKey("custom-verification-files/test.pdf"));
    }

    [Fact]
    public async Task DownloadDocument_MissingApplication_ReturnsNotFoundWithMessage()
    {
        using var db = CreateDb();
        var controller = CreateController(db, new FakeKafkaProducer(), new FakeEmailService(), new FakeBlobStorage());
        var result = await controller.DownloadDocument(Guid.NewGuid());
        Assert.Equal("Application not found.", GetMessage(result));
    }

    [Fact]
    public async Task DownloadDocument_NoDocumentsConfigured_ReturnsNotFoundWithMessage()
    {
        using var db = CreateDb();
        var app = await SeedApplication(db);
        app.LegalDocumentsJson = "[]";
        app.LegalDocumentPath = null;
        await db.SaveChangesAsync();

        var controller = CreateController(db, new FakeKafkaProducer(), new FakeEmailService(), new FakeBlobStorage());
        var result = await controller.DownloadDocument(app.Id);
        Assert.Equal("No verification document was found for this application.", GetMessage(result));
    }

    [Fact]
    public async Task DownloadDocument_BlobMissingInStorage_ReturnsNotFoundWithMessage()
    {
        using var db = CreateDb();
        var app = await SeedApplication(db);
        app.LegalDocumentsJson = JsonSerializer.Serialize(new[] { new { BlobName = "nonexistent.pdf" } });
        await db.SaveChangesAsync();

        var controller = CreateController(db, new FakeKafkaProducer(), new FakeEmailService(), new FakeBlobStorage());
        var result = await controller.DownloadDocument(app.Id);
        Assert.Equal("Document file was not found in Azure Storage.", GetMessage(result));
    }

    [Theory]
    [InlineData("license.png", "image/png")]
    [InlineData("license.jpg", "image/jpeg")]
    [InlineData("license.jpeg", "image/jpeg")]
    [InlineData("license.docx", "application/octet-stream")]
    public async Task DownloadDocument_MimeTypes_ReturnsCorrectContentType(string filename, string expectedContentType)
    {
        using var db = CreateDb();
        var blob = new FakeBlobStorage();
        blob.Files["provider-verification-files/" + filename] = Encoding.UTF8.GetBytes("file content");

        var app = new ProviderApplication {
            Id = Guid.NewGuid(), Email = "mime@test.com", BusinessName = "Mime Co",
            LegalDocumentFileName = filename,
            LegalDocumentsJson = JsonSerializer.Serialize(new[] { new { BlobName = filename, OriginalFileName = filename } })
        };
        db.ProviderApplications.Add(app);
        await db.SaveChangesAsync();

        var controller = CreateController(db, new FakeKafkaProducer(), new FakeEmailService(), blob);
        var result = Assert.IsType<FileStreamResult>(await controller.DownloadDocument(app.Id, 0));
        Assert.Equal(expectedContentType, result.ContentType);
        Assert.Equal(filename, result.FileDownloadName);
    }

    [Fact]
    public async Task DownloadDocument_WhenNoFileNameProvided_FallsBackToDefaultPdfName()
    {
        using var db = CreateDb();
        var blob = new FakeBlobStorage();
        blob.Files["provider-verification-files/blob.bin"] = Encoding.UTF8.GetBytes("content");

        var app = new ProviderApplication {
            Id = Guid.NewGuid(), Email = "fallback@test.com", BusinessName = "Fallback Co",
            LegalDocumentFileName = null, // tests fallback to "Verification_Document.pdf"
            LegalDocumentsJson = JsonSerializer.Serialize(new[] { new { BlobName = "blob.bin", OriginalFileName = (string?)null } })
        };
        db.ProviderApplications.Add(app);
        await db.SaveChangesAsync();

        var controller = CreateController(db, new FakeKafkaProducer(), new FakeEmailService(), blob);
        var result = Assert.IsType<FileStreamResult>(await controller.DownloadDocument(app.Id, 0));
        Assert.Equal("Verification_Document.pdf", result.FileDownloadName);
        Assert.Equal("application/pdf", result.ContentType);
    }

    [Fact]
    public async Task DownloadDocument_LegacyFallback_WhenJsonIsEmpty_UsesLegacyPath()
    {
        using var db = CreateDb();
        var blob = new FakeBlobStorage();
        blob.Files["provider-verification-files/legacy.pdf"] = Encoding.UTF8.GetBytes("legacy pdf");

        var app = new ProviderApplication {
            Id = Guid.NewGuid(), Email = "legacy@test.com", BusinessName = "Legacy Co",
            LegalDocumentsJson = "[]",
            LegalDocumentPath = "uploads/legacy.pdf"
        };
        db.ProviderApplications.Add(app);
        await db.SaveChangesAsync();

        var controller = CreateController(db, new FakeKafkaProducer(), new FakeEmailService(), blob);
        var result = Assert.IsType<FileStreamResult>(await controller.DownloadDocument(app.Id, 0));
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetApplicationDocuments_ReturnsExactSasUrl_AndHandlesLegacyPath()
    {
        using var db = CreateDb();
        var blob = new FakeBlobStorage();
        var controller = CreateController(db, new FakeKafkaProducer(), new FakeEmailService(), blob);

        var modernAppId = Guid.NewGuid();
        var legacyAppId = Guid.NewGuid();

        db.ProviderApplications.AddRange(
            new ProviderApplication {
                Id = modernAppId, Email = "modern@test.com", BusinessName = "Modern Co",
                LegalDocumentsJson = JsonSerializer.Serialize(new[] {
                    new { BlobName = "cert.pdf", OriginalFileName = "Original_Cert.pdf" }
                })
            },
            new ProviderApplication {
                Id = legacyAppId, Email = "legacy@test.com", BusinessName = "Legacy Co",
                LegalDocumentsJson = "[]", // <-- Changed from null to "[]" to satisfy EF Core!
                LegalDocumentPath = "uploads/legacy_deed.pdf",
                LegalDocumentFileName = "My_Deed.pdf"
            }
        );
        await db.SaveChangesAsync();

        // 1. Missing application -> 404
        var notFoundResult = await controller.GetApplicationDocuments(Guid.NewGuid());
        Assert.Equal("Application not found.", GetMessage(notFoundResult));

        // 2. Modern document -> Kills Mutant 291:32 by checking the exact blob in the URL!
        var modernOk = Assert.IsType<OkObjectResult>(await controller.GetApplicationDocuments(modernAppId));
        var modernJson = JsonSerializer.SerializeToElement(modernOk.Value);
        Assert.Equal("Original_Cert.pdf", modernJson[0].GetProperty("fileName").GetString());
        Assert.Equal("https://fake.blob/provider-verification-files/cert.pdf?sas=token", modernJson[0].GetProperty("url").GetString());

        // 3. Legacy path -> Kills NoCoverage on lines 299-303
        var legacyOk = Assert.IsType<OkObjectResult>(await controller.GetApplicationDocuments(legacyAppId));
        var legacyJson = JsonSerializer.SerializeToElement(legacyOk.Value);
        Assert.Equal("My_Deed.pdf", legacyJson[0].GetProperty("fileName").GetString());
        Assert.Equal("https://fake.blob/provider-verification-files/legacy_deed.pdf?sas=token", legacyJson[0].GetProperty("url").GetString());
    }

    [Fact]
    public async Task DownloadDocument_ModernMetadataTakesPrecedenceOverLegacyPath()
    {
        using var db = CreateDb();
        var blob = new FakeBlobStorage();
        blob.Files["provider-verification-files/modern.pdf"] = Encoding.UTF8.GetBytes("modern content");
        // Notice: legacy.pdf does NOT exist in storage!

        var app = new ProviderApplication {
            Id = Guid.NewGuid(), Email = "precedence@test.com", BusinessName = "Precedence Co",
            LegalDocumentsJson = JsonSerializer.Serialize(new[] { new { BlobName = "modern.pdf", OriginalFileName = "modern.pdf" } }),
            LegalDocumentPath = "uploads/legacy.pdf" // Both are present!
        };
        db.ProviderApplications.Add(app);
        await db.SaveChangesAsync();

        var controller = CreateController(db, new FakeKafkaProducer(), new FakeEmailService(), blob);
        var result = Assert.IsType<FileStreamResult>(await controller.DownloadDocument(app.Id, 0));

        // If mutated to ||, blobName is overwritten with legacy.pdf, which fails because it's not in blob storage!
        Assert.Equal("modern.pdf", result.FileDownloadName);
    }

    [Fact]
    public async Task DownloadDocument_UsesLegalDocumentFileName_WhenOriginalFileNameMissing()
    {
        using var db = CreateDb();
        var blob = new FakeBlobStorage();
        blob.Files["provider-verification-files/blob123.bin"] = Encoding.UTF8.GetBytes("doc content");

        var app = new ProviderApplication {
            Id = Guid.NewGuid(), Email = "customname@test.com", BusinessName = "Custom Co",
            LegalDocumentFileName = "SpecialContract.pdf", // Custom name!
            LegalDocumentsJson = JsonSerializer.Serialize(new[] {
                new { BlobName = "blob123.bin" } // No OriginalFileName!
            })
        };
        db.ProviderApplications.Add(app);
        await db.SaveChangesAsync();

        var controller = CreateController(db, new FakeKafkaProducer(), new FakeEmailService(), blob);
        var result = Assert.IsType<FileStreamResult>(await controller.DownloadDocument(app.Id, 0));

        // If Stryker replaces with "Verification_Document.pdf", this fails and kills the mutant!
        Assert.Equal("SpecialContract.pdf", result.FileDownloadName);
    }

    
    [Fact]
    public async Task Controller_WithoutConfigurationKey_FallsBackToDefaultVerificationContainer()
    {
        using var db = CreateDb();
        var blob = new FakeBlobStorage();
        blob.Files["provider-verification-files/default.pdf"] = Encoding.UTF8.GetBytes("default content");

        var emptyConfig = new ConfigurationBuilder().Build(); // No keys!

        var app = new ProviderApplication {
            Id = Guid.NewGuid(), Email = "default@test.com", BusinessName = "Default Co",
            LegalDocumentsJson = JsonSerializer.Serialize(new[] { new { BlobName = "default.pdf", OriginalFileName = "default.pdf" } })
        };
        db.ProviderApplications.Add(app);
        await db.SaveChangesAsync();

        var controller = new ProviderAdminController(db, new FakeKafkaProducer(), new FakeEmailService(), blob, emptyConfig) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = Assert.IsType<FileStreamResult>(await controller.DownloadDocument(app.Id, 0));
        Assert.NotNull(result);
    }
}