using System;
using System.IO;
using System.Threading.Tasks;

namespace Shared.Storage;

public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a stream to Azure Blob Storage and returns the public/direct URL.
    /// </summary>
    Task<string> UploadAsync(Stream content, string blobName, string containerName, string contentType);

    /// <summary>
    /// Deletes a blob from the specified container.
    /// </summary>
    Task<bool> DeleteAsync(string blobName, string containerName);

    /// <summary>
    /// Opens a read stream for private blobs (e.g., provider verification documents).
    /// </summary>
    Task<Stream?> OpenReadAsync(string blobName, string containerName);

    /// <summary>
    /// Generates a temporary Shared Access Signature (SAS) URL for private documents.
    /// </summary>
    string GenerateSasUri(string blobName, string containerName, TimeSpan expiry);
}