using IdentityService.DTOs;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegistrationService _registrationService;
    private readonly AuthService _authService;

    public AuthController(RegistrationService registrationService, AuthService authService)
    {
        _registrationService = registrationService;
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        try
        {
            var user = await _registrationService.RegisterAsync(request);

            // Return 201 Created. No sensitive data returned.
            return Created($"/api/auth/register/{user.Id}", new
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

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var resp = await _authService.AuthenticateAsync(req);
            return Ok(resp);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Stateless tokens: backend does not track sessions. Client should clear stored token.
        return Ok(new { message = "Logged out" });
    }
}