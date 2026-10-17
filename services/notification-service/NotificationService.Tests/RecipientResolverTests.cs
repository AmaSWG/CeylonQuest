using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NotificationService.Data;
using NotificationService.Services;
using Xunit;
using System.Text;
using System.Text.Json;
using Moq;
using Moq.Protected;

namespace NotificationService.Tests;

public class RecipientResolverTests
{
    private static Mock<HttpMessageHandler> MockHandler()
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        return handler;
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("")]
    public async Task Provider_InvalidConfigurationWithoutCatalog_ThrowsWithoutWritingState(string value)
    {
        await using var db = Database(); var listing = Guid.NewGuid();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { [$"Notifications:ListingProviders:{listing}"] = value }).Build();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RecipientResolver(db, config).ProviderAsync(listing, null, default));
        Assert.Contains(listing.ToString(), error.Message);
        Assert.Empty(db.BookingContexts); Assert.Empty(db.Notifications);
    }

    [Fact]
    public async Task Provider_EventIdentityTakesPrecedenceOverConflictingConfiguration_AndNeverCallsHttp()
    {
        await using var db = Database(); var listing = Guid.NewGuid(); var supplied = Guid.NewGuid();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { [$"Notifications:ListingProviders:{listing}"] = Guid.NewGuid().ToString() }).Build();
        var handler = MockHandler();
        using var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://catalog.invalid") };
        Assert.Equal(supplied, await new RecipientResolver(db, config, client).ProviderAsync(listing, supplied, default));
        handler.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        Assert.Empty(db.BookingContexts);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Provider_ActivityHttpError_DoesNotFallThroughToCollections(HttpStatusCode status)
    {
        await using var db = Database(); var listing = Guid.NewGuid(); var handler = MockHandler();
        var path = $"/api/catalog/activity-listings/public/{listing}";
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get && r.RequestUri!.AbsolutePath == path),
            ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage(status));
        using var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://catalog.invalid") };
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => new RecipientResolver(db, new ConfigurationBuilder().Build(), client)
            .ProviderAsync(listing, null, default));
        Assert.Equal(status, error.StatusCode);
        handler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        Assert.Empty(db.BookingContexts);
    }

    [Theory]
    [InlineData("activity")]
    [InlineData("restaurant")]
    [InlineData("accommodation")]
    public async Task Provider_MalformedCatalogJson_StopsAtFailingEndpoint(string type)
    {
        await using var db = Database(); var listing = Guid.NewGuid(); var handler = MockHandler();
        var paths = new List<string>();
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                var path = request.RequestUri!.AbsolutePath; paths.Add(path);
                if (path.Contains($"/{type}-listings/")) return new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent("not-json", Encoding.UTF8, "application/json") };
                return path.Contains("/activity-listings/") ? new HttpResponseMessage(HttpStatusCode.NotFound) : Json(Array.Empty<object>());
            });
        using var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://catalog.invalid") };
        await Assert.ThrowsAsync<JsonException>(() => new RecipientResolver(db, new ConfigurationBuilder().Build(), client)
            .ProviderAsync(listing, null, default));
        var expected = new List<string> { $"/api/catalog/activity-listings/public/{listing}" };
        if (type != "activity") expected.Add("/api/catalog/restaurant-listings/public");
        if (type == "accommodation") expected.Add("/api/catalog/accommodation-listings/public");
        Assert.Equal(expected, paths);
        handler.Protected().Verify("SendAsync", Times.Exactly(expected.Count), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        Assert.Empty(db.Notifications); Assert.Empty(db.BookingContexts);
    }

    [Fact]
    public async Task Provider_NetworkException_PropagatesWithoutFallbackOrPersistence()
    {
        await using var db = Database(); var handler = MockHandler(); var listing = Guid.NewGuid();
        var path = $"/api/catalog/activity-listings/public/{listing}";
        var failure = new HttpRequestException("Simulated Catalog network outage");
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath == path), ItExpr.IsAny<CancellationToken>()).ThrowsAsync(failure);
        using var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://catalog.invalid") };
        Assert.Same(failure, await Assert.ThrowsAsync<HttpRequestException>(() => new RecipientResolver(db, new ConfigurationBuilder().Build(), client)
            .ProviderAsync(listing, null, default)));
        handler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        Assert.Empty(db.BookingContexts);
    }

    [Fact]
    public async Task Provider_CancellationDuringHttp_PropagatesAndDoesNotTryOtherEndpoints()
    {
        await using var db = Database(); var handler = MockHandler(); using var cancellation = new CancellationTokenSource();
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns((HttpRequestMessage request, CancellationToken token) =>
            {
                Assert.True(token.CanBeCanceled); cancellation.Cancel();
                return Task.FromCanceled<HttpResponseMessage>(token);
            });
        using var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://catalog.invalid") };
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new RecipientResolver(db, new ConfigurationBuilder().Build(), client)
            .ProviderAsync(Guid.NewGuid(), null, cancellation.Token));
        Assert.True(cancellation.IsCancellationRequested); Assert.True(error.CancellationToken.IsCancellationRequested);
        handler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        Assert.Empty(db.BookingContexts); Assert.Empty(db.Notifications);
    }

    [Fact]
    public async Task ConfiguredProvider_IsUsedWithoutCatalogRequest()
    {
        var listing = Guid.NewGuid(); var provider = Guid.NewGuid();
        using var handler = new StubHandler(_ => throw new Exception("Unexpected lookup"));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://catalog") };
        await using var db = Database();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
            { [$"Notifications:ListingProviders:{listing}"] = provider.ToString() }).Build();
        Assert.Equal(provider, await new RecipientResolver(db,config,client).ProviderAsync(listing,Guid.Empty,default));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task BookingIdentity_UsesSavedContextOrExplicitEventValues(bool supplied)
    {
        await using var db = Database(); var booking = Guid.NewGuid();
        var savedVisitor = Guid.NewGuid(); var savedProvider = Guid.NewGuid();
        db.BookingContexts.Add(new() { BookingId = booking, VisitorId = savedVisitor, ProviderUserId = savedProvider });
        await db.SaveChangesAsync();
        var visitor = supplied ? Guid.NewGuid() : savedVisitor;
        var provider = supplied ? Guid.NewGuid() : savedProvider;
        var result = await new RecipientResolver(db,new ConfigurationBuilder().Build()).BookingAsync(
            booking,Guid.NewGuid(),supplied ? visitor : null,supplied ? provider : null,default);
        Assert.Equal(visitor,result.VisitorId); Assert.Equal(provider,result.ProviderUserId);
    }

    [Fact]
    public async Task BookingIdentity_UsesConfiguredFallbacksWhenContextIsAbsent()
    {
        await using var db = Database(); var booking = Guid.NewGuid(); var listing = Guid.NewGuid();
        var visitor = Guid.NewGuid(); var provider = Guid.NewGuid();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        { [$"Notifications:BookingVisitors:{booking}"] = visitor.ToString(), [$"Notifications:ListingProviders:{listing}"] = provider.ToString() }).Build();
        var result = await new RecipientResolver(db,config).BookingAsync(booking,listing,null,null,default);
        Assert.Equal(visitor,result.VisitorId); Assert.Equal(provider,result.ProviderUserId);
    }

    [Fact]
    public async Task MissingVisitor_IsRejected()
    {
        await using var db = Database();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new RecipientResolver(db,new ConfigurationBuilder().Build())
            .BookingAsync(Guid.NewGuid(),Guid.NewGuid(),null,Guid.NewGuid(),default));
    }

    [Theory]
    [InlineData("activity")] [InlineData("missing")]
    public async Task CatalogWithoutUsableProvider_IsRejected(string mode)
    {
        var listing = Guid.NewGuid();
        using var handler = new StubHandler(request => request.RequestUri!.AbsolutePath.Contains("activity-listings")
            ? mode == "activity" ? Json(new { Id = listing, ProviderUserId = (Guid?)null }) : new HttpResponseMessage(HttpStatusCode.NotFound)
            : Json(Array.Empty<object>()));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://catalog") };
        await using var db = Database();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new RecipientResolver(db,new ConfigurationBuilder().Build(),client)
            .ProviderAsync(listing,null,default));
    }

    [Theory]
    [InlineData("restaurant")] [InlineData("accommodation")]
    public async Task CollectionEndpointFailure_IsPropagated(string type)
    {
        using var handler = new StubHandler(request => request.RequestUri!.AbsolutePath.Contains("activity-listings")
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : request.RequestUri.AbsolutePath.Contains($"{type}-listings") ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : Json(Array.Empty<object>()));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://catalog") };
        await using var db = Database();
        await Assert.ThrowsAsync<HttpRequestException>(() => new RecipientResolver(db,new ConfigurationBuilder().Build(),client)
            .ProviderAsync(Guid.NewGuid(),null,default));
    }

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
