using IdentityService.DTOs;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegistrationService _registrationService;

    public AuthController(RegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    [HttpPost("register")]
    public IActionResult Register(RegisterRequest request)
    {
        var user = _registrationService.Register(request);

        return Ok(new
        {
            message = "Registration successful",
            userId = user.Id
        });
    }
}