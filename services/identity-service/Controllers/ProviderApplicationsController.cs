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
            return CreatedAtAction(nameof(GetById), new { id = app.Id }, new { message = "Application submitted", applicationId = app.Id });
        }
        catch (DuplicateApplicationException ex)
        {
            return Conflict(new { message = ex.Message ?? "An application with this email already exists." });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        // Minimal implementation to satisfy CreatedAtAction link
        return NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _service.GetAllAsync(100);
        return Ok(list.Select(x => new {
            x.Id,
            x.FirstName,
            x.LastName,
            x.Email,
            x.PhoneNumber,
            x.BusinessName,
            x.ServiceType,
            x.Location,
            x.Description,
            x.LegalDocumentFileName,
            x.Status,
            x.CreatedAt
        }));
    }
}
