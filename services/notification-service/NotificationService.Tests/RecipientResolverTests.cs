using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NotificationService.Data;
using NotificationService.Services;
using Xunit;

namespace NotificationService.Tests;

public class RecipientResolverTests
{
    [Theory]
    [InlineData("activity")]
    [InlineData("restaurant")]
    [InlineData("accommodation")]
    public async Task MissingEventIdentity_ResolvesListingOwnerFromCatalog(string type)
    {
        var listingId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        using var handler = new StubHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.Contains("activity-listings"))
                return type == "activity"
                    ? Json(new { Id = listingId, ProviderUserId = providerId })
                    : new HttpResponseMessage(HttpStatusCode.NotFound);
            return Json(path.Contains($"{type}-listings")
                ? new[] { new { Id = Guid.NewGuid(), ProviderUserId = Guid.NewGuid() }, new { Id = listingId, ProviderUserId = providerId } }
                : Array.Empty<object>());
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://catalog") };
        await using var db = Database();
        var resolver = new RecipientResolver(db, new ConfigurationBuilder().Build(), client);
        Assert.Equal(providerId, await resolver.ProviderAsync(listingId, null, default));
    }

    [Fact]
    public async Task SuppliedIdentity_DoesNotCallCatalog()
    {
        using var handler = new StubHandler(_ => throw new Exception("Unexpected catalog lookup"));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://catalog") };
        await using var db = Database();
        var resolver = new RecipientResolver(db, new ConfigurationBuilder().Build(), client);
        var provider = Guid.NewGuid();
        Assert.Equal(provider, await resolver.ProviderAsync(Guid.NewGuid(), provider, default));
    }

    [Fact]
    public async Task CatalogFailure_IsPropagatedForRetry()
    {
        using var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://catalog") };
        await using var db = Database();
        var resolver = new RecipientResolver(db, new ConfigurationBuilder().Build(), client);
        await Assert.ThrowsAsync<HttpRequestException>(() => resolver.ProviderAsync(Guid.NewGuid(), null, default));
    }

    private static NotificationDbContext Database() => new(new DbContextOptionsBuilder<NotificationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }
}
