using System.Runtime.CompilerServices;
using CeylonQuest.E2E.Tests.Infrastructure;
using CeylonQuest.E2E.Tests.Provider.Infrastructure;
using CeylonQuest.E2E.Tests.Provider.Pages;
using Xunit;
using Xunit.Abstractions;

namespace CeylonQuest.E2E.Tests.Provider.Tests;

[Trait("Workflow", "Provider")]
[Trait("Safety", "ReadOnly")]
public sealed class ProviderAuthenticationTests(ITestOutputHelper output)
{
    [ProviderFact]
    [Trait("CaseId", "PROV-AUTH-03")]
    public void IncorrectProviderPassword_RemainsOnLoginWithoutSession() => Run(page =>
        page.VerifyIncorrectPasswordRejected());

    [VisitorFact]
    [Trait("CaseId", "PROV-AUTH-04")]
    public void VisitorCredentials_CannotAccessProviderDashboardOrBookings() => Run(page =>
        page.VerifyVisitorCannotAccessProvider());

    // Positive Provider login already exists as PROV-AUTH-01. The unregistered
    // email and empty-fields scenarios already exist on the same shared login form
    // in AuthenticationTests. The documented auth filter runs those exact tests
    // once instead of introducing duplicate wrappers or copies in this class.
    private void Run(Action<ProviderAuthenticationPage> test, [CallerMemberName] string name = "ProviderAuthentication")
    {
        using var browser = new BrowserSession(name, output);
        browser.Run(() => test(new ProviderAuthenticationPage(browser)));
    }
}
