#nullable enable
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using ProviderCatalogService.Controllers;
using Shared.Storage;
using Xunit;

namespace ProviderCatalogService.Tests;

public class ImagesControllerTests
{
    private static ImagesController CreateController(IBlobStorageService storage) => new(storage,
        new ConfigurationBuilder().Build())
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };

    [Theory]
    [InlineData("image.png", 0)]
    [InlineData("image.png", 5242881)]
    [InlineData("image.gif", 1)]
    [InlineData("image.pdf", 1)]
    [InlineData("image", 1)]
    public async Task UploadImage_InvalidFile_NeverCallsStorage(string filename, long length)
    {
        using var stream = new MemoryStream(new byte[1]);
        var file = new FormFile(stream, 0, length, "file", filename);
        var storage = new Mock<IBlobStorageService>(MockBehavior.Strict);
        Assert.IsType<BadRequestObjectResult>(await CreateController(storage.Object).UploadImage(file));
        storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadImage_MissingFile_ReturnsBadRequestWithoutStorage()
    {
        var storage = new Mock<IBlobStorageService>(MockBehavior.Strict);
        Assert.IsType<BadRequestObjectResult>(await CreateController(storage.Object).UploadImage(null!));
        storage.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("photo.JPG", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("photo.png", "image/png")]
    [InlineData("photo.webp", "image/webp")]
    public async Task UploadImage_AtSizeLimit_ReturnsStorageAndProxyUrls(string filename, string mime)
    {
        using var stream = new MemoryStream(new byte[5 * 1024 * 1024]);
        var file = new FormFile(stream, 0, stream.Length, "file", filename);
        var storage = new Mock<IBlobStorageService>(MockBehavior.Strict);
        string? blobName = null;
        storage.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "provider-service-images", mime))
            .Callback<Stream, string, string, string>((content, name, container, type) =>
            {
                Assert.Equal(stream.Length, content.Length); blobName = name;
            }).ReturnsAsync("https://storage.invalid/photo");
        var result = Assert.IsType<OkObjectResult>(await CreateController(storage.Object).UploadImage(file));
        var json = JsonSerializer.SerializeToElement(result.Value);
        Assert.Equal("https://storage.invalid/photo", json.GetProperty("blobUrl").GetString());
        Assert.Equal($"/api/catalog/images/{blobName}", json.GetProperty("imageUrl").GetString());
        Assert.StartsWith("service_", blobName); Assert.EndsWith(Path.GetExtension(filename).ToLowerInvariant(), blobName);
        storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), blobName!, "provider-service-images", mime), Times.Once);
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadImage_StorageThrows_PropagatesException()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var storage = new Mock<IBlobStorageService>(MockBehavior.Strict);
        var failure = new IOException("Simulated storage failure");
        storage.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "provider-service-images", "image/png"))
            .ThrowsAsync(failure);
        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => CreateController(storage.Object)
            .UploadImage(new FormFile(stream, 0, stream.Length, "file", "photo.png"))));
        storage.Verify(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), "provider-service-images", "image/png"), Times.Once);
        storage.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("photo.png", "image/png")]
    [InlineData("photo.webp", "image/webp")]
    [InlineData("photo.JPG", "image/jpeg")]
    public async Task GetImage_Found_ReturnsSameStreamWithMimeAndCacheHeader(string name, string mime)
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var storage = new Mock<IBlobStorageService>(MockBehavior.Strict);
        storage.Setup(s => s.OpenReadAsync(name, "provider-service-images")).ReturnsAsync(stream);
        var controller = CreateController(storage.Object);
        var result = Assert.IsType<FileStreamResult>(await controller.GetImage(name));
        Assert.Same(stream, result.FileStream); Assert.Equal(mime, result.ContentType);
        Assert.Equal("public, max-age=31536000, immutable", controller.Response.Headers.CacheControl.ToString());
        storage.Verify(s => s.OpenReadAsync(name, "provider-service-images"), Times.Once);
        storage.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetImage_BlankName_NeverReadsStorage(string? name)
    {
        var storage = new Mock<IBlobStorageService>(MockBehavior.Strict);
        Assert.IsType<BadRequestObjectResult>(await CreateController(storage.Object).GetImage(name!));
        storage.Verify(s => s.OpenReadAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetImage_MissingBlob_ReturnsNotFound()
    {
        var storage = new Mock<IBlobStorageService>(MockBehavior.Strict);
        storage.Setup(s => s.OpenReadAsync("missing.png", "provider-service-images")).ReturnsAsync((Stream?)null);
        Assert.IsType<NotFoundObjectResult>(await CreateController(storage.Object).GetImage("missing.png"));
        storage.Verify(s => s.OpenReadAsync("missing.png", "provider-service-images"), Times.Once);
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetImage_StorageThrows_PropagatesException()
    {
        var storage = new Mock<IBlobStorageService>(MockBehavior.Strict);
        var failure = new IOException("Simulated read failure");
        storage.Setup(s => s.OpenReadAsync("photo.png", "provider-service-images")).ThrowsAsync(failure);
        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => CreateController(storage.Object).GetImage("photo.png")));
        storage.Verify(s => s.OpenReadAsync("photo.png", "provider-service-images"), Times.Once);
        storage.VerifyNoOtherCalls();
    }
}
