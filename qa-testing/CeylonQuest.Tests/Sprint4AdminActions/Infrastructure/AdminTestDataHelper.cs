namespace CeylonQuest.Tests.Sprint4AdminActions.Infrastructure;

public sealed class AdminTestDataHelper
{
    private readonly AdminApiClient _api;

    public AdminTestDataHelper(AdminApiClient api) => _api = api;

    public async Task<TestUserCredentials> CreateActiveVisitorAsync(string? prefix = "QA-Active")
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"qa_user_{suffix}@ceylonquest.test";
        var password = "Password123!";
        var firstName = $"{prefix}";
        var lastName = $"User-{suffix}";

        var userId = await _api.RegisterUserAsync(firstName, lastName, email, password, "Visitor");
        return new TestUserCredentials(userId, email, password, firstName, lastName, "Visitor", true);
    }

    public async Task<TestUserCredentials> CreateSuspendedVisitorAsync(string? prefix = "QA-Suspended")
    {
        var user = await CreateActiveVisitorAsync(prefix);
        var (adminToken, _) = await _api.LoginAsync(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);

        using var resp = await _api.UpdateUserStatusRawAsync(adminToken, user.Id.ToString(), false);
        resp.EnsureSuccessStatusCode();

        return user with { IsActive = false };
    }

    public async Task<TestListingRecord> CreateActiveExperienceAsync(string? prefix = "QA-Test-Exp")
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var title = $"{prefix}-{suffix}";

        var (providerToken, _) = await _api.LoginAsync(AdminTestConfig.ProviderEmail, AdminTestConfig.ProviderPassword);
        var listingId = await _api.CreateExperienceListingAsync(providerToken, title);

        return new TestListingRecord(listingId, title, "Experience", providerToken);
    }
}

public record TestUserCredentials(
    Guid Id,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive
);

public record TestListingRecord(
    Guid Id,
    string Title,
    string ListingType,
    string ProviderToken
);
