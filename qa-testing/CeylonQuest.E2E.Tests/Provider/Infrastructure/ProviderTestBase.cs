using System.Runtime.CompilerServices;
using CeylonQuest.E2E.Tests.Infrastructure;
using CeylonQuest.E2E.Tests.Provider.Pages;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Provider.Infrastructure;

public abstract class ProviderTestBase(ITestOutputHelper output)
{
    private protected void Run(Action<ProviderUi, ProviderDashboardPage> test, bool allowsWrites = false,
        [CallerMemberName] string name = "ProviderTest")
    {
        using var browser = new BrowserSession(name, output);
        browser.Run(() => {
            var ui = new ProviderUi(browser);
            var dashboard = new ProviderDashboardPage(ui);
            dashboard.Login();
            var before = ui.Sequence;
            test(ui, dashboard);
            if (!allowsWrites) ui.AssertNoBusinessWritesSince(before);
            browser.Capture("provider-verified");
        });
    }
}
