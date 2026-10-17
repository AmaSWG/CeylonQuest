using System.Net;
using System.Text;
using System.Text.Json;
using BookingService.Services;
using Moq;
using Moq.Protected;

namespace BookingService.Tests;

public class ReviewerProfileClientTests
{
    private const string Authorization = "Bearer visitor-token";

    private static HttpClient Client(Mock<HttpMessageHandler> handler)
    {
        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>()).CallBase();
        return new(handler.Object) { BaseAddress = new Uri("https://identity.test") };
    }

    private static void VerifyRequest(Mock<HttpMessageHandler> handler)
    {
        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get
                && r.RequestUri == new Uri("https://identity.test/api/users/me")
                && r.Headers.Authorization!.ToString() == Authorization),
            ItExpr.IsAny<CancellationToken>());
        handler.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task NonSuccessResponse_ThrowsServiceUnavailable(HttpStatusCode status)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(status));
        using var client = Client(handler);

        var error = await Assert.ThrowsAsync<ReviewException>(() =>
            new ReviewerProfileClient(client).GetDisplayNameAsync(Authorization, default));

        Assert.Equal(503, error.StatusCode);
        Assert.Equal("Your profile could not be loaded. Please try again.", error.Message);
        VerifyRequest(handler);
    }

    [Theory]
    [InlineData("malformed-json")]
    [InlineData("transport")]
    [InlineData("timeout")]
    public async Task DependencyFailure_ThrowsServiceUnavailable(string failure)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns(() => failure switch
            {
                "transport" => Task.FromException<HttpResponseMessage>(new HttpRequestException("Connection unavailable")),
                "timeout" => Task.FromException<HttpResponseMessage>(new TaskCanceledException("Dependency timed out")),
                _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{broken", Encoding.UTF8, "application/json")
                })
            });
        using var client = Client(handler);
        using var caller = new CancellationTokenSource();

        var error = await Assert.ThrowsAsync<ReviewException>(() =>
            new ReviewerProfileClient(client).GetDisplayNameAsync(Authorization, caller.Token));

        Assert.False(caller.IsCancellationRequested);
        Assert.Equal(503, error.StatusCode);
        Assert.Equal("Your profile could not be loaded. Please try again.", error.Message);
        VerifyRequest(handler);
    }

    [Fact]
    public async Task CallerCancelsDuringSend_PropagatesCallerCancellation()
    {
        using var caller = new CancellationTokenSource();
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns((HttpRequestMessage _, CancellationToken transportToken) =>
            {
                // HttpClient supplies a linked token to its handler.
                Assert.True(transportToken.CanBeCanceled);
                Assert.False(transportToken.IsCancellationRequested);
                caller.Cancel();
                Assert.True(transportToken.IsCancellationRequested);
                return Task.FromCanceled<HttpResponseMessage>(transportToken);
            });
        using var client = Client(handler);

        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ReviewerProfileClient(client).GetDisplayNameAsync(Authorization, caller.Token));

        Assert.Equal(caller.Token, error.CancellationToken);
        VerifyRequest(handler);
    }

    [Theory]
    [InlineData("null", "Visitor")]
    [InlineData("{}", "Visitor")]
    [InlineData("{\"lastName\":\"Perera\"}", "Visitor")]
    [InlineData("{\"firstName\":null}", "Visitor")]
    [InlineData("{\"firstName\":\"\",\"lastName\":\"Perera\"}", "Visitor")]
    [InlineData("{\"firstName\":\"   \"}", "Visitor")]
    [InlineData("{\"firstName\":\" Nimal \"}", "Nimal")]
    [InlineData("{\"firstName\":\" Nimal \",\"lastName\":null}", "Nimal")]
    [InlineData("{\"firstName\":\" Nimal \",\"lastName\":\"\"}", "Nimal")]
    [InlineData("{\"firstName\":\" Nimal \",\"lastName\":\"   \"}", "Nimal")]
    [InlineData("{\"firstName\":\" Nimal \",\"lastName\":\" Perera \",\"email\":\"private@example.test\"}", "Nimal P.")]
    public async Task ProfileNames_UseFallbackOrFormattedPublicName(string json, string expected)
    {
        var handler = JsonHandler(json);
        using var client = Client(handler);

        var name = await new ReviewerProfileClient(client).GetDisplayNameAsync(Authorization, default);

        Assert.Equal(expected, name);
        Assert.DoesNotContain("@", name);
        VerifyRequest(handler);
    }

    [Theory]
    [InlineData(99, false)]
    [InlineData(100, false)]
    [InlineData(101, false)]
    [InlineData(96, true)]
    [InlineData(97, true)]
    [InlineData(98, true)]
    public async Task FormattedName_Enforces100CharacterBoundary(int firstNameLength, bool hasSurname)
    {
        var first = new string('N', firstNameLength);
        var fullDisplayName = first + (hasSurname ? " P." : "");
        var json = JsonSerializer.Serialize(new { firstName = first, lastName = hasSurname ? "Perera" : null });
        var handler = JsonHandler(json);
        using var client = Client(handler);

        var name = await new ReviewerProfileClient(client).GetDisplayNameAsync(Authorization, default);

        Assert.Equal(fullDisplayName[..Math.Min(100, fullDisplayName.Length)], name);
        Assert.Equal(Math.Min(100, fullDisplayName.Length), name.Length);
        VerifyRequest(handler);
    }

    private static Mock<HttpMessageHandler> JsonHandler(string json)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        return handler;
    }
}
