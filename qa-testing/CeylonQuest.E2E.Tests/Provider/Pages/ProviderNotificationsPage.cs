using System.Text.Json;
using CeylonQuest.E2E.Tests.Provider.Infrastructure;
using OpenQA.Selenium;
using Xunit;

namespace CeylonQuest.E2E.Tests.Provider.Pages;

internal sealed class ProviderNotificationsPage(ProviderUi ui, ProviderDashboardPage dashboard)
{
    private JsonElement data;
    private string originUrl = "";
    public void Open()
    {
        var before = ui.Sequence;
        dashboard.Navigate("notifications");
        ui.Browser.Visible(By.CssSelector("section[aria-label='Notifications']"));
        Load(before, 1);
        foreach (var item in data.Items())
            ui.Browser.Step("Provider notification ID: " + item.Text("id") + " / " + item.Text("title") + " / " + (item.Flag("isRead") ? "Read" : "Unread"));
    }

    public void AssertVisibleCards(bool requireItems)
    {
        var items = data.Items();
        if (requireItems) Assert.NotEmpty(items);
        Assert.True(data.Int("unreadCount") >= 0 && data.Int("totalCount") >= items.Length);
        Assert.StartsWith(data.Int("unreadCount") + " unread", ui.Browser.Visible(By.CssSelector(".cq-notifications__heading p")).Text);
        ui.Browser.Until(d => d.FindElements(By.CssSelector(".cq-notifications__item")).Count == items.Length,
            "Notification cards do not match the displayed page.");
        var cards = ui.Browser.Driver.FindElements(By.CssSelector(".cq-notifications__item"));
        for (var index = 0; index < items.Length; index++)
        {
            ProviderUi.EqualText(items[index].Text("title"), cards[index].FindElement(By.TagName("h2")).Text);
            ProviderUi.EqualText(items[index].Text("message"), cards[index].FindElement(By.TagName("p")).Text);
            ProviderUi.EqualText(ProviderUi.SriLankaTime(items[index].Text("createdAtUtc")), cards[index].FindElement(By.TagName("time")).Text);
            var read = cards[index].FindElements(By.CssSelector(".cq-notifications__status")).Count > 0;
            Assert.Equal(items[index].Flag("isRead"), read);
        }
        if (items.Length == 0)
        {
            Assert.Equal(0, data.Int("totalCount"));
            ProviderUi.EqualText("No notifications yet", ui.Browser.Visible(By.CssSelector(".cq-notifications__empty h2")).Text);
        }
    }

    public void AssertConfiguredNotification(string id)
    {
        Find(id);
        AssertVisibleCards(true);
        Assert.Contains(data.Items(), item => item.Text("id") == id);
    }

    public void MarkRead(string id)
    {
        Find(id);
        ProviderSettings.AssertWriteTarget(originUrl);
        var items = data.Items();
        var index = Array.FindIndex(items, item => item.Text("id") == id);
        Assert.False(items[index].Flag("isRead"), "Configure an unread dedicated test notification; an already-read item does not exercise the write.");
        AssertVisibleCards(true);
        var page = data.Int("page");
        var previousUnread = data.Int("unreadCount");
        ui.Browser.Driver.FindElements(By.CssSelector(".cq-notifications__item"))[index]
            .FindElement(By.XPath(".//button[normalize-space()='Mark as read']")).Click();
        ui.Browser.Until(d => {
            var cards = d.FindElements(By.CssSelector(".cq-notifications__item"));
            return cards.Count == items.Length && cards[index].FindElements(By.CssSelector(".cq-notifications__status")).Count == 1;
        }, "Notification did not become read in the UI.");
        var before = ui.Sequence;
        dashboard.Navigate("overview");
        dashboard.Navigate("notifications");
        Load(before, page);
        Assert.True(Assert.Single(data.Items(), item => item.Text("id") == id).Flag("isRead"));
        Assert.True(data.Int("unreadCount") < previousUnread, "Unread count did not decrease.");
        AssertVisibleCards(true);
        ui.Browser.Step("Dedicated notification " + id + " marked read and persisted. The app has no mark-unread operation.");
    }

    private void Find(string id)
    {
        while (!data.Items().Any(item => item.Text("id") == id) && data.Int("page") * data.Int("pageSize") < data.Int("totalCount"))
        {
            var next = data.Int("page") + 1;
            var before = ui.Sequence;
            ui.ClickText("//nav[@aria-label='Notification pages']", "Next");
            Load(before, next);
        }
        Assert.Contains(data.Items(), item => item.Text("id") == id);
    }

    private void Load(int after, int page)
    {
        var response = ui.Wait("/api/notifications", after, uri => ProviderUi.Query(uri, "page") == page.ToString());
        data = response.Body;
        originUrl = response.Url;
        Assert.Equal(page, data.Int("page"));
        ui.Browser.Until(d => !d.FindElements(By.CssSelector(".cq-notifications [role='status']")).Any(element => element.Displayed),
            "Notifications did not finish loading.");
        ui.AssertNoVisibleError(".cq-notifications");
    }
}
