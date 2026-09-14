namespace ProviderCatalogService.Services;

public class DocumentStorageService
{
    private readonly IWebHostEnvironment _environment;

    public DocumentStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// Validates and saves an uploaded verification document to the local
    /// provider-documents folder, returning its stored path and original name
    /// </summary>
    public async Task<(string StoredPath, string OriginalFileName)> SaveAsync(
        IFormFile file)
    {
        if (file.Length == 0)
            throw new ArgumentException("The uploaded file is empty.");

        var extension = Path.GetExtension(file.FileName);

        // Only PDF and common image formats are accepted
        var allowedExtensions = new[]
        {
            ".pdf",
            ".jpg",
            ".jpeg",
            ".png"
        };

        if (!allowedExtensions.Contains(
                extension,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only PDF, JPG, JPEG, and PNG files are allowed.");
        }

        var uploadDirectory = Path.Combine(
            _environment.ContentRootPath,
            "uploads",
            "provider-documents"
        );

        Directory.CreateDirectory(uploadDirectory);

        // Use a fresh GUID filename to avoid collisions and path traversal
        var storedFileName = $"{Guid.NewGuid()}{extension}";

        var fullPath = Path.Combine(
            uploadDirectory,
            storedFileName
        );

        await using var stream = new FileStream(
            fullPath,
            FileMode.Create
        );

        await file.CopyToAsync(stream);

        var relativePath =
            Path.Combine(
                "uploads",
                "provider-documents",
                storedFileName
            );

        return (relativePath, file.FileName);
    }
}