using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Shared.Storage;

namespace ProviderCatalogService.Controllers;

[ApiController]
[Route("api/catalog/images")]
[Authorize(Roles = "Provider,Admin")]
public class ImagesController : ControllerBase
{
    private readonly IBlobStorageService _blobStorage;
    private readonly string _container;

    public ImagesController(IBlobStorageService blobStorage, IConfiguration configuration)
    {
        _blobStorage = blobStorage;
        _container = configuration["AzureStorage:ListingImagesContainer"] ?? "provider-service-images";
    }

    /// <summary>
    /// Uploads a service/listing image to Azure Blob Storage
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No image file was provided." });

        const long maxSizeBytes = 5 * 1024 * 1024; // 5 MB
        if (file.Length > maxSizeBytes)
            return BadRequest(new { message = "Image file size must be 5 MB or smaller." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { message = "Invalid image format. Only JPG, PNG, and WebP are allowed." });

        var blobName = $"service_{Guid.NewGuid():N}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{ext}";
        var contentType = (file.Headers != null && !string.IsNullOrWhiteSpace(file.ContentType))
            ? file.ContentType
            : (ext == ".png" ? "image/png" : ext == ".webp" ? "image/webp" : "image/jpeg");

        await using var stream = file.OpenReadStream();
        var blobUrl = await _blobStorage.UploadAsync(stream, blobName, _container, contentType);

        var proxyUrl = $"/api/catalog/images/{blobName}";
        return Ok(new { imageUrl = proxyUrl, blobUrl = blobUrl });
    }

    /// <summary>
    /// Streams a service/listing image from Azure Blob Storage
    /// </summary>
    [HttpGet("{blobName}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetImage(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            return BadRequest(new { message = "Image name is required." });

        var stream = await _blobStorage.OpenReadAsync(blobName, _container);
        if (stream == null)
            return NotFound(new { message = "Image not found." });

        var ext = Path.GetExtension(blobName).ToLowerInvariant();
        var contentType = ext == ".png" ? "image/png" : ext == ".webp" ? "image/webp" : "image/jpeg";
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(stream, contentType);
    }
}
