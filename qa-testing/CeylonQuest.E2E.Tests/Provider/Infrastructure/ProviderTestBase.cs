using System.Runtime.CompilerServices;
using CeylonQuest.E2E.Tests.Infrastructure;
using CeylonQuest.E2E.Tests.Provider.Pages;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Provider.Infrastructure;

[Trait("Category", "ProviderCatalog")]
[Trait("Workflow", "Provider")]
[Trait("Safety", "ReadOnly")]
public abstract class ProviderTestBase(ITestOutputHelper output)
{
    private protected void Run(Action<ProviderUi, ProviderDashboardPage, ProviderListingsPage> test,
        [CallerMemberName] string name = "ProviderCatalogTest")
    {
        using var browser = new BrowserSession(name, output);
        browser.Run(() => {
            var ui = new ProviderUi(browser);
            var dashboard = new ProviderDashboardPage(ui);
            try
            {
                dashboard.Login();
                test(ui, dashboard, new ProviderListingsPage(ui, dashboard));
                browser.Capture("provider-catalog-verified");
            }
            finally
            {
                // Also check unsuccessful cases. The valid login POST is the only exception.
                if (ui.ObservationInstalled) ui.AssertNoBusinessWrites();
            }
        });
    }
}
