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
public class AdminBookingReportsController : ControllerBase
{
    private readonly IAdminBookingsRevenueReportService _reportService;

    public AdminBookingReportsController(IAdminBookingsRevenueReportService reportService) => _reportService = reportService;

    [HttpGet("bookings-revenue")]
    [ProducesResponseType(typeof(AdminBookingsRevenueReportResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReport([FromQuery] AdminBookingsRevenueReportQuery query)
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(authorization[7..]))
            return Unauthorized(new { message = "Admin authentication is required." });
        try { return Ok(await _reportService.GenerateAsync(authorization[7..].Trim(), query)); }
        catch (ValidationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "The booking report is temporarily unavailable." }); }
    }
}