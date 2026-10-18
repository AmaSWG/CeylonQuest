using System.Runtime.CompilerServices;
using CeylonQuest.E2E.Tests.Admin.Pages;
using CeylonQuest.E2E.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Admin.Infrastructure;

[Trait("Category", "AdminCatalog")]
[Trait("Workflow", "Admin")]
[Trait("Safety", "ReadOnly")]
public abstract class AdminTestBase(ITestOutputHelper output)
{
    private protected void Run(Action<AdminUi, AdminDashboardPage> scenario, [CallerMemberName] string name = "AdminCatalog")
    {
        using var browser = new BrowserSession(name, output);
        var ui = new AdminUi(browser);
        // Intentionally avoid BrowserSession.Run and Capture: both successful and
        // failing Admin cases use console/TRX evidence without screenshots.
        try
        {
            var dashboard = new AdminDashboardPage(ui);
            dashboard.Login();
            scenario(ui, dashboard);
            browser.Step("Admin catalog scenario completed: " + name);
        }
        catch
        {
            browser.Step("Admin catalog scenario failed: " + name + ". Inspect TRX assertion/stack details; screenshots are disabled.");
            throw;
        }
        finally
        {
            if (ui.Observing) ui.NoWrites();
        }
    }
}
