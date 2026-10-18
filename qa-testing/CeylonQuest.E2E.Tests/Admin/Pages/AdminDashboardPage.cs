using System.Text.Json;
using CeylonQuest.E2E.Tests.Admin.Infrastructure;
using CeylonQuest.E2E.Tests.Infrastructure;
using CeylonQuest.E2E.Tests.Pages;
using OpenQA.Selenium;
using Xunit;

namespace CeylonQuest.E2E.Tests.Admin.Pages;

internal sealed class AdminDashboardPage(AdminUi ui)
{
    public const string ApplicationsPath = "/api/catalog/admin/providers";
    public JsonElement[] Applications { get; private set; } = [];

    public void Login()
    {
        var login = new LoginPage(ui.Browser);
        login.OpenFromHome();
        ui.Observe();
        login.Submit(TestSettings.Required("CQ_E2E_ADMIN_EMAIL"), TestSettings.Required("CQ_E2E_ADMIN_PASSWORD"));
        ui.Browser.Visible(By.Id("nav-overview"));
        Assert.Equal("Admin", ui.Browser.Script.ExecuteScript("return localStorage.getItem('userRole')"));
        Assert.True(ui.Browser.Script.ExecuteScript("return Boolean(localStorage.getItem('authToken'))") is true);
        foreach (var tab in new[] { "applications", "listings", "reports" }) ui.Browser.Visible(By.Id("nav-" + tab));
        ui.Heading("Admin Overview");
        var data = ui.Wait(ApplicationsPath).Body;
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
        Applications = data.EnumerateArray().Select(item => item.Clone()).ToArray();
        Assert.Equal(Applications.Length, Applications.Select(item => item.Text("id")).Distinct().Count());
        foreach (var item in Applications)
        {
            Assert.True(Guid.TryParse(item.Text("id"), out var id) && id != Guid.Empty);
            Assert.Contains(item.Text("status").ToLowerInvariant(), new[] { "pending", "approved", "rejected" });
        }
        ui.Browser.Step($"Valid Admin login and catalog applications loaded: {Applications.Length} records.");
    }
    public void Open(string tab, string heading)
    {
        ui.Browser.Click(By.Id("nav-" + tab));
        ui.Heading(heading);
    }
}
