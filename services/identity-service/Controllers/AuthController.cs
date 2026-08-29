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
    private readonly ProviderActivationService _providerActivationService;
    private readonly PasswordResetService _passwordResetService;

    public AuthController(
        RegistrationService registrationService,
        AuthService authService,
        ProviderActivationService providerActivationService,
        PasswordResetService passwordResetService)
    {
        _registrationService = registrationService;
        _authService = authService;
        _providerActivationService = providerActivationService;
        _passwordResetService = passwordResetService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        try
        {
            var user = await _registrationService.RegisterAsync(request);

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
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out" });
    }

    [HttpPost("provider/activate")]
    public async Task<IActionResult> ActivateProviderAccount(ProviderActivateRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            await _providerActivationService.ActivateAsync(request);
            return Ok(new { message = "Account activated successfully. You can now log in." });
        }
        catch (ExpiredOtpException)
        {
            return BadRequest(new { message = "OTP has expired. Please request a new one." });
        }
        catch (InvalidOtpException)
        {
            return Unauthorized(new { message = "Invalid OTP or email address." });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            await _passwordResetService.InitiateForgotPasswordAsync(request.Email);
        }
        catch (Exception)
        {
        }

        return Ok(new
        {
            message = "If an account exists with this email address, you will receive a password reset link. Please check your email."
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var (success, errorMessage) = await _passwordResetService.ResetPasswordAsync(request);

            if (!success)
            {
                return BadRequest(new { message = errorMessage ?? "Unable to reset password. Please try again." });
            }

            return Ok(new { message = "Password reset successfully. You can now log in with your new password." });
        }
        catch (Exception)
        {
            return BadRequest(new { message = "Password reset link is invalid or has expired. Please request a new password reset." });
        }
    }

    [HttpGet("debug/test-token-info")]
    public IActionResult GetTestTokenInfo()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        
        if (environment == "Production")
        {
            return BadRequest(new { message = "This endpoint is only available in development." });
        }

        return Ok(new 
        { 
            message = "In development mode, the POST /api/auth/forgot-password endpoint returns the actual reset token in the response. This token can then be used with POST /api/auth/reset-password.",
            note = "In production, tokens are sent via email only and never exposed in API responses."
        });
    }
}