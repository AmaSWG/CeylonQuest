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
