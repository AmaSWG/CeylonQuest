using BookingService.Controllers;
using BookingService.DTOs;
using BookingService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BookingService.Tests;

public class ProviderPerformanceReportsControllerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DependencyTimeout_WithActiveCaller_Returns503(bool taskCanceled)
    {
        using var caller = new CancellationTokenSource();
        var query = new ProviderPerformanceReportQuery { Category = "Experience" };
        var service = new Mock<IProviderPerformanceReportService>(MockBehavior.Strict);
        Exception timeout = taskCanceled
            ? new TaskCanceledException("Dependency timed out")
            : new OperationCanceledException("Dependency timed out");
        service.Setup(s => s.GenerateAsync("admin-token", query, caller.Token)).ThrowsAsync(timeout);
        var controller = Controller(service);

        var result = Assert.IsType<ObjectResult>(await controller.GetReport(query, caller.Token));

        Assert.False(caller.IsCancellationRequested);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.NotNull(result.Value);
        Assert.Equal("The provider performance report is temporarily unavailable.",
            result.Value.GetType().GetProperty("message")!.GetValue(result.Value));
        service.Verify(s => s.GenerateAsync("admin-token", query, caller.Token), Times.Once);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CallerCancellation_PropagatesAndForwardsExactTokenAndQuery()
    {
        using var caller = new CancellationTokenSource();
        var query = new ProviderPerformanceReportQuery { Category = "Accommodation" };
        var service = new Mock<IProviderPerformanceReportService>(MockBehavior.Strict);
        service.Setup(s => s.GenerateAsync("admin-token", query, caller.Token))
            .Returns((string _, ProviderPerformanceReportQuery receivedQuery, CancellationToken receivedToken) =>
            {
                Assert.Same(query, receivedQuery);
                Assert.Equal(caller.Token, receivedToken);
                caller.Cancel();
                return Task.FromCanceled<ProviderPerformanceReportResponse>(receivedToken);
            });
        var controller = Controller(service);

        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            controller.GetReport(query, caller.Token));

        Assert.True(caller.IsCancellationRequested);
        Assert.Equal(caller.Token, error.CancellationToken);
        service.Verify(s => s.GenerateAsync("admin-token", query, caller.Token), Times.Once);
        service.VerifyNoOtherCalls();
    }

    private static ProviderPerformanceReportsController Controller(Mock<IProviderPerformanceReportService> service)
    {
        var controller = new ProviderPerformanceReportsController(service.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers.Authorization = "Bearer   admin-token  ";
        return controller;
    }
}
