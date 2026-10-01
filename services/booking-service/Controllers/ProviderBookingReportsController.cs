using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using BookingService.DTOs;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Controllers;

[ApiController]
[Route("api/provider-bookings/reports")]
[Authorize(Roles = "Provider")]
public class ProviderBookingReportsController : ControllerBase
{
    private readonly IBookingsRevenueReportService _reportService;

    public ProviderBookingReportsController(IBookingsRevenueReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("bookings-revenue")]
    [ProducesResponseType(typeof(BookingsRevenueReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReport([FromQuery] BookingsRevenueReportQuery query)
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(authorization[7..]))
            return Unauthorized(new { message = "Provider authentication is required." });

        try
        {
            return Ok(await _reportService.GenerateAsync(authorization[7..].Trim(), query));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Provider ownership could not be verified. Please try again later." });
        }
    }
}
