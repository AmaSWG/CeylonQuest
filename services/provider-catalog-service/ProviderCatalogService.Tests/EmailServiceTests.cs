#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ProviderCatalogService.Services;
using Xunit;

namespace ProviderCatalogService.Tests;

public class EmailServiceTests
{
    [Fact]
    public async Task SendApplicationRejectionEmailAsync_InDevMode_LogsWithoutThrowing()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Email:Host", ""},
            {"Email:Username", ""},
            {"Email:Password", ""},
            {"Email:From", "noreply@ceylonquest.com"}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var logger = NullLogger<EmailService>.Instance;

        var emailService = new EmailService(config, logger, env: null);

        var exception = await Record.ExceptionAsync(() =>
            emailService.SendApplicationRejectionEmailAsync("applicant@test.com", "Grand Hotel", "Missing business registration")
        );

        Assert.Null(exception);
    }
}