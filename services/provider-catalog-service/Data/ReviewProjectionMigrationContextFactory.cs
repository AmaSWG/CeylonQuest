using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProviderCatalogService.Data;

public class ReviewProjectionMigrationContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(typeof(CatalogDbContext).Assembly.Location)!)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddUserSecrets<CatalogDbContext>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connection = configuration.GetConnectionString("ProviderCatalogDb")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:ProviderCatalogDb is not configured.");

        return new CatalogDbContext(
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseMySql(connection, new MySqlServerVersion(new Version(8, 0, 30)))
                .Options);
    }
}
