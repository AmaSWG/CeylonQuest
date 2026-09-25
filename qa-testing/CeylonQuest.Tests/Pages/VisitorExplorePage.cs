using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;

namespace CeylonQuest.Tests.Pages
{
    public class VisitorExplorePage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public VisitorExplorePage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
        }

        private IWebElement ExploreNavButton => _wait.Until(d => d.FindElement(By.Id("nav-explore")));
        private IWebElement ServiceTypeFilter => _wait.Until(d => d.FindElement(By.CssSelector(".vd-filter-select")));

        public void NavigateToExplore()
        {
            ExploreNavButton.Click();
            _wait.Until(d => d.FindElement(By.CssSelector(".vd-filter-select")).Displayed);
        }

        public void FilterByExperiences()
        {
            Console.WriteLine($"[DIAG:filter] start URL = {_driver.Url}");

            NavigateToExplore();

            Console.WriteLine($"[DIAG:filter] after nav URL = {_driver.Url}");
            Console.WriteLine($"[DIAG:filter] .vd-filter-select count = " +
                _driver.FindElements(By.CssSelector(".vd-filter-select")).Count);
            Console.WriteLine($"[DIAG:filter] .vd-service-card count = " +
                _driver.FindElements(By.CssSelector(".vd-service-card")).Count);

            var select = new SelectElement(ServiceTypeFilter);

            Console.WriteLine($"[DIAG:filter] select options before: " +
                string.Join(" | ", select.Options.Select(o =>
                    $"'{o.Text}'={o.GetAttribute("value")}")));

            select.SelectByValue("experience");

            Console.WriteLine($"[DIAG:filter] select options after: " +
                string.Join(" | ", select.Options.Select(o =>
                    $"'{o.Text}'={o.GetAttribute("value")}")));

            _wait.Until(d => d.FindElements(By.CssSelector(".vd-service-card")).Count > 0);

            Console.WriteLine($"[DIAG:filter] card count after wait = " +
                _driver.FindElements(By.CssSelector(".vd-service-card")).Count);
        }

        public void ClickFirstExperienceBookNow()
        {
            // 1. Wait until the Book Now button is both visible and enabled
            var bookBtn = _wait.Until(d =>
            {
                var btn = d.FindElement(By.XPath("//button[contains(@class, 'vd-btn--primary') and contains(text(), 'Book Now')]"));
                return (btn != null && btn.Displayed && btn.Enabled) ? btn : null;
            });

            // 2. Scroll into view so Chrome doesn't miss the click
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({behavior: 'instant', block: 'center'});", bookBtn);

            // 3. Click with fallback
            try
            {
                bookBtn.Click();
            }
            catch (WebDriverException)
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", bookBtn);
            }
        }
    }
}