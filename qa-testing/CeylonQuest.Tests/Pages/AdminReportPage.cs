using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;

namespace CeylonQuest.Tests.Pages;

public class AdminReportPage
{
    private readonly IWebDriver driver;
    private readonly WebDriverWait wait;

    public AdminReportPage(IWebDriver driver)
    {
        this.driver = driver;

        wait = new WebDriverWait(
            driver,
            TimeSpan.FromSeconds(10)
        );
    }

    public void OpenReportsTab()
    {
        IWebElement reportsTab = wait.Until(d =>
            d.FindElement(By.Id("ad-nav-reports"))
        );

        reportsTab.Click();

        wait.Until(d =>
            d.FindElements(By.CssSelector(".ad-report"))
             .Any(e => e.Displayed)
        );
    }

    public bool IsReportDisplayed()
    {
        try
        {
            return wait.Until(d =>
                d.FindElements(By.CssSelector(".ad-report"))
                 .Any(e => e.Displayed)
            );
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public bool IsGeneratedTimestampDisplayed()
    {
        try
        {
            return wait.Until(d =>
                d.FindElements(
                    By.CssSelector(".ad-report__generated")
                )
                .Any(e => e.Displayed)
            );
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public int GetTotalUsers()
    {
        return GetCardValue("Total Users");
    }

    public int GetVisitors()
    {
        return GetCardValue("Visitors");
    }

    public int GetProviders()
    {
        return GetCardValue("Providers");
    }

    public int GetAdmins()
    {
        return GetCardValue("Admins");
    }

    public int GetTotalApplications()
    {
        return GetCardValueByClass(
            ".ad-report__card--total"
        );
    }

    public int GetPendingApplications()
    {
        return GetCardValueByClass(
            ".ad-report__card--pending"
        );
    }

    public int GetApprovedApplications()
    {
        return GetCardValueByClass(
            ".ad-report__card--approved"
        );
    }

    public int GetRejectedApplications()
    {
        return GetCardValueByClass(
            ".ad-report__card--rejected"
        );
    }
    private int GetCardValue(string label)
    {
        var cards = wait.Until(d =>
            d.FindElements(By.CssSelector(".ad-report__card"))
        );

        Console.WriteLine($"Looking for report card: {label}");

        foreach (IWebElement card in cards)
        {
            var labels = card.FindElements(
                By.CssSelector(".ad-report__card-label")
            );

            if (labels.Any())
            {
                Console.WriteLine(
                    $"Found card label: '{labels[0].Text}'"
                );
            }
        }

        IWebElement? matchingCard =
            cards.FirstOrDefault(card =>
            {
                var labels = card.FindElements(
                    By.CssSelector(".ad-report__card-label")
                );

                return labels.Any(l =>
                    l.Text.Trim().Equals(
                        label,
                        StringComparison.OrdinalIgnoreCase
                    )
                );
            });

        if (matchingCard == null)
        {
            throw new NoSuchElementException(
                $"Report card with label '{label}' was not found."
            );
        }

        string value = matchingCard
            .FindElement(
                By.CssSelector(".ad-report__card-value")
            )
            .Text
            .Trim();

        return int.Parse(value);
    }

    private void AssertSelectedRole(string role)
    {
         wait.Until(d =>
         {
             SelectElement select = new(
                 d.FindElement(By.Id("ad-report-role"))
             );

              return select.SelectedOption.Text
                 .Trim()
                 .Equals(
                     role,
                     StringComparison.OrdinalIgnoreCase
                 );
         });
    }

    public void FilterByRole(string role)
    {
        IWebElement roleElement = wait.Until(d =>
            d.FindElement(By.Id("ad-report-role"))
        );

        SelectElement roleSelect =
            new SelectElement(roleElement);

        roleSelect.SelectByText(role);

        AssertSelectedRole(role);

        IWebElement applyButton = wait.Until(d =>
            d.FindElement(By.Id("ad-report-apply-btn"))
        );

        applyButton.Click();

        WaitForReport();
    }

    public void FilterByApplicationStatus(string status)
    {
        SelectElement select = new(
            wait.Until(d =>
                d.FindElement(By.Id("ad-report-status"))
            )
        );

        select.SelectByText(status);

        driver
            .FindElement(By.Id("ad-report-apply-btn"))
            .Click();

        WaitForReport();
    }

    public bool IsActiveFilterDisplayed(string filterText)
    {
        try
        {
            return wait.Until(d =>
                d.FindElements(
                    By.CssSelector(".ad-report__filter-badge")
                )
                .Any(e =>
                    e.Displayed &&
                    e.Text.Contains(
                        filterText,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            );
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public void ClearFilters()
    {
        IWebElement button = wait.Until(d =>
            d.FindElement(By.Id("ad-report-clear-btn"))
        );

        button.Click();

        WaitForReport();
    }

    private void WaitForReport()
    {
        wait.Until(d =>
            d.FindElements(
                By.CssSelector(".ad-report__card")
            )
            .Any(e => e.Displayed)
        );
    }

    private int GetCardValueByClass(string cardSelector)
    {
        IWebElement card = wait.Until(d =>
        {
            var matchingCards =
                d.FindElements(By.CssSelector(cardSelector))
                 .Where(e => e.Displayed)
                 .ToList();

            return matchingCards.LastOrDefault();
        });

        string value = card
            .FindElement(
                By.CssSelector(".ad-report__card-value")
            )
            .Text
            .Trim();

        return int.Parse(value);
    }

    private int GetProviderApplicationCardValue(
           string cardClass)
       {
           IWebElement providerSection = wait.Until(d =>
               d.FindElements(By.CssSelector(".ad-report__section"))
                .FirstOrDefault(section =>
                    section.Text.Contains(
                        "Provider Applications",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
           );

           if (providerSection == null)
           {
               throw new NoSuchElementException(
                   "Provider Applications report section was not found."
               );
           }

           IWebElement card =
               providerSection.FindElement(
                   By.CssSelector(cardClass)
               );

           string value = card
               .FindElement(
                   By.CssSelector(".ad-report__card-value")
               )
               .Text
               .Trim();

           return int.Parse(value);
       }

       public string GetSelectedRoleFilter()
       {
           IWebElement roleElement = wait.Until(d =>
               d.FindElement(By.Id("ad-report-role"))
           );

           SelectElement roleSelect = new(roleElement);

           return roleSelect.SelectedOption.Text.Trim();
       }

       public string GetSelectedApplicationStatusFilter()
       {
           IWebElement statusElement = wait.Until(d =>
               d.FindElement(By.Id("ad-report-status"))
           );

           SelectElement statusSelect = new(statusElement);

           return statusSelect.SelectedOption.Text.Trim();
       }


}