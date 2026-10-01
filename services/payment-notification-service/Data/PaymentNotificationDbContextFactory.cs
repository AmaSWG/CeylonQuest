using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PaymentNotificationService.Data;

// Enables migration commands without starting Kafka or the HTTP application.
public class PaymentNotificationDbContextFactory : IDesignTimeDbContextFactory<PaymentNotificationDbContext>
{
    public PaymentNotificationDbContext CreateDbContext(string[] args)
    {
        var serviceDirectory = Directory.GetCurrentDirectory();

        if (!File.Exists(Path.Combine(serviceDirectory, "PaymentNotificationService.csproj")))
            serviceDirectory = Path.Combine(serviceDirectory, "services", "payment-notification-service");
        
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        var configuration = new ConfigurationBuilder().SetBasePath(serviceDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true);
        
        if (environment == "Development")
            configuration.AddUserSecrets<PaymentNotificationDbContextFactory>(optional: true);
        
        var config = configuration.AddEnvironmentVariables().AddCommandLine(args).Build();
        
        var connection = config.GetConnectionString("PaymentNotificationDb");
        
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Configure ConnectionStrings:PaymentNotificationDb using user secrets or environment variables.");
        
        var version = Version.Parse(config["DatabaseServerVersion"] ?? "8.0.30");
        
        var options = new DbContextOptionsBuilder<PaymentNotificationDbContext>()
            .UseMySql(connection, new MySqlServerVersion(version)).Options;
        return new(options);
    }
}
