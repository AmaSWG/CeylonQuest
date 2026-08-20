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
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        try
        {
            var user = await _registrationService.RegisterAsync(request);

            return Ok(new
            {
                message = "Registration successful",
                userId = user.Id
            });
        }
        catch (DuplicateEmailException)
        {
            return Conflict(new { message = "Email already in use" });
        }
    }
}