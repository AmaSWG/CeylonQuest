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
using System.Linq;
using System.Text.Json;
using Moq;

namespace ProviderCatalogService.Tests;

public class ProviderApplicationsControllerTests
{
    private static string? GetMessage(IActionResult result)
    {
        var objResult = Assert.IsAssignableFrom<ObjectResult>(result);
        return JsonSerializer.SerializeToElement(objResult.Value).GetProperty("message").GetString();
    }

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

    private static CreateProviderApplicationRequest ValidRequest(params IFormFile[] files) => new()
    {
        BusinessName = "  Island Tours  ", Email = "  TOURS@EXAMPLE.COM  ", PhoneNumber = " 0771234567 ",
        ServiceType = " Activity ", Location = " Galle ", Description = " Lagoon tours ",
        LegalDocuments = files.ToList()
    };

    private static ProviderApplicationsController CreateController(CatalogDbContext db, IBlobStorageService storage) =>
        new(db, storage, new ConfigurationBuilder().Build());

    [Theory]
    [InlineData("license.exe", 1, "File 'license.exe' has an invalid format. Only PDF, JPG, and PNG are accepted.")]
    [InlineData("license", 1, "File 'license' has an invalid format. Only PDF, JPG, and PNG are accepted.")]
    [InlineData("license.pdf", 10485761, "File 'license.pdf' exceeds the 10MB file size limit.")]
    public async Task Submit_InvalidDocument_DoesNotUploadOrPersist(string filename, long length, string expectedMessage)
    {
        using var db = CreateDb();
        using var stream = new MemoryStream(new byte[1]);
        var file = new FormFile(stream, 0, length, "LegalDocuments", filename);
        var storage = new Mock<IBlobStorageService>(MockBehavior.Strict);
        
        var result = await CreateController(db, storage.Object).Submit(ValidRequest(file));
        
        Assert.Equal(expectedMessage, GetMessage(result)); // <-- Kills String Mutants (Image 1)
        Assert.Empty(db.ProviderApplications);
        storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Submit_SixDocuments_RejectsBeforeAnyUpload()
    {
        using var db = CreateDb();
        var storage = new Mock<IBlobStorageService>(MockBehavior.Strict);
        var files = Enumerable.Range(0, 6).Select(i => CreateMockFile($"doc{i}.pdf")).ToArray();
        try
        {
            var result = await CreateController(db, storage.Object).Submit(ValidRequest(files));
            Assert.Equal("You can upload a maximum of 5 verification documents.", GetMessage(result)); // <-- Kills Mutant (Line 47)
            Assert.Empty(db.ProviderApplications);
            storage.VerifyNoOtherCalls();
        }
        finally { foreach (var file in files) file.OpenReadStream().Dispose(); }
    }

    [Theory]
    [InlineData("license.PDF", "application/pdf")]
    [InlineData("license.jpg", "image/jpeg")]
    [InlineData("license.jpeg", "image/jpeg")]
    [InlineData("license.png", "image/png")]
    public async Task Submit_MaximumSizeDocument_DeduplicatesLegacyFile_AndPersistsMetadata(string filename, string contentType)
    {
        using var db = CreateDb();
        using var stream = new MemoryStream(new byte[10 * 1024 * 1024]);
        var file = new FormFile(stream, 0, stream.Length, "LegalDocuments", filename);
        var request = ValidRequest(file);
        request.LegalDocument = file;
        var storage = new Mock<IBlobStorageService>(MockBehavior.Strict);
        string? uploadedName = null;
        storage.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "provider-verification-files", contentType))
            .Callback<Stream, string, string, string>((content, name, container, type) =>
            {
                Assert.Equal(10 * 1024 * 1024, content.Length);
                uploadedName = name;
            }).ReturnsAsync("https://storage.invalid/document");

        Assert.IsType<CreatedResult>(await CreateController(db, storage.Object).Submit(request));

        db.ChangeTracker.Clear();
        var saved = await db.ProviderApplications.SingleAsync();
        Assert.Equal("Island Tours", saved.BusinessName); Assert.Equal("tours@example.com", saved.Email);
        Assert.Equal("0771234567", saved.PhoneNumber); Assert.Equal("Activity", saved.ServiceType);
        Assert.Equal("Galle", saved.Location); Assert.Equal("Lagoon tours", saved.Description);
        Assert.Equal(ProviderStatus.Pending, saved.Status); Assert.Null(saved.ReviewedAt);
        using var metadata = JsonDocument.Parse(saved.LegalDocumentsJson);
        var doc = Assert.Single(metadata.RootElement.EnumerateArray());
        Assert.Equal(uploadedName, doc.GetProperty("BlobName").GetString());
        Assert.Equal(filename, doc.GetProperty("OriginalFileName").GetString());
        Assert.Equal(filename, saved.LegalDocumentFileName);
        Assert.EndsWith(System.IO.Path.GetExtension(filename).ToLowerInvariant(), uploadedName);
        storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), uploadedName!, "provider-verification-files", contentType), Times.Once);
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Submit_FiveDocuments_AcceptsMaximumCount()
    {
        using var db = CreateDb();
        var storage = new Mock<IBlobStorageService>(MockBehavior.Strict);
        storage.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "provider-verification-files", "application/pdf"))
            .ReturnsAsync("https://storage.invalid/document");
        var files = Enumerable.Range(0, 5).Select(i => CreateMockFile($"doc{i}.pdf")).ToArray();
        Assert.IsType<CreatedResult>(await CreateController(db, storage.Object).Submit(ValidRequest(files)));
        db.ChangeTracker.Clear();
        using var metadata = JsonDocument.Parse((await db.ProviderApplications.SingleAsync()).LegalDocumentsJson);
        Assert.Equal(5, metadata.RootElement.GetArrayLength());
        Assert.Equal(5, metadata.RootElement.EnumerateArray().Select(d => d.GetProperty("BlobName").GetString()).Distinct().Count());
        storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "provider-verification-files", "application/pdf"), Times.Exactly(5));
        storage.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Submit_LaterDocumentFails_DoesNotPersistApplicationOrCleanUpEarlierUpload(bool storageFails)
    {
        using var db = CreateDb();
        var storage = new Mock<IBlobStorageService>(MockBehavior.Strict);
        var failure = new IOException("Simulated storage outage");
        if (storageFails)
            storage.SetupSequence(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "provider-verification-files", "application/pdf"))
                .ReturnsAsync("https://storage.invalid/first").ThrowsAsync(failure);
        else
            storage.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "provider-verification-files", "application/pdf"))
                .ReturnsAsync("https://storage.invalid/first");
        var request = ValidRequest(CreateMockFile("first.pdf"), CreateMockFile(storageFails ? "second.pdf" : "second.exe"));
        var controller = CreateController(db, storage.Object);
        if (storageFails) Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => controller.Submit(request)));
        else Assert.IsType<BadRequestObjectResult>(await controller.Submit(request));
        db.ChangeTracker.Clear();
        Assert.Empty(db.ProviderApplications);
        storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "provider-verification-files", "application/pdf"),
            Times.Exactly(storageFails ? 2 : 1));
        storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        storage.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetStatus_BlankEmail_ReturnsBadRequest(string? email)
    {
        using var db = CreateDb();
        var storage = new Mock<IBlobStorageService>(MockBehavior.Strict);
        var result = await CreateController(db, storage.Object).GetStatus(email!);

        Assert.Equal("Email is required.", GetMessage(result));
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.ProviderApplications); storage.VerifyNoOtherCalls();
    }

    /// <summary>
    /// Verifies that a valid application submission uploads the documents,
    /// persists the record, and returns a 201 Created response
    /// </summary>
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

        Assert.Equal("/api/catalog/provider-applications/status?email=bluelagoon@example.com", result.Location);

        var saved = await db.ProviderApplications.FirstOrDefaultAsync(a => a.Email == "bluelagoon@example.com");
        Assert.NotNull(saved);
        Assert.Equal(2, blob.UploadedBlobs.Count);
    }

    /// <summary>
    /// Verifies that submitting an application without any legal documents
    /// is rejected with a 400 Bad Request response
    /// </summary>
    [Fact]
    public async Task Submit_NoFiles_ReturnsBadRequest()
    {
        using var db = CreateDb();
        var blob = new FakeBlobStorage();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var controller = new ProviderApplicationsController(db, blob, config);
        var request = new CreateProviderApplicationRequest
        {
            BusinessName = "No Docs Hotel", Email = "nodocs@example.com", PhoneNumber = "123",
            ServiceType = "Hotel", Location = "Colombo", Description = "Test desc",
            LegalDocuments = new List<IFormFile>()
        };
        var result = await controller.Submit(request);
        Assert.Equal("At least one legal verification document is required.", GetMessage(result)); // <-- Kills Mutant (Line 45)
    }

    /// <summary>
    /// Verifies that the status endpoint returns the application for a
    /// known email and a not-found response for an unknown email
    /// </summary>
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

        var notFoundResult = await controller.GetStatus("unknown@example.com");
        Assert.Equal("No provider application found.", GetMessage(notFoundResult));
    }

    [Fact]
    public async Task Submit_WithCustomContainerConfiguration_UsesConfiguredContainer()
    {
        using var db = CreateDb();
        var storage = new Mock<IBlobStorageService>(MockBehavior.Strict);
        storage.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "custom-app-container", "application/pdf"))
            .ReturnsAsync("https://storage.invalid/custom/doc.pdf");
        var inMemorySettings = new Dictionary<string, string?> {
            {"AzureStorage:VerificationFilesContainer", "custom-app-container"}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var controller = new ProviderApplicationsController(db, storage.Object, config);
        var file = CreateMockFile("doc.pdf");
        var result = await controller.Submit(ValidRequest(file));
        Assert.IsType<CreatedResult>(result);
        storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "custom-app-container", "application/pdf"), Times.Once);
    }
}