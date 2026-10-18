using System.Runtime.CompilerServices;
using CeylonQuest.E2E.Tests.Infrastructure;
using CeylonQuest.E2E.Tests.Pages;
using CeylonQuest.E2E.Tests.Visitor.Pages;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Visitor.Infrastructure;

[Trait("Category", "VisitorCatalog")]
[Trait("Workflow", "VisitorCatalog")]
[Trait("Safety", "ReadOnly")]
public abstract class VisitorCatalogTestBase(ITestOutputHelper output)
{
    private protected void Run(Action<VisitorCatalogUi, VisitorExplorePage> scenario,
        [CallerMemberName] string name = "VisitorCatalogTest")
    {
        using var browser = new BrowserSession(name, output);
        browser.Run(() => {
            new LoginPage(browser).LoginVisitor();
            var ui = new VisitorCatalogUi(browser);
            ui.Observe();
            var explore = new VisitorExplorePage(ui);
            explore.Open();
            try
            {
                scenario(ui, explore);
                browser.Capture("visitor-catalog-verified");
            }
            finally
            {
                ui.AssertNoBusinessWrites();
            }
        });
    }
}
