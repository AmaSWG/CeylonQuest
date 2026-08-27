using IdentityService.DTOs;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProviderApplicationsController : ControllerBase
{
    private readonly ProviderApplicationService _service;

    public ProviderApplicationsController(ProviderApplicationService service)
    {
        _service = service;
    }

    [HttpPost("/api/provider-applications")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProviderApplicationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var app = await _service.CreateAsync(request);
            return StatusCode(201, new { message = "Application submitted", applicationId = app.Id });
        }
        catch (DuplicateApplicationException ex)
        {
            return Conflict(new { message = ex.Message ?? "An application with this email already exists." });
        }
    }

    [HttpGet("status")]
    [HttpGet("/api/provider-applications/status")]
    public async Task<IActionResult> GetStatus([FromQuery] string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { message = "Email address is required." });
        }

        var status = await _service.GetStatusByEmailAsync(email);
        if (status == null)
        {
            return NotFound(new { message = "No provider application found for this email address." });
        }

        return Ok(status);
    }

    [HttpPost("status")]
    [HttpPost("/api/provider-applications/status")]
    public async Task<IActionResult> PostStatus([FromBody] ProviderApplicationStatusRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email address is required." });
        }

        var status = await _service.GetStatusByEmailAsync(request.Email);
        if (status == null)
        {
            return NotFound(new { message = "No provider application found for this email address." });
        }

        return Ok(status);
    }
}
