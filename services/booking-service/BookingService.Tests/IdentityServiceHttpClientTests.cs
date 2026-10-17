using System.Net;
using System.Text;
using System.Text.Json;
using BookingService.Services;
using Moq;
using Moq.Protected;

namespace BookingService.Tests;

public class IdentityServiceHttpClientTests
{
    private static readonly Guid CustomerId = Guid.Parse("88c71df5-90f8-4b67-9170-262127386911");
    private const string AccessToken = "provider-access-token";

    [Fact]
    public async Task SuccessfulResponse_MapsCustomerAndForwardsAuthenticatedRequest()
    {
        var json = JsonSerializer.Serialize(new
        {
            id = CustomerId, firstName = "Nimal", lastName = "Perera", email = "nimal@example.test"
        });
        var handler = Handler(HttpStatusCode.OK, json);
        using var client = Client(handler);

        var customer = await new IdentityService(client).GetBookingCustomerAsync(CustomerId, AccessToken);

        Assert.NotNull(customer);
        Assert.Equal(CustomerId, customer.Id);
        Assert.Equal("Nimal", customer.FirstName);
        Assert.Equal("Perera", customer.LastName);
        Assert.Equal("nimal@example.test", customer.Email);
        Assert.Equal("Nimal Perera", customer.FullName);
        VerifyRequest(handler);
    }

    [Fact]
    public async Task NotFound_ReturnsNullWithoutParsingErrorBody()
    {
        var handler = Handler(HttpStatusCode.NotFound, "not a profile or JSON");
        using var client = Client(handler);

        Assert.Null(await new IdentityService(client).GetBookingCustomerAsync(CustomerId, AccessToken));

        VerifyRequest(handler);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task HttpFailure_ThrowsWithOriginalStatusCode(HttpStatusCode status)
    {
        var handler = Handler(status, "not a profile or JSON");
        using var client = Client(handler);

        var error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            new IdentityService(client).GetBookingCustomerAsync(CustomerId, AccessToken));

        Assert.Equal(status, error.StatusCode);
        VerifyRequest(handler);
    }

    private static HttpClient Client(Mock<HttpMessageHandler> handler)
    {
        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>()).CallBase();
        return new(handler.Object) { BaseAddress = new Uri("https://identity.test") };
    }

    private static Mock<HttpMessageHandler> Handler(HttpStatusCode status, string body)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        return handler;
    }

    private static void VerifyRequest(Mock<HttpMessageHandler> handler)
    {
        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get
                && r.RequestUri == new Uri($"https://identity.test/api/users/{CustomerId}/booking-profile")
                && r.Headers.Authorization!.Scheme == "Bearer"
                && r.Headers.Authorization.Parameter == AccessToken),
            ItExpr.IsAny<CancellationToken>());
        handler.VerifyNoOtherCalls();
    }
}
