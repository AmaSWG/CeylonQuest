using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using BookingService.DTOs;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public class ProviderPerformanceReportsController : ControllerBase
{
    private readonly IProviderPerformanceReportService _service;

    public ProviderPerformanceReportsController(
        IProviderPerformanceReportService service)
    {
        _service = service;
    }

    [HttpGet("provider-performance")]
    [ProducesResponseType(
        typeof(ProviderPerformanceReportResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReport(
        [FromQuery] ProviderPerformanceReportQuery query,
        CancellationToken cancellationToken)
    {
        var authorization = Request.Headers.Authorization.ToString();

        if (!authorization.StartsWith(
                "Bearer ", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(authorization[7..]))
        {
            return Unauthorized(new
            {
                message = "Admin authentication is required."
            });
        }

        try
        {
            var report = await _service.GenerateAsync(
                authorization[7..].Trim(), query, cancellationToken);

            return Ok(report);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (
            ex is HttpRequestException or JsonException ||
            (ex is OperationCanceledException &&
             !cancellationToken.IsCancellationRequested))
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message = "The provider performance report is temporarily unavailable."
                });
        }
    }
}