using CeylonQuest.Tests.Configuration;
using CeylonQuest.Tests.Pages;
using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;


namespace CeylonQuest.Tests.Tests;

public class AdminReportTest : BaseTest
{
    //registration information is displayed
    [Fact]
    public void AdminReport_ShouldDisplayCurrentRegistrationInformation()
    {
        LoginPage loginPage = new(Driver);
        AdminReportPage reportPage = new(Driver);

        loginPage.Open();

        loginPage.Login(
            TestConfiguration.AdminEmail,
            TestConfiguration.AdminPassword
        );

        Assert.True(
            loginPage.WaitForLoginSuccess(),
            "Admin login did not complete successfully."
        );

        Assert.Equal(
            "Admin",
            loginPage.GetUserRole()
        );

        reportPage.OpenReportsTab();

        Assert.True(
            reportPage.IsReportDisplayed(),
            "Registration and verification report should be displayed."
        );

        Assert.True(
            reportPage.IsGeneratedTimestampDisplayed(),
            "Dynamic report should display a generated timestamp."
        );

        Assert.True(
            reportPage.GetTotalUsers() >= 0,
            "Total user registration count should be available."
        );
    }

    //verification status summary
    [Fact]
    public void AdminReport_ShouldDisplayProviderVerificationStatusSummary()
    {
        LoginPage loginPage = new(Driver);
                AdminReportPage reportPage = new(Driver);

                loginPage.Open();

        loginPage.Login(
            TestConfiguration.AdminEmail,
            TestConfiguration.AdminPassword
        );

        Assert.True(
            loginPage.WaitForLoginSuccess(),
            "Admin login did not complete successfully."
        );

        reportPage.OpenReportsTab();

        int total =
            reportPage.GetTotalApplications();

        int pending =
            reportPage.GetPendingApplications();

        int approved =
            reportPage.GetApprovedApplications();

        int rejected =
            reportPage.GetRejectedApplications();

        Assert.True(total >= 0);
        Assert.True(pending >= 0);
        Assert.True(approved >= 0);
        Assert.True(rejected >= 0);

        Assert.Equal(
            total,
            pending + approved + rejected
        );
    }

    //role filter
    [Fact]
    public void AdminReport_ShouldFilterRegistrationsByRole()
    {
        LoginPage loginPage = new(Driver);
        AdminReportPage reportPage = new(Driver);

        loginPage.Open();

        loginPage.Login(
            TestConfiguration.AdminEmail,
            TestConfiguration.AdminPassword
        );

        Assert.True(
            loginPage.WaitForLoginSuccess(),
            "Admin login did not complete successfully."
        );

        Assert.Equal(
            "Admin",
            loginPage.GetUserRole()
        );

        reportPage.OpenReportsTab();

        reportPage.FilterByRole("Visitor");

        Assert.Equal(
            "Visitor",
            reportPage.GetSelectedRoleFilter()
        );
    }

    // Verification status filter
    [Theory]
    [InlineData("Pending")]
    [InlineData("Approved")]
    [InlineData("Rejected")]
    public void AdminReport_ShouldFilterByVerificationStatus(string status)
    {
        LoginPage loginPage = new(Driver);
        AdminReportPage reportPage = new(Driver);

        loginPage.Open();

        loginPage.Login(
            TestConfiguration.AdminEmail,
            TestConfiguration.AdminPassword
        );

        Assert.True(
            loginPage.WaitForLoginSuccess(),
            "Admin login did not complete successfully."
        );

        Assert.Equal(
            "Admin",
            loginPage.GetUserRole()
        );

        reportPage.OpenReportsTab();

        reportPage.FilterByApplicationStatus(status);

        Assert.Equal(
            status,
            reportPage.GetSelectedApplicationStatusFilter()
        );
    }
    //
    [Fact]
    public async Task DynamicReport_ShouldMatchCurrentSystemRecords()
    {
        LoginPage loginPage = new(Driver);
                AdminReportPage reportPage = new(Driver);

                loginPage.Open();

        loginPage.Login(
            TestConfiguration.AdminEmail,
            TestConfiguration.AdminPassword
        );

        Assert.True(
            loginPage.WaitForLoginSuccess(),
            "Admin login did not complete successfully."
        );

        string? token =
            loginPage.GetAuthToken();

        Assert.False(
            string.IsNullOrWhiteSpace(token),
            "Admin authentication token was not available."
        );

        using HttpClient client = new();
        client.BaseAddress =
            new Uri("http://localhost:5000");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token
            );

        HttpResponseMessage usersResponse =
            await client.GetAsync(
                "/api/admin/users"
            );

        HttpResponseMessage applicationsResponse =
            await client.GetAsync(
                "/api/admin/provider-applications"
            );

        HttpResponseMessage reportResponse =
            await client.GetAsync(
                "/api/admin/reports"
            );

        usersResponse.EnsureSuccessStatusCode();
        applicationsResponse.EnsureSuccessStatusCode();
        reportResponse.EnsureSuccessStatusCode();

        string usersJson =
            await usersResponse.Content.ReadAsStringAsync();

        string applicationsJson =
            await applicationsResponse.Content.ReadAsStringAsync();

        string reportJson =
            await reportResponse.Content.ReadAsStringAsync();

        using JsonDocument usersDocument =
            JsonDocument.Parse(usersJson);

        using JsonDocument applicationsDocument =
            JsonDocument.Parse(applicationsJson);

        using JsonDocument reportDocument =
            JsonDocument.Parse(reportJson);

        int actualUsers =
            usersDocument.RootElement.GetArrayLength();

        int actualApplications =
            applicationsDocument.RootElement.GetArrayLength();

        int reportedUsers =
            reportDocument.RootElement
                .GetProperty("registrations")
                .GetProperty("totalUsers")
                .GetInt32();

        int reportedApplications =
            reportDocument.RootElement
                .GetProperty("applications")
                .GetProperty("totalApplications")
                .GetInt32();

        Assert.Equal(
            actualUsers,
            reportedUsers
        );

        Assert.Equal(
            actualApplications,
            reportedApplications
        );
    }
}