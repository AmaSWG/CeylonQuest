using Microsoft.Extensions.DependencyInjection;

namespace Shared.Storage;

public static class StorageServiceExtensions
{
    public static IServiceCollection AddBlobStorage(this IServiceCollection services)
    {
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        return services;
    }
}