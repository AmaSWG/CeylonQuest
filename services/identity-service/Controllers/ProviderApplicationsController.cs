using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

/// <summary>
/// Lightweight development mock controller for provider applications.
/// Consumes the multipart form stream and returns 201 Created so local development
/// and UI testing succeed without needing a database table or Provider/Catalog Service.
/// </summary>
[ApiController]
[Route("api/provider-applications")]
public class ProviderApplicationsController : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB limit
    public IActionResult SubmitApplication([FromForm] IFormCollection form)
    {
        return StatusCode(201, new
        {
            message = "Your service provider application has been submitted successfully and is pending admin verification."
        });
    }

    [HttpGet("status")]
    public IActionResult GetStatus([FromQuery] string? email)
    {
        return Ok(new
        {
            status = "Pending",
            message = "Your application is currently under review."
        });
    }
}
