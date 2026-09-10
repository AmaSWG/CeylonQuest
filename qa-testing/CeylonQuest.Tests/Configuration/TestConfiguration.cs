using Microsoft.Extensions.Configuration;

namespace CeylonQuest.Tests.Configuration;

public static class TestConfiguration
{
    public static IConfigurationRoot Load()
    {
        var environment =
            Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") ?? "Local";

        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile($"appsettings.{environment}.json", optional: false)
            .Build();
    }
}