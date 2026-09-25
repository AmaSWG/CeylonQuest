using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace CeylonQuest.Tests.Configuration
{
    public class TestSettings
    {
        public string BaseUrl { get; set; } = "http://localhost:5173";
        public string Browser { get; set; } = "Chrome";
        public string Environment { get; set; } = "Local";
        public int ImplicitWaitSeconds { get; set; } = 10;
        public string VisitorEmail { get; set; } = "amayagunasekara4@gmail.com";
        public string VisitorPassword { get; set; } = "Amaya@123!";
    }

    public static class TestConfiguration
    {
        public static TestSettings Settings { get; }

        static TestConfiguration()
        {
            var env = Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") ?? "Local";

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
                .Build();

            Settings = config.GetSection("TestSettings").Get<TestSettings>() ?? new TestSettings();
        }
    }
}