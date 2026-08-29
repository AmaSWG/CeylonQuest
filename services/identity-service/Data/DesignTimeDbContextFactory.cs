using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace IdentityService.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Prefer environment variable fallback for non-committed secrets.
        var conn = config.GetConnectionString("IdentityDb")
                   ?? Environment.GetEnvironmentVariable("ConnectionStrings__IdentityDb");

        if (string.IsNullOrWhiteSpace(conn))
        {
            throw new InvalidOperationException(
                "DesignTimeDbContextFactory: could not find a connection string.\n" +
                "Set 'ConnectionStrings:IdentityDb' in appsettings.Development.json or the environment variable 'ConnectionStrings__IdentityDb'.");
        }

        // Allow an optional server version override via configuration; otherwise use a reasonable default.
        var serverVersionString = config["DatabaseServerVersion"];
        MySqlServerVersion serverVersion;
        if (!string.IsNullOrWhiteSpace(serverVersionString) && Version.TryParse(serverVersionString, out var parsed))
        {
            serverVersion = new MySqlServerVersion(parsed);
        }
        else
        {
            serverVersion = new MySqlServerVersion(new Version(8, 0, 29));
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseMySql(conn, serverVersion);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
