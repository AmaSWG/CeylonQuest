using CeylonQuest.Tests.Configuration;
using Xunit;

namespace CeylonQuest.Tests.Tests;

public class ConfigurationTest
{
    [Fact]
    public void LoadLocalConfiguration()
    {
        var config = TestConfiguration.Load();

        var baseUrl = config["TestSettings:BaseUrl"];

        Assert.Equal("http://localhost:5173", baseUrl);
    }
}