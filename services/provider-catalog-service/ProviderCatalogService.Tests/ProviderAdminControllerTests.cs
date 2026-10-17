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
        Assert.IsType<BadRequestObjectResult>(result);
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