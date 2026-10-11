using OpenQA.Selenium;

namespace CeylonQuest.Tests.Sprint4Notifications.Infrastructure;

/// <summary>Page object for frontend/src/components/NotificationPanel.jsx.</summary>
public sealed class NotificationPanelPage
{
    private readonly IWebDriver _driver;
    public NotificationPanelPage(IWebDriver driver) => _driver = driver;

    public IReadOnlyList<IWebElement> Items => _driver.FindElements(By.CssSelector(".cq-notifications__item"));

    public bool IsEmptyStateShown => _driver.FindElements(By.CssSelector(".cq-notifications__empty")).Count > 0;

    public IWebElement? FindItemContaining(string text) =>
        Items.FirstOrDefault(i => i.Text.Contains(text, StringComparison.OrdinalIgnoreCase));

    public bool IsUnread(IWebElement item) => (item.GetAttribute("class") ?? "").Contains("cq-notifications__item--unread");

    public void MarkAsRead(IWebElement item) => item.FindElement(By.CssSelector("button")).Click();

    public void MarkAllAsRead() => _driver.FindElement(By.CssSelector(".cq-notifications__heading button")).Click();
}
