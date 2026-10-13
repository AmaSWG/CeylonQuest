using System.Net;
using CeylonQuest.Tests.Sprint4AdminActions.Infrastructure;
using Xunit;

namespace CeylonQuest.Tests.Sprint4AdminActions.Api;

[Trait("Story", "13.1")]
public sealed class AdminActionsApiTests
{
    private readonly AdminApiClient _api = new();
    private readonly AdminTestDataHelper _data;

    public AdminActionsApiTests() => _data = new AdminTestDataHelper(_api);

    [Fact(DisplayName = "TC-13.1-API-01: authentication required")]
    public async Task Api01_AuthenticationRequired_Returns401()
    {
        var user = await _data.CreateSuspendedVisitorAsync("QA-API01");
        var listing = await _data.CreateActiveExperienceAsync("QA-API01");

        // 1. No token
        using var reactivateNoToken = await _api.UpdateUserStatusRawAsync(null, user.Id.ToString(), true);
        Assert.Equal(HttpStatusCode.Unauthorized, reactivateNoToken.StatusCode);

        using var removeNoToken = await _api.RemoveListingRawAsync(null, "Experience", listing.Id.ToString(), true, "Removal test");
        Assert.Equal(HttpStatusCode.Unauthorized, removeNoToken.StatusCode);

        // 2. Invalid token
        var invalidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid.token";

        using var reactivateInvalidToken = await _api.UpdateUserStatusRawAsync(invalidToken, user.Id.ToString(), true);
        Assert.Equal(HttpStatusCode.Unauthorized, reactivateInvalidToken.StatusCode);

        using var removeInvalidToken = await _api.RemoveListingRawAsync(invalidToken, "Experience", listing.Id.ToString(), true, "Removal test");
        Assert.Equal(HttpStatusCode.Unauthorized, removeInvalidToken.StatusCode);

        // Verify data unchanged
        var (adminToken, _) = await _api.LoginAsync(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);
        var users = await _api.GetUsersAsync(adminToken);
        Assert.False(users.Single(u => u.Id == user.Id).IsActive);

        var listings = await _api.GetAdminListingsAsync(adminToken);
        Assert.Equal("Active", listings.Single(l => l.Id == listing.Id).Status);
    }

    [Fact(DisplayName = "TC-13.1-API-02: admin-only authorization")]
    public async Task Api02_AdminOnlyAuthorization_Returns403()
    {
        var user = await _data.CreateSuspendedVisitorAsync("QA-API02");
        var listing = await _data.CreateActiveExperienceAsync("QA-API02");

        var visitor = await _data.CreateActiveVisitorAsync("QA-API02-Vis");
        var (visitorToken, _) = await _api.LoginAsync(visitor.Email, visitor.Password);
        var (providerToken, _) = await _api.LoginAsync(AdminTestConfig.ProviderEmail, AdminTestConfig.ProviderPassword);

        // 1. Visitor token
        using var reactivateVisitor = await _api.UpdateUserStatusRawAsync(visitorToken, user.Id.ToString(), true);
        Assert.Equal(HttpStatusCode.Forbidden, reactivateVisitor.StatusCode);

        using var removeVisitor = await _api.RemoveListingRawAsync(visitorToken, "Experience", listing.Id.ToString(), true, "Reason");
        Assert.Equal(HttpStatusCode.Forbidden, removeVisitor.StatusCode);

        // 2. Provider token
        using var reactivateProvider = await _api.UpdateUserStatusRawAsync(providerToken, user.Id.ToString(), true);
        Assert.Equal(HttpStatusCode.Forbidden, reactivateProvider.StatusCode);

        using var removeProvider = await _api.RemoveListingRawAsync(providerToken, "Experience", listing.Id.ToString(), true, "Reason");
        Assert.Equal(HttpStatusCode.Forbidden, removeProvider.StatusCode);

        // Verify data unchanged
        var (adminToken, _) = await _api.LoginAsync(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);
        var users = await _api.GetUsersAsync(adminToken);
        Assert.False(users.Single(u => u.Id == user.Id).IsActive);

        var listings = await _api.GetAdminListingsAsync(adminToken);
        Assert.Equal("Active", listings.Single(l => l.Id == listing.Id).Status);
    }

    [Fact(DisplayName = "TC-13.1-API-03: S1 reactivate a suspended user as admin")]
    public async Task Api03_ReactivateSuspendedUser_Succeeds()
    {
        var user = await _data.CreateSuspendedVisitorAsync("QA-API03");
        var (adminToken, _) = await _api.LoginAsync(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);

        using var resp = await _api.UpdateUserStatusRawAsync(adminToken, user.Id.ToString(), true);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var users = await _api.GetUsersAsync(adminToken);
        var updated = users.Single(u => u.Id == user.Id);
        Assert.True(updated.IsActive);
    }

    [Fact(DisplayName = "TC-13.1-API-04: S1 data retained")]
    public async Task Api04_UserDataRetainedAfterReactivation()
    {
        var user = await _data.CreateSuspendedVisitorAsync("QA-API04");
        var (adminToken, _) = await _api.LoginAsync(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);

        var beforeUsers = await _api.GetUsersAsync(adminToken);
        var before = beforeUsers.Single(u => u.Id == user.Id);

        using var resp = await _api.UpdateUserStatusRawAsync(adminToken, user.Id.ToString(), true);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var afterUsers = await _api.GetUsersAsync(adminToken);
        var after = afterUsers.Single(u => u.Id == user.Id);

        Assert.Equal(before.Id, after.Id);
        Assert.Equal(before.Email, after.Email);
        Assert.Equal(before.FirstName, after.FirstName);
        Assert.Equal(before.LastName, after.LastName);
        Assert.Equal(before.Role, after.Role);
        Assert.Equal(before.Nationality, after.Nationality);
        Assert.Equal(before.PhoneNumber, after.PhoneNumber);
        Assert.True((after.CreatedAt - before.CreatedAt).Duration() < TimeSpan.FromSeconds(2));
        Assert.True(after.IsActive);
    }

    [Fact(DisplayName = "TC-13.1-API-05: S1 login after reactivation")]
    public async Task Api05_LoginRejectedBeforeAndSucceedsAfterReactivation()
    {
        var user = await _data.CreateSuspendedVisitorAsync("QA-API05");

        // 1. Login rejected before
        using var beforeResp = await _api.LoginRawAsync(user.Email, user.Password);
        Assert.Equal(HttpStatusCode.Unauthorized, beforeResp.StatusCode);
        var beforeContent = await beforeResp.Content.ReadAsStringAsync();
        Assert.Contains("not active", beforeContent);

        // 2. Reactivate
        var (adminToken, _) = await _api.LoginAsync(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);
        using var reactivateResp = await _api.UpdateUserStatusRawAsync(adminToken, user.Id.ToString(), true);
        Assert.Equal(HttpStatusCode.OK, reactivateResp.StatusCode);

        // 3. Login succeeds after
        using var afterResp = await _api.LoginRawAsync(user.Email, user.Password);
        Assert.Equal(HttpStatusCode.OK, afterResp.StatusCode);
        var afterContent = await afterResp.Content.ReadAsStringAsync();
        Assert.Contains("accessToken", afterContent);
    }

    [Fact(DisplayName = "TC-13.1-API-06: S2 already-active user")]
    public async Task Api06_ReactivatingAlreadyActiveUser_BehaviorVerification()
    {
        var user = await _data.CreateActiveVisitorAsync("QA-API06");
        var (adminToken, _) = await _api.LoginAsync(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);

        var beforeUsers = await _api.GetUsersAsync(adminToken);
        var before = beforeUsers.Single(u => u.Id == user.Id);

        using var resp = await _api.UpdateUserStatusRawAsync(adminToken, user.Id.ToString(), true);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        // Verify data unchanged and user remains active
        var afterUsers = await _api.GetUsersAsync(adminToken);
        var after = afterUsers.Single(u => u.Id == user.Id);
        Assert.True(after.IsActive);
        Assert.Equal(before.Email, after.Email);
        Assert.Equal(before.FirstName, after.FirstName);
        Assert.Equal(before.LastName, after.LastName);
    }

    [Fact(DisplayName = "TC-13.1-API-07: invalid user ID")]
    public async Task Api07_InvalidUserId_ReturnsExpectedErrors()
    {
        var (adminToken, _) = await _api.LoginAsync(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);
        var nonExistentId = Guid.NewGuid();

        // 1. Nonexistent GUID
        using var notFoundResp = await _api.UpdateUserStatusRawAsync(adminToken, nonExistentId.ToString(), true);
        Assert.Equal(HttpStatusCode.NotFound, notFoundResp.StatusCode);

        // 2. Malformed ID ("abc")
        using var malformedResp = await _api.UpdateUserStatusRawAsync(adminToken, "abc", true);
        Assert.Contains((int)malformedResp.StatusCode, new[] { 400, 404 });
    }

    [Fact(DisplayName = "TC-13.1-API-08: S3 remove an active listing as admin")]
    public async Task Api08_RemoveActiveListingAsAdmin_Succeeds()
    {
        var listing = await _data.CreateActiveExperienceAsync("QA-API08");
        var (adminToken, _) = await _api.LoginAsync(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);

        using var resp = await _api.RemoveListingRawAsync(adminToken, "Experience", listing.Id.ToString(), true, "Violates community standards");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var listings = await _api.GetAdminListingsAsync(adminToken);
        var removed = listings.Single(l => l.Id == listing.Id);
        Assert.Equal("Removed", removed.Status);
        Assert.False(removed.IsActive);
        Assert.True(removed.IsRemoved);
    }

    [Fact(DisplayName = "TC-13.1-API-09: S3 listing record retained")]
    public async Task Api09_ListingRecordRetainedInAdminViewAfterRemoval()
    {
        var listing = await _data.CreateActiveExperienceAsync("QA-API09");
        var (adminToken, _) = await _api.LoginAsync(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);

        using var resp = await _api.RemoveListingRawAsync(adminToken, "Experience", listing.Id.ToString(), true, "Retained test reason");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var adminListings = await _api.GetAdminListingsAsync(adminToken);
        var record = adminListings.FirstOrDefault(l => l.Id == listing.Id);

        Assert.NotNull(record);
        Assert.Equal(listing.Id, record!.Id);
        Assert.Equal(listing.Title, record.ServiceName);
        Assert.Equal("Experience", record.BookingType);
        Assert.Equal("Removed", record.Status);
    }

    [Fact(DisplayName = "TC-13.1-API-10: S3 excluded from visitor results")]
    public async Task Api10_RemovedListingExcludedFromVisitorSearchAndDetail()
    {
        var listing = await _data.CreateActiveExperienceAsync("QA-API10");

        // Verify visible before
        var beforeDetail = await _api.GetPublicActivityByIdRawAsync(listing.Id);
        Assert.Equal(HttpStatusCode.OK, beforeDetail.StatusCode);

        // Remove
        var (adminToken, _) = await _api.LoginAsync(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);
        using var removeResp = await _api.RemoveListingRawAsync(adminToken, "Experience", listing.Id.ToString(), true, "Test exclusion");
        Assert.Equal(HttpStatusCode.OK, removeResp.StatusCode);

        // Excluded from visitor search
        using var searchResp = await _api.SearchActivitiesRawAsync(listing.Title);
        searchResp.EnsureSuccessStatusCode();
        var searchContent = await searchResp.Content.ReadAsStringAsync();
        Assert.DoesNotContain(listing.Id.ToString(), searchContent);

        // Excluded from visitor public category listing
        using var catResp = await _api.GetPublicActivitiesRawAsync(listing.Title);
        catResp.EnsureSuccessStatusCode();
        var catContent = await catResp.Content.ReadAsStringAsync();
        Assert.DoesNotContain(listing.Id.ToString(), catContent);

        // Excluded from direct detail call
        using var afterDetail = await _api.GetPublicActivityByIdRawAsync(listing.Id);
        Assert.Equal(HttpStatusCode.NotFound, afterDetail.StatusCode);
    }

    [Fact(DisplayName = "TC-13.1-API-11: invalid or repeated removal")]
    public async Task Api11_InvalidOrRepeatedRemoval_ReturnsAppropriateRejection()
    {
        var listing = await _data.CreateActiveExperienceAsync("QA-API11");
        var (adminToken, _) = await _api.LoginAsync(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);

        // 1. Nonexistent listing
        using var notFoundResp = await _api.RemoveListingRawAsync(adminToken, "Experience", Guid.NewGuid().ToString(), true, "Reason");
        Assert.Equal(HttpStatusCode.NotFound, notFoundResp.StatusCode);

        // 2. Malformed ID
        using var malformedResp = await _api.RemoveListingRawAsync(adminToken, "Experience", "not-a-guid", true, "Reason");
        Assert.Contains((int)malformedResp.StatusCode, new[] { 400, 404 });

        // 3. First removal succeeds
        using var firstRemove = await _api.RemoveListingRawAsync(adminToken, "Experience", listing.Id.ToString(), true, "Initial removal");
        Assert.Equal(HttpStatusCode.OK, firstRemove.StatusCode);

        // 4. Repeated removal -> 409 Conflict ("The listing has already been removed.")
        using var secondRemove = await _api.RemoveListingRawAsync(adminToken, "Experience", listing.Id.ToString(), true, "Second removal");
        Assert.Equal(HttpStatusCode.Conflict, secondRemove.StatusCode);

        // Verify listing data was not deleted or altered by repeated call
        var adminListings = await _api.GetAdminListingsAsync(adminToken);
        var current = adminListings.Single(l => l.Id == listing.Id);
        Assert.Equal("Removed", current.Status);
    }

    [Fact(DisplayName = "TC-13.1-API-12: S4 audit record for reactivation")]
    public async Task Api12_ReactivationAuditRecordCreated()
    {
        var user = await _data.CreateSuspendedVisitorAsync("QA-API12");
        var (adminToken, _) = await _api.LoginAsync(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);

        // Reactivate suspended user
        using var reactivateResp = await _api.UpdateUserStatusRawAsync(adminToken, user.Id.ToString(), true);
        Assert.Equal(HttpStatusCode.OK, reactivateResp.StatusCode);

        // Verify audit log exists in identity-service
        var logs = await _api.GetAdminAuditLogsAsync(adminToken, $"actionType=UserReactivated&targetId={user.Id}");
        var entry = Assert.Single(logs.Items);
        Assert.Equal("UserReactivated", entry.ActionType);
        Assert.Equal(user.Id, entry.TargetId);
        Assert.Equal("User", entry.TargetType);
        Assert.Equal("Inactive", entry.PreviousStatus);
        Assert.Equal("Active", entry.NewStatus);
    }

    [Fact(DisplayName = "TC-13.1-API-13: S4 audit record for removal")]
    public async Task Api13_RemovalAuditRecordCreated()
    {
        var listing = await _data.CreateActiveExperienceAsync("QA-API13");
        var (adminToken, _) = await _api.LoginAsync(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);

        const string reason = "Listing violated quality standards for QA test";
        using var removeResp = await _api.RemoveListingRawAsync(adminToken, "Experience", listing.Id.ToString(), true, reason);
        Assert.Equal(HttpStatusCode.OK, removeResp.StatusCode);

        // Verify audit log exists in Catalog audit endpoint
        using var catAuditResp = await _api.GetCatalogAuditLogsRawAsync(adminToken, $"targetId={listing.Id}");
        Assert.Equal(HttpStatusCode.OK, catAuditResp.StatusCode);
        var catContent = await catAuditResp.Content.ReadAsStringAsync();
        Assert.Contains(listing.Id.ToString(), catContent);
        Assert.Contains("ListingRemoved", catContent);
        Assert.Contains(reason, catContent);
    }

    [Fact(DisplayName = "TC-13.1-API-14: S4 audit retrieval and failed actions")]
    public async Task Api14_AuditRetrievalAccessControlAndFailedActions()
    {
        var (adminToken, _) = await _api.LoginAsync(AdminTestConfig.AdminEmail, AdminTestConfig.AdminPassword);

        // 1. Unauthorized (no token) -> 401
        using var noTokenResp = await _api.GetAdminAuditLogsRawAsync(null);
        Assert.Equal(HttpStatusCode.Unauthorized, noTokenResp.StatusCode);

        // 2. Forbidden (visitor token) -> 403
        var visitor = await _data.CreateActiveVisitorAsync("QA-API14-Vis");
        var (visitorToken, _) = await _api.LoginAsync(visitor.Email, visitor.Password);
        using var visitorResp = await _api.GetAdminAuditLogsRawAsync(visitorToken);
        Assert.Equal(HttpStatusCode.Forbidden, visitorResp.StatusCode);

        // 3. Validation - invalid page bounds -> 400
        using var invalidPageResp = await _api.GetAdminAuditLogsRawAsync(adminToken, "page=0&pageSize=25");
        Assert.Equal(HttpStatusCode.BadRequest, invalidPageResp.StatusCode);

        using var invalidPageSizeResp = await _api.GetAdminAuditLogsRawAsync(adminToken, "page=1&pageSize=105");
        Assert.Equal(HttpStatusCode.BadRequest, invalidPageSizeResp.StatusCode);

        // 4. Validation - dateFrom > dateTo -> 400
        using var invalidDateResp = await _api.GetAdminAuditLogsRawAsync(adminToken, "dateFrom=2026-10-15T00:00:00Z&dateTo=2026-10-01T00:00:00Z");
        Assert.Equal(HttpStatusCode.BadRequest, invalidDateResp.StatusCode);

        // 5. Validation - unsupported actionType -> 400
        using var invalidActionResp = await _api.GetAdminAuditLogsRawAsync(adminToken, "actionType=InvalidActionType");
        Assert.Equal(HttpStatusCode.BadRequest, invalidActionResp.StatusCode);

        // 6. Successful query with pagination
        using var validResp = await _api.GetAdminAuditLogsRawAsync(adminToken, "page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, validResp.StatusCode);
    }
}
