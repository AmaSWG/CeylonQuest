using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using CeylonQuest.Tests.Configuration;

namespace CeylonQuest.Tests.Pages;

public class ProviderApplicationPage
{
    private readonly IWebDriver driver;
    private readonly WebDriverWait wait;

    public ProviderApplicationPage(IWebDriver driver)
    {
        this.driver = driver;
        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    private IWebElement FirstName =>
        driver.FindElement(By.Id("pa-firstName"));

    private IWebElement LastName =>
        driver.FindElement(By.Id("pa-lastName"));

    private IWebElement Email =>
        driver.FindElement(By.Id("pa-email"));

    private IWebElement Phone =>
        driver.FindElement(By.Id("pa-phone"));

    private IWebElement Password =>
        driver.FindElement(By.Id("pa-password"));

    private IWebElement ConfirmPassword =>
        driver.FindElement(By.Id("pa-confirmPassword"));

    private IWebElement BusinessName =>
        driver.FindElement(By.Id("pa-businessName"));

    private IWebElement ServiceType =>
        driver.FindElement(By.Id("pa-serviceType"));

    private IWebElement Location =>
        driver.FindElement(By.Id("pa-location"));

    private IWebElement Description =>
        driver.FindElement(By.Id("pa-description"));

    private IWebElement LegalDocument =>
        driver.FindElement(By.Id("pa-legalDoc"));

    private IWebElement SubmitButton =>
        driver.FindElement(By.Id("submit-application"));

    public void Open()
    {
        driver.Navigate().GoToUrl(TestConfiguration.BaseUrl);
    }

    public void GoToProviderApplication()
    {
        driver.FindElement(By.Id("apply-as-provider")).Click();

        wait.Until(d =>
            d.FindElement(By.Id("submit-application")).Displayed
        );
    }

    public void FillApplication(
        string firstName,
        string lastName,
        string email,
        string phone,
        string password,
        string businessName,
        string serviceType,
        string location,
        string description,
        string legalDocumentPath)
    {
        FirstName.SendKeys(firstName);
        LastName.SendKeys(lastName);
        Email.SendKeys(email);
        Phone.SendKeys(phone);
        Password.SendKeys(password);
        ConfirmPassword.SendKeys(password);

        BusinessName.SendKeys(businessName);

        SelectElement serviceTypeSelect =
            new SelectElement(ServiceType);

        serviceTypeSelect.SelectByValue(serviceType);

        Location.SendKeys(location);
        Description.SendKeys(description);

        LegalDocument.SendKeys(legalDocumentPath);
    }

    public void Submit()
    {
        SubmitButton.Click();
    }

     public bool IsApplicationSuccessful()
     {
         try
         {
             return wait.Until(d =>
                 d.FindElement(By.CssSelector(".reg-toast--success")).Displayed
             );
         }
         catch (WebDriverTimeoutException)
         {
             return false;
         }
     }

    public string GetSuccessTitle()
    {
        IWebElement title = wait.Until(d =>
            d.FindElement(
                By.CssSelector(".reg-toast__title")
            )
        );

        return title.Text;
    }

   public string GetErrorMessage()
   {
       try
       {
           IWebElement error = wait.Until(d =>
               d.FindElement(By.CssSelector(".pa-form-error"))
           );

           return error.Text;
       }
       catch (WebDriverTimeoutException)
       {
           return "NO_ERROR_MESSAGE_FOUND";
       }
   }

   public void OpenProviderApplications()
   {
       wait.Until(d =>
           d.FindElement(By.Id("ad-nav-applications"))
       ).Click();
   }

   public bool IsPendingApplicationDisplayed()
   {
       try
       {
           return wait.Until(d =>
               d.FindElements(By.CssSelector(".ad-badge--pending"))
                .Any(e =>
                    e.Displayed &&
                    e.Text.Equals(
                        "Pending",
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

   public void ClickFirstReviewDetails()
   {
       IWebElement button = wait.Until(d =>
           d.FindElements(By.CssSelector(".ad-row-btn--view"))
            .FirstOrDefault(e =>
                e.Displayed &&
                e.Text.Equals(
                    "Review Details",
                    StringComparison.OrdinalIgnoreCase
                )
            )
       );

       button.Click();
   }

   public bool IsApplicationDetailsModalDisplayed()
   {
       try
       {
           return wait.Until(d =>
               d.FindElement(By.CssSelector(
                   ".ad-modal[role='dialog']"
               )).Displayed
           );
       }
       catch (WebDriverTimeoutException)
       {
           return false;
       }
   }

}