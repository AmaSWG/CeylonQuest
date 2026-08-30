using CeylonQuest.Tests.Configuration;
using CeylonQuest.Tests.Pages;
using Xunit;

namespace CeylonQuest.Tests.Tests;

public class ProviderApplicationStatusTest : BaseTest
{
    [Fact]
    public void PendingApplication_ShouldDisplayPendingStatus()
    {
        ProviderApplicationStatusPage statusPage =
            new(Driver);

        statusPage.Open();

        statusPage.SearchByEmail(
            TestConfiguration.PendingProviderEmail
        );

        Assert.True(
            statusPage.WaitForResults(),
            "Application status result was not displayed."
        );

        Assert.True(
            statusPage.IsPendingStatusDisplayed(),
            "Pending application should display status Pending."
        );

        Assert.False(
            statusPage.IsActivationButtonDisplayed(),
            "Pending application must not be eligible for Provider activation."
        );
    }

    [Fact]
    public void ApprovedApplication_ShouldDisplayApprovedStatusAndActivationOption()
    {
        ProviderApplicationStatusPage statusPage =
            new(Driver);

        statusPage.Open();

        statusPage.SearchByEmail(
            TestConfiguration.ApprovedProviderEmail
        );

        Assert.True(
            statusPage.WaitForResults(),
            "Approved application result was not displayed."
        );

        Assert.True(
            statusPage.IsApprovedStatusDisplayed(),
            "Approved application should display status Approved."
        );
    }

    [Fact]
    public void UnknownApplication_ShouldDisplayNotFoundAndNoApplicantInformation()
    {
        ProviderApplicationStatusPage statusPage = new(Driver);

        string unknownEmail =
            $"does-not-exist-{Guid.NewGuid():N}@example.com";

        statusPage.Open();

        statusPage.SearchByEmail(unknownEmail);

        Assert.True(
            statusPage.IsNotFoundErrorDisplayed(),
            "Unknown application should display a not-found error."
        );

        string errorMessage = statusPage.GetErrorMessage();

        Assert.Contains(
            "No provider application was found",
            errorMessage,
            StringComparison.OrdinalIgnoreCase
        );

        Assert.False(
            statusPage.IsResultsDisplayed(),
            "Application details must not be displayed for an unknown applicant."
        );
    }


}