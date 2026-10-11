using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BookingService.Data;

// Schema generation must not require Stripe, Kafka or a running application.
public class ReviewMigrationContextFactory : IDesignTimeDbContextFactory<BookingDbContext>
{
    public BookingDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(typeof(BookingDbContext).Assembly.Location)!)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddUserSecrets<BookingDbContext>(optional: true)
            .AddEnvironmentVariables().Build();
        var connection = configuration.GetConnectionString("BookingDb")
            ?? throw new InvalidOperationException("ConnectionStrings:BookingDb is not configured. Set it in booking-service user secrets or environment variables.");
        return new BookingDbContext(new DbContextOptionsBuilder<BookingDbContext>()
            .UseMySql(connection, new MySqlServerVersion(new Version(8, 0, 30))).Options);
    }
}
