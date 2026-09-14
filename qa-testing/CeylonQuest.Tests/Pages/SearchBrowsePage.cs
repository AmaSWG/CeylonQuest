using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace CeylonQuest.Tests.Pages;

public class SearchBrowsePage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public SearchBrowsePage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
    }

    private IWebElement ExploreNavigation =>
        _wait.Until(d =>
            d.FindElements(By.Id("nav-explore"))
                .First(e => e.Displayed));

    private IWebElement SearchInput =>
        _wait.Until(d =>
            d.FindElements(
                    By.CssSelector("input[aria-label='Search listings']"))
                .First(e => e.Displayed));

    public void Open()
    {
        ExploreNavigation.Click();

        _wait.Until(d =>
            d.FindElements(
                    By.XPath("//h1[normalize-space()='Explore Sri Lanka']"))
                .Any(e => e.Displayed));

        WaitForResultsToFinishLoading();
    }

    public void Search(string keyword)
    {
        var input = SearchInput;

        input.Click();
        input.Clear();
        input.SendKeys(keyword);
        input.SendKeys(Keys.Enter);

        WaitForSearchToSettle();
    }

    public void ClearSearch()
    {
        var clearButton = _driver
            .FindElements(By.CssSelector("button[aria-label='Clear search']"))
            .FirstOrDefault(e => e.Displayed && e.Enabled);

        if (clearButton != null)
        {
            clearButton.Click();
        }
        else
        {
            var input = SearchInput;
            input.Click();
            input.Clear();
            input.SendKeys(Keys.Enter);
        }

        WaitForSearchToSettle();
    }

    public IReadOnlyCollection<IWebElement> VisibleCards()
    {
        return _driver
            .FindElements(By.CssSelector(".vd-service-card"))
            .Where(e =>
            {
                try
                {
                    return e.Displayed;
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
            })
            .ToList();
    }

    public int VisibleCardCount()
    {
        return VisibleCards().Count;
    }

    public bool HasListing(string title)
    {
        try
        {
            return VisibleCards().Any(card =>
                card.FindElements(
                        By.CssSelector(".vd-service-card__title"))
                    .Any(e =>
                    {
                        try
                        {
                            return e.Displayed &&
                                   string.Equals(
                                       e.Text.Trim(),
                                       title,
                                       StringComparison.OrdinalIgnoreCase);
                        }
                        catch (StaleElementReferenceException)
                        {
                            return false;
                        }
                    }));
        }
        catch (StaleElementReferenceException)
        {
            return false;
        }
    }

    public IWebElement GetListingCard(string title)
    {
        IWebElement? foundCard = null;

        _wait.Until(_ =>
        {
            try
            {
                foundCard = VisibleCards()
                    .FirstOrDefault(card =>
                    {
                        try
                        {
                            return card
                                .FindElements(
                                    By.CssSelector(".vd-service-card__title"))
                                .Any(e =>
                                    e.Displayed &&
                                    string.Equals(
                                        e.Text.Trim(),
                                        title,
                                        StringComparison.OrdinalIgnoreCase));
                        }
                        catch (StaleElementReferenceException)
                        {
                            return false;
                        }
                    });

                return foundCard != null;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });

        return foundCard!;
    }

    public string GetListingType(string title)
    {
        var card = GetListingCard(title);

        return card
            .FindElement(By.CssSelector(".vd-type-badge"))
            .Text
            .Trim();
    }

    public IReadOnlyList<string> VisibleTitles()
    {
        var titles = new List<string>();

        foreach (var card in VisibleCards())
        {
            try
            {
                var title = card
                    .FindElement(By.CssSelector(".vd-service-card__title"))
                    .Text
                    .Trim();

                titles.Add(title);
            }
            catch (StaleElementReferenceException)
            {
                // DOM refreshed; ignore this card
            }
        }

        return titles;
    }

    public string ResultsCountText()
    {
        return _wait
            .Until(d =>
                d.FindElements(By.CssSelector(".vd-results-count"))
                    .First(e => e.Displayed))
            .Text
            .Trim();
    }

    public bool NoResultsMessageVisible()
    {
        return _driver
            .FindElements(By.CssSelector(".vd-empty-search"))
            .Any(e =>
            {
                try
                {
                    return e.Displayed &&
                           e.Text.Contains(
                               "No listings matched your search",
                               StringComparison.OrdinalIgnoreCase);
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
            });
    }

    public string WaitForNoResults(string keyword)
    {
        string? result = null;

        _wait.Until(d =>
        {
            try
            {
                var empty = d
                    .FindElements(By.CssSelector(".vd-empty-search"))
                    .FirstOrDefault(e => e.Displayed);

                if (empty == null)
                    return false;

                var text = empty.Text;

                if (!text.Contains(
                        "No listings matched your search",
                        StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!text.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase))
                    return false;

                result = text;
                return true;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });

        return result ?? string.Empty;
    }

    public string NoResultsText()
    {
        return _wait
            .Until(d =>
                d.FindElements(By.CssSelector(".vd-empty-search"))
                    .First(e => e.Displayed))
            .Text;
    }

    public bool PaginationVisible()
    {
        return _driver
            .FindElements(By.CssSelector(".vd-pagination"))
            .Any(e =>
            {
                try
                {
                    return e.Displayed;
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
            });
    }

    public string PageIndicatorText()
    {
        return $"Page {CurrentPage()}";
    }

    public int CurrentPage()
    {
        int currentPage = 0;

        _wait.Until(d =>
        {
            try
            {
                // First try aria-current
                var active = d.FindElements(
                        By.CssSelector(
                            ".vd-pagination [aria-current='page']"))
                    .FirstOrDefault(e =>
                    {
                        try
                        {
                            return e.Displayed;
                        }
                        catch (StaleElementReferenceException)
                        {
                            return false;
                        }
                    });

                // Fallback: active page button/class
                active ??= d.FindElements(
                        By.CssSelector(
                            ".vd-pagination .vd-page-num.active, " +
                            ".vd-pagination button.active"))
                    .FirstOrDefault(e =>
                    {
                        try
                        {
                            return e.Displayed;
                        }
                        catch (StaleElementReferenceException)
                        {
                            return false;
                        }
                    });

                if (active == null)
                    return false;

                var text = active.Text.Trim();

                if (!int.TryParse(text, out var page))
                    return false;

                currentPage = page;
                return true;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });

        return currentPage;
    }

    public void GoToNextPage()
    {
        var oldPage = CurrentPage();

        IWebElement? nextButton = null;

        _wait.Until(d =>
        {
            try
            {
                nextButton = d.FindElements(
                        By.CssSelector(
                            ".vd-pagination button[aria-label='Next page']"))
                    .FirstOrDefault(e =>
                    {
                        try
                        {
                            return e.Displayed && e.Enabled;
                        }
                        catch (StaleElementReferenceException)
                        {
                            return false;
                        }
                    });

                return nextButton != null;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });

        if (nextButton == null)
            throw new NoSuchElementException(
                "Next page button was not found or is disabled.");

        nextButton.Click();

        _wait.Until(_ =>
        {
            try
            {
                return TryGetCurrentPage(out var newPage)
                       && newPage > oldPage;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });

        WaitForResultsToFinishLoading();
    }

    public void GoToPreviousPage()
    {
        var oldPage = CurrentPage();

        IWebElement? previousButton = null;

        _wait.Until(d =>
        {
            try
            {
                previousButton = d.FindElements(
                        By.CssSelector(
                            ".vd-pagination button[aria-label='Previous page']"))
                    .FirstOrDefault(e =>
                    {
                        try
                        {
                            return e.Displayed && e.Enabled;
                        }
                        catch (StaleElementReferenceException)
                        {
                            return false;
                        }
                    });

                return previousButton != null;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });

        if (previousButton == null)
            throw new NoSuchElementException(
                "Previous page button was not found or is disabled.");

        previousButton.Click();

        _wait.Until(_ =>
        {
            try
            {
                return TryGetCurrentPage(out var newPage)
                       && newPage < oldPage;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });

        WaitForResultsToFinishLoading();
    }

    private bool TryGetCurrentPage(out int page)
    {
        page = 0;

        try
        {
            var active = _driver.FindElements(
                    By.CssSelector(
                        ".vd-pagination [aria-current='page']"))
                .FirstOrDefault(e =>
                {
                    try
                    {
                        return e.Displayed;
                    }
                    catch (StaleElementReferenceException)
                    {
                        return false;
                    }
                });

            active ??= _driver.FindElements(
                    By.CssSelector(
                        ".vd-pagination .vd-page-num.active, " +
                        ".vd-pagination button.active"))
                .FirstOrDefault(e =>
                {
                    try
                    {
                        return e.Displayed;
                    }
                    catch (StaleElementReferenceException)
                    {
                        return false;
                    }
                });

            if (active == null)
                return false;

            return int.TryParse(active.Text.Trim(), out page);
        }
        catch (StaleElementReferenceException)
        {
            return false;
        }
    }

    public void WaitForListing(string title)
    {
        _wait.Until(d =>
        {
            try
            {
                var titles = d.FindElements(
                    By.CssSelector(".vd-service-card__title"));

                return titles.Any(e =>
                {
                    try
                    {
                        return e.Displayed &&
                               string.Equals(
                                   e.Text.Trim(),
                                   title,
                                   StringComparison.OrdinalIgnoreCase);
                    }
                    catch (StaleElementReferenceException)
                    {
                        return false;
                    }
                });
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });
    }

    private void WaitForSearchToSettle()
    {
        _wait.Until(d =>
        {
            try
            {
                var loading = d
                    .FindElements(By.CssSelector(".vd-loading-card"))
                    .Any(e =>
                    {
                        try
                        {
                            return e.Displayed;
                        }
                        catch (StaleElementReferenceException)
                        {
                            return false;
                        }
                    });

                if (loading)
                    return false;

                var cards = d
                    .FindElements(By.CssSelector(".vd-service-card"))
                    .Any(e =>
                    {
                        try
                        {
                            return e.Displayed;
                        }
                        catch (StaleElementReferenceException)
                        {
                            return false;
                        }
                    });

                var empty = d
                    .FindElements(By.CssSelector(".vd-empty-search"))
                    .Any(e =>
                    {
                        try
                        {
                            return e.Displayed;
                        }
                        catch (StaleElementReferenceException)
                        {
                            return false;
                        }
                    });

                return cards || empty;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });
    }

    private void WaitForResultsToFinishLoading()
    {
        _wait.Until(d =>
        {
            try
            {
                return !d.FindElements(
                        By.CssSelector(".vd-loading-card"))
                    .Any(e =>
                    {
                        try
                        {
                            return e.Displayed;
                        }
                        catch (StaleElementReferenceException)
                        {
                            return false;
                        }
                    });
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });
    }
}