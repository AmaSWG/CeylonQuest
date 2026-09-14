using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shared.Storage;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _logger = logger;
        var connectionString = configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");

        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadAsync(Stream content, string blobName, string containerName, string contentType)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var blobClient = containerClient.GetBlobClient(blobName);

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await blobClient.UploadAsync(content, options);
        _logger.LogInformation("Uploaded blob '{BlobName}' to container '{ContainerName}'.", blobName, containerName);

        return blobClient.Uri.ToString();
    }

    public async Task<bool> DeleteAsync(string blobName, string containerName)
    {
        if (string.IsNullOrWhiteSpace(blobName)) return false;

        var sanitizedBlobName = SanitizeBlobName(blobName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(sanitizedBlobName);

        var response = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        return response.Value;
    }

    public async Task<Stream?> OpenReadAsync(string blobName, string containerName)
    {
        if (string.IsNullOrWhiteSpace(blobName)) return null;

        var sanitizedBlobName = SanitizeBlobName(blobName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(sanitizedBlobName);

        if (!await blobClient.ExistsAsync()) return null;

        return await blobClient.OpenReadAsync();
    }

    public string GenerateSasUri(string blobName, string containerName, TimeSpan expiry)
    {
        var sanitizedBlobName = SanitizeBlobName(blobName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(sanitizedBlobName);

        if (!blobClient.CanGenerateSasUri)
        {
            return blobClient.Uri.ToString();
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = sanitizedBlobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }

    // Helper: Safely handles both full URLs and relative filenames
    private static string SanitizeBlobName(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName)) return string.Empty;

        if (Uri.TryCreate(blobName, UriKind.Absolute, out var uri))
        {
            return Path.GetFileName(uri.LocalPath);
        }

        return Path.GetFileName(blobName);
    }
}