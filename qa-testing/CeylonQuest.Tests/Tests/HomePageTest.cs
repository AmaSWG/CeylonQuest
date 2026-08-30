using CeylonQuest.Tests.Pages;
using Xunit;

namespace CeylonQuest.Tests.Tests;

public class HomePageTest : BaseTest
{
    [Fact]
    public void OpenCeylonQuest()
    {
        HomePage homePage = new HomePage(Driver);

        homePage.Open();

        Assert.Contains("frontend", homePage.GetTitle());
    }
}